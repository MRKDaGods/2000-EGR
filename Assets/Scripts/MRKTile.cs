using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System;

namespace MRK {
    public class MRKTile {
        public class TextureFetcherLock {
            public volatile int Recursion;
        }

        MeshRenderer m_MeshRenderer;
        MRKMap m_Map;
        static Mesh ms_TileMesh;
        readonly static ConcurrentDictionary<string, ConcurrentDictionary<MRKTileID, Texture2D>>[] ms_CachedTiles;
        readonly static MRKFileTileFetcher ms_FileFetcher;
        readonly static MRKRemoteTileFetcher ms_RemoteFetcher;
        readonly static TextureFetcherLock ms_LowFetcherLock;
        readonly static TextureFetcherLock ms_HighFetcherLock;
        readonly static ObjectPool<Material> ms_MaterialPool;
        readonly static ObjectPool<GameObject> ms_ObjectPool;
        float m_MaterialBlend;
        object m_Tween;
        Material m_Material;
        bool m_FetchingTile;
        float m_MaterialEmission;
        CoroutineRunner m_Runnable;
        int m_SiblingIndex;
        TextureFetcherLock m_LastLock;

        public MRKTileID ID { get; private set; }
        public RectD Rect { get; private set; }
        public GameObject Obj { get; private set; }
        public int SiblingIndex => m_SiblingIndex;

        static MRKTile() {
            //low - high
            ms_CachedTiles = new ConcurrentDictionary<string, ConcurrentDictionary<MRKTileID, Texture2D>>[2] {
                new ConcurrentDictionary<string, ConcurrentDictionary<MRKTileID, Texture2D>>(),
                new ConcurrentDictionary<string, ConcurrentDictionary<MRKTileID, Texture2D>>()
            };

            ms_FileFetcher = new MRKFileTileFetcher();
            ms_RemoteFetcher = new MRKRemoteTileFetcher();
            ms_LowFetcherLock = new TextureFetcherLock();
            ms_HighFetcherLock = new TextureFetcherLock();
            ms_MaterialPool = new ObjectPool<Material>(() => {
                return UnityEngine.Object.Instantiate(EGRMain.Instance.FlatMap.TileMaterial);
            });
            ms_ObjectPool = new ObjectPool<GameObject>(() => {
                GameObject obj = new GameObject();
                obj.layer = 6; //PostProcessing
                obj.AddComponent<MeshFilter>().mesh = ms_TileMesh;
                //obj.AddComponent<MeshRenderer>();

                return obj;
            }, true);
        }

        static void CreateTileMesh(float size) {
            float halfSize = size / 2;
            ms_TileMesh = new Mesh {
                vertices = new Vector3[4] { new Vector3(-halfSize, 0f, -halfSize), new Vector3(halfSize, 0f, -halfSize), 
                    new Vector3(halfSize, 0, halfSize), new Vector3(-halfSize, 0, halfSize) },
                normals = new Vector3[4] { Vector3.up, Vector3.up, Vector3.up, Vector3.up },
                triangles = new int[6] { 0, 2, 1, 0, 3, 2 },
                uv = new Vector2[4] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) }
            };
        }

        public void InitTile(MRKMap map, MRKTileID id, int siblingIdx) {
            m_Map = map;
            ID = id;
            m_SiblingIndex = siblingIdx;
            Rect = MRKMapUtils.TileBounds(id);

            if (ms_TileMesh == null) {
                CreateTileMesh(m_Map.TileSize);
            }

            if (Obj == null) {
                Reference<int> objIdx = new Reference<int>();
                Obj = ms_ObjectPool.Rent(objIdx);
                Obj.SetActive(true);
                Obj.transform.parent = map.transform;
                Obj.name = $"{objIdx.Value} {id.Z} / {id.X} / {id.Y} inited";

                m_MeshRenderer = Obj.AddComponent<MeshRenderer>();
                m_Material = ms_MaterialPool.Rent();
                m_MaterialEmission = m_Map.GetDesiredTilesetEmission();
                m_MeshRenderer.material = m_Material;

                if (ID.Stationary) {
                    SetTexture(m_Map.StationaryTexture);
                }
                else if (m_SiblingIndex < 5 && ms_CachedTiles[0].ContainsKey(m_Map.Tileset) && ms_CachedTiles[0][m_Map.Tileset].ContainsKey(ID)) {
                    SetTexture(ms_CachedTiles[0][m_Map.Tileset][ID]);
                }
                else {
                    m_Runnable = Obj.GetComponent<CoroutineRunner>();
                    if (m_Runnable == null)
                        m_Runnable = Obj.AddComponent<CoroutineRunner>();

                    m_Runnable.enabled = true;
                    if (Obj.activeInHierarchy) {
                        Obj.name += "LOW";
                        m_Runnable.Run(FetchTexture(true));
                        //m_Runnable.Run(LateFetchHighTex());
                    }
                }
            }
            else {
                if (!m_FetchingTile) {
                    if (m_LastLock == ms_LowFetcherLock && m_Runnable.Count == 0 && m_SiblingIndex < 6) {
                        m_Runnable.Run(LateFetchHighTex());
                    }
                }
                else {
                    if (m_SiblingIndex >= 6 && m_LastLock == ms_HighFetcherLock) {
                        m_Runnable.StopAll();

                        lock (ms_HighFetcherLock) {
                            ms_HighFetcherLock.Recursion--;
                        }
                    }
                }
            }
        }

        IEnumerator FetchTexture(bool low = false) {
            if (low) {
                SetLoadingTexture();
            }

            TextureFetcherLock texLock = low ? ms_LowFetcherLock : ms_HighFetcherLock;
            int recursionMax = low ? 6 : 2;
            while (texLock.Recursion > recursionMax) {
                yield return new WaitForSeconds(0.8f);
            }

            //check for zoom/pan velocity
            EGRCameraFlat cam = EGRMain.Instance.ActiveEGRCamera as EGRCameraFlat;
            if (cam == null) {
                yield break; //we're not even in the map, but still trying to fetch texture?
            }

            //DateTime t0 = DateTime.Now;

            //Debug.Log($"{(DateTime.Now - t0).TotalMilliseconds} ms elapsed");

            //cancel fetch if we got disposed, kinda evil eh?
            if (Obj == null || !Obj.activeInHierarchy) {
                yield break;
            }

            lock (texLock) {
                texLock.Recursion++;
            }

            //incase we get destroyed while loading, we MUST decrement our lock somewhere
            m_FetchingTile = true;
            m_LastLock = texLock;

            float sqrMag;
            do {
                sqrMag = cam.GetMapVelocity().sqrMagnitude;
                yield return new WaitForSeconds(0.1f);
            }
            while (sqrMag > 5f * 5f);

            int lowIdx = low.ToInt();
            if (ms_CachedTiles[lowIdx].ContainsKey(m_Map.Tileset) && ms_CachedTiles[lowIdx][m_Map.Tileset].ContainsKey(ID)) {
                SetTexture(ms_CachedTiles[lowIdx][m_Map.Tileset][ID]);
            }
            else {
                string tileset = m_Map.Tileset;
                MRKTileFetcher fetcher = ms_FileFetcher.Exists(tileset, ID, low) ? (MRKTileFetcher)ms_FileFetcher : ms_RemoteFetcher;

                MRKTileFetcherContext context = new MRKTileFetcherContext();
                yield return fetcher.Fetch(context, tileset, ID, low);

                if (context.Error) {
                    Debug.Log($"{fetcher.GetType().Name}: low={low} Error for tile {ID} {(fetcher as MRKFileTileFetcher)?.GetFolderPath(tileset)}");
                }
                else {
                    SetTexture(context.Texture);
                    if (context.Texture != null) {
                        if (!ms_CachedTiles[lowIdx].ContainsKey(tileset)) {
                            ms_CachedTiles[lowIdx][tileset] = new ConcurrentDictionary<MRKTileID, Texture2D>();
                        }

                        ms_CachedTiles[lowIdx][tileset].AddOrUpdate(ID, context.Texture, (x, y) => context.Texture);

                        if (context.Texture.isReadable)
                            MRKTileRequestor.Instance.AddToSaveQueue(context.Data, tileset, ID, low);
                    }
                }
            }

            lock (texLock) {
                texLock.Recursion--;
            }

            m_FetchingTile = false;

            if (low && m_SiblingIndex < 6)
                m_Runnable.Run(LateFetchHighTex());
        }

        IEnumerator LateFetchHighTex() {
            yield return new WaitForSeconds((m_SiblingIndex + 1) * 0.1f);

            while (m_FetchingTile)
                yield return new WaitForSeconds(0.1f);

            yield return FetchTexture();

            Obj.name += "HIGH";
        }

        public void SetLoadingTexture() {
            if (m_MeshRenderer != null) {
                m_MeshRenderer.material.SetTexture("_SecTex", m_Map.LoadingTexture);
                m_MaterialBlend = 1f;
                UpdateMaterialBlend();
            }
        }

        public void SetTexture(Texture2D tex) {
            if (m_MeshRenderer != null) {
                tex.wrapMode = TextureWrapMode.Clamp;
                m_MeshRenderer.material.mainTexture = tex;
                Obj.name += "texed";

                m_Tween = DOTween.To(() => m_MaterialBlend, x => m_MaterialBlend = x, 0f, 0.4f)
                    .SetEase(Ease.OutSine)
                    .OnUpdate(UpdateMaterialBlend);
            }
        }

        public void OnDestroy() {
            if (m_Tween != null)
                DOTween.Kill(m_Tween);

            //elbt3 da 3amel 2l2, destroy!!
            if (m_MeshRenderer != null) {
                m_MeshRenderer.material = null;
                UnityEngine.Object.DestroyImmediate(m_MeshRenderer);
            }

            ms_MaterialPool.Free(m_Material);

            Obj.SetActive(false);
            ms_ObjectPool.Free(Obj);

            if (m_Runnable != null) {
                m_Runnable.StopAll();
                m_Runnable.enabled = false;
                //Object.DestroyImmediate(m_Runnable);
            }

            if (m_FetchingTile) {
                lock (m_LastLock) {
                    m_LastLock.Recursion--;
                    //Debug.Log($"{m_ObjPoolIndex} Decrement");
                }
            }
        }

        void UpdateMaterialBlend() {
            if (m_MeshRenderer != null) {
                m_MeshRenderer.material.SetFloat("_Blend", m_MaterialBlend);
                m_MeshRenderer.material.SetFloat("_Emission", Mathf.Lerp(2f, m_MaterialEmission, (1f - m_MaterialBlend)));
            }
        }
    }
}

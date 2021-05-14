using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

namespace MRK {
    public class MRKTile {
        class TextureFetcherLock {
            public int Recursion;
        }

        MeshRenderer m_MeshRenderer;
        MeshFilter m_MeshFilter;
        MRKMap m_Map;
        static Mesh ms_TileMesh;
        readonly static ConcurrentDictionary<string, ConcurrentDictionary<MRKTileID, Texture2D>> ms_CachedTiles;
        readonly static MRKFileTileFetcher ms_FileFetcher;
        readonly static MRKRemoteTileFetcher ms_RemoteFetcher;
        readonly static TextureFetcherLock ms_FetcherLock;
        readonly static ObjectPool<Material> ms_MaterialPool;
        float m_MaterialBlend;
        object m_Tween;
        Material m_Material;
        bool m_FetchingTile;
        float m_MaterialEmission;

        public MRKTileID ID { get; private set; }
        public RectD Rect { get; private set; }
        public GameObject Obj { get; private set; }
        public bool Dead { get; set; }

        static MRKTile() {
            ms_CachedTiles = new ConcurrentDictionary<string, ConcurrentDictionary<MRKTileID, Texture2D>>();
            ms_FileFetcher = new MRKFileTileFetcher();
            ms_RemoteFetcher = new MRKRemoteTileFetcher();
            ms_FetcherLock = new TextureFetcherLock();
            ms_MaterialPool = new ObjectPool<Material>(() => {
                return Object.Instantiate(EGRMain.Instance.FlatMap.TileMaterial);
            });
        }

        public MRKTile() {
        }

        public void InitTile(MRKMap map, MRKTileID id) {
            m_Map = map;
            ID = id;
            //RelativeScale = 1f / Mathf.Cos(Mathf.Deg2Rad * (float)map.CenterLatLng.x);
            Rect = MRKMapUtils.TileBounds(id);

            if (Obj == null) {
                Obj = new GameObject();
                Obj.layer = 6; //PostProcessing
                Obj.transform.parent = map.transform;
                Obj.name = $"{id.Z} / {id.X} / {id.Y} inited";
                m_MeshFilter = Obj.AddComponent<MeshFilter>();
                m_MeshRenderer = Obj.AddComponent<MeshRenderer>();

                if (ms_TileMesh == null) {
                    float halfSize = m_Map.TileSize / 2;

                    Vector3[] verts = new Vector3[4];
                    Vector3[] norms = new Vector3[4];
                    verts[0] = new Vector3(-halfSize, 0, -halfSize);
                    verts[1] = new Vector3(halfSize, 0, -halfSize);
                    verts[2] = new Vector3(halfSize, 0, halfSize);
                    verts[3] = new Vector3(-halfSize, 0, halfSize);
                    norms[0] = Vector3.up;
                    norms[1] = Vector3.up;
                    norms[2] = Vector3.up;
                    norms[3] = Vector3.up;

                    int[] trilist = new int[6] { 0, 2, 1, 0, 3, 2 };

                    Vector2[] uvlist = new Vector2[4] {
                        new Vector2(0,0),
                        new Vector2(1,0),
                        new Vector2(1,1),
                        new Vector2(0,1)
                    };

                    ms_TileMesh = new Mesh();
                    ms_TileMesh.vertices = verts;
                    ms_TileMesh.normals = norms;
                    ms_TileMesh.triangles = trilist;
                    ms_TileMesh.uv = uvlist;
                }

                m_MeshFilter.mesh = ms_TileMesh;
                m_Material = ms_MaterialPool.Rent();
                m_MaterialEmission = m_Map.GetDesiredTilesetEmission();
                m_MeshRenderer.material = m_Material;

                if (ID.Stationary) {
                    SetTexture(m_Map.StationaryTexture);
                }
                else if (ms_CachedTiles.ContainsKey(m_Map.Tileset) && ms_CachedTiles[m_Map.Tileset].ContainsKey(ID)) {
                    SetTexture(ms_CachedTiles[m_Map.Tileset][ID]);
                }
                else {
                    if (m_Map.gameObject.activeInHierarchy)
                        m_Map.StartCoroutine(FetchTexture());
                }
            }
        }

        IEnumerator FetchTexture() {
            SetLoadingTexture();

            while (ms_FetcherLock.Recursion > 2) {
                yield return new WaitForSeconds(0.2f);
            }

            //cancel fetch if we got disposed, kinda evil eh?
            if (Obj == null || !Obj.activeInHierarchy) {
                yield break;
            }

            lock (ms_FetcherLock) {
                ms_FetcherLock.Recursion++;
            }

            yield return new WaitForSeconds(0.2f * Random.value);

            //incase we get destroyed while loading, we MUST decrement our lock somewhere
            m_FetchingTile = true;

            string tileset = m_Map.Tileset;
            MRKTileFetcher fetcher = ms_FileFetcher.Exists(tileset, ID) ? (MRKTileFetcher)ms_FileFetcher : ms_RemoteFetcher;

            MRKTileFetcherContext context = new MRKTileFetcherContext();
            yield return fetcher.Fetch(context, tileset, ID);

            lock (ms_FetcherLock) {
                ms_FetcherLock.Recursion--;
            }

            m_FetchingTile = false;

            if (context.Error) {
                Debug.Log($"{fetcher.GetType().Name}: Error for tile {ID}");
            }
            else {
                SetTexture(context.Texture);
                if (context.Texture != null) {
                    if (!ms_CachedTiles.ContainsKey(tileset)) {
                        ms_CachedTiles[tileset] = new ConcurrentDictionary<MRKTileID, Texture2D>();
                    }

                    ms_CachedTiles[tileset].AddOrUpdate(ID, context.Texture, (x, y) => context.Texture);

                    if (context.Texture.isReadable)
                        MRKTileRequestor.Instance.AddToSaveQueue(context.Data, tileset, ID);
                }
            }
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

            ms_MaterialPool.Free(m_Material);

            if (m_FetchingTile) {
                lock (ms_FetcherLock) {
                    ms_FetcherLock.Recursion--;
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

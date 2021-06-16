using DG.Tweening;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Networking;

namespace MRK {
    public class MRKTile {
        public class TextureFetcherLock {
            public volatile int Recursion;
        }

        MeshRenderer m_MeshRenderer;
        MRKMap m_Map;
        static Mesh ms_TileMesh;
        readonly static ConcurrentDictionary<string, ConcurrentDictionary<MRKTileID, MRKMonitoredTexture>>[] ms_CachedTiles;
        readonly static MRKFileTileFetcher ms_FileFetcher;
        readonly static MRKRemoteTileFetcher ms_RemoteFetcher;
        readonly static TextureFetcherLock ms_LowFetcherLock;
        readonly static TextureFetcherLock ms_HighFetcherLock;
        readonly static ObjectPool<Material> ms_MaterialPool;
        readonly static ObjectPool<GameObject> ms_ObjectPool;
        readonly static ObjectPool<MRKTilePlane> ms_PlanePool;
        static GameObject ms_PlaneContainer;
        float m_MaterialBlend;
        int m_Tween;
        Material m_Material;
        bool m_FetchingTile;
        float m_MaterialEmission;
        MRKRunnable m_Runnable;
        int m_SiblingIndex;
        TextureFetcherLock m_LastLock;
        MRKMonitoredTexture m_Texture;
        Reference<UnityWebRequest> m_WebRequest;

        public MRKTileID ID { get; private set; }
        public RectD Rect { get; private set; }
        public GameObject Obj { get; private set; }
        public int SiblingIndex => m_SiblingIndex;
        public Material Material => m_Material;
        public bool HasAnyTexture { get; private set; }
        public static ObjectPool<MRKTilePlane> PlanePool => ms_PlanePool;
        public static GameObject PlaneContainer => ms_PlaneContainer;
        public bool IsFetching => m_FetchingTile;
        public bool IsFetchingLow => m_LastLock == ms_LowFetcherLock;
        public static ConcurrentDictionary<string, ConcurrentDictionary<MRKTileID, MRKMonitoredTexture>>[] CachedTiles => ms_CachedTiles;

        static MRKTile() {
            //low - high
            ms_CachedTiles = new ConcurrentDictionary<string, ConcurrentDictionary<MRKTileID, MRKMonitoredTexture>>[2] {
                new ConcurrentDictionary<string, ConcurrentDictionary<MRKTileID, MRKMonitoredTexture>>(),
                new ConcurrentDictionary<string, ConcurrentDictionary<MRKTileID, MRKMonitoredTexture>>()
            };

            ms_FileFetcher = new MRKFileTileFetcher();
            ms_RemoteFetcher = new MRKRemoteTileFetcher();
            ms_LowFetcherLock = new TextureFetcherLock();
            ms_HighFetcherLock = new TextureFetcherLock();
            ms_MaterialPool = new ObjectPool<Material>(() => {
                return Object.Instantiate(EGRMain.Instance.FlatMap.TileMaterial);
            });
            ms_ObjectPool = new ObjectPool<GameObject>(() => {
                GameObject obj = new GameObject();
                obj.layer = 6; //PostProcessing
                obj.AddComponent<MeshFilter>().mesh = ms_TileMesh;
                //obj.AddComponent<MeshRenderer>();

                return obj;
            }, true);

            ms_PlanePool = new ObjectPool<MRKTilePlane>(() => {
                GameObject obj = new GameObject("Tile Plane");
                obj.transform.parent = ms_PlaneContainer.transform;
                obj.layer = 6; //PostProcessing

                return obj.AddComponent<MRKTilePlane>();
            });
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

            if (ms_PlaneContainer == null) {
                ms_PlaneContainer = new GameObject("Plane Container");
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
                    m_Runnable = Obj.GetComponent<MRKRunnable>();
                    if (m_Runnable == null)
                        m_Runnable = Obj.AddComponent<MRKRunnable>();

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
                    if (m_SiblingIndex >= 5 && m_LastLock == ms_HighFetcherLock) {
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

            //DateTime t0 = DateTime.Now;

            TextureFetcherLock texLock = low ? ms_LowFetcherLock : ms_HighFetcherLock;
            int recursionMax = low ? (ID.Z >= 19 ? 5 : 9) : 2;
            while (texLock.Recursion > recursionMax) {
                yield return new WaitForEndOfFrame();//new WaitForSeconds(0.4f + 0.1f * m_SiblingIndex);
            }

            //check for zoom/pan velocity
            EGRCameraFlat cam = EGRMain.Instance.ActiveEGRCamera as EGRCameraFlat;
            if (cam == null) {
                yield break; //we're not even in the map, but still trying to fetch texture?
            }

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

            int lowIdx = low.ToInt();
            bool isLocalTile = ms_CachedTiles[lowIdx].ContainsKey(m_Map.Tileset) && ms_CachedTiles[lowIdx][m_Map.Tileset].ContainsKey(ID);
            float sqrMag;
            while ((sqrMag = isLocalTile ? Mathf.Pow(cam.GetMapVelocity().z, 2) : cam.GetMapVelocity().sqrMagnitude) > 5f * 5f) {
                yield return new WaitForSeconds(Time.deltaTime * m_SiblingIndex);
            }

            //Debug.Log($"{(DateTime.Now - t0).TotalMilliseconds} ms elapsed");

            if (isLocalTile) {
                SetTexture(ms_CachedTiles[lowIdx][m_Map.Tileset][ID]);
            }
            else {
                string tileset = m_Map.Tileset;
                MRKTileFetcher fetcher = ms_FileFetcher.Exists(tileset, ID, low) ? (MRKTileFetcher)ms_FileFetcher : ms_RemoteFetcher;

                m_WebRequest = ObjectPool<Reference<UnityWebRequest>>.Default.Rent();
                MRKTileFetcherContext context = new MRKTileFetcherContext();
                yield return fetcher.Fetch(context, tileset, ID, m_WebRequest, low);

                if (context.Error) {
                    Debug.Log($"{fetcher.GetType().Name}: low={low} Error for tile {ID} {(fetcher as MRKFileTileFetcher)?.GetFolderPath(tileset)}");
                    HasAnyTexture = true; //free the poor tile plane, so users can still use the map, welp
                }
                else {
                    if (context.Texture != null) {
                        if (!ms_CachedTiles[lowIdx].ContainsKey(tileset)) {
                            ms_CachedTiles[lowIdx][tileset] = new ConcurrentDictionary<MRKTileID, MRKMonitoredTexture>();
                        }

                        context.Texture.name = ID.ToString();
                        MRKMonitoredTexture tex = new MRKMonitoredTexture(context.Texture);
                        SetTexture(tex);

                        ms_CachedTiles[lowIdx][tileset].AddOrUpdate(ID, tex, (x, y) => tex);

                        if (context.Texture.isReadable)
                            MRKTileRequestor.Instance.AddToSaveQueue(context.Data, tileset, ID, low);
                    }
                }

                ObjectPool<Reference<UnityWebRequest>>.Default.Free(m_WebRequest);
                m_WebRequest = null;
            }


            lock (texLock) {
                texLock.Recursion--;
            }

            m_FetchingTile = false;

            if (low && m_SiblingIndex < 5)
                m_Runnable.Run(LateFetchHighTex());
        }

        IEnumerator LateFetchHighTex() {
            yield return new WaitForSeconds((m_SiblingIndex + 1) * 0.5f);

            while (m_FetchingTile)
                yield return new WaitForSeconds(0.1f);

            yield return FetchTexture();

            Obj.name += "HIGH";
        }

        bool HasTexture(MRKTileID id, bool low, Reference<Texture2D> tex = null) {
            int lowIdx = low.ToInt();
            bool exists = ms_CachedTiles[lowIdx].ContainsKey(m_Map.Tileset) && ms_CachedTiles[lowIdx][m_Map.Tileset].ContainsKey(id);
            if (exists && tex != null) {
                tex.Value = ms_CachedTiles[lowIdx][m_Map.Tileset][id].Texture;
            }

            return exists;
        }

        MRKTileID GetParentID(MRKTileID id = null) {
            if (id == null)
                id = ID;

            var center = MRKMapUtils.TileBounds(id).Center;
            Vector2d geoCenter = MRKMapUtils.MetersToLatLon(center);
            return MRKMapUtils.CoordinateToTileId(geoCenter, id.Z - 1);
        }

        public void SetLoadingTexture() {
            if (m_MeshRenderer != null) {
                //so uh
                Texture2D tex = m_Map.LoadingTexture;

                MRKTileID previous = GetParentID(); //m_Map.GetPreviousTileID(m_SiblingIndex);
                Reference<Texture2D> reference = ObjectPool<Reference<Texture2D>>.Default.Rent();
                if (HasTexture(previous, true, reference)) {
                    tex = reference.Value;
                }
                else {
                    int count = m_Map.PreviousTilesCount;
                    int index = m_SiblingIndex;
                    while (count-- > 0) {
                        previous = m_Map.GetPreviousTileID(index++);
                        if (previous == null)
                            break;

                        if (HasTexture(previous, true, reference)) {
                            tex = reference.Value;
                            break;
                        }

                        if (index >= m_Map.PreviousTilesCount)
                            index = 0;
                    }
                }

                ObjectPool<Reference<Texture2D>>.Default.Free(reference);

                m_MeshRenderer.material.SetTexture("_SecTex", tex);

                if (tex != m_Map.LoadingTexture) {
                    var tileZoom = ID.Z;
                    var parentZoom = previous.Z;

                    var scale = 1f;
                    var offsetX = 0f;
                    var offsetY = 0f;

                    var current = ID;
                    var currentParent = previous;

                    for (int i = tileZoom - 1; i >= parentZoom; i--) {
                        scale /= 2;

                        var bottomLeftChildX = currentParent.X * 2;
                        var bottomLeftChildY = currentParent.Y * 2;

                        //top left
                        if (current.X == bottomLeftChildX && current.Y == bottomLeftChildY) {
                            offsetY = 0.5f + (offsetY / 2);
                            offsetX = offsetX / 2;
                        }
                        //top right
                        else if (current.X == bottomLeftChildX + 1 && current.Y == bottomLeftChildY) {
                            offsetX = 0.5f + (offsetX / 2);
                            offsetY = 0.5f + (offsetY / 2);
                        }
                        //bottom left
                        else if (current.X == bottomLeftChildX && current.Y == bottomLeftChildY + 1) {
                            offsetX = offsetX / 2;
                            offsetY = offsetY / 2;
                        }
                        //bottom right
                        else if (current.X == bottomLeftChildX + 1 && current.Y == bottomLeftChildY + 1) {
                            offsetX = 0.5f + (offsetX / 2);
                            offsetY = offsetY / 2;
                        }

                        current = previous;
                        currentParent = GetParentID(previous);
                    }

                    m_MeshRenderer.material.SetTextureScale("_SecTex", new Vector2(scale, scale));
                    m_MeshRenderer.material.SetTextureOffset("_SecTex", new Vector2(offsetX, offsetY));
                }

                m_MaterialBlend = 1f;
                UpdateMaterialBlend();
            }
        }

        public void SetTexture(MRKMonitoredTexture tex) {
            if (m_Texture != null)
                m_Texture.IsActive = false;

            m_Texture = tex;

            if (m_MeshRenderer != null) {
                if (m_Texture != null) {
                    m_Texture.IsActive = true;
                    m_Texture.Texture.wrapMode = TextureWrapMode.Clamp;
                }

                m_MeshRenderer.material.mainTexture = m_Texture?.Texture;
                Obj.name += "texed";

                if (m_Tween.IsValidTween())
                    DOTween.Kill(m_Tween);

                m_Tween = DOTween.To(() => m_MaterialBlend, x => m_MaterialBlend = x, 0f, 0.2f)
                    .SetEase(Ease.OutSine)
                    .OnUpdate(UpdateMaterialBlend)
                    .intId = EGRTweenIDs.IntId;

                HasAnyTexture = true;
            }
        }

        public void OnDestroy() {
            if (m_WebRequest != null && !m_WebRequest.Value.isDone)
                m_WebRequest.Value.Abort();

            if (m_Tween.IsValidTween())
                DOTween.Kill(m_Tween);

            //elbt3 da 3amel 2l2, destroy!!
            if (m_MeshRenderer != null) {
                if (m_SiblingIndex < 9 && m_Map.TileDestroyZoomUpdatedDirty) {
                    MRKTilePlane tilePlane = ms_PlanePool.Rent();
                    tilePlane.InitPlane((Texture2D)m_MeshRenderer.material.mainTexture, m_Map.TileSize, Rect, ID.Z, () => {
                        MRKTile tile = m_Map.GetTileFromSiblingIndex(m_SiblingIndex);
                        if (tile != null)
                            return tile.HasAnyTexture;

                        return false;
                    }, m_SiblingIndex);

                    m_Map.ActivePlanes.Add(tilePlane);
                }

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

            if (m_Texture != null) {
                m_Texture.IsActive = false;
            }
        }

        void UpdateMaterialBlend() {
            if (m_MeshRenderer != null) {
                m_MeshRenderer.material.SetFloat("_Blend", m_MaterialBlend);
                m_MeshRenderer.material.SetFloat("_Emission", Mathf.Lerp(0.5f, m_MaterialEmission, (1f - m_MaterialBlend)));

                //m_MeshRenderer.material.mainTextureScale = Vector2.Lerp(m_MeshRenderer.material.mainTextureScale, Vector2.one, 1f - m_MaterialBlend);
                //m_MeshRenderer.material.mainTextureOffset = Vector2.Lerp(m_MeshRenderer.material.mainTextureOffset, Vector2.zero, 1f - m_MaterialBlend);
            }
        }

        public void Test() {
            float scaleX = Mathf.Cos(Time.time) * 0.5f + 1;
            float scaleY = Mathf.Sin(Time.time) * 0.5f + 1;
            m_MeshRenderer.material.SetTextureScale("_MainTex", new Vector2(scaleX, scaleY));
        }
    }
}

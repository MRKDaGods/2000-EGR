using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using UnityEngine;
using DG.Tweening;

namespace MRK {
    public class MRKTile {
        class TextureFetcherLock {
            public int Recursion;
        }

        MeshRenderer m_MeshRenderer;
        MeshFilter m_MeshFilter;
        MRKMap m_Map;
        static Mesh ms_TileMesh;
        readonly static ConcurrentDictionary<MRKTileID, Texture2D> ms_CachedTiles;
        readonly static MRKFileTileFetcher ms_FileFetcher;
        readonly static MRKRemoteTileFetcher ms_RemoteFetcher;
        readonly static TextureFetcherLock ms_FetcherLock;
        float m_MaterialBlend;
        object m_Tween;

        public MRKTileID ID { get; private set; }
        public float RelativeScale { get; private set; }
        public RectD Rect { get; private set; }
        public GameObject Obj { get; private set; }
        public bool Dead { get; set; }

        static MRKTile() {
            ms_CachedTiles = new ConcurrentDictionary<MRKTileID, Texture2D>();
            ms_FileFetcher = new MRKFileTileFetcher();
            ms_RemoteFetcher = new MRKRemoteTileFetcher();
            ms_FetcherLock = new TextureFetcherLock();
        }

        public MRKTile() {
        }

        public void InitTile(MRKMap map, MRKTileID id) {
            m_Map = map;
            ID = id;
            RelativeScale = 1f / Mathf.Cos(Mathf.Deg2Rad * (float)map.CenterLatLng.x);
            Rect = MRKMapUtils.TileBounds(id);

            if (Obj == null) {
                Obj = new GameObject();
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
                m_MeshRenderer.material = Object.Instantiate(map.TileMaterial);
                if (ms_CachedTiles.ContainsKey(ID)) {
                    SetTexture(ms_CachedTiles.First(x => x.Key == ID).Value);
                }
                else {
                    if (m_Map.gameObject.activeInHierarchy)
                        m_Map.StartCoroutine(FetchTexture());
                }
            }
        }

        IEnumerator FetchTexture() {
            SetLoadingTexture();

            while (ms_FetcherLock.Recursion > 4) {
                yield return new WaitForSeconds(0.2f);
            }

            //cancel fetch if we got disposed, kinda evil eh?
            if (Obj == null || !Obj.activeInHierarchy) {
                yield break;
            }

            lock (ms_FetcherLock) {
                ms_FetcherLock.Recursion++;
            }

            MRKTileFetcher fetcher = ms_FileFetcher.Exists(m_Map.Tileset, ID) ? (MRKTileFetcher)ms_FileFetcher : ms_RemoteFetcher;

            MRKTileFetcherContext context = new MRKTileFetcherContext();
            yield return fetcher.Fetch(context, m_Map.Tileset, ID);

            lock (ms_FetcherLock) {
                ms_FetcherLock.Recursion--;
            }

            if (context.Error) {
                Debug.Log($"{fetcher.GetType().Name}: Error for tile {ID}");
            }
            else {
                SetTexture(context.Texture);
                if (context.Texture != null) {
                    ms_CachedTiles.AddOrUpdate(ID, context.Texture, (x, y) => context.Texture);

                    if (context.Texture.isReadable)
                        MRKTileRequestor.Instance.AddToSaveQueue(context.Data, ID);
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
        }

        void UpdateMaterialBlend() {
            if (m_MeshRenderer != null) {
                m_MeshRenderer.material.SetFloat("_Blend", m_MaterialBlend);
                //m_MeshRenderer.material.SetFloat("_Emission", Mathf.Lerp(30f, 1f, (1f - m_MaterialBlend)));
            }
        }

        public int GetKey() {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                hash = hash * 23 + "mapbox".GetHashCode();
                hash = hash * 23 + "mapbox://styles/2000egypt/ckn0u14us1gyt17mslzfx343e".GetHashCode();
                hash = hash * 23 + ID.GetHashCode();
                return hash;
            }
        }
    }
}

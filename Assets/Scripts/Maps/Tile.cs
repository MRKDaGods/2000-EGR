#define MRK_OVERCLOCK_MAP

using DG.Tweening;
using MRK.Cameras;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Networking;

namespace MRK.Maps
{
    public class Tile
    {
        public class TextureFetcherLock
        {
            public volatile int Recursion;
        }

        private MeshRenderer _meshRenderer;
        private Map _map;
        private float _materialBlend;
        private int _tween;
        private Material _material;
        private bool _fetchingTile;
        private float _materialEmission;
        private Runnable _runnable;
        private int _siblingIndex;
        private TextureFetcherLock _lastLock;
        private MonitoredTexture _texture;
        private Reference<UnityWebRequest> _webRequest;
        private bool _isTextureIDDirty;
        private TileID _textureID;

        private static Mesh _tileMesh;
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<TileID, MonitoredTexture>>[] _cachedTiles;
        private static readonly FileTileFetcher _fileFetcher;
        private static readonly RemoteTileFetcher _remoteFetcher;
        private static readonly TextureFetcherLock _lowFetcherLock;
        private static readonly TextureFetcherLock _highFetcherLock;
        private static readonly ObjectPool<Material> _materialPool;
        private static readonly ObjectPool<GameObject> _objectPool;
        private static readonly ObjectPool<TilePlane> _planePool;
        private static GameObject _planeContainer;

        public TileID ID
        {
            get; private set;
        }

        public TileID TextureID
        {
            get
            {
                return _isTextureIDDirty ? _textureID : ID;
            }

            set
            {
                _isTextureIDDirty = true;
                _textureID = value;
            }
        }

        public Vector2Int Revolution
        {
            get; private set;
        }

        public Rectd? Rect
        {
            get; private set;
        }

        public GameObject Obj
        {
            get; private set;
        }

        public int SiblingIndex
        {
            get
            {
                return _siblingIndex;
            }
        }

        public Material Material
        {
            get
            {
                return _material;
            }
        }

        public bool HasAnyTexture
        {
            get; private set;
        }

        public static ObjectPool<TilePlane> PlanePool
        {
            get
            {
                return _planePool;
            }
        }

        public bool IsFetching
        {
            get
            {
                return _fetchingTile;
            }
        }

        public bool IsFetchingLow
        {
            get
            {
                return _lastLock == _lowFetcherLock;
            }
        }

        public static GameObject PlaneContainer
        {
            get
            {
                return _planeContainer;
            }
        }

        public static ConcurrentDictionary<string, ConcurrentDictionary<TileID, MonitoredTexture>>[] CachedTiles
        {
            get
            {
                return _cachedTiles;
            }
        }

        static Tile()
        {
            //low - high
            _cachedTiles = new ConcurrentDictionary<string, ConcurrentDictionary<TileID, MonitoredTexture>>[2] {
                new ConcurrentDictionary<string, ConcurrentDictionary<TileID, MonitoredTexture>>(),
                new ConcurrentDictionary<string, ConcurrentDictionary<TileID, MonitoredTexture>>()
            };

            _fileFetcher = new FileTileFetcher();
            _remoteFetcher = new RemoteTileFetcher();
            _lowFetcherLock = new TextureFetcherLock();
            _highFetcherLock = new TextureFetcherLock();

            _materialPool = new ObjectPool<Material>(() => {
                return Object.Instantiate(EGR.Instance.FlatMap.TileMaterial);
            });

            _objectPool = new ObjectPool<GameObject>(() => {
                GameObject obj = new GameObject
                {
                    layer = 6 //PostProcessing
                };
                obj.AddComponent<MeshFilter>().mesh = _tileMesh;
                //obj.AddComponent<MeshRenderer>();

                return obj;
            }, true);

            _planePool = new ObjectPool<TilePlane>(() => {
                GameObject obj = new GameObject("Tile Plane");
                obj.transform.parent = _planeContainer.transform;
                obj.layer = 6; //PostProcessing

                return obj.AddComponent<TilePlane>();
            });
        }

        private static void CreateTileMesh(float size)
        {
            float halfSize = size / 2;
            _tileMesh = new Mesh
            {
                vertices = new Vector3[4] { new Vector3(-halfSize, 0f, -halfSize), new Vector3(halfSize, 0f, -halfSize),
                    new Vector3(halfSize, 0, halfSize), new Vector3(-halfSize, 0, halfSize) },
                normals = new Vector3[4] { Vector3.up, Vector3.up, Vector3.up, Vector3.up },
                triangles = new int[6] { 0, 2, 1, 0, 3, 2 },
                uv = new Vector2[4] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) }
            };
        }

        public void InitTile(Map map, TileID id, int siblingIdx)
        {
            _map = map;
            ID = id;
            _siblingIndex = siblingIdx;
            Revolution = Vector2Int.one; //occurance in map

            if (!Rect.HasValue)
            {
                Rect = MapUtils.TileBounds(id);
            }

            if (_tileMesh == null)
            {
                CreateTileMesh(_map.TileSize);
            }

            if (_planeContainer == null)
            {
                _planeContainer = new GameObject("Plane Container");
            }

            if (Obj == null)
            {
                Reference<int> objIdx = new Reference<int>();
                Obj = _objectPool.Rent(objIdx);
                Obj.SetActive(true);
                Obj.transform.parent = map.transform;
                Obj.name = $"{objIdx.Value} {id.Z} / {id.X} / {id.Y} inited";

                _meshRenderer = Obj.AddComponent<MeshRenderer>();
                _material = _materialPool.Rent();
                _materialEmission = _map.GetDesiredTilesetEmission();
                _meshRenderer.material = _material;

                if (ID.Stationary)
                {
                    //Fix ups :)
                    //map the tile to the inverse most valid tile ex: if x=-1, x becomes maxValidTile

                    bool isXStationary = ID.X < 0 || ID.X > _map.MaxValidTile;
                    bool isYStationary = ID.Y < 0 || ID.Y > _map.MaxValidTile;
                    int newX = isXStationary ? _map.MaxValidTile - (ID.X - (int)Mathf.Sign(ID.X)) : ID.X;
                    int newY = isYStationary ? _map.MaxValidTile - (ID.Y - (int)Mathf.Sign(ID.Y)) : ID.Y;
                    TextureID = new TileID(ID.Z, newX, newY);
                    /*Rect = MRKMapUtils.TileBounds(TextureID);

                    int revolutionX = 1;
                    int revolutionY = 1;
                    if (isXStationary) {
                        revolutionX = Mathf.FloorToInt(ID.X / (float)m_Map.MaxValidTile) + (int)Mathf.Sign(ID.X);
                    }

                    if (isYStationary) {
                        revolutionY = Mathf.FloorToInt(ID.Y / (float)m_Map.MaxValidTile) + (int)Mathf.Sign(ID.Y);
                    }

                    Revolution = new Vector2Int(revolutionX, revolutionY); */

                    //test cases:
                    // -1 / 3 = -0.33333 fti= 0, 0 - 1 = -1
                    // 4 / 3 = 1.3333 fti= 1, 1 + 1 = 2
                    // 8 / 3 = 2.3333 fti= 2, 2 + 1 = 3
                    // -4 / 3 = -1.3333 fti=-1, -1 - 1 = -2

                    //Debug.Log($"OLD={ID} NEW={TextureID}, REV={Revolution}");

                    //SetTexture(m_Map.StationaryTexture);
                }

                Reference<MonitoredTexture> texRef = ReferencePool<MonitoredTexture>.Default.Rent();
                if (_siblingIndex < 5 && HasTexture(TextureID, false, null, texRef))
                {
                    SetTexture(texRef.Value);
                }
                else
                {
                    _runnable = Obj.GetComponent<Runnable>();
                    if (_runnable == null)
                        _runnable = Obj.AddComponent<Runnable>();

                    _runnable.enabled = true;
                    if (Obj.activeInHierarchy)
                    {
                        Obj.name += "LOW";
                        _runnable.Run(FetchTexture(true));
                        //m_Runnable.Run(LateFetchHighTex());
                    }
                }

                ReferencePool<MonitoredTexture>.Default.Free(texRef);
            }
            else
            {
                if (!_fetchingTile)
                {
                    if (_lastLock == _lowFetcherLock && _runnable.Count == 0 && _siblingIndex < 6)
                    {
                        _runnable.Run(LateFetchHighTex());
                    }
                }
                else
                {
                    if (_siblingIndex >= 5 && _lastLock == _highFetcherLock)
                    {
                        _runnable.StopAll();

                        lock (_highFetcherLock)
                        {
                            _highFetcherLock.Recursion--;
                        }
                    }
                }
            }
        }

        private IEnumerator FetchTexture(bool low = false)
        {
            if (low)
            {
                SetLoadingTexture();
            }

            //DateTime t0 = DateTime.Now;

            TextureFetcherLock texLock = low ? _lowFetcherLock : _highFetcherLock;
            int recursionMax = low ? (TextureID.Z >= 19 ? 5 : 9) : 2;
            while (texLock.Recursion > recursionMax)
            {
                yield return new WaitForEndOfFrame();//new WaitForSeconds(0.4f + 0.1f * m_SiblingIndex);
            }

            //check for zoom/pan velocity
            CameraFlat cam = EGR.Instance.ActiveEGRCamera as CameraFlat;
            if (cam == null)
            {
                yield break; //we're not even in the map, but still trying to fetch texture?
            }

            //cancel fetch if we got disposed, kinda evil eh?
            if (Obj == null || !Obj.activeInHierarchy)
            {
                yield break;
            }

            lock (texLock)
            {
                texLock.Recursion++;
            }

            //incase we get destroyed while loading, we MUST decrement our lock somewhere
            _fetchingTile = true;
            _lastLock = texLock;

            int lowIdx = low.ToInt();
            bool isLocalTile = HasTexture(TextureID, low);

#if MRK_OVERCLOCK_MAP
            float interval = 75f;
#else
            float interval = 25f;
#endif
            float sqrMag;
            while ((sqrMag = isLocalTile ? Mathf.Pow(cam.GetMapVelocity().z, 2) : cam.GetMapVelocity().sqrMagnitude) > interval)
            {
                yield return new WaitForSeconds(Time.deltaTime * _siblingIndex);
            }

            //Debug.Log($"{(DateTime.Now - t0).TotalMilliseconds} ms elapsed");

            isLocalTile = HasTexture(TextureID, low); //after waiting
            if (isLocalTile)
            {
                SetTexture(_cachedTiles[lowIdx][_map.Tileset][TextureID]);
            }
            else
            {
                string tileset = _map.Tileset;
                TileFetcher fetcher = _fileFetcher.Exists(tileset, TextureID, low) ? (TileFetcher)_fileFetcher : _remoteFetcher;

                _webRequest = ReferencePool<UnityWebRequest>.Default.Rent();
                TileFetcherContext context = new TileFetcherContext();
                yield return fetcher.Fetch(context, tileset, TextureID, _webRequest, low);

                if (context.Error)
                {
                    Debug.Log($"{fetcher.GetType().Name}: low={low} Error for tile {TextureID} {(fetcher as FileTileFetcher)?.GetFolderPath(tileset)}");
                    HasAnyTexture = true; //free the poor tile plane, so users can still use the map, welp
                }
                else
                {
                    if (context.Texture != null)
                    {
                        if (!_cachedTiles[lowIdx].ContainsKey(tileset))
                        {
                            _cachedTiles[lowIdx][tileset] = new ConcurrentDictionary<TileID, MonitoredTexture>();
                        }

                        context.Texture.name = TextureID.ToString();
                        MonitoredTexture tex = new MonitoredTexture(context.Texture);
                        SetTexture(tex);

                        _cachedTiles[lowIdx][tileset].AddOrUpdate(TextureID, tex, (x, y) => tex);

                        if (context.Texture.isReadable)
                        {
                            TileRequestor.Instance.AddToSaveQueue(context.Data, tileset, TextureID, low);
                        }
                    }
                }

                ReferencePool<UnityWebRequest>.Default.Free(_webRequest);
                _webRequest = null;
            }


            lock (texLock)
            {
                texLock.Recursion--;
            }

            _fetchingTile = false;

            if (low && _siblingIndex < 5)
            {
                _runnable.Run(LateFetchHighTex());
            }
        }

        private IEnumerator LateFetchHighTex()
        {
            yield return new WaitForSeconds((_siblingIndex + 1) * 0.5f);

            while (_fetchingTile)
            {
                yield return new WaitForSeconds(0.1f);
            }

            yield return FetchTexture();

            Obj.name += "HIGH";
        }

        private bool HasTexture(TileID id, bool low, Reference<Texture2D> tex = null, Reference<MonitoredTexture> monitoredTex = null)
        {
            int lowIdx = low.ToInt();
            bool exists = _cachedTiles[lowIdx].ContainsKey(_map.Tileset) && _cachedTiles[lowIdx][_map.Tileset].ContainsKey(id);
            if (exists)
            {
                MonitoredTexture _tex = _cachedTiles[lowIdx][_map.Tileset][id];
                if (tex != null)
                {
                    tex.Value = _tex.Texture;
                }

                if (monitoredTex != null)
                {
                    monitoredTex.Value = _tex;
                }
            }

            return exists;
        }

        private TileID GetParentID(TileID id = null)
        {
            if (id == null)
            {
                id = TextureID;
            }

            Vector2d center = MapUtils.TileBounds(id).Center;
            Vector2d geoCenter = MapUtils.MetersToLatLon(center);
            return MapUtils.CoordinateToTileId(geoCenter, id.Z - 1);
        }

        public void SetLoadingTexture()
        {
            if (_meshRenderer != null)
            {
                //so uh
                Texture2D tex = _map.LoadingTexture;

                TileID previous = GetParentID(); //m_Map.GetPreviousTileID(m_SiblingIndex);
                Reference<Texture2D> reference = ObjectPool<Reference<Texture2D>>.Default.Rent();
                if (HasTexture(previous, true, reference))
                {
                    tex = reference.Value;
                }
                else
                {
                    MonitoredTexture topMost = _map.GetCurrentTopMostTile();
                    if (topMost != null)
                    {
                        tex = topMost.Texture;
                        previous = TileID.TopMost;
                    }
                }

                ObjectPool<Reference<Texture2D>>.Default.Free(reference);

                _meshRenderer.material.SetTexture("_SecTex", tex);

                if (tex != _map.LoadingTexture)
                {
                    var tileZoom = TextureID.Z;
                    var parentZoom = previous.Z;

                    var scale = 1f;
                    var offsetX = 0f;
                    var offsetY = 0f;

                    var current = TextureID;
                    var currentParent = previous;

                    for (int i = tileZoom - 1; i >= parentZoom; i--)
                    {
                        scale /= 2;

                        var bottomLeftChildX = currentParent.X * 2;
                        var bottomLeftChildY = currentParent.Y * 2;

                        //top left
                        if (current.X == bottomLeftChildX && current.Y == bottomLeftChildY)
                        {
                            offsetY = 0.5f + (offsetY / 2);
                            offsetX /= 2;
                        }
                        //top right
                        else if (current.X == bottomLeftChildX + 1 && current.Y == bottomLeftChildY)
                        {
                            offsetX = 0.5f + (offsetX / 2);
                            offsetY = 0.5f + (offsetY / 2);
                        }
                        //bottom left
                        else if (current.X == bottomLeftChildX && current.Y == bottomLeftChildY + 1)
                        {
                            offsetX /= 2;
                            offsetY /= 2;
                        }
                        //bottom right
                        else if (current.X == bottomLeftChildX + 1 && current.Y == bottomLeftChildY + 1)
                        {
                            offsetX = 0.5f + (offsetX / 2);
                            offsetY /= 2;
                        }

                        current = previous;
                        currentParent = GetParentID(previous);
                    }

                    _meshRenderer.material.SetTextureScale("_SecTex", new Vector2(scale, scale));
                    _meshRenderer.material.SetTextureOffset("_SecTex", new Vector2(offsetX, offsetY));
                }

                _materialBlend = 1f;
                UpdateMaterialBlend();
            }
        }

        public void SetTexture(MonitoredTexture tex)
        {
            if (_texture != null)
            {
                _texture.IsActive = false;
            }

            _texture = tex;

            if (_meshRenderer != null)
            {
                if (_texture != null)
                {
                    _texture.IsActive = true;
                    _texture.Texture.wrapMode = TextureWrapMode.Clamp;
                }

                _meshRenderer.material.mainTexture = _texture?.Texture;
                Obj.name += "texed";

                if (_tween.IsValidTween())
                {
                    DOTween.Kill(_tween);
                }

                _tween = DOTween.To(() => _materialBlend, x => _materialBlend = x, 0f, 0.2f)
                    .SetEase(Ease.OutSine)
                    .OnUpdate(UpdateMaterialBlend)
                    .intId = EGRTweenIDs.IntId;

                HasAnyTexture = true;
            }
        }

        public void OnDestroy()
        {
            if (_webRequest != null && !_webRequest.Value.isDone)
            {
                _webRequest.Value.Abort();
            }

            if (_tween.IsValidTween())
            {
                DOTween.Kill(_tween);
            }

            //elbt3 da 3amel 2l2, destroy!!
            if (_meshRenderer != null)
            {
                if (/*m_SiblingIndex < 15 && */_map.TileDestroyZoomUpdatedDirty && !ID.Stationary)
                {
                    TilePlane tilePlane = _planePool.Rent();
                    tilePlane.InitPlane(
                        _texture != null ? _texture.Texture : (Texture2D)_meshRenderer.material.mainTexture,
                        _map.TileSize,
                        Rect.Value,
                        ID.Z,
                        () => {
                            Tile tile = _map.GetTileFromSiblingIndex(_siblingIndex);
                            return tile != null ? tile.HasAnyTexture : false;
                        },
                        _siblingIndex
                    );

                    _map.ActivePlanes.Add(tilePlane);
                }

                _meshRenderer.material = null;
                Object.DestroyImmediate(_meshRenderer);
            }

            _materialPool.Free(_material);

            Obj.SetActive(false);
            _objectPool.Free(Obj);

            if (_runnable != null)
            {
                _runnable.StopAll();
                _runnable.enabled = false;
                //Object.DestroyImmediate(m_Runnable);
            }

            if (_fetchingTile)
            {
                lock (_lastLock)
                {
                    _lastLock.Recursion--;
                    //Debug.Log($"{m_ObjPoolIndex} Decrement");
                }
            }

            if (_texture != null)
            {
                _texture.IsActive = false;
            }

            Rect = null;
        }

        private void UpdateMaterialBlend()
        {
            if (_meshRenderer != null)
            {
                _meshRenderer.material.SetFloat("_Blend", _materialBlend);
                _meshRenderer.material.SetFloat("_Emission", Mathf.Lerp(0.5f, _materialEmission, (1f - _materialBlend)));

                //m_MeshRenderer.material.mainTextureScale = Vector2.Lerp(m_MeshRenderer.material.mainTextureScale, Vector2.one, 1f - m_MaterialBlend);
                //m_MeshRenderer.material.mainTextureOffset = Vector2.Lerp(m_MeshRenderer.material.mainTextureOffset, Vector2.zero, 1f - m_MaterialBlend);
            }
        }
    }
}

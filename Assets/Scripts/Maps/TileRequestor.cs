using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace MRK.Maps
{
    [Serializable]
    public struct TilesetProvider
    {
        public string Name;
        public string API;
    }

    public class TileRequestor : BaseBehaviour
    {
        private class CachedTileInfo
        {
            public byte[] Texture;
            public string Tileset;
            public TileID ID;
            public bool Low;
        }

        private readonly Queue<CachedTileInfo> _queuedTiles;
        private readonly FileTileFetcher _fileFetcher;
        private readonly RemoteTileFetcher _remoteTileFetcher;
        [SerializeField]
        private TilesetProvider[] _tilesetProviders;
        private CancellationTokenSource _lastCancellationToken;

        public TilesetProvider[] TilesetProviders
        {
            get
            {
                return _tilesetProviders;
            }
        }

        public FileTileFetcher FileTileFetcher
        {
            get
            {
                return _fileFetcher;
            }
        }

        public RemoteTileFetcher RemoteTileFetcher
        {
            get
            {
                return _remoteTileFetcher;
            }
        }

        public static TileRequestor Instance
        {
            get; private set;
        }

        public TileRequestor()
        {
            _queuedTiles = new Queue<CachedTileInfo>();
            _fileFetcher = new FileTileFetcher();
            _remoteTileFetcher = new RemoteTileFetcher();
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            StartCoroutine(Loop());
        }

        public void AddToSaveQueue(byte[] tex, string tileset, TileID id, bool low)
        {
            _queuedTiles.Enqueue(new CachedTileInfo { Texture = tex, Tileset = tileset, ID = id, Low = low });
        }

        private IEnumerator Loop()
        {
            while (true)
            {
                if (_queuedTiles.Count > 0)
                {
                    CachedTileInfo tile = _queuedTiles.Peek();
                    if (tile != null)
                    {
                        CancellationTokenSource src = new CancellationTokenSource();
                        Task t = _fileFetcher.SaveToDisk(tile.Tileset, tile.ID, tile.Texture, tile.Low, src.Token);
                        _lastCancellationToken = src;

                        while (!t.IsCompleted)
                        {
                            yield return new WaitForSeconds(0.2f);
                        }

                        lock (_queuedTiles)
                        {
                            _queuedTiles.Dequeue();
                        }
                    }
                }

                yield return new WaitForSeconds(0.4f);
            }
        }

        public IEnumerator RequestTile(TileID id, bool low, Action<TileFetcherContext> callback, string tileset = null)
        {
            //dont exec if callback is null
            if (callback == null)
            {
                yield break;
            }

            if (tileset == null)
            {
                tileset = Client.FlatMap.Tileset;
            }

            TileFetcher fetcher = _fileFetcher.Exists(tileset, id, low) ? (TileFetcher)_fileFetcher : _remoteTileFetcher;
            TileFetcherContext ctx = new TileFetcherContext();
            Reference<UnityWebRequest> req = ReferencePool<UnityWebRequest>.Default.Rent();
            yield return fetcher.Fetch(ctx, tileset, id, req, low);

            callback(ctx);

            ReferencePool<UnityWebRequest>.Default.Free(req);
        }

        public TilesetProvider GetTilesetProvider(string tileset)
        {
            foreach (TilesetProvider provider in _tilesetProviders)
            {
                if (provider.Name == tileset)
                {
                    return provider;
                }
            }

            return default;
        }

        public TilesetProvider GetCurrentTilesetProvider()
        {
            return GetTilesetProvider(Client.FlatMap.Tileset);
        }

        public void DeleteLocalProvidersCache()
        {
            if (_lastCancellationToken != null)
            {
                _lastCancellationToken.Cancel();
            }

            lock (_queuedTiles)
            {
                _queuedTiles.Clear();
            }

            foreach (TilesetProvider provider in TilesetProviders)
            {
                //get directory of tileset
                string dir = null;
                Client.Runnable.RunOnMainThread(() =>
                {
                    dir = FileTileFetcher.GetFolderPath(provider.Name);
                });

                SpinWait.SpinUntil(() => dir != null);

                if (Directory.Exists(dir))
                {
                    //get all PNGs
                    foreach (string filename in Directory.EnumerateFiles(dir, "*.png"))
                    {
                        File.Delete(filename);
                    }
                }
            }
        }
    }
}

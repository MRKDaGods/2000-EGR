using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;

namespace MRK {
    [Serializable]
    public struct MRKTilesetProvider {
        public string Name;
        public string API;
    }

    public class MRKTileRequestor : MRKBehaviour {
        class CachedTileInfo {
            public byte[] Texture;
            public string Tileset;
            public MRKTileID ID;
            public bool Low;
        }

        readonly Queue<CachedTileInfo> m_QueuedTiles;
        readonly MRKFileTileFetcher m_FileFetcher;
        readonly MRKRemoteTileFetcher m_RemoteTileFetcher;
        [SerializeField]
        MRKTilesetProvider[] m_TilesetProviders;
        CancellationTokenSource m_LastCancellationToken;

        public static MRKTileRequestor Instance { get; private set; }
        public MRKTilesetProvider[] TilesetProviders => m_TilesetProviders;
        public MRKFileTileFetcher FileTileFetcher => m_FileFetcher;
        public MRKRemoteTileFetcher RemoteTileFetcher => m_RemoteTileFetcher;

        public MRKTileRequestor() {
            m_QueuedTiles = new Queue<CachedTileInfo>();
            m_FileFetcher = new MRKFileTileFetcher();
            m_RemoteTileFetcher = new MRKRemoteTileFetcher();
        }

        void Awake() {
            Instance = this;
        }

        void Start() {
            StartCoroutine(Loop());
        }

        public void AddToSaveQueue(byte[] tex, string tileset, MRKTileID id, bool low) {
            m_QueuedTiles.Enqueue(new CachedTileInfo { Texture = tex, Tileset = tileset, ID = id, Low = low });
        }

        IEnumerator Loop() {
            while (true) {
                if (m_QueuedTiles.Count > 0) {
                    CachedTileInfo tile = m_QueuedTiles.Peek();
                    if (tile != null) {
                        CancellationTokenSource src = new CancellationTokenSource();
                        Task t = m_FileFetcher.SaveToDisk(tile.Tileset, tile.ID, tile.Texture, tile.Low, src.Token);
                        m_LastCancellationToken = src;

                        while (!t.IsCompleted)
                            yield return new WaitForSeconds(0.2f);

                        lock (m_QueuedTiles) {
                            m_QueuedTiles.Dequeue();
                        }
                    }
                }

                yield return new WaitForSeconds(0.4f);
            }
        }

        public IEnumerator RequestTile(MRKTileID id, bool low, Action<MRKTileFetcherContext> callback, string tileset = null) {
            //dont exec if callback is null
            if (callback == null) {
                yield break;
            }
            
            if (tileset == null) {
                tileset = Client.FlatMap.Tileset;
            }

            MRKTileFetcher fetcher = m_FileFetcher.Exists(tileset, id, low) ? (MRKTileFetcher)m_FileFetcher : m_RemoteTileFetcher;
            MRKTileFetcherContext ctx = new MRKTileFetcherContext();
            Reference<UnityWebRequest> req = ReferencePool<UnityWebRequest>.Default.Rent();
            yield return fetcher.Fetch(ctx, tileset, id, req, low);

            callback(ctx);

            ReferencePool<UnityWebRequest>.Default.Free(req);
        }

        public MRKTilesetProvider GetTilesetProvider(string tileset) {
            foreach (MRKTilesetProvider provider in m_TilesetProviders) {
                if (provider.Name == tileset) {
                    return provider;
                }
            }

            return default;
        }

        public MRKTilesetProvider GetCurrentTilesetProvider() {
            return GetTilesetProvider(Client.FlatMap.Tileset);
        }

        public void DeleteLocalProvidersCache() {
            if (m_LastCancellationToken != null) {
                m_LastCancellationToken.Cancel();
            }

            lock (m_QueuedTiles) {
                m_QueuedTiles.Clear();
            }

            foreach (MRKTilesetProvider provider in TilesetProviders) {
                //get directory of tileset
                string dir = null;
                Client.Runnable.RunOnMainThread(() => {
                    dir = FileTileFetcher.GetFolderPath(provider.Name);
                });

                SpinWait.SpinUntil(() => dir != null);

                if (Directory.Exists(dir)) {
                    //get all PNGs
                    foreach (string filename in Directory.EnumerateFiles(dir, "*.png")) {
                        File.Delete(filename);
                    }
                }
            }
        }
    }
}

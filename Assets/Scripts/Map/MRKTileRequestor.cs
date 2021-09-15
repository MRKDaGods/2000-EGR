using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;
using System;
using System.Threading.Tasks;

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

        readonly ConcurrentQueue<CachedTileInfo> m_QueuedTiles;
        readonly MRKFileTileFetcher m_FileFetcher;
        [SerializeField]
        MRKTilesetProvider[] m_TilesetProviders;

        public static MRKTileRequestor Instance { get; private set; }
        public MRKTilesetProvider[] TilesetProviders => m_TilesetProviders;
        public MRKFileTileFetcher FileTileFetcher => m_FileFetcher;

        public MRKTileRequestor() {
            m_QueuedTiles = new ConcurrentQueue<CachedTileInfo>();
            m_FileFetcher = new MRKFileTileFetcher();
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
                CachedTileInfo tile;
                if (m_QueuedTiles.TryPeek(out tile)) {
                    Task t = m_FileFetcher.SaveToDisk(tile.Tileset, tile.ID, tile.Texture, tile.Low);
                    while (!t.IsCompleted)
                        yield return new WaitForSeconds(0.2f);

                    do
                        yield return new WaitForSeconds(0.2f);
                    while (!m_QueuedTiles.TryDequeue(out _));
                }

                yield return new WaitForSeconds(0.4f);
            }
        }

        public MRKTilesetProvider GetCurrentTilesetProvider() {
            foreach (MRKTilesetProvider provider in m_TilesetProviders) {
                if (provider.Name == Client.FlatMap.Tileset) {
                    return provider;
                }
            }

            return default;
        }
    }
}

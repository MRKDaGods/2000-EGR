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

    public class MRKTileRequestor : EGRBehaviour {
        class CachedTileInfo {
            public byte[] Texture;
            public MRKTileID ID;
        }

        readonly ConcurrentQueue<CachedTileInfo> m_QueuedTiles;
        readonly MRKFileTileFetcher m_FileFetcher;
        [SerializeField]
        MRKTilesetProvider[] m_TilesetProviders;

        public static MRKTileRequestor Instance { get; private set; }

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

        public void AddToSaveQueue(byte[] tex, MRKTileID id) {
            m_QueuedTiles.Enqueue(new CachedTileInfo { Texture = tex, ID = id });
        }

        IEnumerator Loop() {
            while (true) {
                CachedTileInfo tile;
                if (m_QueuedTiles.TryPeek(out tile)) {
                    Task t = m_FileFetcher.SaveToDisk(Client.FlatMap.Tileset, tile.ID, tile.Texture);
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

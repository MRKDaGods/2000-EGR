using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MRK {
    public class MRKMonitoredTexture {
        Texture2D m_Texture;

        public long Ticks { get; private set; }
        public Texture2D Texture {
            get {
                Ticks = DateTime.Now.Ticks;
                return m_Texture;
            }
            set {
                Ticks = DateTime.Now.Ticks;
                m_Texture = value;
            }
        }
        public bool IsActive { get; set; }
        public bool IsStatic { get; private set; }

        public MRKMonitoredTexture() {
            Texture = null;
        }

        public MRKMonitoredTexture(Texture2D texture, bool _static = false) {
            Texture = texture;
            IsStatic = _static;
        }
    }

    public class MRKTileMonitor : MRKBehaviour {
        const long HIGH_DIFF = TimeSpan.TicksPerSecond * 8L;
        const long LOW_DIFF = TimeSpan.TicksPerSecond * 4L;

        readonly List<MRKMonitoredTexture> m_DestroyingTextures;
        readonly Reference<Action> m_QueuedToMainThread;
        readonly CancellationTokenSource m_TokenSource;

        public static MRKTileMonitor Instance { get; private set; }

        public MRKTileMonitor() {
            m_DestroyingTextures = new List<MRKMonitoredTexture>();
            m_QueuedToMainThread = new Reference<Action>();
            m_TokenSource = new CancellationTokenSource();
        }

        void Awake() {
            Instance = this;
        }

        void Start() {
            Task.Run(Loop, m_TokenSource.Token);
        }

        void Update() {
            if (m_QueuedToMainThread.Value != null) {
                lock (m_QueuedToMainThread) {
                    m_QueuedToMainThread.Value();
                    m_QueuedToMainThread.Value = null;
                }
            }
        }

        async Task Loop() {
            List<MRKTileID> clearBuffer = new List<MRKTileID>();

            while (Client.IsRunning && !m_TokenSource.IsCancellationRequested) {
                //high = 0
                //low = 1
                lock (m_DestroyingTextures) {
                    long nowTicks = DateTime.Now.Ticks;

                    for (int i = 0; i < 2; i++) {
                        var cache = MRKTile.CachedTiles[i];
                        foreach (var pair in cache) {
                            //check if it is the current tileset
                            if (pair.Key != Client.FlatMap.Tileset) {
                                //delete all
                                foreach (var texs in pair.Value) {
                                    //free each tex
                                    m_DestroyingTextures.Add(texs.Value);
                                }

                                pair.Value.Clear();
                                continue;
                            }

                            foreach (var texPair in pair.Value) {
                                if (texPair.Value.IsActive || texPair.Value.IsStatic) //dont mess with active texs
                                    continue;

                                //highs should get disposed faster than lows
                                long timeDiff = nowTicks - texPair.Value.Ticks;
                                if (timeDiff > (i == 0 ? HIGH_DIFF : LOW_DIFF)) {
                                    clearBuffer.Add(texPair.Key);
                                    m_DestroyingTextures.Add(texPair.Value);
                                }
                            }

                            //clear here
                            foreach (MRKTileID id in clearBuffer) {
                                bool removed;
                                do
                                    removed = pair.Value.TryRemove(id, out _);
                                while (!removed);
                            }

                            clearBuffer.Clear();
                        }
                    }

                    //queue to main thread
                    lock (m_QueuedToMainThread) {
                        m_QueuedToMainThread.Value = () => {
                            if (m_DestroyingTextures.Count > 0) {
                                lock (m_DestroyingTextures) {
                                    int texDestroyed = 0;
                                    foreach (MRKMonitoredTexture tex in m_DestroyingTextures) {
                                        if (texDestroyed >= 20)
                                            break;

                                        if (!tex.IsActive && tex.Texture != null)
                                            Destroy(tex.Texture);
                                        texDestroyed++;
                                    }

                                    for (int i = 0; i < texDestroyed; i++)
                                        m_DestroyingTextures.RemoveAt(0);
                                }
                            }
                        };
                    }
                }

                await Task.Delay(2000); //2s
            }
        }

        void OnApplicationQuit() {
            m_TokenSource.Cancel();
        }

        public void DestroyLeaks() {
            if (Client.FlatMap.IsAnyTileFetching()) {
                //nasty crash
                Client.FlatMap.DestroyAllTiles();
            }

            Texture2D[] textures = FindObjectsOfType<Texture2D>(true);
            foreach (Texture2D tex in textures) {
                if (tex.width != 512 && tex.width != 1024)
                    continue;

                if (tex.name.Length > 0)
                    continue;

                if (tex != null) {
                    Destroy(tex);
                }
            }
        }
    }
}

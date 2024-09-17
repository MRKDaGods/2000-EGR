using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MRK.Maps
{
    public class MonitoredTexture
    {
        private Texture2D _texture;

        public long Ticks
        {
            get; private set;
        }

        public Texture2D Texture
        {
            get
            {
                Ticks = DateTime.Now.Ticks;
                return _texture;
            }
            set
            {
                Ticks = DateTime.Now.Ticks;
                _texture = value;
            }
        }

        public bool IsActive
        {
            get; set;
        }

        public bool IsStatic
        {
            get; private set;
        }

        public MonitoredTexture()
        {
            Texture = null;
        }

        public MonitoredTexture(Texture2D texture, bool _static = false)
        {
            Texture = texture;
            IsStatic = _static;
        }
    }

    public class TileMonitor : BaseBehaviour
    {
        private const long HighDiff = TimeSpan.TicksPerSecond * 8L;
        private const long LowDiff = TimeSpan.TicksPerSecond * 4L;

        private readonly List<MonitoredTexture> _destroyingTextures;
        private readonly Reference<Action> _queuedToMainThread;
        private readonly CancellationTokenSource _tokenSource;

        public static TileMonitor Instance
        {
            get; private set;
        }

        public TileMonitor()
        {
            _destroyingTextures = new List<MonitoredTexture>();
            _queuedToMainThread = new Reference<Action>();
            _tokenSource = new CancellationTokenSource();
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            Task.Run(Loop, _tokenSource.Token);
        }

        private void Update()
        {
            if (_queuedToMainThread.Value != null)
            {
                lock (_queuedToMainThread)
                {
                    _queuedToMainThread.Value();
                    _queuedToMainThread.Value = null;
                }
            }
        }

        private async Task Loop()
        {
            List<TileID> clearBuffer = new List<TileID>();

            while (Client.IsRunning && !_tokenSource.IsCancellationRequested)
            {
                //high = 0
                //low = 1
                lock (_destroyingTextures)
                {
                    long nowTicks = DateTime.Now.Ticks;

                    for (int i = 0; i < 2; i++)
                    {
                        var cache = Tile.CachedTiles[i];
                        foreach (var pair in cache)
                        {
                            //check if it is the current tileset
                            if (pair.Key != Client.FlatMap.Tileset)
                            {
                                //delete all
                                foreach (var texs in pair.Value)
                                {
                                    //free each tex
                                    _destroyingTextures.Add(texs.Value);
                                }

                                pair.Value.Clear();
                                continue;
                            }

                            foreach (var texPair in pair.Value)
                            {
                                if (texPair.Value.IsActive || texPair.Value.IsStatic) //dont mess with active texs
                                    continue;

                                //highs should get disposed faster than lows
                                long timeDiff = nowTicks - texPair.Value.Ticks;
                                if (timeDiff > (i == 0 ? HighDiff : LowDiff))
                                {
                                    clearBuffer.Add(texPair.Key);
                                    _destroyingTextures.Add(texPair.Value);
                                }
                            }

                            //clear here
                            foreach (TileID id in clearBuffer)
                            {
                                bool removed;
                                do
                                {
                                    removed = pair.Value.TryRemove(id, out _);
                                }
                                while (!removed);
                            }

                            clearBuffer.Clear();
                        }
                    }

                    //queue to main thread
                    lock (_queuedToMainThread)
                    {
                        _queuedToMainThread.Value = () => {
                            if (_destroyingTextures.Count > 0)
                            {
                                lock (_destroyingTextures)
                                {
                                    int texDestroyed = 0;
                                    foreach (MonitoredTexture tex in _destroyingTextures)
                                    {
                                        if (texDestroyed >= 20)
                                            break;

                                        if (!tex.IsActive && tex.Texture != null)
                                        {
                                            Destroy(tex.Texture);
                                        }

                                        texDestroyed++;
                                    }

                                    for (int i = 0; i < texDestroyed; i++)
                                    {
                                        _destroyingTextures.RemoveAt(0);
                                    }
                                }
                            }
                        };
                    }
                }

                await Task.Delay(2000); //2s
            }
        }

        private void OnApplicationQuit()
        {
            _tokenSource.Cancel();
        }

        public void DestroyLeaks()
        {
            if (Client.FlatMap.IsAnyTileFetching())
            {
                //nasty crash
                Client.FlatMap.DestroyAllTiles();
            }

            Texture2D[] textures = FindObjectsOfType<Texture2D>(true);
            foreach (Texture2D tex in textures)
            {
                if (tex.width != 512 && tex.width != 1024)
                {
                    continue;
                }

                if (tex.name.Length > 0)
                {
                    continue;
                }

                if (tex != null)
                {
                    Destroy(tex);
                }
            }
        }
    }
}

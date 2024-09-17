using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MRK.UI
{
    public class ScreenManager : MonoBehaviour
    {
        [SerializeField]
        private Canvas _screensCanvas;
        [SerializeField]
        private int _maxLayerCount;
        private readonly Dictionary<string, Screen> _screens;
        private readonly List<Canvas> _layers;
        private Screen _topScreen;
        private int _targetScreenCount;
        private List<ProxyScreen> _proxiedScreens;
        private readonly Dictionary<Type, Screen> _screensTypes;
        private readonly Dictionary<int, HashSet<Screen>> _layerToScreens;
        private static readonly List<ProxyScreen> _proxyPipe;
        [SerializeField]
        private Canvas[] _screenSpaceLayers;
        private readonly MRKSelfContainedPtr<HUD> _mapInterface;
        private readonly MRKSelfContainedPtr<MessageBox> _messageBox;
        private readonly MRKSelfContainedPtr<Main> _mainScreen;

        private static ScreenManager _instance;

        public static int SceneChangeIndex
        {
            get; private set;
        }

        public static ScreenManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject target = GameObject.Find("ScreenManager");
                    if (target == null)
                        target = new GameObject("ScreenManager");

                    _instance = target.AddComponent<ScreenManager>();
                }

                return _instance;
            }
        }
        public int ScreenCount
        {
            get
            {
                return _screens.Keys.Count;
            }
        }

        public bool FullyInitialized
        {
            get
            {
                return _targetScreenCount == ScreenCount;
            }
        }

        public HUD MapInterface
        {
            get
            {
                return _mapInterface;
            }
        }

        public MessageBox MessageBox
        {
            get
            {
                return _messageBox;
            }
        }

        public Main MainScreen
        {
            get
            {
                return _mainScreen;
            }
        }

        static ScreenManager()
        {
            SceneManager.activeSceneChanged += OnSceneChanged;
            _proxyPipe = new List<ProxyScreen>();
        }

        public ScreenManager()
        {
            _screens = new Dictionary<string, Screen>();
            _screensTypes = new Dictionary<Type, Screen>();
            _layers = new List<Canvas>();
            _layerToScreens = new Dictionary<int, HashSet<Screen>>();

            _mapInterface = new MRKSelfContainedPtr<MapInterface>(() => GetScreen<MapInterface>());
            _messageBox = new MRKSelfContainedPtr<MessageBox>(() => GetPopup<MessageBox>());
            _mainScreen = new MRKSelfContainedPtr<Main>(() => GetScreen<Main>());
        }

        private void Awake()
        {
            _instance = this;

            _targetScreenCount = _screensCanvas.GetComponentsInChildren<Screen>().Length;


            GameObject container = new GameObject("Screens");

            for (int i = 0; i < _maxLayerCount; i++)
            {
                Canvas canv = Instantiate(_screensCanvas);
                canv.transform.SetParent(container.transform);

                while (canv.transform.childCount > 0)
                {
                    Transform child = canv.transform.GetChild(0);
                    child.SetParent(null);
                    Destroy(child.gameObject);
                }

                canv.sortingOrder = i;
                canv.name = "Canvas-" + (i + 1);
                _layers.Add(canv);

                _layerToScreens[i] = new HashSet<Screen>();
            }

            _proxiedScreens = new List<ProxyScreen>();
            foreach (ProxyScreen proxyScreen in _proxyPipe)
                _proxiedScreens.Add(proxyScreen);

            _proxyPipe.Clear();
        }

        private void Start()
        {
            StartCoroutine(ExecuteProxies());
        }

        public IEnumerator WaitForInitialization()
        {
            while (!FullyInitialized)
                yield return new WaitForSeconds(0.2f);
        }

        private IEnumerator ExecuteProxies()
        {
            while (!FullyInitialized)
                yield return new WaitForSeconds(0.2f);

            foreach (ProxyScreen proxyScreen in _proxiedScreens)
            {
                if (proxyScreen.RequestIndex > SceneChangeIndex)
                {
                    _proxyPipe.Add(proxyScreen); //copy to next scene change
                    continue;
                }

                if (proxyScreen.RequestIndex < SceneChangeIndex)
                {
                    //too old
                    Debug.LogWarning($"Old proxy screen, name: {proxyScreen.Name}, reqIdx: {proxyScreen.RequestIndex}, now: {SceneChangeIndex}");
                    continue;
                }

                Screen target = GetScreen(proxyScreen.Name);
                if (target == null)
                {
                    Debug.LogError($"Proxied screen does not exist, name: {proxyScreen.Name}");
                    continue;
                }

                if ((proxyScreen.Tasks & ProxyTask.Show) != 0)
                {
                    target.ShowScreen();
                    proxyScreen.ProxyOnShow?.Invoke(target);
                }

                if ((proxyScreen.Tasks & ProxyTask.Hide) != 0)
                    target.HideScreen();

                if ((proxyScreen.Tasks & ProxyTask.Move) != 0)
                    target.MoveToFront();

                proxyScreen.ProxyAction?.Invoke(target);
            }

            _proxiedScreens.Clear();
        }

        private static void OnSceneChanged(Scene s1, Scene s2)
        {
            SceneChangeIndex++;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Screen topMost = GetTopMostVisibleScreen((screen) => screen is ISupportsBackKey);
                if (topMost != null)
                {
                    ((ISupportsBackKey)topMost).OnBackKeyDown();
                }
            }
        }

        public void AddScreen(string name, Screen screen)
        {
            if (!_screens.ContainsKey(name))
            {
                MoveScreenToLayer(screen, screen.Layer);
                _screens[name] = screen;
                _screensTypes[screen.GetType()] = screen;

                //Layer isnt an idx
                _layerToScreens[screen.Layer - 1].Add(screen);
            }
        }

        public Screen GetScreen(string name)
        {
            if (!_screens.ContainsKey(name))
                return null;

            return _screens[name];
        }

        public T GetScreen<T>(string name) where T : Screen
        {
            return (T)GetScreen(name);
        }

        public T GetScreen<T>() where T : Screen
        {
            return (T)_screensTypes[typeof(T)];
        }

        public Popup GetPopup(string name)
        {
            return (Popup)GetScreen(name);
        }

        public T GetPopup<T>(string name) where T : Popup
        {
            return (T)GetPopup(name);
        }

        public T GetPopup<T>() where T : Popup
        {
            return GetScreen<T>();
        }

        public Screen GetTopMostVisibleScreen(Predicate<Screen> filter = null)
        {
            Screen topMost = null;

            EGRUtils.ReverseIterator(_maxLayerCount, (idx, exit) =>
            {
                foreach (Screen screen in _layerToScreens[idx])
                {
                    if (screen.Visible)
                    {
                        if (filter != null && !filter(screen))
                            continue;

                        exit.Value = true;
                        topMost = screen;
                        break;
                    }
                }
            });

            return topMost;
        }

        public void MoveScreenToLayer(Screen screen, int layer)
        {
            screen.transform.SetParent(_layers[Mathf.Clamp(layer, 1, _maxLayerCount) - 1].transform);
        }

        public void MoveScreenOnTop(Screen screen)
        {
            if (_topScreen != null)
                MoveScreenToLayer(_topScreen, _maxLayerCount - 1);
            _topScreen = screen;
            MoveScreenToLayer(screen, _maxLayerCount);
        }

        public Canvas GetLayer(int layer)
        {
            return _layers[layer - 1];
        }

        public Canvas GetLayer(Screen screen)
        {
            return GetLayer(screen.Layer);
        }

        public Canvas GetScreenSpaceLayer(int idx)
        {
            return _screenSpaceLayers[idx];
        }

        public ProxyScreen CreateProxy(string name, uint expectedInc)
        {
            ProxyScreen screen = new ProxyScreen(name, (uint)SceneChangeIndex + expectedInc);
            _proxyPipe.Add(screen);
            return screen;
        }
    }
}

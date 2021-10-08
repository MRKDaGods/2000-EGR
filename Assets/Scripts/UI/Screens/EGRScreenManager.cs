using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MRK.UI {
    public class EGRScreenManager : MonoBehaviour {
        [SerializeField]
        Canvas m_ScreensCanvas;
        [SerializeField]
        int m_MaxLayerCount;
        readonly Dictionary<string, EGRScreen> m_Screens;
        readonly List<Canvas> m_Layers;
        static EGRScreenManager ms_Instance;
        EGRScreen m_TopScreen;
        int m_TargetScreenCount;
        List<EGRProxyScreen> m_ProxiedScreens;
        readonly Dictionary<Type, EGRScreen> m_ScreensTypes;
        readonly Dictionary<int, HashSet<EGRScreen>> m_LayerToScreens;
        static readonly List<EGRProxyScreen> m_ProxyPipe;
        [SerializeField]
        Canvas[] m_ScreenSpaceLayers;
        readonly MRKSelfContainedPtr<EGRScreenMapInterface> m_MapInterface;
        readonly MRKSelfContainedPtr<EGRPopupMessageBox> m_MessageBox;
        readonly MRKSelfContainedPtr<EGRScreenMain> m_MainScreen;

        public static int SceneChangeIndex { get; private set; }

        public static EGRScreenManager Instance {
            get {
                if (ms_Instance == null) {
                    GameObject target = GameObject.Find("ScreenManager");
                    if (target == null)
                        target = new GameObject("ScreenManager");

                    ms_Instance = target.AddComponent<EGRScreenManager>();
                }

                return ms_Instance;
            }
        }
        public int ScreenCount => m_Screens.Keys.Count;
        public bool FullyInitialized => m_TargetScreenCount == ScreenCount;
        public EGRScreenMapInterface MapInterface => m_MapInterface;
        public EGRPopupMessageBox MessageBox => m_MessageBox;
        public EGRScreenMain MainScreen => m_MainScreen;

        static EGRScreenManager() {
            SceneManager.activeSceneChanged += OnSceneChanged;
            m_ProxyPipe = new List<EGRProxyScreen>();
        }

        public EGRScreenManager() {
            m_Screens = new Dictionary<string, EGRScreen>();
            m_ScreensTypes = new Dictionary<Type, EGRScreen>();
            m_Layers = new List<Canvas>();
            m_LayerToScreens = new Dictionary<int, HashSet<EGRScreen>>();

            m_MapInterface = new MRKSelfContainedPtr<EGRScreenMapInterface>(() => GetScreen<EGRScreenMapInterface>());
            m_MessageBox = new MRKSelfContainedPtr<EGRPopupMessageBox>(() => GetPopup<EGRPopupMessageBox>());
            m_MainScreen = new MRKSelfContainedPtr<EGRScreenMain>(() => GetScreen<EGRScreenMain>());
        }

        void Awake() {
            ms_Instance = this;

            m_TargetScreenCount = m_ScreensCanvas.GetComponentsInChildren<EGRScreen>().Length;
            

            GameObject container = new GameObject("Screens");

            for (int i = 0; i < m_MaxLayerCount; i++) {
                Canvas canv = Instantiate(m_ScreensCanvas);
                canv.transform.SetParent(container.transform);

                while (canv.transform.childCount > 0) {
                    Transform child = canv.transform.GetChild(0);
                    child.SetParent(null);
                    Destroy(child.gameObject);
                }

                canv.sortingOrder = i;
                canv.name = "Canvas-" + (i + 1);
                m_Layers.Add(canv);

                m_LayerToScreens[i] = new HashSet<EGRScreen>();
            }

            m_ProxiedScreens = new List<EGRProxyScreen>();
            foreach (EGRProxyScreen proxyScreen in m_ProxyPipe)
                m_ProxiedScreens.Add(proxyScreen);

            m_ProxyPipe.Clear();
        }

        void Start() {
            StartCoroutine(ExecuteProxies());
        }

        public IEnumerator WaitForInitialization() {
            while (!FullyInitialized)
                yield return new WaitForSeconds(0.2f);
        }

        IEnumerator ExecuteProxies() {
            while (!FullyInitialized)
                yield return new WaitForSeconds(0.2f);

            foreach (EGRProxyScreen proxyScreen in m_ProxiedScreens) {
                if (proxyScreen.RequestIndex > SceneChangeIndex) {
                    m_ProxyPipe.Add(proxyScreen); //copy to next scene change
                    continue;
                }

                if (proxyScreen.RequestIndex < SceneChangeIndex) {
                    //too old
                    Debug.LogWarning($"Old proxy screen, name: {proxyScreen.Name}, reqIdx: {proxyScreen.RequestIndex}, now: {SceneChangeIndex}");
                    continue;
                }

                EGRScreen target = GetScreen(proxyScreen.Name);
                if (target == null) {
                    Debug.LogError($"Proxied screen does not exist, name: {proxyScreen.Name}");
                    continue;
                }

                if ((proxyScreen.Tasks & ProxyTask.Show) != 0) {
                    target.ShowScreen();
                    proxyScreen.ProxyOnShow?.Invoke(target);
                }

                if ((proxyScreen.Tasks & ProxyTask.Hide) != 0)
                    target.HideScreen();

                if ((proxyScreen.Tasks & ProxyTask.Move) != 0)
                    target.MoveToFront();

                proxyScreen.ProxyAction?.Invoke(target);
            }

            m_ProxiedScreens.Clear();
        }

        static void OnSceneChanged(Scene s1, Scene s2) {
            SceneChangeIndex++;
        }

        void Update() {
            if (Input.GetKeyDown(KeyCode.Escape)) {
                EGRScreen topMost = GetTopMostVisibleScreen((screen) => screen is IEGRScreenSupportsBackKey);
                if (topMost != null) {
                    ((IEGRScreenSupportsBackKey)topMost).OnBackKeyDown();
                }
            }
        }

        public void AddScreen(string name, EGRScreen screen) {
            if (!m_Screens.ContainsKey(name)) {
                MoveScreenToLayer(screen, screen.Layer);
                m_Screens[name] = screen;
                m_ScreensTypes[screen.GetType()] = screen;

                //Layer isnt an idx
                m_LayerToScreens[screen.Layer - 1].Add(screen);
            }
        }

        public EGRScreen GetScreen(string name) {
            if (!m_Screens.ContainsKey(name))
                return null;

            return m_Screens[name];
        }

        public T GetScreen<T>(string name) where T : EGRScreen {
            return (T)GetScreen(name);
        }

        public T GetScreen<T>() where T : EGRScreen {
            return (T)m_ScreensTypes[typeof(T)];
        }

        public EGRPopup GetPopup(string name) {
            return (EGRPopup)GetScreen(name);
        }

        public T GetPopup<T>(string name) where T : EGRPopup {
            return (T)GetPopup(name);
        }

        public T GetPopup<T>() where T : EGRPopup {
            return GetScreen<T>();
        }

        public EGRScreen GetTopMostVisibleScreen(Predicate<EGRScreen> filter = null) {
            EGRScreen topMost = null;

            EGRUtils.ReverseIterator(m_MaxLayerCount, (idx, exit) => {
                foreach (EGRScreen screen in m_LayerToScreens[idx]) {
                    if (screen.Visible) {
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

        public void MoveScreenToLayer(EGRScreen screen, int layer) {
            screen.transform.SetParent(m_Layers[Mathf.Clamp(layer, 1, m_MaxLayerCount) - 1].transform);
        }

        public void MoveScreenOnTop(EGRScreen screen) {
            if (m_TopScreen != null)
                MoveScreenToLayer(m_TopScreen, m_MaxLayerCount - 1);
            m_TopScreen = screen;
            MoveScreenToLayer(screen, m_MaxLayerCount);
        }

        public Canvas GetLayer(int layer) {
            return m_Layers[layer - 1];
        }

        public Canvas GetLayer(EGRScreen screen) {
            return GetLayer(screen.Layer);
        }

        public Canvas GetScreenSpaceLayer(int idx) {
            return m_ScreenSpaceLayers[idx];
        }

        public EGRProxyScreen CreateProxy(string name, uint expectedInc) {
            EGRProxyScreen screen = new EGRProxyScreen(name, (uint)SceneChangeIndex + expectedInc);
            m_ProxyPipe.Add(screen);
            return screen;
        }
    }
}

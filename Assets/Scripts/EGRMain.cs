//#define NO_LOADING_SCREEN

using DG.Tweening;
using MRK.Networking;
using MRK.Networking.Packets;
using MRK.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using System.IO;

namespace MRK {
    public enum EGRMapMode {
        Globe,
        Flat,
        General,

        MAX
    }

    public delegate void EGRMapModeChangedDelegate(EGRMapMode mode);

    public class EGRMain : MonoBehaviour {
        [Serializable]
        struct EGRCameraConfig {
            public EGRMapMode Mode;
            public Vector3 Position;
            public Vector3 EulerRotation;
        }

        const string HASH_SALT = "LrtLpJL4DeGxG5Atjza46OHEiyasOOXtbROGiSbP";

        readonly List<EGRLogger> m_Loggers;
        [SerializeField]
        EGRCameraConfig[] m_CameraConfigs;
        [SerializeField]
        EGRMapMode m_MapMode;
        float m_CamDelta;
        bool m_CamDirty;
        GameObject m_GlobalMap;
        MRKMap m_FlatMap;
        EGRMapModeChangedDelegate m_OnMapModeChanged;
        EGRCameraGlobe m_GlobeCamera;
        EGRCameraFlat m_FlatCamera;
        EGRCameraGeneral m_GeneralCamera;
        EGRNetwork m_Network;
        bool m_MapsInitialized;
        readonly List<EGRScreen> m_ActiveScreens;
        bool m_LockScreens;
        [SerializeField]
        bool m_DrawFPS;
        float m_DeltaTime;
        GUIStyle m_FPSStyle;
        readonly List<EGRController> m_Controllers;
        [SerializeField]
        Transform[] m_Planets;
        [SerializeField]
        Transform m_Sun;
        [SerializeField]
        Camera m_ExCamera;
        EGRDevSettingsManager m_DevSettingsManager;
        Vector3 m_CamStartPos;
        Vector3 m_CamStartRot;
        EGRMapMode m_PreviousMapMode;
        bool m_InitialModeTransition;
        uint m_StatusBarColor;
        bool m_StatusBarTextureDirty;
        Texture2D m_StatusBarTexture;
        [SerializeField]
        ParticleSystem m_EnvironmentEmitter;
        readonly Dictionary<Transform, float> m_PlanetRotationCache;
        float m_LastPhysicsSimulationTime; //we need to manually simulate physics for the sun flare to stop appearing through planets

        public static EGRMain Instance { get; private set; }

        EGRScreenManager ScreenManager => EGRScreenManager.Instance;
        public Camera ActiveCamera => Camera.main;
        public EGRMapMode MapMode => m_MapMode;
        public GameObject GlobalMap => m_GlobalMap;
        public MRKMap FlatMap => m_FlatMap;
        public EGRCamera ActiveEGRCamera => m_MapMode == EGRMapMode.Flat ? (EGRCamera)m_FlatCamera : m_MapMode == EGRMapMode.Globe ? (EGRCamera)m_GlobeCamera : m_GeneralCamera;
        public EGRNetwork Network => m_Network;
        public EGRLanguageManager LanguageManager { get; private set; }
        public bool CamDirty => m_CamDirty;
        public List<EGRScreen> ActiveScreens => m_ActiveScreens;
        public EGRPlaceManager PlaceManager { get; private set; }
        public bool InitialModeTransition => m_InitialModeTransition;
        public EGRMapMode PreviousMapMode => m_PreviousMapMode;
        public Transform[] Planets => m_Planets;
        public CoroutineRunner Runnable { get; private set; }

        public EGRMain() {
            m_Loggers = new List<EGRLogger>();
            m_ActiveScreens = new List<EGRScreen>();
            m_Controllers = new List<EGRController>();
            m_PlanetRotationCache = new Dictionary<Transform, float>();
        }

        void Awake() {
            Instance = this;

            Screen.fullScreen = false;
            Application.targetFrameRate = 60;
            //Application.runInBackground = true;
            Physics.autoSimulation = false;

            m_Loggers.Add(new UnityLogger());

            m_GlobalMap = GameObject.Find("EGRGlobeMap");
            m_FlatMap = GameObject.Find("EGRMap").GetComponent<MRKMap>();

            m_GlobeCamera = m_GlobalMap.GetComponent<EGRCameraGlobe>();
            m_FlatCamera = m_FlatMap.GetComponent<EGRCameraFlat>();
            m_FlatMap.SetMapController(m_FlatCamera);

            m_GeneralCamera = GameObject.Find("EGRGeneralCamera").GetComponent<EGRCameraGeneral>();

            PlaceManager = gameObject.AddComponent<EGRPlaceManager>();

            if (Input.touchSupported)
                m_Controllers.Add(new EGRVirtualController());

            if (m_Controllers.Count == 0 || Input.stylusTouchSupported)
                m_Controllers.Add(new EGRPhysicalController());

            foreach (EGRController ctrl in m_Controllers)
                ctrl.InitController();

            LanguageManager = new EGRLanguageManager();
            LanguageManager.Init();

            EGREventManager.Instance.Register<EGREventScreenShown>(OnScreenShown);
            EGREventManager.Instance.Register<EGREventScreenHidden>(OnScreenHidden);
            EGREventManager.Instance.Register<EGREventGraphicsApplied>(OnGraphicsApplied);

            Runnable = gameObject.AddComponent<CoroutineRunner>();
        }

        IEnumerator Start() {
            Log($"2000-EGR started v{EGRVersion.VersionString()} - {EGRVersion.VersionSignature()}");

            while (!ScreenManager.FullyInitialized)
                yield return new WaitForSeconds(0.2f);

            Log("EGRScreenManager initialized");

#if UNITY_ANDROID
            EGRAndroidUtils.StatusBarColor = EGRAndroidUtils.NavigationBarColor = 0x00000000;
#endif

            //load settings
            EGRSettings.Load();
            EGRSettings.Apply();

            //initial mode should be globe
            SetMapMode(EGRMapMode.Globe);

            m_ExCamera.targetTexture.width = Screen.width;
            m_ExCamera.targetTexture.height = Screen.height;

#if !NO_LOADING_SCREEN
            //show loading screen
            ScreenManager.GetScreen(EGRUI_Main.EGRScreen_Loading.SCREEN_NAME).ShowScreen();
#else
            Debug.Log("Skipping loading screen");
            ScreenManager.GetScreen(EGRUI_Main.EGRScreen_Main.SCREEN_NAME).ShowScreen();
#endif

            foreach (Type type in Assembly.GetExecutingAssembly().GetLoadedModules()[0].GetTypes()) {
                if (type.Namespace != "MRK.Networking.Packets")
                    continue;

                PacketRegInfo regInfo = type.GetCustomAttribute<PacketRegInfo>();
                if (regInfo != null) {
                    if (regInfo.PacketNature == PacketNature.Out)
                        Packet.RegisterOut(regInfo.PacketType, type);
                    else
                        Packet.RegisterIn(regInfo.PacketType, type);
                }
            }

            m_Network = new EGRNetwork("37.58.62.171", EGRConstants.EGR_MAIN_NETWORK_PORT, EGRConstants.EGR_MAIN_NETWORK_KEY);
            m_Network.Connect();
        }

        void OnDestroy() {
            EGREventManager.Instance.Unregister<EGREventScreenShown>(OnScreenShown);
            EGREventManager.Instance.Unregister<EGREventScreenHidden>(OnScreenHidden);
            EGREventManager.Instance.Unregister<EGREventGraphicsApplied>(OnGraphicsApplied);
        }

        public void RegisterDevSettings<T>() where T : EGRDevSettings, new() {
            //manually init
            if (m_DevSettingsManager == null)
                m_DevSettingsManager = gameObject.AddComponent<EGRDevSettingsManager>();

            m_DevSettingsManager.RegisterSettings<T>();
        }

        public void SetMapMode(EGRMapMode mode) {
            if (m_MapMode == mode)
                return;

            m_PreviousMapMode = m_MapMode;
            m_MapMode = mode;
            m_CamDirty = true;
            m_CamDelta = 0f;
            m_CamStartPos = ActiveCamera.transform.position;
            m_CamStartRot = ActiveCamera.transform.rotation.eulerAngles;

            m_OnMapModeChanged?.Invoke(mode);

            SetGlobalCameraClearFlags(mode == EGRMapMode.Flat ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox);
        }

        void Update() {
            if (m_CamDirty) {
                m_CamDelta += m_PreviousMapMode == EGRMapMode.General ? Time.deltaTime : 1f;

                EGRMapMode inverseMode = (EGRMapMode)((int)(m_MapMode + 1) % (int)EGRMapMode.MAX);
                EGRCameraConfig inverseConfig = m_CameraConfigs[(int)inverseMode];
                EGRCameraConfig currentConfig = m_CameraConfigs[(int)m_MapMode];

                (Vector3, Vector3) target = (currentConfig.Position, currentConfig.EulerRotation);
                if (m_PreviousMapMode == EGRMapMode.General && !m_InitialModeTransition) {
                    //get direct config from globe cam
                    target = m_GlobeCamera.GetSamplePosRot();
                    m_InitialModeTransition = true;

                    ActiveCamera.transform.DORotate(target.Item2, 1.5f, RotateMode.FastBeyond360)
                        .SetEase(Ease.OutBack);
                    ActiveCamera.transform.DOMove(target.Item1, 1f)
                        .SetEase(Ease.OutBack);
                }

                if (!m_InitialModeTransition) {
                    ActiveCamera.transform.position = Vector3.Lerp(m_CamStartPos, target.Item1, m_CamDelta);
                    ActiveCamera.transform.rotation = Quaternion.Euler(Vector3.Lerp(m_CamStartRot, target.Item2, Mathf.Clamp01(m_CamDelta * 2f)));
                }

                if (m_CamDelta >= (m_InitialModeTransition ? 1.5f : 1f)) {
                    m_CamDirty = false;
                    m_InitialModeTransition = false;

                    ActiveEGRCamera.SetInterfaceState(ScreenManager.GetScreen(EGRUI_Main.EGRScreen_MapInterface.SCREEN_NAME).Visible);
                }
            }

            foreach (EGRController ctrl in m_Controllers)
                ctrl.UpdateController();

            if (m_MapsInitialized) {
                m_GlobalMap.SetActive(m_MapMode == EGRMapMode.Globe);
                m_FlatMap.gameObject.SetActive(m_MapMode == EGRMapMode.Flat);
                m_GeneralCamera.gameObject.SetActive(m_MapMode == EGRMapMode.General);
            }

            if (m_Network != null) {
                m_Network.UpdateNetwork();
            }

            if (m_DrawFPS) {
                m_DeltaTime += (Time.unscaledDeltaTime - m_DeltaTime) * 0.1f;
            }

            if (m_MapMode == EGRMapMode.Globe && !m_GlobeCamera.IsLocked) {
                bool storeRotations = m_PlanetRotationCache.Count == 0;

                foreach (Transform trans in m_Planets) {
                    if (storeRotations) {
                        int sibIdx = trans.GetSiblingIndex();
                        m_PlanetRotationCache[trans] = sibIdx == 1 || sibIdx == 5 ? -1f : 1f;
                    }

                    trans.RotateAround(m_Sun.position, Vector3.up, m_PlanetRotationCache[trans] * 2f * Time.deltaTime * (1f - (Vector3.Distance(trans.position, m_Sun.position) / 80000f)));
                }
            }

            //simulate physics if we're only in space
            if (ActiveEGRCamera.InterfaceActive) {
                if (m_MapMode == EGRMapMode.Globe) {
                    if (Time.time - m_LastPhysicsSimulationTime > 0.5f) {
                        m_LastPhysicsSimulationTime = Time.time;
                        Physics.Simulate(0.5f);
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.K)) ScreenManager.GetScreen("FU").ShowScreen();
        }

        void OnGUI() {
            if (m_DrawFPS) {
                if (m_FPSStyle == null) {
                    m_FPSStyle = new GUIStyle {
                        alignment = TextAnchor.LowerLeft,
                        fontSize = 27,
                        normal =
                        {
                            textColor = Color.yellow
                        },
                        richText = true
                    };
                }

                GUI.Label(new Rect(40f, Screen.height - 50f, 100f, 50f), string.Format("<b>{0:0.0}</b> ms (<b>{1:0.}</b> fps) {2}", 
                    m_DeltaTime * 1000f, 1f / m_DeltaTime, DOTween.TotalPlayingTweens()), m_FPSStyle);
            }

            Rect safeArea = Screen.safeArea;
            safeArea.y = Screen.height - safeArea.height;

            if (safeArea.y < Mathf.Epsilon)
                return;

            if (m_StatusBarTextureDirty || m_StatusBarTexture == null) {
                m_StatusBarTextureDirty = false;

                byte a = (byte)((m_StatusBarColor & 0xFF000000) >> 24);
                byte r = (byte)((m_StatusBarColor & 0x00FF0000) >> 16);
                byte g = (byte)((m_StatusBarColor & 0x0000FF00) >> 8);
                byte b = (byte)(m_StatusBarColor & 0x000000FF);

                m_StatusBarTexture = EGRUIUtilities.GetPlainTexture(new Color32(r, g, b, a));
            }

            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, safeArea.y), m_StatusBarTexture);
        }

        public void RegisterMapModeDelegate(EGRMapModeChangedDelegate del) {
            m_OnMapModeChanged += del;
        }

        public void UnregisterMapModeDelegate(EGRMapModeChangedDelegate del) {
            m_OnMapModeChanged -= del;
        }

        public void RegisterControllerReceiver(EGRControllerMessageReceivedDelegate receivedDelegate) {
            foreach (EGRController ctrl in m_Controllers)
                ctrl.RegisterReceiver(receivedDelegate);
        }

        public void UnregisterControllerReceiver(EGRControllerMessageReceivedDelegate receivedDelegate) {
            foreach (EGRController ctrl in m_Controllers)
                ctrl.UnregisterReceiver(receivedDelegate);
        }

        public EGRController GetControllerFromMessage(EGRControllerMessage msg) {
            return m_Controllers.Find(x => x.MessageKind == msg.Kind);
        }

        public void InitializeMaps() {
            m_MapsInitialized = true;
            m_FlatMap.Initialize(new Vector2d(30.04584d, 30.98313d), 4);
        }

        public void Initialize() {
            ActiveCamera.clearFlags = CameraClearFlags.Skybox;
            ActiveCamera.cullingMask = LayerMask.NameToLayer("Everything");
            SetPostProcessState(false);
            //m_Sun.parent.gameObject.SetActive(true);
        }

        public void SetPostProcessState(bool active) {
            if (EGRSettings.Quality == EGRSettingsQuality.Low)
                active = false;

            ActiveCamera.GetComponent<PostProcessLayer>().enabled = active;
        }

        public T GetActivePostProcessEffect<T>() where T : PostProcessEffectSettings {
            return ActiveEGRCamera.GetComponent<PostProcessVolume>().profile.GetSetting<T>();
        }

        public void FixInvalidTiles() {
            foreach (MRKTilesetProvider provider in MRKTileRequestor.Instance.TilesetProviders) {
                string dir = MRKTileRequestor.Instance.FileTileFetcher.GetFolderPath(provider.Name);
                if (Directory.Exists(dir)) {
                    foreach (string filename in Directory.EnumerateFiles(dir, "*.png")) {
                        if (new FileInfo(filename).Length < 100) {
                            File.Delete(filename);
                        }
                    }
                }
            }
        }

        public static string CalculateHash(string input) {
            using (MD5 md5 = MD5.Create()) {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input + HASH_SALT);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                    sb.Append(hashBytes[i].ToString("X2"));

                return sb.ToString();
            }
        }

        void OnScreenShown(EGREventScreenShown evt) {
            if (evt.Screen.ScreenName != "EGRDEV")
                m_ActiveScreens.Add(evt.Screen);
        }

        void OnScreenHidden(EGREventScreenHidden evt) {
            if (!m_LockScreens)
                m_ActiveScreens.Remove(evt.Screen);
        }

        public void DisableAllScreensExcept<T>(params Type[] excluded) {
            m_LockScreens = true;
            lock (m_ActiveScreens) {
                for (int i = m_ActiveScreens.Count - 1; i > -1; i--) {
                    if (m_ActiveScreens[i] is T)
                        continue;

                    bool ex = false;
                    foreach (Type t in excluded) {
                        if (m_ActiveScreens[i].GetType() == t) {
                            ex = true;
                            break;
                        }
                    }

                    if (ex)
                        continue;

                    m_ActiveScreens[i].ForceHideScreen();
                    m_ActiveScreens.RemoveAt(i);
                }
            }
            m_LockScreens = false;
        }

        void OnGraphicsApplied(EGREventGraphicsApplied evt) {
            foreach (Transform trans in m_Planets) {
                trans.Find("Halo").gameObject.SetActive(evt.Quality == EGRSettingsQuality.Ultra);
                trans.gameObject.SetActive(evt.Quality > EGRSettingsQuality.Medium);
                if (trans.gameObject.activeInHierarchy) {
                    trans.RotateAround(m_Sun.position, Vector3.up, UnityEngine.Random.value * 360f);
                }
            }

            m_EnvironmentEmitter.gameObject.SetActive(evt.Quality > EGRSettingsQuality.Medium);
            m_Sun.gameObject.SetActive(evt.Quality > EGRSettingsQuality.Low);
            m_GlobalMap.transform.Find("Halo").gameObject.SetActive(evt.Quality == EGRSettingsQuality.Ultra);
            m_GlobeCamera.GetComponent<PostProcessVolume>().profile.GetSetting<Bloom>().threshold.value = evt.Quality == EGRSettingsQuality.Ultra ? 0.9f : 1f;
        }

        public void SetGlobalCameraClearFlags(CameraClearFlags flags) {
            ActiveCamera.clearFlags = flags;
            m_ExCamera.clearFlags = flags;
        }

        public RenderTexture CaptureScreenBuffer() {
            m_ExCamera.gameObject.SetActive(true);
            m_ExCamera.transform.position = ActiveCamera.transform.position;
            m_ExCamera.transform.rotation = ActiveCamera.transform.rotation;
            m_ExCamera.Render();

            RenderTexture newRt = new RenderTexture(m_ExCamera.targetTexture);
            Graphics.CopyTexture(m_ExCamera.targetTexture, newRt);

            m_ExCamera.gameObject.SetActive(false);
            return newRt;
        }

        public void Logout() {
            m_LockScreens = true;
            m_ActiveScreens.ForEach(x => x.HideScreen());
            m_LockScreens = false;

            PlayerPrefs.SetInt(EGRConstants.EGR_LOCALPREFS_REMEMBERME, 0);
            PlayerPrefs.SetString(EGRConstants.EGR_LOCALPREFS_PASSWORD, "");
            ScreenManager.GetScreen(EGRUI_Main.EGRScreen_Login.SCREEN_NAME).ShowScreen();

            //send logout packet to server?
            Network.SendStationaryPacket<Packet>(PacketType.LGNOUT, DeliveryMethod.ReliableOrdered, null);

            EGRLocalUser.Initialize(null);
        }

        public bool NetRegisterAccount(string name, string email, string password, EGRPacketReceivedCallback<PacketInStandardResponse> callback) {
            return Network.SendPacket(new PacketOutRegisterAccount(name, email, CalculateHash(password)), DeliveryMethod.ReliableOrdered, callback);
        }

        public bool NetLoginAccount(string email, string password, EGRPacketReceivedCallback<PacketInLoginAccount> callback) {
            return Network.SendPacket(new PacketOutLoginAccount(email, CalculateHash(password)), DeliveryMethod.ReliableOrdered, callback);
        }

        public bool NetLoginAccountToken(string token, EGRPacketReceivedCallback<PacketInLoginAccount> callback) {
            return Network.SendPacket(new PacketOutLoginAccountToken(token), DeliveryMethod.ReliableOrdered, callback);
        }

        public bool NetLoginAccountDev(EGRPacketReceivedCallback<PacketInLoginAccount> callback) {
            return Network.SendStationaryPacket(PacketType.LGNACCDEV, DeliveryMethod.ReliableOrdered, callback, writer => {
                //we'll send the deviceName and deviceModel as our first/last name
                writer.WriteString(SystemInfo.deviceName);
                writer.WriteString(SystemInfo.deviceModel);
            });
        }

        public bool NetUpdateAccountInfo(string name, string email, sbyte gender, EGRPacketReceivedCallback<PacketInStandardResponse> callback) {
            return Network.SendPacket(new PacketOutUpdateAccountInfo(EGRLocalUser.Instance.Token, name, email, gender), DeliveryMethod.ReliableOrdered, callback);
        }

        public bool NetFetchPlace(ulong cid, EGRPacketReceivedCallback<PacketInFetchPlaces> callback) {
            return Network.SendPacket(new PacketOutFetchPlaces(cid), DeliveryMethod.ReliableOrdered, callback);
        }

        public bool NetFetchPlacesIDs(ulong ctx, double minLat, double minLng, double maxLat, double maxLng, int zoom, EGRPacketReceivedCallback<PacketInFetchPlacesIDs> callback) {
            return Network.SendPacket(new PacketOutFetchPlacesIDs(ctx, minLat, minLng, maxLat, maxLng, zoom), DeliveryMethod.ReliableOrdered, callback);
        }

        public bool NetFetchPlacesV2(int hash, double minLat, double minLng, double maxLat, double maxLng, int zoom, EGRPacketReceivedCallback<PacketInFetchPlacesV2> callback) {
            return Network.SendPacket(new PacketOutFetchPlacesV2(hash, minLat, minLng, maxLat, maxLng, zoom), DeliveryMethod.ReliableOrdered, callback);
        }

        public bool NetFetchTile(string tileSet, MRKTileID id, EGRPacketReceivedCallback<PacketInFetchTile> callback) {
            return Network.SendPacket(new PacketOutFetchTile(tileSet, id), DeliveryMethod.ReliableOrdered, callback);
        }

        public bool NetUpdateAccountPassword(string pass, bool logoutAll, EGRPacketReceivedCallback<PacketInStandardResponse> callback) {
            return Network.SendPacket(new PacketOutUpdatePassword(EGRLocalUser.Instance.Token, pass, logoutAll), DeliveryMethod.ReliableOrdered, callback);
        }

        void _Log(DateTime timestamp, LogType type, string msg) {
            foreach (EGRLogger logger in m_Loggers)
                logger.Log(timestamp, type, msg);
        }

        public static void Log(string msg) {
            Log(LogType.Info, msg);
        }

        public static void Log(LogType type, string msg) {
            if (Instance != null)
                Instance._Log(DateTime.Now, type, msg);
            else
                Debug.Log(msg);
        }

        public static void SetContextualColor(EGRScreen screen) {
            /* #if UNITY_ANDROID
                        if (screen.CanChangeBar) {
                            EGRAndroidUtils.StatusBarColor = EGRAndroidUtils.NavigationBarColor = screen.BarColor;
                        }
            #endif */

            if (screen.CanChangeBar) {
                Instance.m_StatusBarTextureDirty = true;
                Instance.m_StatusBarColor = screen.BarColor;

                //attempt to change navbar
#if UNITY_ANDROID
                EGRAndroidUtils.NavigationBarColor = screen.BarColor;
#endif 
            }
        }
    }
}

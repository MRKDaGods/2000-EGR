//#define NO_LOADING_SCREEN
#define MRK_LOCAL_SERVER

using DG.Tweening;
using MRK.Navigation;
using MRK.Networking;
using MRK.Networking.Packets;
using MRK.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace MRK {
    /// <summary>
    /// Available Camera/Map modes in EGR
    /// </summary>
    public enum EGRMapMode {
        /// <summary>
        /// Space/Globe
        /// </summary>
        Globe,

        /// <summary>
        /// Flat/Geographical Map
        /// </summary>
        Flat,

        /// <summary>
        /// Free moving (used in Login/Register screens)
        /// </summary>
        General,

        /// <summary>
        /// MAX ID, always keep it last
        /// </summary>
        MAX
    }

    /// <summary>
    /// Delegate type that is invoked when the mapmode has changed
    /// </summary>
    /// <param name="mode"></param>
    public delegate void EGRMapModeChangedDelegate(EGRMapMode mode);

    /// <summary>
    /// Main entrypoint class of 2000EGR
    /// </summary>
    public class EGRMain : MonoBehaviour {
        /// <summary>
        /// Holds information of a camera configuration for a certain EGRMapMode
        /// </summary>
        [Serializable]
        struct EGRCameraConfig {
            /// <summary>
            /// The map mode corresponding to the camera configuration
            /// </summary>
            public EGRMapMode Mode;
            /// <summary>
            /// Position of the camera
            /// </summary>
            public Vector3 Position;
            /// <summary>
            /// Rotation of the camera in euler coordinates
            /// </summary>
            public Vector3 EulerRotation;
        }

        [SerializeField]
        EGRRuntimeConfiguration m_RuntimeConfiguration;

        /// <summary>
        /// Available camera configurations
        /// </summary>
        [SerializeField]
        EGRCameraConfig[] m_CameraConfigs;

        /// <summary>
        /// Currently selected map mode
        /// </summary>
        [SerializeField]
        EGRMapMode m_MapMode;

        /// <summary>
        /// Time transition progress between 2 camera configurations, 0 to 1
        /// </summary>
        float m_CamDelta;

        /// <summary>
        /// Indicates if the camera configuration has changed and that the camera state has to be updated ASAP
        /// </summary>
        bool m_CamDirty;

        /// <summary>
        /// The Earth globe object
        /// </summary>
        GameObject m_GlobalMap;

        /// <summary>
        /// The flat/geographical map
        /// </summary>
        MRKMap m_FlatMap;

        /// <summary>
        /// Gets called when the map mode is changed
        /// </summary>
        EGRMapModeChangedDelegate m_OnMapModeChanged;

        /// <summary>
        /// Camera handler when the selected map mode is Globe
        /// </summary>
        EGRCameraGlobe m_GlobeCamera;

        /// <summary>
        /// Camera handler when the selected map mode is Flat
        /// </summary>
        EGRCameraFlat m_FlatCamera;

        /// <summary>
        /// Camera handler when the selected map mode is General
        /// </summary>
        EGRCameraGeneral m_GeneralCamera;

        /// <summary>
        /// Indicates if the flat/geographical has initialized
        /// </summary>
        bool m_MapsInitialized;

        /// <summary>
        /// Currently active screens
        /// </summary>
        readonly List<EGRScreen> m_ActiveScreens;

        /// <summary>
        /// Indicates if the active screens should become locked and may not become modified
        /// </summary>
        bool m_LockScreens;

        /// <summary>
        /// Indicates if the FPS should be drawn
        /// </summary>
        [SerializeField]
        bool m_DrawFPS;

        /// <summary>
        /// Time difference since last frame, used for calculating FPS
        /// </summary>
        float m_DeltaTime;

        /// <summary>
        /// Render style of the FPS label
        /// </summary>
        GUIStyle m_FPSStyle;

        /// <summary>
        /// Active input controllers
        /// </summary>
        readonly List<EGRController> m_Controllers;

        /// <summary>
        /// The planets' transform, does not include Earth
        /// </summary>
        [SerializeField]
        Transform[] m_Planets;

        /// <summary>
        /// The sun's transform
        /// </summary>
        [SerializeField]
        Transform m_Sun;

        /// <summary>
        /// Extra camera that is used on-demand for Post-Processing and transition effects
        /// </summary>
        [SerializeField]
        Camera m_ExCamera;

        /// <summary>
        /// Developer settings manager, manually initialized
        /// </summary>
        EGRDevSettingsManager m_DevSettingsManager;

        /// <summary>
        /// Starting position of camera as soon as the map mode has changed
        /// </summary>
        Vector3 m_CamStartPos;

        /// <summary>
        /// Starting rotation of camera as soon as the map mode has changed
        /// </summary>
        Vector3 m_CamStartRot;

        /// <summary>
        /// The previous map mode that was active before the current map mode
        /// </summary>
        EGRMapMode m_PreviousMapMode;

        /// <summary>
        /// Indicates if the initial transition between General and Globe map modes is active
        /// </summary>
        bool m_InitialModeTransition;

        /// <summary>
        /// Active status bar color in ARGB (Android and iOS only)
        /// </summary>
        uint m_StatusBarColor;

        /// <summary>
        /// Indicates if the status bar texture should be re-generated, for example: the status bar color being changed
        /// </summary>
        bool m_StatusBarTextureDirty;

        /// <summary>
        /// The status bar texture
        /// </summary>
        Texture2D m_StatusBarTexture;

        /// <summary>
        /// A particle emitter to simulate some space dust around the globe
        /// </summary>
        ParticleSystem m_EnvironmentEmitter;

        /// <summary>
        /// A table that stores planet rotation coefficients, some planets rotate differently than others
        /// </summary>
        readonly Dictionary<Transform, float> m_PlanetRotationCache;

        /// <summary>
        /// Time when physics were last simulated
        /// <para>We need to manually simulate physics for the sun flare to stop appearing through planets</para>
        /// </summary>
        float m_LastPhysicsSimulationTime;

        /// <summary>
        /// Current skybox rotation along the Y-axis, 0 to 360
        /// </summary>
        float m_SkyboxRotation;

        /// <summary>
        /// EGRMain's singleton instance
        /// </summary>
        public static EGRMain Instance { get; private set; }

        public EGRRuntimeConfiguration RuntimeConfiguration => m_RuntimeConfiguration;
        /// <summary>
        /// The screen manager
        /// </summary>
        public EGRScreenManager ScreenManager => EGRScreenManager.Instance;

        /// <summary>
        /// Currently active camera
        /// </summary>
        public Camera ActiveCamera => Camera.main;

        /// <summary>
        /// Currently selected map mode
        /// </summary>
        public EGRMapMode MapMode => m_MapMode;

        /// <summary>
        /// The Earth globe object
        /// </summary>
        public GameObject GlobalMap => m_GlobalMap;

        /// <summary>
        /// The flat/geographical map
        /// </summary>
        public MRKMap FlatMap => m_FlatMap;

        /// <summary>
        /// Currently active camera handler
        /// </summary>
        public EGRCamera ActiveEGRCamera => m_MapMode == EGRMapMode.Flat ? (EGRCamera)m_FlatCamera
            : m_MapMode == EGRMapMode.Globe ? (EGRCamera)m_GlobeCamera : m_GeneralCamera;

        /// <summary>
        /// Camera handler when the selected map mode is Flat
        /// </summary>
        public EGRCameraFlat FlatCamera => m_FlatCamera;

        /// <summary>
        /// Camera handler when the selected map mode is Globe
        /// </summary>
        public EGRCameraGlobe GlobeCamera => m_GlobeCamera;

        public EGRNetworkingClient NetworkingClient { get; private set; }

        /// <summary>
        /// The language manager
        /// </summary>
        public EGRLanguageManager LanguageManager { get; private set; }

        /// <summary>
        /// Indicates if the camera configuration has changed and that the camera state has to be updated ASAP
        /// </summary>
        public bool CamDirty => m_CamDirty;

        /// <summary>
        /// Currently active screens
        /// </summary>
        public List<EGRScreen> ActiveScreens => m_ActiveScreens;

        /// <summary>
        /// The place manager
        /// </summary>
        public EGRPlaceManager PlaceManager { get; private set; }

        /// <summary>
        /// Indicates if the initial transition between General and Globe map modes is active
        /// </summary>
        public bool InitialModeTransition => m_InitialModeTransition;

        /// <summary>
        /// The previous map mode that was active before the current map mode
        /// </summary>
        public EGRMapMode PreviousMapMode => m_PreviousMapMode;

        /// <summary>
        /// The planets' transform, does not include Earth
        /// </summary>
        public Transform[] Planets => m_Planets;

        /// <summary>
        /// The sun's transform
        /// </summary>
        public Transform Sun => m_Sun;

        /// <summary>
        /// A runnable having the same lifetime as the client (owned by EGRMain)
        /// </summary>
        public MRKRunnable Runnable { get; private set; }

        /// <summary>
        /// Currently active input model
        /// </summary>
        public EGRInputModel InputModel { get; private set; }

        /// <summary>
        /// The navigation manager
        /// </summary>
        public EGRNavigationManager NavigationManager { get; private set; }

        /// <summary>
        /// Indicates if the application is running/alive
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Initializes the location service and provides location information
        /// </summary>
        public EGRLocationService LocationService { get; private set; }

        /// <summary>
        /// The location manager
        /// </summary>
        public EGRLocationManager LocationManager { get; private set; }
        public IEGRScreenFOVStabilizer FOVStabilizer { get; private set; }
        public MRKThreadPool GlobalThreadPool { get; private set; }
        public EGRAuthenticationManager AuthenticationManager { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public EGRMain() {
            m_ActiveScreens = new List<EGRScreen>();
            m_Controllers = new List<EGRController>();
            m_PlanetRotationCache = new Dictionary<Transform, float>();

            //application has started running
            IsRunning = true;

            GlobalThreadPool = new MRKThreadPool(EGRConstants.EGR_DEFAULT_THREAD_POOL_INTERVAL);
            AuthenticationManager = new EGRAuthenticationManager();
        }

        /// <summary>
        /// Initialization
        /// </summary>
        void Awake() {
            //assign Instance to the current instances given by Unity
            Instance = this;

            //no fullscreen, we want to show the status bar on mobile platforms
            Screen.fullScreen = false;
            //defualt fallback framerate set to 60
            Application.targetFrameRate = 60;

            //we will only simulate physics manually to save performance
            Physics.autoSimulation = false;

            MRKSysUtils.Initialize();

            //add logger
            MRKLogger.AddLogger<UnityLogger>();

            MRKCryptography.CookSalt();
            MRKPlayerPrefs.Init();

            m_GlobalMap = m_RuntimeConfiguration.EarthGlobe;
            m_GlobeCamera = m_GlobalMap.GetComponent<EGRCameraGlobe>();

            m_FlatMap = m_RuntimeConfiguration.FlatMap;
            m_FlatCamera = m_FlatMap.GetComponent<EGRCameraFlat>();
            m_FlatMap.SetMapController(m_FlatCamera);

            m_GeneralCamera = m_RuntimeConfiguration.GeneralCamera;

            //manually add EGRPlaceManager to our main object
            PlaceManager = gameObject.AddComponent<EGRPlaceManager>();

            NavigationManager = m_RuntimeConfiguration.NavigationManager;
            LocationService = m_RuntimeConfiguration.LocationService;

            m_EnvironmentEmitter = m_RuntimeConfiguration.EnvironmentEmitter;

            //add a virtual controller if the current device supports touch input
            if (Input.touchSupported)
                m_Controllers.Add(new EGRVirtualController());

            //add a physical controller if we do not have any controllers (no touch support) or a stylus is supported
            if (m_Controllers.Count == 0 || Input.stylusTouchSupported)
                m_Controllers.Add(new EGRPhysicalController());

            //initialize all controllers
            foreach (EGRController ctrl in m_Controllers)
                ctrl.InitController();

            //initialize language manager
            LanguageManager = new EGRLanguageManager();
            LanguageManager.Init();

            //register some events
            EGREventManager.Instance.Register<EGREventScreenShown>(OnScreenShown);
            EGREventManager.Instance.Register<EGREventScreenHidden>(OnScreenHidden);
            EGREventManager.Instance.Register<EGREventGraphicsApplied>(OnGraphicsApplied);
            EGREventManager.Instance.Register<EGREventSettingsSaved>(OnSettingsSaved);

            //manually add MRKRunnable to our main object
            Runnable = gameObject.AddComponent<MRKRunnable>();

            NetworkingClient = new EGRNetworkingClient();
        }

        /// <summary>
        /// late initialization
        /// </summary>
        /// <returns></returns>
        IEnumerator Start() {
            MRKLogger.Log($"2000-EGR started v{EGRVersion.VersionString()} - {EGRVersion.VersionSignature()}");

            //keep waiting until all screens have initialized
            yield return ScreenManager.WaitForInitialization();

            MRKLogger.Log("EGRScreenManager initialized");

#if UNITY_ANDROID
            //set native status bar and navigation bar to transparent in android
            EGRAndroidUtils.StatusBarColor = EGRAndroidUtils.NavigationBarColor = 0x00000000;
#endif

            //load settings
            EGRSettings.Load();
            //apply graphical settings
            EGRSettings.Apply();

            //init location manager
            LocationManager = gameObject.AddComponent<EGRLocationManager>();

            //update input model
            UpdateInputModel();

            //initial mode should be globe
            SetMapMode(EGRMapMode.Globe);

            //set the extra camera's dimensions to fit the screen
            m_ExCamera.targetTexture.width = Screen.width;
            m_ExCamera.targetTexture.height = Screen.height;

#if !NO_LOADING_SCREEN
            //show loading screen
            ScreenManager.GetScreen<EGRScreenLoading>().ShowScreen();
#else
            MRKLogger.Log("Skipping loading screen");
            ScreenManager.GetScreen(EGRUI_Main.EGRScreen_Main.SCREEN_NAME).ShowScreen();
#endif

            NetworkingClient.Initialize();
        }

        /// <summary>
        /// Called when the app is quiting/closing
        /// </summary>
        void OnDestroy() {
            //unregister all events previously registered
            EGREventManager.Instance.Unregister<EGREventScreenShown>(OnScreenShown);
            EGREventManager.Instance.Unregister<EGREventScreenHidden>(OnScreenHidden);
            EGREventManager.Instance.Unregister<EGREventGraphicsApplied>(OnGraphicsApplied);
            EGREventManager.Instance.Unregister<EGREventSettingsSaved>(OnSettingsSaved);
        }

        /// <summary>
        /// Registers a developer setting
        /// </summary>
        /// <typeparam name="T">Type of developer setting</typeparam>
        public void RegisterDevSettings<T>() where T : EGRDevSettings, new() {
            //manually initialize developer settings manager
            if (m_DevSettingsManager == null) {
                //add EGRDevSettingsManager to our main object
                m_DevSettingsManager = gameObject.AddComponent<EGRDevSettingsManager>();
            }

            //register the setting
            m_DevSettingsManager.RegisterSettings<T>();
        }

        /// <summary>
        /// Sets the currently active map mode
        /// </summary>
        /// <param name="mode">The new map mode</param>
        public void SetMapMode(EGRMapMode mode) {
            //ignore if the new map mode is the same as the old one
            if (m_MapMode == mode)
                return;

            //set the previous map mode
            m_PreviousMapMode = m_MapMode;
            //set the new map mode
            m_MapMode = mode;
            //map mode has changed so camera needs to get updated
            m_CamDirty = true;
            //reset the camera transition progress
            m_CamDelta = 0f;
            //set the camera starting position
            m_CamStartPos = ActiveCamera.transform.position;
            //set the camera starting rotation
            m_CamStartRot = ActiveCamera.transform.rotation.eulerAngles;

            //invoke any subscribers
            m_OnMapModeChanged?.Invoke(mode);

            //let the camera know if we should render the skybox too, we do not need the skybox when
            //mode is Flat, saves performance
            SetGlobalCameraClearFlags(mode == EGRMapMode.Flat ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox);
        }

        /// <summary>
        /// Called at every frame
        /// </summary>
        void Update() {
            //if camera is dirty, a transition is being updated
            if (m_CamDirty) {
                //increment transition progress, transition is only updated if the previous map mode
                //is General, as it is the only condition of us needing a transition
                m_CamDelta += m_PreviousMapMode == EGRMapMode.General ? Time.deltaTime : 1f;

                //get current camera configurtion from selected map mode
                EGRCameraConfig currentConfig = m_CameraConfigs[(int)m_MapMode];

                (Vector3, Vector3) target = (currentConfig.Position, currentConfig.EulerRotation);
                if (m_PreviousMapMode == EGRMapMode.General && !m_InitialModeTransition) {
                    //get direct config from globe cam
                    target = m_GlobeCamera.GetSamplePosRot();

                    //start special transition
                    m_InitialModeTransition = true;

                    //ease rotation and positon to the target ones
                    ActiveCamera.transform.DORotate(target.Item2, 1.5f, RotateMode.FastBeyond360)
                        .SetEase(Ease.OutBack);
                    ActiveCamera.transform.DOMove(target.Item1, 1f)
                        .SetEase(Ease.OutBack);
                }

                //Linear transition of camera config
                if (!m_InitialModeTransition) {
                    ActiveCamera.transform.position = Vector3.Lerp(m_CamStartPos, target.Item1, m_CamDelta);
                    ActiveCamera.transform.rotation = Quaternion.Euler(Vector3.Lerp(m_CamStartRot, target.Item2, Mathf.Clamp01(m_CamDelta * 2f)));
                }

                //has the transition finished?
                if (m_CamDelta >= (m_InitialModeTransition ? 1.5f : 1f)) {
                    m_CamDirty = false;
                    m_InitialModeTransition = false;

                    //update map interface state
                    ActiveEGRCamera.SetInterfaceState(ScreenManager.GetScreen<EGRScreenMapInterface>().Visible);
                }
            }

            //update and handle all controller messages
            foreach (EGRController ctrl in m_Controllers)
                ctrl.UpdateController();

            //update the active state of camera handlers
            if (m_MapsInitialized) {
                m_GlobalMap.SetActive(m_MapMode == EGRMapMode.Globe);
                m_FlatMap.gameObject.SetActive(m_MapMode == EGRMapMode.Flat);
                m_GeneralCamera.gameObject.SetActive(m_MapMode == EGRMapMode.General);
            }

            //update and process network messages
            NetworkingClient.Update();

            //calculate delta time for FPS
            if (m_DrawFPS) {
                m_DeltaTime += (Time.unscaledDeltaTime - m_DeltaTime) * 0.1f;
            }

            //if mode is globe and we are not moving to another planet
            if (m_MapMode == EGRMapMode.Globe && !m_GlobeCamera.IsLocked) {
                //store all planet rotation coefficients if none has been stored
                bool storeRotations = m_PlanetRotationCache.Count == 0;

                foreach (Transform trans in m_Planets) {
                    if (storeRotations) {
                        int sibIdx = trans.GetSiblingIndex();

                        //-1 if planet is Venus or Uranus, they both rotate in the opposite direction
                        m_PlanetRotationCache[trans] = sibIdx == 1 || sibIdx == 5 ? -1f : 1f;
                    }

                    //rotate around the sun with axis being vertically upwards at 2 degrees per time proportional to distance from the sun
                    trans.RotateAround(m_Sun.position, Vector3.up, m_PlanetRotationCache[trans] * 2f * Time.deltaTime
                        * (1f - (Vector3.Distance(trans.position, m_Sun.position) / 80000f)));
                }
            }

            //is map interface active?
            if (ActiveEGRCamera.InterfaceActive) {
                //simulate physics only if we're in the space
                if (m_MapMode == EGRMapMode.Globe) {
                    //0.5 second interval
                    if (Time.time - m_LastPhysicsSimulationTime > 0.5f) {
                        m_LastPhysicsSimulationTime = Time.time;
                        //simulate!
                        Physics.Simulate(0.5f);
                    }

                    //rotate the skybox by 0.5 degrees per second
                    m_SkyboxRotation += Time.deltaTime * 0.5f;
                    //clamp the angle between 0 and 360 degrees
                    if (m_SkyboxRotation > 360f)
                        m_SkyboxRotation -= 360f;

                    RenderSettings.skybox.SetFloat("_Rotation", m_SkyboxRotation);
                }
            }

            //update input model if it needs to
            if (InputModel != null && InputModel.NeedsUpdate) {
                InputModel.UpdateInputModel();
            }
        }

        /// <summary>
        /// Render GUI
        /// </summary>
        void OnGUI() {
            //should we draw the fps?
            if (m_DrawFPS) {
                if (m_FPSStyle == null) {
                    //init fps render style
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

                //render the fps along with the time per frame and the total number of active tweens
                GUI.Label(new Rect(40f, Screen.height - 50f, 100f, 50f), string.Format("<b>{0:0.0}</b> ms (<b>{1:0.}</b> fps) {2}",
                    m_DeltaTime * 1000f, 1f / m_DeltaTime, DOTween.TotalPlayingTweens()), m_FPSStyle);
            }

            //get screen safe area (notch, etc)
            Rect safeArea = Screen.safeArea;
            //offset the y
            safeArea.y = Screen.height - safeArea.height;

            if (safeArea.y < Mathf.Epsilon)
                return;

            //re-generate status bar texture?
            if (m_StatusBarTextureDirty || m_StatusBarTexture == null) {
                m_StatusBarTextureDirty = false;

                byte a = (byte)((m_StatusBarColor & 0xFF000000) >> 24);
                byte r = (byte)((m_StatusBarColor & 0x00FF0000) >> 16);
                byte g = (byte)((m_StatusBarColor & 0x0000FF00) >> 8);
                byte b = (byte)(m_StatusBarColor & 0x000000FF);

                m_StatusBarTexture = EGRUIUtilities.GetPlainTexture(new Color32(r, g, b, a));
            }

            //render status bar
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, safeArea.y), m_StatusBarTexture);
        }

        /// <summary>
        /// Registers a new map mode handler
        /// </summary>
        /// <param name="del">The delegate</param>
        public void RegisterMapModeDelegate(EGRMapModeChangedDelegate del) {
            m_OnMapModeChanged += del;
        }

        /// <summary>
        /// Unregisters an existing map mode handler
        /// </summary>
        /// <param name="del">The delegate</param>
        public void UnregisterMapModeDelegate(EGRMapModeChangedDelegate del) {
            m_OnMapModeChanged -= del;
        }

        /// <summary>
        /// Registers a new controller message handler
        /// </summary>
        /// <param name="receivedDelegate">The delegate</param>
        public void RegisterControllerReceiver(EGRControllerMessageReceivedDelegate receivedDelegate) {
            foreach (EGRController ctrl in m_Controllers)
                ctrl.RegisterReceiver(receivedDelegate);
        }

        /// <summary>
        /// Registers a new controller message handler
        /// </summary>
        /// <param name="receivedDelegate">The delegate</param>
        public void UnregisterControllerReceiver(EGRControllerMessageReceivedDelegate receivedDelegate) {
            foreach (EGRController ctrl in m_Controllers)
                ctrl.UnregisterReceiver(receivedDelegate);
        }

        /// <summary>
        /// Gets the active input controller responsible for the provided message
        /// </summary>
        /// <param name="msg">A controller message</param>
        public EGRController GetControllerFromMessage(EGRControllerMessage msg) {
            //find controller by comparing the message kind
            return m_Controllers.Find(x => x.MessageKind == msg.Kind);
        }

        /// <summary>
        /// Initializes the map
        /// </summary>
        public void InitializeMaps() {
            m_MapsInitialized = true;
            //m_FlatMap.AdjustTileSizeForScreen();
            m_FlatMap.Initialize(new Vector2d(30.04584d, 30.98313d), 4);
        }

        /// <summary>
        /// Late manual initialization
        /// </summary>
        public void Initialize() {
            //render skybox
            ActiveCamera.clearFlags = CameraClearFlags.Skybox;
            //render everything else too
            ActiveCamera.cullingMask = LayerMask.NameToLayer("Everything");
            //disable post-processing
            SetPostProcessState(false);

            //Shader.WarmupAllShaders();
            //m_Sun.parent.gameObject.SetActive(true);

            EGREventManager.Instance.BroadcastEvent(new EGREventAppInitialized());
        }

        /// <summary>
        /// Enables/Disables post-processing, always disabled if quality is set to Low
        /// </summary>
        /// <param name="active">Enable?</param>
        public void SetPostProcessState(bool active) {
            //always off if quality is low
            if (EGRSettings.Quality == EGRSettingsQuality.Low)
                active = false;

            //set active state
            ActiveCamera.GetComponent<PostProcessLayer>().enabled = active;
        }

        /// <summary>
        /// Gets an active post-processing effect
        /// </summary>
        /// <typeparam name="T">The effect</typeparam>
        public T GetActivePostProcessEffect<T>() where T : PostProcessEffectSettings {
            return ActiveEGRCamera.GetComponent<PostProcessVolume>().profile.GetSetting<T>();
        }

        /// <summary>
        /// Deletes corrupted tiles from local storage
        /// </summary>
        /// <param name="maxSz">Maximum size of a tile to be considered as corrupted</param>
        public void FixInvalidTiles(long maxSz = 100L) {
            //loop through all tileset providers
            foreach (MRKTilesetProvider provider in MRKTileRequestor.Instance.TilesetProviders) {
                //get directory of tileset
                string dir = MRKTileRequestor.Instance.FileTileFetcher.GetFolderPath(provider.Name);
                if (Directory.Exists(dir)) {
                    //get all PNGs
                    foreach (string filename in Directory.EnumerateFiles(dir, "*.png")) {
                        //delete if file size less or equal to maxSz
                        if (new FileInfo(filename).Length <= maxSz) {
                            File.Delete(filename);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets called when a screen is shown
        /// </summary>
        /// <param name="evt"></param>
        void OnScreenShown(EGREventScreenShown evt) {
            //add to active screens, make sure to ignore developer settings screen
            if (evt.Screen.ScreenName != "EGRDEV") {
                m_ActiveScreens.Add(evt.Screen);

                if (evt.Screen is IEGRScreenFOVStabilizer stabilizer) {
                    FOVStabilizer = stabilizer;
                }
            }
        }

        /// <summary>
        /// Gets called when a screen is hidden
        /// </summary>
        /// <param name="evt"></param>
        void OnScreenHidden(EGREventScreenHidden evt) {
            //only remove when unlocked as m_ActiveScreens might have an active enumerator
            if (!m_LockScreens) {
                m_ActiveScreens.Remove(evt.Screen);

                if (evt.Screen is IEGRScreenFOVStabilizer stabilizer) {
                    if (stabilizer == FOVStabilizer) {
                        foreach (EGRScreen screen in m_ActiveScreens) {
                            if (screen is IEGRScreenFOVStabilizer secStabilizer) {
                                FOVStabilizer = secStabilizer;
                                return;
                            }
                        }

                        FOVStabilizer = null;
                    }
                }
            }
        }

        /// <summary>
        /// Disables all screens except for the specified ones
        /// </summary>
        /// <typeparam name="T">The excluded type</typeparam>
        /// <param name="excluded">Extra exclusions</param>
        public void DisableAllScreensExcept<T>(params Type[] excluded) {
            //lock screens, we'll be modifying m_ActiveScreens ourselves
            m_LockScreens = true;
            lock (m_ActiveScreens) {
                //iterate from end to start
                for (int i = m_ActiveScreens.Count - 1; i > -1; i--) {
                    //skip if screen is excluded
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

                    //force hide screen
                    m_ActiveScreens[i].ForceHideScreen();
                    //manually remove screen ourselves
                    m_ActiveScreens.RemoveAt(i);
                }
            }

            //unlock screens
            m_LockScreens = false;
        }

        /// <summary>
        /// Gets called when graphic settings are applied
        /// </summary>
        /// <param name="evt"></param>
        void OnGraphicsApplied(EGREventGraphicsApplied evt) {
            //apply planet specific graphic settings
            foreach (Transform trans in m_Planets) {
                //enable planet halos only in Ultra
                trans.Find("Halo").gameObject.SetActive(evt.Quality == EGRSettingsQuality.Ultra);
                //enable planet objects only when quality is greater than Medium
                trans.gameObject.SetActive(evt.Quality > EGRSettingsQuality.Medium);

                //rotate each planet randomly around the sun if active, so planets are spread in space
                if (trans.gameObject.activeInHierarchy) {
                    trans.RotateAround(m_Sun.position, Vector3.up, UnityEngine.Random.value * 360f);
                }
            }

            //enable space dust particle emitter when quality is greater than Medium
            m_EnvironmentEmitter.gameObject.SetActive(evt.Quality > EGRSettingsQuality.Medium);
            //enable sun when quality is greater than Low
            m_Sun.gameObject.SetActive(evt.Quality > EGRSettingsQuality.Low);
            //enable Earth's halo only in Ultra
            m_GlobalMap.transform.Find("Halo").gameObject.SetActive(evt.Quality == EGRSettingsQuality.Ultra);

            //Adjust the bloom post-processing effect's strength depending on quality, strongest when quality is Ultra
            m_GlobeCamera.GetComponent<PostProcessVolume>().profile.GetSetting<Bloom>().threshold.value = evt.Quality == EGRSettingsQuality.Ultra ? 0.9f : 1f;
        }

        /// <summary>
        /// Updates input model from settings
        /// </summary>
        void UpdateInputModel() {
            InputModel = EGRInputModel.Get(EGRSettings.InputModel);
        }

        /// <summary>
        /// Gets called when settings are saved
        /// </summary>
        /// <param name="evt"></param>
        void OnSettingsSaved(EGREventSettingsSaved evt) {
            //update input model as it might have been changed
            UpdateInputModel();
        }

        /// <summary>
        /// Sets all scene cameras' clear flags
        /// </summary>
        /// <param name="flags">The flag</param>
        public void SetGlobalCameraClearFlags(CameraClearFlags flags) {
            ActiveCamera.clearFlags = flags;
            m_ExCamera.clearFlags = flags;
        }

        /// <summary>
        /// Captures the current screen buffer on-demandly using the extra camera and copies it to a new RenderTexture
        /// </summary>
        public RenderTexture CaptureScreenBuffer() {
            //enable the extra camera
            m_ExCamera.gameObject.SetActive(true);
            //position and rotate it to match ActiveCamera
            m_ExCamera.transform.position = ActiveCamera.transform.position;
            m_ExCamera.transform.rotation = ActiveCamera.transform.rotation;
            m_ExCamera.fieldOfView = ActiveCamera.fieldOfView;
            //on-demand render
            m_ExCamera.Render();

            //create a new render texture from the template RenderTexture
            RenderTexture newRt = new RenderTexture(m_ExCamera.targetTexture);
            //let the GPU copy the texture contents from the extra camera to our new one
            Graphics.CopyTexture(m_ExCamera.targetTexture, newRt);

            //disable the extra camera
            m_ExCamera.gameObject.SetActive(false);
            return newRt;
        }

        /// <summary>
        /// Logs out and returns to the Login screen
        /// </summary>
        public void Logout() {
            //lock all screens
            m_LockScreens = true;
            //manually force hide all active screens
            m_ActiveScreens.ForEach(x => x.ForceHideScreen());
            //unlock screens
            m_LockScreens = false;

            //clear the active screens buffer as it is invalid
            m_ActiveScreens.Clear();

            //clear saved account preferences
            MRKPlayerPrefs.Set<bool>(EGRConstants.EGR_LOCALPREFS_REMEMBERME, false);
            MRKPlayerPrefs.Set<string>(EGRConstants.EGR_LOCALPREFS_PASSWORD, "");
            MRKPlayerPrefs.Save();

            //show login screen
            ScreenManager.GetScreen<EGRScreenLogin>().ShowScreen();

            //send logout packet to server if we are connected
            NetworkingClient.MainNetwork.SendStationaryPacket<Packet>(PacketType.LGNOUT, DeliveryMethod.ReliableOrdered, null);

            //clear the local user state
            EGRLocalUser.Initialize(null);
        }

        /// <summary>
        /// Called when the application quits
        /// </summary>
        void OnApplicationQuit() {
            IsRunning = false;

            //save unsaved changes
            MRKPlayerPrefs.Save();
            NetworkingClient.Shutdown();
        }

        /// <summary>
        /// Sets the contextual color of a screen, currently only status bar is supported
        /// </summary>
        /// <param name="screen"></param>
        public static void SetContextualColor(EGRScreen screen) {
            /* #if UNITY_ANDROID
                        if (screen.CanChangeBar) {
                            EGRAndroidUtils.StatusBarColor = EGRAndroidUtils.NavigationBarColor = screen.BarColor;
                        }
#endif */

            if (screen.CanChangeBar) {
                //regenerate status bar texture
                Instance.m_StatusBarTextureDirty = true;
                //sets status bar color
                Instance.m_StatusBarColor = screen.BarColor;

                //attempt to change navbar
#if UNITY_ANDROID
                EGRAndroidUtils.NavigationBarColor = screen.BarColor;
#endif 
            }
        }
    }
}

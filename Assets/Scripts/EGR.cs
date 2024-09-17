//#define NO_LOADING_SCREEN
#define MRK_LOCAL_SERVER

using DG.Tweening;
using MRK.Authentication;
using MRK.Cameras;
using MRK.Cryptography;
using MRK.Events;
using MRK.InputControllers;
using MRK.Localization;
using MRK.Maps;
using MRK.Navigation;
using MRK.Networking;
using MRK.Networking.Packets;
using MRK.Threading;
using MRK.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace MRK
{
    /// <summary>
    /// Available Camera/Map modes in EGR
    /// </summary>
    public enum EGRMapMode
    {
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
    public class EGR : MonoBehaviour
    {
        /// <summary>
        /// Holds information of a camera configuration for a certain EGRMapMode
        /// </summary>
        [Serializable]
        private struct EGRCameraConfig
        {
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
        private RuntimeConfiguration _runtimeConfiguration;

        /// <summary>
        /// Available camera configurations
        /// </summary>
        [SerializeField]
        private EGRCameraConfig[] _cameraConfigs;

        /// <summary>
        /// Currently selected map mode
        /// </summary>
        [SerializeField]
        private EGRMapMode _mapMode;

        /// <summary>
        /// Time transition progress between 2 camera configurations, 0 to 1
        /// </summary>
        private float _camDelta;

        /// <summary>
        /// Indicates if the camera configuration has changed and that the camera state has to be updated ASAP
        /// </summary>
        private bool _camDirty;

        /// <summary>
        /// The Earth globe object
        /// </summary>
        private GameObject _globalMap;

        /// <summary>
        /// The flat/geographical map
        /// </summary>
        private Map _flatMap;

        /// <summary>
        /// Gets called when the map mode is changed
        /// </summary>
        private EGRMapModeChangedDelegate _onMapModeChanged;

        /// <summary>
        /// Camera handler when the selected map mode is Globe
        /// </summary>
        private CameraGlobe _globeCamera;

        /// <summary>
        /// Camera handler when the selected map mode is Flat
        /// </summary>
        private CameraFlat _flatCamera;

        /// <summary>
        /// Camera handler when the selected map mode is General
        /// </summary>
        private CameraGeneral m_GeneralCamera;

        /// <summary>
        /// Indicates if the flat/geographical has initialized
        /// </summary>
        private bool _mapsInitialized;

        /// <summary>
        /// Currently active screens
        /// </summary>
        private readonly List<UI.Screen> _activeScreens;

        /// <summary>
        /// Indicates if the active screens should become locked and may not become modified
        /// </summary>
        private bool _lockScreens;

        /// <summary>
        /// Indicates if the FPS should be drawn
        /// </summary>
        [SerializeField]
        private bool _drawFPS;

        /// <summary>
        /// Time difference since last frame, used for calculating FPS
        /// </summary>
        private float _deltaTime;

        /// <summary>
        /// Render style of the FPS label
        /// </summary>
        private GUIStyle _fpsStyle;

        /// <summary>
        /// Active input controllers
        /// </summary>
        private readonly List<InputController> _controllers;

        /// <summary>
        /// The planets' transform, does not include Earth
        /// </summary>
        [SerializeField]
        private Planet[] _planets;

        /// <summary>
        /// The sun's transform
        /// </summary>
        [SerializeField]
        private Transform _sun;

        /// <summary>
        /// Extra camera that is used on-demand for Post-Processing and transition effects
        /// </summary>
        [SerializeField]
        private Camera _exCamera;

        /// <summary>
        /// Developer settings manager, manually initialized
        /// </summary>
        private DevSettingsManager _devSettingsManager;

        /// <summary>
        /// Starting position of camera as soon as the map mode has changed
        /// </summary>
        private Vector3 _camStartPos;

        /// <summary>
        /// Starting rotation of camera as soon as the map mode has changed
        /// </summary>
        private Vector3 _camStartRot;

        /// <summary>
        /// The previous map mode that was active before the current map mode
        /// </summary>
        private EGRMapMode _previousMapMode;

        /// <summary>
        /// Indicates if the initial transition between General and Globe map modes is active
        /// </summary>
        private bool _initialModeTransition;

        /// <summary>
        /// Active status bar color in ARGB (Android and iOS only)
        /// </summary>
        private uint _statusBarColor;

        /// <summary>
        /// Indicates if the status bar texture should be re-generated, for example: the status bar color being changed
        /// </summary>
        private bool _statusBarTextureDirty;

        /// <summary>
        /// The status bar texture
        /// </summary>
        private Texture2D _statusBarTexture;

        /// <summary>
        /// A particle emitter to simulate some space dust around the globe
        /// </summary>
        private ParticleSystem _environmentEmitter;

        /// <summary>
        /// Time when physics were last simulated
        /// <para>We need to manually simulate physics for the sun flare to stop appearing through planets</para>
        /// </summary>
        private float _lastPhysicsSimulationTime;

        /// <summary>
        /// Current skybox rotation along the Y-axis, 0 to 360
        /// </summary>
        private float _skyboxRotation;

        /// <summary>
        /// EGRMain's singleton instance
        /// </summary>
        public static EGR Instance
        {
            get; private set;
        }

        public RuntimeConfiguration RuntimeConfiguration
        {
            get
            {
                return _runtimeConfiguration;
            }
        }

        /// <summary>
        /// The screen manager
        /// </summary>
        public ScreenManager ScreenManager
        {
            get
            {
                return ScreenManager.Instance;
            }
        }

        /// <summary>
        /// Currently active camera
        /// </summary>
        public Camera ActiveCamera
        {
            get
            {
                return Camera.main;
            }
        }

        /// <summary>
        /// Currently selected map mode
        /// </summary>
        public EGRMapMode MapMode
        {
            get
            {
                return _mapMode;
            }
        }

        /// <summary>
        /// The Earth globe object
        /// </summary>
        public GameObject GlobalMap
        {
            get
            {
                return _globalMap;
            }
        }

        /// <summary>
        /// The flat/geographical map
        /// </summary>
        public Map FlatMap
        {
            get
            {
                return _flatMap;
            }
        }

        /// <summary>
        /// Currently active camera handler
        /// </summary>
        public BaseCamera ActiveEGRCamera
        {
            get
            {
                return _mapMode == EGRMapMode.Flat ? (BaseCamera)_flatCamera : _mapMode == EGRMapMode.Globe ? (BaseCamera)_globeCamera : m_GeneralCamera;
            }
        }

        /// <summary>
        /// Camera handler when the selected map mode is Flat
        /// </summary>
        public CameraFlat FlatCamera
        {
            get
            {
                return _flatCamera;
            }
        }

        /// <summary>
        /// Camera handler when the selected map mode is Globe
        /// </summary>
        public CameraGlobe GlobeCamera
        {
            get
            {
                return _globeCamera;
            }
        }

        public EGRNetworkingClient NetworkingClient
        {
            get; private set;
        }

        /// <summary>
        /// The language manager
        /// </summary>
        public LanguageManager LanguageManager
        {
            get; private set;
        }

        /// <summary>
        /// Indicates if the camera configuration has changed and that the camera state has to be updated ASAP
        /// </summary>
        public bool CamDirty
        {
            get
            {
                return _camDirty;
            }
        }

        /// <summary>
        /// Currently active screens
        /// </summary>
        public List<UI.Screen> ActiveScreens
        {
            get
            {
                return _activeScreens;
            }
        }

        /// <summary>
        /// The place manager
        /// </summary>
        public EGRPlaceManager PlaceManager
        {
            get; private set;
        }

        /// <summary>
        /// Indicates if the initial transition between General and Globe map modes is active
        /// </summary>
        public bool InitialModeTransition
        {
            get
            {
                return _initialModeTransition;
            }
        }

        /// <summary>
        /// The previous map mode that was active before the current map mode
        /// </summary>
        public EGRMapMode PreviousMapMode
        {
            get
            {
                return _previousMapMode;
            }
        }

        /// <summary>
        /// The sun's transform
        /// </summary>
        public Transform Sun
        {
            get
            {
                return _sun;
            }
        }

        /// <summary>
        /// A runnable having the same lifetime as the client (owned by EGRMain)
        /// </summary>
        public Runnable Runnable
        {
            get; private set;
        }

        /// <summary>
        /// Currently active input model
        /// </summary>
        public InputModel InputModel
        {
            get; private set;
        }

        /// <summary>
        /// The navigation manager
        /// </summary>
        public EGRNavigationManager NavigationManager
        {
            get; private set;
        }

        /// <summary>
        /// Indicates if the application is running/alive
        /// </summary>
        public bool IsRunning
        {
            get; private set;
        }

        /// <summary>
        /// Initializes the location service and provides location information
        /// </summary>
        public EGRLocationService LocationService
        {
            get; private set;
        }

        /// <summary>
        /// The location manager
        /// </summary>
        public EGRLocationManager LocationManager
        {
            get; private set;
        }

        public IFOVStabilizer FOVStabilizer
        {
            get; private set;
        }

        public ThreadPool GlobalThreadPool
        {
            get; private set;
        }

        public AuthenticationManager AuthenticationManager
        {
            get; private set;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public EGR()
        {
            _activeScreens = new List<UI.Screen>();
            _controllers = new List<InputController>();

            //application has started running
            IsRunning = true;

            GlobalThreadPool = new ThreadPool(EGRConstants.EGR_DEFAULT_THREAD_POOL_INTERVAL);
            AuthenticationManager = new AuthenticationManager();
        }

        /// <summary>
        /// Initialization
        /// </summary>
        private void Awake()
        {
            //assign Instance to the current instances given by Unity
            Instance = this;

            //no fullscreen, we want to show the status bar on mobile platforms
            UnityEngine.Screen.fullScreen = false;
            //defualt fallback framerate set to 60
            Application.targetFrameRate = 60;

            //we will only simulate physics manually to save performance
            Physics.autoSimulation = false;

            MRKSysUtils.Initialize();

            //add logger
            MRKLogger.AddLogger<UnityLogger>();

            Crypto.CookSalt();
            CryptoPlayerPrefs.Init();

            _globalMap = _runtimeConfiguration.EarthGlobe;
            _globeCamera = _globalMap.GetComponent<CameraGlobe>();
            _globeCamera.SetDistance(_runtimeConfiguration.GlobeSettings.UnfocusedOffset);

            _flatMap = _runtimeConfiguration.FlatMap;
            _flatCamera = _flatMap.GetComponent<CameraFlat>();
            _flatMap.SetMapController(_flatCamera);

            m_GeneralCamera = _runtimeConfiguration.GeneralCamera;

            //manually add EGRPlaceManager to our main object
            PlaceManager = gameObject.AddComponent<EGRPlaceManager>();

            NavigationManager = _runtimeConfiguration.NavigationManager;

            //crash if in scene
            LocationService = new GameObject("Location Service").AddComponent<EGRLocationService>();

            _environmentEmitter = _runtimeConfiguration.EnvironmentEmitter;

            //add a virtual controller if the current device supports touch input
            if (Input.touchSupported)
                _controllers.Add(new VirtualController());

            //add a physical controller if we do not have any controllers (no touch support) or a stylus is supported
            if (_controllers.Count == 0 || Input.stylusTouchSupported)
                _controllers.Add(new PhysicalController());

            //initialize all controllers
            foreach (InputController ctrl in _controllers)
            {
                ctrl.InitController();
            }

            //initialize language manager
            LanguageManager = new LanguageManager();
            LanguageManager.Init();

            //register some events
            EventManager.Instance.Register<ScreenShown>(OnScreenShown);
            EventManager.Instance.Register<ScreenHidden>(OnScreenHidden);
            EventManager.Instance.Register<GraphicsApplied>(OnGraphicsApplied);
            EventManager.Instance.Register<SettingsSaved>(OnSettingsSaved);

            //manually add MRKRunnable to our main object
            Runnable = gameObject.AddComponent<Runnable>();

            NetworkingClient = new EGRNetworkingClient();
        }

        /// <summary>
        /// late initialization
        /// </summary>
        /// <returns></returns>
        private IEnumerator Start()
        {
            MRKLogger.Log($"2000-EGR started v{EGRVersion.VersionString()} - {EGRVersion.VersionSignature()}");

            //keep waiting until all screens have initialized
            yield return ScreenManager.WaitForInitialization();

            MRKLogger.Log("EGRScreenManager initialized");

#if UNITY_ANDROID
            //set native status bar and navigation bar to transparent in android
            EGRAndroidUtils.StatusBarColor = EGRAndroidUtils.NavigationBarColor = 0x00000000;
#endif

            //load settings
            Settings.Load();
            //apply graphical settings
            Settings.Apply();

            //init location manager
            LocationManager = gameObject.AddComponent<EGRLocationManager>();

            //update input model
            UpdateInputModel();

            //initial mode should be globe
            SetMapMode(EGRMapMode.Globe);

            //set the extra camera's dimensions to fit the screen
            _exCamera.targetTexture.width = UnityEngine.Screen.width;
            _exCamera.targetTexture.height = UnityEngine.Screen.height;

#if !NO_LOADING_SCREEN
            //show loading screen
            ScreenManager.GetScreen<Loading>().ShowScreen();
#else
            MRKLogger.Log("Skipping loading screen");
            ScreenManager.GetScreen(EGRUI_Main.EGRScreen_Main.SCREEN_NAME).ShowScreen();
#endif

            NetworkingClient.Initialize();
        }

        /// <summary>
        /// Called when the app is quiting/closing
        /// </summary>
        private void OnDestroy()
        {
            //unregister all events previously registered
            EventManager.Instance.Unregister<ScreenShown>(OnScreenShown);
            EventManager.Instance.Unregister<ScreenHidden>(OnScreenHidden);
            EventManager.Instance.Unregister<GraphicsApplied>(OnGraphicsApplied);
            EventManager.Instance.Unregister<SettingsSaved>(OnSettingsSaved);
        }

        /// <summary>
        /// Registers a developer setting
        /// </summary>
        /// <typeparam name="T">Type of developer setting</typeparam>
        public void RegisterDevSettings<T>() where T : DevSettings, new()
        {
            //manually initialize developer settings manager
            if (_devSettingsManager == null)
            {
                //add EGRDevSettingsManager to our main object
                _devSettingsManager = gameObject.AddComponent<DevSettingsManager>();
            }

            //register the setting
            _devSettingsManager.RegisterSettings<T>();
        }

        /// <summary>
        /// Sets the currently active map mode
        /// </summary>
        /// <param name="mode">The new map mode</param>
        public void SetMapMode(EGRMapMode mode)
        {
            //ignore if the new map mode is the same as the old one
            if (_mapMode == mode)
            {
                return;
            }

            //set the previous map mode
            _previousMapMode = _mapMode;
            //set the new map mode
            _mapMode = mode;
            //map mode has changed so camera needs to get updated
            _camDirty = true;
            //reset the camera transition progress
            _camDelta = 0f;
            //set the camera starting position
            _camStartPos = ActiveCamera.transform.position;
            //set the camera starting rotation
            _camStartRot = ActiveCamera.transform.rotation.eulerAngles;

            //invoke any subscribers
            _onMapModeChanged?.Invoke(mode);

            //let the camera know if we should render the skybox too, we do not need the skybox when
            //mode is Flat, saves performance
            SetGlobalCameraClearFlags(mode == EGRMapMode.Flat ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox);
        }

        /// <summary>
        /// Called at every frame
        /// </summary>
        private void Update()
        {
            //if camera is dirty, a transition is being updated
            if (_camDirty)
            {
                //increment transition progress, transition is only updated if the previous map mode
                //is General, as it is the only condition of us needing a transition
                _camDelta += _previousMapMode == EGRMapMode.General ? Time.deltaTime : 1f;

                //get current camera configurtion from selected map mode
                EGRCameraConfig currentConfig = _cameraConfigs[(int)_mapMode];

                (Vector3, Vector3) target = (currentConfig.Position, currentConfig.EulerRotation);
                if (_previousMapMode == EGRMapMode.General && !_initialModeTransition)
                {
                    //get direct config from globe cam
                    target = _globeCamera.GetSamplePosRot();

                    //start special transition
                    _initialModeTransition = true;

                    //ease rotation and positon to the target ones
                    ActiveCamera.transform.DORotate(target.Item2, 1.5f, RotateMode.FastBeyond360)
                        .SetEase(Ease.OutBack);
                    ActiveCamera.transform.DOMove(target.Item1, 1f)
                        .SetEase(Ease.OutBack);
                }

                //Linear transition of camera config
                if (!_initialModeTransition)
                {
                    ActiveCamera.transform.position = Vector3.Lerp(_camStartPos, target.Item1, _camDelta);
                    ActiveCamera.transform.rotation = Quaternion.Euler(Vector3.Lerp(_camStartRot, target.Item2, Mathf.Clamp01(_camDelta * 2f)));
                }

                //has the transition finished?
                if (_camDelta >= (_initialModeTransition ? 1.5f : 1f))
                {
                    _camDirty = false;
                    _initialModeTransition = false;

                    //update map interface state
                    ActiveEGRCamera.SetInterfaceState(ScreenManager.GetScreen<HUD>().Visible);
                }
            }

            //update and handle all controller messages
            foreach (InputController ctrl in _controllers)
            {
                ctrl.UpdateController();
            }

            //update the active state of camera handlers
            if (_mapsInitialized)
            {
                _globalMap.SetActive(_mapMode == EGRMapMode.Globe);
                _flatMap.gameObject.SetActive(_mapMode == EGRMapMode.Flat);
                m_GeneralCamera.gameObject.SetActive(_mapMode == EGRMapMode.General);
            }

            //update and process network messages
            NetworkingClient.Update();

            //calculate delta time for FPS
            if (_drawFPS)
            {
                _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
            }

            //is map interface active?
            if (ActiveEGRCamera.InterfaceActive)
            {
                //simulate physics only if we're in the space
                if (_mapMode == EGRMapMode.Globe)
                {
                    //0.5 second interval
                    if (Time.time - _lastPhysicsSimulationTime > 0.5f)
                    {
                        _lastPhysicsSimulationTime = Time.time;
                        //simulate!
                        Physics.Simulate(0.5f);
                    }

                    //rotate the skybox by 0.5 degrees per second
                    _skyboxRotation += Time.deltaTime * 0.5f;
                    //clamp the angle between 0 and 360 degrees
                    if (_skyboxRotation > 360f)
                        _skyboxRotation -= 360f;

                    RenderSettings.skybox.SetFloat("_Rotation", _skyboxRotation);
                }
            }

            //update input model if it needs to
            if (InputModel != null && InputModel.NeedsUpdate)
            {
                InputModel.UpdateInputModel();
            }
        }

        /// <summary>
        /// Render GUI
        /// </summary>
        private void OnGUI()
        {
            //should we draw the fps?
            if (_drawFPS)
            {
                if (_fpsStyle == null)
                {
                    //init fps render style
                    _fpsStyle = new GUIStyle
                    {
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
                GUI.Label(new Rect(40f, UnityEngine.Screen.height - 50f, 100f, 50f), string.Format("<b>{0:0.0}</b> ms (<b>{1:0.}</b> fps) {2}",
                    _deltaTime * 1000f, 1f / _deltaTime, DOTween.TotalPlayingTweens()), _fpsStyle);
            }

            //get screen safe area (notch, etc)
            Rect safeArea = UnityEngine.Screen.safeArea;
            //offset the y
            safeArea.y = UnityEngine.Screen.height - safeArea.height;

            if (safeArea.y < Mathf.Epsilon)
            {
                return;
            }

            //re-generate status bar texture?
            if (_statusBarTextureDirty || _statusBarTexture == null)
            {
                _statusBarTextureDirty = false;

                byte a = (byte)((_statusBarColor & 0xFF000000) >> 24);
                byte r = (byte)((_statusBarColor & 0x00FF0000) >> 16);
                byte g = (byte)((_statusBarColor & 0x0000FF00) >> 8);
                byte b = (byte)(_statusBarColor & 0x000000FF);

                _statusBarTexture = Utilities.GetPlainTexture(new Color32(r, g, b, a));
            }

            //render status bar
            GUI.DrawTexture(new Rect(0f, 0f, UnityEngine.Screen.width, safeArea.y), _statusBarTexture);
        }

        /// <summary>
        /// Registers a new map mode handler
        /// </summary>
        /// <param name="del">The delegate</param>
        public void RegisterMapModeDelegate(EGRMapModeChangedDelegate del)
        {
            _onMapModeChanged += del;
        }

        /// <summary>
        /// Unregisters an existing map mode handler
        /// </summary>
        /// <param name="del">The delegate</param>
        public void UnregisterMapModeDelegate(EGRMapModeChangedDelegate del)
        {
            _onMapModeChanged -= del;
        }

        /// <summary>
        /// Registers a new controller message handler
        /// </summary>
        /// <param name="receivedDelegate">The delegate</param>
        public void RegisterControllerReceiver(MessageReceivedDelegate receivedDelegate)
        {
            foreach (InputController ctrl in _controllers)
            {
                ctrl.RegisterReceiver(receivedDelegate);
            }
        }

        /// <summary>
        /// Registers a new controller message handler
        /// </summary>
        /// <param name="receivedDelegate">The delegate</param>
        public void UnregisterControllerReceiver(MessageReceivedDelegate receivedDelegate)
        {
            foreach (InputController ctrl in _controllers)
            {
                ctrl.UnregisterReceiver(receivedDelegate);
            }
        }

        /// <summary>
        /// Gets the active input controller responsible for the provided message
        /// </summary>
        /// <param name="msg">A controller message</param>
        public InputController GetControllerFromMessage(Message msg)
        {
            //find controller by comparing the message kind
            return _controllers.Find(x => x.MessageKind == msg.Kind);
        }

        /// <summary>
        /// Initializes the map
        /// </summary>
        public void InitializeMaps()
        {
            _mapsInitialized = true;
            //m_FlatMap.AdjustTileSizeForScreen();
            _flatMap.Initialize(new Vector2d(30.04584d, 30.98313d), 4);
        }

        /// <summary>
        /// Late manual initialization
        /// </summary>
        public void Initialize()
        {
            //render skybox
            ActiveCamera.clearFlags = CameraClearFlags.Skybox;
            //render everything else too
            ActiveCamera.cullingMask = LayerMask.NameToLayer("Everything");
            //disable post-processing
            SetPostProcessState(false);

            //Shader.WarmupAllShaders();
            //m_Sun.parent.gameObject.SetActive(true);

            EventManager.Instance.BroadcastEvent(new AppInitialized());
        }

        /// <summary>
        /// Enables/Disables post-processing, always disabled if quality is set to Low
        /// </summary>
        /// <param name="active">Enable?</param>
        public void SetPostProcessState(bool active)
        {
            //always off if quality is low
            if (Settings.Quality == SettingsQuality.Low)
                active = false;

            //set active state
            ActiveCamera.GetComponent<PostProcessLayer>().enabled = active;
        }

        /// <summary>
        /// Gets an active post-processing effect
        /// </summary>
        /// <typeparam name="T">The effect</typeparam>
        public T GetActivePostProcessEffect<T>() where T : PostProcessEffectSettings
        {
            return ActiveEGRCamera.GetComponent<PostProcessVolume>().profile.GetSetting<T>();
        }

        /// <summary>
        /// Deletes corrupted tiles from local storage
        /// </summary>
        /// <param name="maxSz">Maximum size of a tile to be considered as corrupted</param>
        public void FixInvalidTiles(long maxSz = 100L)
        {
            //loop through all tileset providers
            foreach (TilesetProvider provider in TileRequestor.Instance.TilesetProviders)
            {
                //get directory of tileset
                string dir = TileRequestor.Instance.FileTileFetcher.GetFolderPath(provider.Name);
                if (Directory.Exists(dir))
                {
                    //get all PNGs
                    foreach (string filename in Directory.EnumerateFiles(dir, "*.png"))
                    {
                        //delete if file size less or equal to maxSz
                        if (new FileInfo(filename).Length <= maxSz)
                        {
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
        private void OnScreenShown(ScreenShown evt)
        {
            //add to active screens, make sure to ignore developer settings screen
            if (evt.Screen.ScreenName != "EGRDEV")
            {
                _activeScreens.Add(evt.Screen);

                if (evt.Screen is IFOVStabilizer stabilizer)
                {
                    FOVStabilizer = stabilizer;
                }
            }
        }

        /// <summary>
        /// Gets called when a screen is hidden
        /// </summary>
        /// <param name="evt"></param>
        private void OnScreenHidden(ScreenHidden evt)
        {
            //only remove when unlocked as m_ActiveScreens might have an active enumerator
            if (!_lockScreens)
            {
                _activeScreens.Remove(evt.Screen);

                if (evt.Screen is IFOVStabilizer stabilizer)
                {
                    if (stabilizer == FOVStabilizer)
                    {
                        foreach (UI.Screen screen in _activeScreens)
                        {
                            if (screen is IFOVStabilizer secStabilizer)
                            {
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
        public void DisableAllScreensExcept<T>(params Type[] excluded)
        {
            //lock screens, we'll be modifying m_ActiveScreens ourselves
            _lockScreens = true;
            lock (_activeScreens)
            {
                //iterate from end to start
                for (int i = _activeScreens.Count - 1; i > -1; i--)
                {
                    //skip if screen is excluded
                    if (_activeScreens[i] is T)
                        continue;

                    bool ex = false;
                    foreach (Type t in excluded)
                    {
                        if (_activeScreens[i].GetType() == t)
                        {
                            ex = true;
                            break;
                        }
                    }

                    if (ex)
                    {
                        continue;
                    }

                    //force hide screen
                    _activeScreens[i].ForceHideScreen();
                    //manually remove screen ourselves
                    _activeScreens.RemoveAt(i);
                }
            }

            //unlock screens
            _lockScreens = false;
        }

        /// <summary>
        /// Gets called when graphic settings are applied
        /// </summary>
        /// <param name="evt"></param>
        private void OnGraphicsApplied(GraphicsApplied evt)
        {
            //apply planet specific graphic settings
            foreach (Planet planet in _planets)
            {
                bool planetActive = planet.PlanetType == PlanetType.Earth || evt.Quality > SettingsQuality.Medium;
                planet.gameObject.SetActive(planetActive);

                if (planetActive)
                {
                    planet.SetHaloActiveState(evt.Quality == SettingsQuality.Ultra);
                }
            }

            //enable space dust particle emitter when quality is greater than Medium
            //m_EnvironmentEmitter.gameObject.SetActive(evt.Quality > EGRSettingsQuality.Medium);
            //enable sun when quality is greater than Low
            //m_Sun.gameObject.SetActive(evt.Quality > EGRSettingsQuality.Low);
            //enable Earth's halo only in Ultra
            //m_GlobalMap.transform.Find("Halo").gameObject.SetActive(evt.Quality == EGRSettingsQuality.Ultra);

            //Adjust the bloom post-processing effect's strength depending on quality, strongest when quality is Ultra
            _globeCamera.GetComponent<PostProcessVolume>().profile.GetSetting<Bloom>().threshold.value = evt.Quality == SettingsQuality.Ultra ? 0.9f : 1f;
        }

        /// <summary>
        /// Updates input model from settings
        /// </summary>
        private void UpdateInputModel()
        {
            InputModel = InputModel.Get(Settings.InputModel);
        }

        /// <summary>
        /// Gets called when settings are saved
        /// </summary>
        /// <param name="evt"></param>
        private void OnSettingsSaved(SettingsSaved evt)
        {
            //update input model as it might have been changed
            UpdateInputModel();
        }

        /// <summary>
        /// Sets all scene cameras' clear flags
        /// </summary>
        /// <param name="flags">The flag</param>
        public void SetGlobalCameraClearFlags(CameraClearFlags flags)
        {
            ActiveCamera.clearFlags = flags;
            _exCamera.clearFlags = flags;
        }

        /// <summary>
        /// Captures the current screen buffer on-demandly using the extra camera and copies it to a new RenderTexture
        /// </summary>
        public RenderTexture CaptureScreenBuffer()
        {
            //enable the extra camera
            _exCamera.gameObject.SetActive(true);
            //position and rotate it to match ActiveCamera
            _exCamera.transform.position = ActiveCamera.transform.position;
            _exCamera.transform.rotation = ActiveCamera.transform.rotation;
            _exCamera.fieldOfView = ActiveCamera.fieldOfView;
            //on-demand render
            _exCamera.Render();

            //create a new render texture from the template RenderTexture
            RenderTexture newRt = new RenderTexture(_exCamera.targetTexture);
            //let the GPU copy the texture contents from the extra camera to our new one
            Graphics.CopyTexture(_exCamera.targetTexture, newRt);

            //disable the extra camera
            _exCamera.gameObject.SetActive(false);
            return newRt;
        }

        /// <summary>
        /// Logs out and returns to the Login screen
        /// </summary>
        public void Logout()
        {
            //lock all screens
            _lockScreens = true;
            //manually force hide all active screens
            _activeScreens.ForEach(x => x.ForceHideScreen());
            //unlock screens
            _lockScreens = false;

            //clear the active screens buffer as it is invalid
            _activeScreens.Clear();

            //clear saved account preferences
            CryptoPlayerPrefs.Set(EGRConstants.EGR_LOCALPREFS_REMEMBERME, false);
            CryptoPlayerPrefs.Set(EGRConstants.EGR_LOCALPREFS_PASSWORD, "");
            CryptoPlayerPrefs.Save();

            //show login screen
            ScreenManager.GetScreen<Login>().ShowScreen();

            //send logout packet to server if we are connected
            NetworkingClient.MainNetwork.SendStationaryPacket<Packet>(PacketType.LGNOUT, DeliveryMethod.ReliableOrdered, null);

            //clear the local user state
            LocalUser.Initialize(null);
        }

        /// <summary>
        /// Called when the application quits
        /// </summary>
        private void OnApplicationQuit()
        {
            IsRunning = false;

            //save unsaved changes
            CryptoPlayerPrefs.Save();
            NetworkingClient.Shutdown();
        }

        /// <summary>
        /// Sets the contextual color of a screen, currently only status bar is supported
        /// </summary>
        /// <param name="screen"></param>
        public static void SetContextualColor(UI.Screen screen)
        {
            /* #if UNITY_ANDROID
                        if (screen.CanChangeBar) {
                            EGRAndroidUtils.StatusBarColor = EGRAndroidUtils.NavigationBarColor = screen.BarColor;
                        }
#endif */

            if (screen.CanChangeBar)
            {
                //regenerate status bar texture
                Instance._statusBarTextureDirty = true;
                //sets status bar color
                Instance._statusBarColor = screen.BarColor;

                //attempt to change navbar
#if UNITY_ANDROID
                EGRAndroidUtils.NavigationBarColor = screen.BarColor;
#endif 
            }
        }
    }
}

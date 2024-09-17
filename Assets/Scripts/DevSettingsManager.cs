using MRK.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace MRK
{
    public enum DevSettingsType
    {
        None,
        ServerInfo,
        UsersInfo
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DevSettingsInfo : Attribute
    {
        public DevSettingsType SettingsType { get; private set; }

        public DevSettingsInfo(DevSettingsType type)
        {
            SettingsType = type;
        }
    }

    [DevSettingsInfo(DevSettingsType.None)]
    public abstract class DevSettings : BaseBehaviourPlain
    {
        private GameObject _object;

        public bool Enabled
        {
            get
            {
                return _object.activeInHierarchy;
            }

            set
            {
                _object.SetActive(value);
            }
        }

        public abstract string Name
        {
            get;
        }

        public abstract string ChildName
        {
            get;
        }

        public virtual void Initialize(Transform trans)
        {
            _object = trans.gameObject;
        }
    }

    [DevSettingsInfo(DevSettingsType.ServerInfo)]
    public class DevSettingsServerInfo : DevSettings
    {
        private TMP_InputField _ip;
        private TMP_InputField _port;
        private TextMeshProUGUI _ipLabel;
        private TextMeshProUGUI _portLabel;
        private TextMeshProUGUI _stateLabel;

        public override string Name
        {
            get
            {
                return "Server Info";
            }
        }

        public override string ChildName
        {
            get
            {
                return "ServerInfo";
            }
        }

        public override void Initialize(Transform trans)
        {
            base.Initialize(trans);

            _ip = trans.Find("IPTb").GetComponent<TMP_InputField>();
            _port = trans.Find("PortTb").GetComponent<TMP_InputField>();
            _ipLabel = trans.Find("BgLow/IP").GetComponent<TextMeshProUGUI>();
            _portLabel = trans.Find("BgLow/Port").GetComponent<TextMeshProUGUI>();
            _stateLabel = trans.Find("BgLow/State").GetComponent<TextMeshProUGUI>();

            trans.Find("ConnectButton").GetComponent<Button>().onClick.AddListener(OnConnectClick);

            EGREventManager.Instance.Register<NetworkConnected>(OnNetworkConnected);
            EGREventManager.Instance.Register<NetworkDisconnected>(OnNetworkDisconnected);

            UpdateConnectionLabels();
        }

        private void OnConnectClick()
        {
            EGR.Instance.NetworkingClient.MainNetwork.AlterConnection(_ip.text, _port.text);
            UpdateConnectionLabels();
        }

        private void UpdateConnectionLabels()
        {
            var ep = EGR.Instance.NetworkingClient.MainNetwork.Endpoint;
            _ipLabel.text = ep.Address.ToString();
            _portLabel.text = ep.Port.ToString();

            UpdateStateLabel();
        }

        private void UpdateStateLabel()
        {
            bool connected = EGR.Instance.NetworkingClient.MainNetwork.IsConnected;
            _stateLabel.text = connected ? "Connected" : "Disconnected";
            _stateLabel.color = connected ? Color.green : Color.red;
        }

        private void OnNetworkConnected(NetworkConnected evt)
        {
            UpdateStateLabel();
        }

        private void OnNetworkDisconnected(NetworkDisconnected evt)
        {
            UpdateStateLabel();
        }
    }

    [DevSettingsInfo(DevSettingsType.UsersInfo)]
    public class DevSettingsUsersInfo : DevSettings
    {
        public override string Name
        {
            get
            {
                return "AUTH";
            }
        }

        public override string ChildName
        {
            get
            {
                return "UsersInfo";
            }
        }

        public override void Initialize(Transform trans)
        {
            base.Initialize(trans);

            trans.GetElement<Button>("Bg/Button").onClick.AddListener(OnButtonClick);
        }

        private void OnButtonClick()
        {
            if (!ScreenManager.GetScreen<Login>().Visible)
            {
                Debug.Log("Login isnt active");
                return;
            }

            Client.AuthenticationManager.BuiltInLogin();
        }
    }

    public class DevSettingsManager : BaseBehaviour
    {
        private readonly List<DevSettings> _registeredSettings;
        private bool _guiActive;
        private Transform _screen;
        private GameObject _main;
        private SegmentedControl _toolbar;
        private int _lastSelectedSettings;
        private DevSettings _activeSettings;
        private Button _toggler;
        private bool _hiddenToggler;

        public DevSettingsManager()
        {
            _registeredSettings = new List<DevSettings>();
            _lastSelectedSettings = -1;
        }

        private void Awake()
        {
            UI.Screen screen = ScreenManager.Instance.GetScreen("EGRDEV");
            screen.ShowScreen();
            _screen = screen.transform;

            _main = _screen.Find("Main").gameObject;
            _toolbar = _main.transform.Find("Toolbar").GetComponent<SegmentedControl>();
            _toolbar.onValueChanged.AddListener(OnToolbarValueChanged);

            _toggler = _screen.Find("Toggler").GetComponent<Button>();
            _toggler.onClick.AddListener(ToggleGUI);

            _main.transform.GetElement<Button>("Hider").onClick.AddListener(() => {
                _toggler.gameObject.SetActive(false);
                _hiddenToggler = true;
                ToggleGUI();
            });

            UpdateGUIVisibility();
            UpdateToolbar();
        }

        public void RegisterSettings<T>() where T : DevSettings, new()
        {
            if (_hiddenToggler)
            {
                _toggler.gameObject.SetActive(true);
                _hiddenToggler = false;
            }

            DevSettingsType type = typeof(T).GetCustomAttribute<DevSettingsInfo>().SettingsType;
            if (_registeredSettings.Find(x => x.GetType().GetCustomAttribute<DevSettingsInfo>().SettingsType == type) != null)
            {
                MRKLogger.Log($"Cant register dev setting of type {type}");
                return;
            }

            T setting = new T();
            setting.Initialize(_main.transform.Find(setting.ChildName));
            _registeredSettings.Add(setting);

            UpdateToolbar();

            MRKLogger.Log($"Registered dev settings - {typeof(T).FullName}");
        }

        private void ToggleGUI()
        {
            _guiActive = !_guiActive;
            UpdateGUIVisibility();
        }

        private void UpdateGUIVisibility()
        {
            _main.SetActive(_guiActive);
            UpdateVisibleSetting();
        }

        private void UpdateVisibleSetting()
        {
            if (_activeSettings != null)
            {
                _activeSettings.Enabled = false;
            }

            if (_lastSelectedSettings > -1)
            {
                _activeSettings = _registeredSettings[_lastSelectedSettings];
                _activeSettings.Enabled = true;
            }
        }

        private void UpdateToolbar()
        {
            for (int i = 0; i < _toolbar.segments.Length; i++)
            {
                _toolbar.segments[i].GetComponentInChildren<TextMeshProUGUI>().text = _registeredSettings.Count <= i ? "---" : _registeredSettings[i].Name;
            }
        }

        private void OnToolbarValueChanged(int idx)
        {
            if (_registeredSettings.Count <= idx)
            {
                _toolbar.selectedSegmentIndex = _lastSelectedSettings;
                UpdateVisibleSetting();
                return;
            }

            _lastSelectedSettings = idx;
            UpdateVisibleSetting();
        }
    }
}

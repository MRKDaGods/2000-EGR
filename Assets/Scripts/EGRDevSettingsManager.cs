using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MRK.UI;
using static UnityEngine.GUILayout;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using TMPro;

namespace MRK {
    public enum EGRDevSettingsType {
        None,
        ServerInfo,
        UsersInfo
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class EGRDevSettingsInfo : Attribute {
        public EGRDevSettingsType SettingsType { get; private set; }

        public EGRDevSettingsInfo(EGRDevSettingsType type) {
            SettingsType = type;
        }
    }

    [EGRDevSettingsInfo(EGRDevSettingsType.None)]
    public abstract class EGRDevSettings : MRKBehaviourPlain {
        GameObject m_Object;

        public bool Enabled {
            get {
                return m_Object.activeInHierarchy;
            }
            set {
                m_Object.SetActive(value);
            }
        }
        public abstract string Name { get; }
        public abstract string ChildName { get; }

        public virtual void Initialize(Transform trans) {
            m_Object = trans.gameObject;
        }
    }

    [EGRDevSettingsInfo(EGRDevSettingsType.ServerInfo)]
    public class EGRDevSettingsServerInfo : EGRDevSettings {
        TMP_InputField m_Ip;
        TMP_InputField m_Port;
        TextMeshProUGUI m_IpLabel;
        TextMeshProUGUI m_PortLabel;
        TextMeshProUGUI m_StateLabel;

        public override string Name => "Server Info";
        public override string ChildName => "ServerInfo";

        public override void Initialize(Transform trans) {
            base.Initialize(trans);

            m_Ip = trans.Find("IPTb").GetComponent<TMP_InputField>();
            m_Port = trans.Find("PortTb").GetComponent<TMP_InputField>();
            m_IpLabel = trans.Find("BgLow/IP").GetComponent<TextMeshProUGUI>();
            m_PortLabel = trans.Find("BgLow/Port").GetComponent<TextMeshProUGUI>();
            m_StateLabel = trans.Find("BgLow/State").GetComponent<TextMeshProUGUI>();

            trans.Find("ConnectButton").GetComponent<Button>().onClick.AddListener(OnConnectClick);

            EGREventManager.Instance.Register<EGREventNetworkConnected>(OnNetworkConnected);
            EGREventManager.Instance.Register<EGREventNetworkDisconnected>(OnNetworkDisconnected);

            UpdateConnectionLabels();
        }

        void OnConnectClick() {
            EGRMain.Instance.NetworkingClient.MainNetwork.AlterConnection(m_Ip.text, m_Port.text);
            UpdateConnectionLabels();
        }

        void UpdateConnectionLabels() {
            var ep = EGRMain.Instance.NetworkingClient.MainNetwork.Endpoint;
            m_IpLabel.text = ep.Address.ToString();
            m_PortLabel.text = ep.Port.ToString();

            UpdateStateLabel();
        }

        void UpdateStateLabel() {
            bool connected = EGRMain.Instance.NetworkingClient.MainNetwork.IsConnected;
            m_StateLabel.text = connected ? "Connected" : "Disconnected";
            m_StateLabel.color = connected ? Color.green : Color.red;
        }

        void OnNetworkConnected(EGREventNetworkConnected evt) {
            UpdateStateLabel();
        }

        void OnNetworkDisconnected(EGREventNetworkDisconnected evt) {
            UpdateStateLabel();
        }
    }

    [EGRDevSettingsInfo(EGRDevSettingsType.UsersInfo)]
    public class EGRDevSettingsUsersInfo : EGRDevSettings {
        public override string Name => "AUTH";
        public override string ChildName => "UsersInfo";

        public override void Initialize(Transform trans) {
            base.Initialize(trans);

            trans.GetElement<Button>("Bg/Button").onClick.AddListener(OnButtonClick);
        }

        void OnButtonClick() {
            if (!ScreenManager.GetScreen<EGRScreenLogin>().Visible) {
                Debug.Log("Login isnt active");
                return;
            }

            Client.AuthenticationManager.BuiltInLogin();
        }
    }

    public class EGRDevSettingsManager : MRKBehaviour {
        readonly List<EGRDevSettings> m_RegisteredSettings;
        bool m_GUIActive;
        Transform m_Screen;
        GameObject m_Main;
        SegmentedControl m_Toolbar;
        int m_LastSelectedSettings;
        EGRDevSettings m_ActiveSettings;
        Button m_Toggler;
        bool m_HiddenToggler;

        public EGRDevSettingsManager() {
            m_RegisteredSettings = new List<EGRDevSettings>();
            m_LastSelectedSettings = -1;
        }

        void Awake() {
            EGRScreen screen = EGRScreenManager.Instance.GetScreen("EGRDEV");
            screen.ShowScreen();
            m_Screen = screen.transform;

            m_Main = m_Screen.Find("Main").gameObject;
            m_Toolbar = m_Main.transform.Find("Toolbar").GetComponent<SegmentedControl>();
            m_Toolbar.onValueChanged.AddListener(OnToolbarValueChanged);

            m_Toggler = m_Screen.Find("Toggler").GetComponent<Button>();
            m_Toggler.onClick.AddListener(ToggleGUI);
            m_Main.transform.GetElement<Button>("Hider").onClick.AddListener(() => {
                m_Toggler.gameObject.SetActive(false);
                m_HiddenToggler = true;
                ToggleGUI();
            });

            UpdateGUIVisibility();
            UpdateToolbar();
        }

        public void RegisterSettings<T>() where T : EGRDevSettings, new() {
            if (m_HiddenToggler) {
                m_Toggler.gameObject.SetActive(true);
                m_HiddenToggler = false;
            }

            EGRDevSettingsType type = typeof(T).GetCustomAttribute<EGRDevSettingsInfo>().SettingsType;
            if (m_RegisteredSettings.Find(x => x.GetType().GetCustomAttribute<EGRDevSettingsInfo>().SettingsType == type) != null) {
                EGRMain.Log($"Cant register dev setting of type {type}");
                return;
            }

            T setting = new T();
            setting.Initialize(m_Main.transform.Find(setting.ChildName));
            m_RegisteredSettings.Add(setting);

            UpdateToolbar();

            EGRMain.Log($"Registered dev settings - {typeof(T).FullName}");
        }

        void ToggleGUI() {
            m_GUIActive = !m_GUIActive;
            UpdateGUIVisibility();
        }

        void UpdateGUIVisibility() {
            m_Main.SetActive(m_GUIActive);
            UpdateVisibleSetting();
        }

        void UpdateVisibleSetting() {
            if (m_ActiveSettings != null)
                m_ActiveSettings.Enabled = false;

            if (m_LastSelectedSettings > -1) {
                m_ActiveSettings = m_RegisteredSettings[m_LastSelectedSettings];
                m_ActiveSettings.Enabled = true;
            }
        }

        void UpdateToolbar() {
            for (int i = 0; i < m_Toolbar.segments.Length; i++) {
                m_Toolbar.segments[i].GetComponentInChildren<TextMeshProUGUI>().text = m_RegisteredSettings.Count <= i ? "---" : m_RegisteredSettings[i].Name;
            }
        }

        void OnToolbarValueChanged(int idx) {
            if (m_RegisteredSettings.Count <= idx) {
                m_Toolbar.selectedSegmentIndex = m_LastSelectedSettings;
                UpdateVisibleSetting();
                return;
            }

            m_LastSelectedSettings = idx;
            UpdateVisibleSetting();
        }
    }
}

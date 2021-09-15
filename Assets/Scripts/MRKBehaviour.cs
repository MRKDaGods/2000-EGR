using MRK.Networking;
using MRK.UI;
using UnityEngine;

namespace MRK {
    /// <summary>
    /// Base class of any behaviour that provides ease of access to EGR's main internals
    /// </summary>
    public class MRKBehaviour : MonoBehaviour {
        Transform m_Transform;
        RectTransform m_RectTransform;
        GameObject m_GameObject;

        public EGRMain Client => EGRMain.Instance;
        public EGRNetworkingClient NetworkingClient => Client.NetworkingClient;
        public EGRScreenManager ScreenManager => EGRScreenManager.Instance;
        public EGREventManager EventManager => EGREventManager.Instance;

        //cached properties
        public new Transform transform => m_Transform ??= base.transform;
        public RectTransform rectTransform => m_RectTransform ??= (RectTransform)transform;
        public new GameObject gameObject => m_GameObject ??= base.gameObject;
    }
}

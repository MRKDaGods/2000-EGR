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
        public new Transform transform { 
            get {
                if (m_Transform == null) {
                    m_Transform = base.transform;
                }

                return m_Transform;
            }
        }
        public RectTransform rectTransform {
            get {
                if (m_RectTransform == null) {
                    m_RectTransform = (RectTransform)transform;
                }

                return m_RectTransform;
            }
        }
        public new GameObject gameObject {
            get {
                if (m_GameObject == null) {
                    m_GameObject = base.gameObject;
                }

                return m_GameObject;
            }
        }
    }
}

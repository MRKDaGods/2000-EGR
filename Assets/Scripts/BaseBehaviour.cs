using MRK.Networking;
using MRK.UI;
using UnityEngine;

namespace MRK
{
    /// <summary>
    /// Base class of any behaviour that provides ease of access to EGR's main internals
    /// </summary>
    public class BaseBehaviour : MonoBehaviour
    {
        private Transform _transform;
        private RectTransform _rectTransform;
        private GameObject _gameObject;

        public EGR Client
        {
            get
            {
                return EGR.Instance;
            }
        }

        public EGRNetworkingClient NetworkingClient
        {
            get
            {
                return Client.NetworkingClient;
            }
        }

        public ScreenManager ScreenManager
        {
            get
            {
                return ScreenManager.Instance;
            }
        }

        public EGREventManager EventManager
        {
            get
            {
                return EGREventManager.Instance;
            }
        }

        //cached properties
        public new Transform transform
        {
            get
            {
                if (_transform == null)
                {
                    _transform = base.transform;
                }

                return _transform;
            }
        }

        public RectTransform rectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    _rectTransform = (RectTransform)transform;
                }

                return _rectTransform;
            }
        }

        public new GameObject gameObject
        {
            get
            {
                if (_gameObject == null)
                {
                    _gameObject = base.gameObject;
                }

                return _gameObject;
            }
        }
    }
}

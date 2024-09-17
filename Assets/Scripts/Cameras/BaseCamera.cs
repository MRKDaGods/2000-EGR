using MRK.InputControllers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MRK.Cameras
{
    public class BaseCamera : BaseBehaviour
    {
        protected readonly bool[] _down;
        protected readonly float[] _delta;
        protected readonly Vector3[] _deltas;
        protected readonly bool[] _passedThreshold;
        protected bool _interfaceActive;
        protected InputController _lastController;
        private float _lastControllerTime;

        protected Camera Camera => Client.ActiveCamera;
        public bool InterfaceActive => _interfaceActive;

        public BaseCamera()
        {
            _down = new bool[2];
            _delta = new float[2];
            _deltas = new Vector3[2];
            _passedThreshold = new bool[2];
        }

        public virtual void SetInterfaceState(bool active, bool force = false)
        {
            if (_interfaceActive == active && !force)
                return;

            _interfaceActive = active;
            if (!_interfaceActive)
            {
                ResetStates();
            }
        }

        public void ResetStates()
        {
            //reset
            for (int i = 0; i < 2; i++)
            {
                _down[i] = false;
                _delta[i] = 0f;
                _deltas[i] = Vector3.zero;
                _passedThreshold[i] = false;
            }
        }

        public bool ShouldProcessControllerMessage(Message msg, bool ignoreUI = false)
        {
            //if (Client.ActiveScreens.Count > 1)
            //    return false;

            if (!ignoreUI)
            {
                MouseData data = (MouseData)msg.Proposer;
                int id = msg.Kind == MessageKind.Virtual ? data.Index : -1;
                if (EventSystem.current.IsPointerOverGameObject(id))
                    return false;
            }

            bool res = true;

            InputController proposed = Client.GetControllerFromMessage(msg);
            if (_lastController == null || _lastController != proposed)
            {
                if (Time.time - _lastControllerTime > 0.3f)
                {
                    _lastController = proposed;
                }
                else
                {
                    res = false;
                }

                _lastControllerTime = Time.time;
            }

            return res;
        }
    }
}

using UnityEngine;

namespace MRK {
    public class EGRCamera : EGRBehaviour {
        protected readonly bool[] m_Down;
        protected readonly float[] m_Delta;
        protected readonly Vector3[] m_Deltas;
        protected bool m_InterfaceActive;
        protected EGRController m_LastController;
        float m_LastControllerTime;

        protected Camera m_Camera => Client.ActiveCamera;

        public EGRCamera() {
            m_Down = new bool[2];
            m_Delta = new float[2];
            m_Deltas = new Vector3[2];
        }

        public virtual void SetInterfaceState(bool active) {
            if (m_InterfaceActive == active)
                return;

            m_InterfaceActive = active;
            if (!m_InterfaceActive) {
                ResetStates();
            }
        }

        public void ResetStates() {
            //reset
            for (int i = 0; i < 2; i++) {
                m_Down[i] = false;
                m_Delta[i] = 0f;
                m_Deltas[0] = Vector3.zero;
            }
        }

        protected bool ShouldProcessControllerMessage(EGRControllerMessage msg) {
            if (Client.ActiveScreens.Count > 1)
                return false;
            
            bool res = true;

            EGRController proposed = Client.GetControllerFromMessage(msg);
            if (m_LastController == null || m_LastController != proposed) {
                if (Time.time - m_LastControllerTime > 0.3f) {
                    m_LastController = proposed;
                }
                else
                    res = false;

                m_LastControllerTime = Time.time;
            }

            return res;
        }
    }
}

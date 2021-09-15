using MRK.UI;
using UnityEngine;

namespace MRK {
    public class EGRShowScreenKeyCode : MRKBehaviour {
        [SerializeField]
        KeyCode m_KeyCode;
        [SerializeField]
        EGRScreen m_Screen;

        void Start() {
            //if we're attached to a screen we need to attach ourselves to a global handler
            if (m_Screen.transform == transform) { //better than GetComponent? haha..
                Client.Runnable.RunOnMainThread(Update, true);
            }
        }

        void Update() {
            if (m_Screen == null)
                return;

            if (Client.Runnable.IsCalledByRunnable && gameObject.activeInHierarchy) //dont invoke twice
                return;

            if (Input.GetKeyDown(m_KeyCode)) {
                if (!m_Screen.Visible)
                    m_Screen.ShowScreen();
                else
                    m_Screen.HideScreen();
            }
        }

        void OnValidate() {
            m_Screen = GetComponent<EGRScreen>();
        }
    }
}

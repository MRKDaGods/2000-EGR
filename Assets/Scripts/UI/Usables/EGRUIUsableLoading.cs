using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI {
    public class EGRUIUsableLoading : EGRUIUsable {
        [SerializeField]
        float m_SpinSpeed;
        [SerializeField]
        Image m_Spinner;

        public float SpinSpeed { get => m_SpinSpeed; set => m_SpinSpeed = value; }

        void Update() {
            m_Spinner.rectTransform.Rotate(0f, 0f, m_SpinSpeed * Time.deltaTime);
        }
    }
}

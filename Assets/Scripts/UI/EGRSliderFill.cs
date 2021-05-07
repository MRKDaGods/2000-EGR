using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MRK {
    public class EGRSliderFill : MonoBehaviour {
        Image m_Image;
        Slider m_Owner;

        void Awake() {
            if (!gameObject.activeInHierarchy)
                return;

            m_Image = GetComponent<Image>();
            m_Owner = GetComponentInParent<Slider>();
            m_Owner.onValueChanged.RemoveAllListeners();
            m_Owner.onValueChanged.AddListener(OnValueChanged);
        }

        void OnValueChanged(float val) {
            m_Image.fillAmount = val;
        }

        void OnValidate() {
            Awake();
        }
    }
}

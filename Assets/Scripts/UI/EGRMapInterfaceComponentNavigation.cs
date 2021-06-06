using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Coffee.UIEffects;

namespace MRK.UI {
    public class EGRMapInterfaceComponentNavigation : EGRMapInterfaceComponent {
        Transform m_NavigationTransform;
        RectTransform m_Bottom;
        TextMeshProUGUI m_Distance;
        TextMeshProUGUI m_Time;
        Button m_Start;
        GameObject m_RoutePrefab;
        float m_StartAnimDelta;
        UIHsvModifier m_StartAnimHSV;

        public override EGRMapInterfaceComponentType ComponentType => EGRMapInterfaceComponentType.Navigation;

        public override void OnComponentInit(EGRScreenMapInterface mapInterface) {
            base.OnComponentInit(mapInterface);

            m_NavigationTransform = mapInterface.transform.Find("Navigation");
            m_NavigationTransform.gameObject.SetActive(false);

            m_Bottom = (RectTransform)m_NavigationTransform.Find("Bot");

            m_Distance = m_Bottom.Find("Main/Info/Distance").GetComponent<TextMeshProUGUI>();
            m_Time = m_Bottom.Find("Main/Info/Time").GetComponent<TextMeshProUGUI>();
            m_Start = m_Bottom.Find("Main/Destination/Button").GetComponent<Button>();
            m_StartAnimHSV = m_Start.transform.Find("Sep").GetComponent<UIHsvModifier>();

            m_RoutePrefab = m_Bottom.Find("Routes/Route").gameObject;
        }

        public override void OnComponentUpdate() {
            if (m_Start.gameObject.activeInHierarchy) {
                m_StartAnimDelta += Time.deltaTime * 0.2f;
                if (m_StartAnimDelta > 0.5f)
                    m_StartAnimDelta = -0.5f;

                m_StartAnimHSV.hue = m_StartAnimDelta;
            }
        }

        public void Show() {
            m_NavigationTransform.gameObject.SetActive(true);
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI.MapInterface {
    public class EGRUIMapButton {
        MRKBehaviour m_Behaviour;
        EGRUIMapButtonsGroup m_Group;
        EGRUIMapButtonInfo m_Info;
        Image m_Sprite;
        TextMeshProUGUI m_Text;
        Image m_Shadow;
        EGRUIMapButtonEffector m_Effector;

        public MRKBehaviour Behaviour => m_Behaviour;
        public EGRUIMapButtonsGroup Group => m_Group;
        public EGRUIMapButtonInfo Info => m_Info;
        public EGRUIMapButtonEffector Effector => m_Effector;

        public EGRUIMapButton(MRKBehaviour behaviour, EGRUIMapButtonsGroup group) {
            m_Behaviour = behaviour;
            m_Group = group;

            behaviour.transform.GetComponent<Button>().onClick.AddListener(OnButtonClick);

            m_Sprite = behaviour.transform.GetElement<Image>("Layout/Sprite");
            m_Text = behaviour.transform.GetElement<TextMeshProUGUI>("Layout/Text");
            m_Shadow = behaviour.transform.GetElement<Image>("Layout/Shadow");
        }

        void OnButtonClick() {
            m_Group.NotifyChildButtonClicked(m_Info.ID);
        }

        public void Initialize(EGRUIMapButtonInfo info, EGRUIMapButtonEffector effector, int siblingIdx) {
            m_Info = info;
            m_Effector = effector;

            m_Behaviour.transform.SetSiblingIndex(siblingIdx);

            m_Sprite.sprite = info.Sprite;
            m_Text.text = EGRLanguageManager.Localize(info.Name);

            SetTextActive(false);

            m_Effector.Initialize(this);
        }

        public void SetTextActive(bool active) {
            m_Text.gameObject.SetActive(active);
            m_Shadow.gameObject.SetActive(active);

            if (active) {
                //auto size and position shadow
                float textPreferredWidth = m_Text.GetPreferredValues().x;

                RectTransform shadowRectTransform = m_Shadow.rectTransform;
                Vector2 shadowOffsetMin = shadowRectTransform.offsetMin;
                shadowOffsetMin.x = shadowRectTransform.offsetMax.x - textPreferredWidth;
                shadowRectTransform.offsetMin = shadowOffsetMin;

                shadowRectTransform.localScale = new Vector3(1.8f, 1.5f, 1f);
            }
        }

        public void SetSpriteSize(float w, float h) {
            m_Sprite.rectTransform.sizeDelta = new Vector2(w, h);

            //set transform height as well?
            Vector2 oldSz = m_Behaviour.rectTransform.sizeDelta;
            oldSz.y = h;
            m_Behaviour.rectTransform.sizeDelta = oldSz;
        }
    }
}

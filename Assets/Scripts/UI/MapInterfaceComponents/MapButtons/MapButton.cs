using MRK.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI.MapInterface
{
    public class MapButton
    {
        private BaseBehaviour _behaviour;
        private MapButtonGroup _group;
        private MapButtonInfo _info;
        private Image _sprite;
        private TextMeshProUGUI _text;
        private Image _shadow;
        private MapButtonEffector _effector;

        public BaseBehaviour Behaviour
        {
            get
            {
                return _behaviour;
            }
        }

        public MapButtonGroup Group
        {
            get
            {
                return _group;
            }
        }

        public MapButtonInfo Info
        {
            get
            {
                return _info;
            }
        }

        public MapButtonEffector Effector
        {
            get
            {
                return _effector;
            }
        }

        public MapButton(BaseBehaviour behaviour, MapButtonGroup group)
        {
            _behaviour = behaviour;
            _group = group;

            behaviour.transform.GetComponent<Button>().onClick.AddListener(OnButtonClick);

            _sprite = behaviour.transform.GetElement<Image>("Layout/Sprite");
            _text = behaviour.transform.GetElement<TextMeshProUGUI>("Layout/Text");
            _shadow = behaviour.transform.GetElement<Image>("Layout/Shadow");
        }

        private void OnButtonClick()
        {
            _group.NotifyChildButtonClicked(_info.ID);
        }

        public void Initialize(MapButtonInfo info, MapButtonEffector effector, int siblingIdx)
        {
            _info = info;
            _effector = effector;

            _behaviour.transform.SetSiblingIndex(siblingIdx);

            _sprite.sprite = info.Sprite;
            _text.text = LanguageManager.Localize(info.Name);

            SetTextActive(false);

            _effector.Initialize(this);
        }

        public void SetTextActive(bool active)
        {
            _text.gameObject.SetActive(active);
            _shadow.gameObject.SetActive(active);

            if (active)
            {
                //auto size and position shadow
                float textPreferredWidth = _text.GetPreferredValues().x;

                RectTransform shadowRectTransform = _shadow.rectTransform;
                Vector2 shadowOffsetMin = shadowRectTransform.offsetMin;
                shadowOffsetMin.x = shadowRectTransform.offsetMax.x - textPreferredWidth;
                shadowRectTransform.offsetMin = shadowOffsetMin;

                shadowRectTransform.localScale = new Vector3(1.8f, 1.5f, 1f);
            }
        }

        public void SetTextOpacity(float alpha)
        {
            _text.alpha = alpha;
            _shadow.color = _shadow.color.AlterAlpha(alpha);
        }

        public void SetSpriteSize(float w, float h)
        {
            _sprite.rectTransform.sizeDelta = new Vector2(w, h);

            //set transform height as well?
            Vector2 oldSz = _behaviour.rectTransform.sizeDelta;
            oldSz.y = h;
            _behaviour.rectTransform.sizeDelta = oldSz;
        }
    }
}

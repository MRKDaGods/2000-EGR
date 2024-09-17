using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI
{
    public class SliderFill : MonoBehaviour
    {
        private Image _image;
        private Slider _owner;

        private void Awake()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            _image = GetComponent<Image>();
            _owner = GetComponentInParent<Slider>();
            _owner.onValueChanged.RemoveAllListeners();
            _owner.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnValueChanged(float val)
        {
            _image.fillAmount = val;
        }

        private void OnValidate()
        {
            Awake();
        }
    }
}

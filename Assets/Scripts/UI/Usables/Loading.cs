using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI.Usables
{
    public class Loading : Usable
    {
        [SerializeField]
        private float _spinSpeed;
        [SerializeField]
        private Image _spinner;

        public float SpinSpeed
        {
            get
            {
                return _spinSpeed;
            }

            set
            {
                _spinSpeed = value;
            }
        }

        private void Update()
        {
            _spinner.rectTransform.Rotate(0f, 0f, _spinSpeed * Time.deltaTime);
        }
    }
}

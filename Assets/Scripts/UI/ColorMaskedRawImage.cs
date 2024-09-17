using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI
{
    public class ColorMaskedRawImage : RawImage
    {
        [SerializeField]
        private Color _maskColor;
        private bool _isMaskedTexture = true;

        public new Texture texture
        {
            get
            {
                return _isMaskedTexture ? null : base.texture;
            }

            set
            {
                SetTexture(value);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            //update tex
            SetTexture(_isMaskedTexture ? null : texture);
        }

        public void SetTexture(Texture tex)
        {
            if (tex == null)
            {
                tex = Utilities.GetPlainTexture(_maskColor);
                _isMaskedTexture = true;
            }
            else
            {
                _isMaskedTexture = false;
            }

            base.texture = tex;
        }
    }
}

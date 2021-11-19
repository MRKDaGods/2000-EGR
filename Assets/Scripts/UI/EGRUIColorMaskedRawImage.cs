using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI {
    public class EGRUIColorMaskedRawImage : RawImage {
        [SerializeField]
        Color m_MaskColor;
        bool m_IsMaskedTexture = true;

        public new Texture texture {
            get => m_IsMaskedTexture ? null : base.texture;
            set => SetTexture(value);
        }

        protected override void OnEnable() {
            base.OnEnable();

            //update tex
            SetTexture(m_IsMaskedTexture ? null : texture);
        }

        public void SetTexture(Texture tex) {
            if (tex == null) {
                tex = EGRUIUtilities.GetPlainTexture(m_MaskColor);
                m_IsMaskedTexture = true;
            }
            else
                m_IsMaskedTexture = false;

            base.texture = tex;
        }
    }
}

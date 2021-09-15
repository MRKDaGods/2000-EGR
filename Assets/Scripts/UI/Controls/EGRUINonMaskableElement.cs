using UnityEngine.UI;

namespace MRK.UI {
    public class EGRUINonMaskableElement : MRKBehaviour {
        void Start() {
            MaskableGraphic[] maskableGraphics = transform.GetComponentsInChildren<MaskableGraphic>();
            foreach (MaskableGraphic gfx in maskableGraphics) {
                gfx.maskable = false;
            }
        }
    }
}

using UnityEngine.UI;

namespace MRK.UI
{
    public class NonMaskableElement : BaseBehaviour
    {
        private void Start()
        {
            MaskableGraphic[] maskableGraphics = transform.GetComponentsInChildren<MaskableGraphic>();
            foreach (MaskableGraphic gfx in maskableGraphics)
            {
                gfx.maskable = false;
            }
        }
    }
}

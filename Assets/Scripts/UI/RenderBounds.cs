using UnityEngine;

namespace MRK.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class RenderBounds : BaseBehaviour
    {
        private void OnGUI()
        {
            RectTransform rt = (RectTransform)transform;
            Rect wr = rt.WorldRect();
            MRKGL.DrawBox(MRKProjections.ProjectRectWorldToScreen(wr), Color.blue, 2f);
        }
    }
}

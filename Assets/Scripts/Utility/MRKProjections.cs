using UnityEngine;

namespace MRK {
    public class MRKProjections {
        static Camera ms_MainCamera;

        static void VerifyCamera() {
            if (ms_MainCamera == null)
                ms_MainCamera = Camera.main;
        }

        public static Rect ProjectRectWorldToScreen(Rect worldRect, Camera cam = null) {
            VerifyCamera();

            if (cam == null)
                cam = ms_MainCamera;

            Vector3 spos = cam.WorldToScreenPoint(new Vector3(worldRect.x, worldRect.y, 0f));
            spos.y = Screen.height - spos.y;
            //float w = //cam.WorldToScreenPoint(new Vector3(worldRect.xMax, 0f, 0f)).x - x;
            //float h = //cam.WorldToScreenPoint(new Vector3(0f, worldRect.yMax, 0f)).y - y;

            return new Rect(spos, worldRect.size);
        }
    }
}

using UnityEngine;

namespace MRK.UI {
    public class EGRUINestedElement {
        protected RectTransform RectTransform { get; }
        protected GameObject GameObject { get; }

        public EGRUINestedElement(RectTransform transform) {
            if (transform == null) {
                return;
            }

            RectTransform = transform;
            GameObject = transform.gameObject;
        }

        public void SetActive(bool active) {
            GameObject.SetActive(active);
        }
    }
}

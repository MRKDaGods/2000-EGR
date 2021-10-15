using UnityEngine;

namespace MRK.UI.MapInterface {
    public partial class EGRMapInterfaceComponentNavigation {
        public partial class NavInterface : EGRUINestedElement {
            public NavCurrentStep CurrentStep { get; }

            public NavInterface(RectTransform transform) : base(transform) {
                CurrentStep = new NavCurrentStep((RectTransform)transform.Find("CurrentStep"));
            }
        }
    }
}
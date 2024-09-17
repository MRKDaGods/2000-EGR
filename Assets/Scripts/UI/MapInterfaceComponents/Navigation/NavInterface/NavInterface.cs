using UnityEngine;

namespace MRK.UI.MapInterface
{
    public partial class Navigation
    {
        public partial class NavInterface : NestedElement
        {
            public NavCurrentStep CurrentStep
            {
                get;
            }

            public NavInterface(RectTransform transform) : base(transform)
            {
                CurrentStep = new NavCurrentStep((RectTransform)transform.Find("CurrentStep"));
            }
        }
    }
}
using UnityEngine.UI;

namespace MRK.UI
{
    public class AudioSettings : AnimatedLayout, ISupportsBackKey
    {
        public override bool CanChangeBar
        {
            get
            {
                return true;
            }
        }

        public override uint BarColor
        {
            get
            {
                return 0xFF000000;
            }
        }

        protected override void OnScreenInit()
        {
            base.OnScreenInit();
            GetElement<Button>("bBack").onClick.AddListener(OnBackClick);
        }

        private void OnBackClick()
        {
            HideScreen();
        }

        public void OnBackKeyDown()
        {
            OnBackClick();
        }
    }
}

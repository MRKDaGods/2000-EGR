using MRK.UI;

namespace MRK.Events
{
    public class ScreenShown : Event
    {
        public override EventType EventType
        {
            get
            {
                return EventType.ScreenShown;
            }
        }

        public Screen Screen
        {
            get; private set;
        }

        public ScreenShown()
        {
        }

        public ScreenShown(Screen screen)
        {
            Screen = screen;
        }
    }
}

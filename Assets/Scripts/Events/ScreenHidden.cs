using MRK.UI;

namespace MRK.Events
{
    public class ScreenHidden : Event
    {
        public override EventType EventType => EventType.ScreenHidden;

        public Screen Screen
        {
            get; private set;
        }

        public ScreenHidden()
        {
        }

        public ScreenHidden(Screen screen)
        {
            Screen = screen;
        }
    }
}

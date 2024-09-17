using MRK.UI;

namespace MRK.Events
{
    public class ScreenHideRequest : Event
    {
        public override EventType EventType => EventType.ScreenHideRequest;

        public Screen Screen
        {
            get; private set;
        }

        public bool Cancelled
        {
            get; set;
        }

        public ScreenHideRequest()
        {
        }

        public ScreenHideRequest(Screen screen)
        {
            Screen = screen;
        }
    }
}

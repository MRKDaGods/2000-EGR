using MRK.UI;

namespace MRK {
    public class EGREventScreenHideRequest : EGREvent {
        public override EGREventType EventType => EGREventType.ScreenHideRequest;
        public EGRScreen Screen { get; private set; }
        public bool Cancelled { get; set; }

        public EGREventScreenHideRequest() {
        }

        public EGREventScreenHideRequest(EGRScreen screen) {
            Screen = screen;
        }
    }
}

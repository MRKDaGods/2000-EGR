using MRK.UI;

namespace MRK {
    public class EGREventScreenShown : EGREvent {
        public override EGREventType EventType => EGREventType.ScreenShown;
        public EGRScreen Screen { get; private set; }

        public EGREventScreenShown() {
        }

        public EGREventScreenShown(EGRScreen screen) {
            Screen = screen;
        }
    }
}

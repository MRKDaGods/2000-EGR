using MRK.UI;

namespace MRK {
    public class EGREventScreenHidden : EGREvent {
        public override EGREventType EventType => EGREventType.ScreenHidden;
        public EGRScreen Screen { get; private set; }

        public EGREventScreenHidden() {
        }

        public EGREventScreenHidden(EGRScreen screen) {
            Screen = screen;
        }
    }
}

using MRK.UI.MapInterface;

namespace MRK {
    public class EGREventUIMapButtonGroupExpansionStateChanged : EGREvent {
        public override EGREventType EventType => EGREventType.UIMapButtonExpansionStateChanged;
        public EGRUIMapButtonsGroup Group { get; private set; }
        public bool Expanded { get; private set; }

        public EGREventUIMapButtonGroupExpansionStateChanged() {
        }

        public EGREventUIMapButtonGroupExpansionStateChanged(EGRUIMapButtonsGroup group, bool expanded) {
            Group = group;
            Expanded = expanded;
        }
    }
}

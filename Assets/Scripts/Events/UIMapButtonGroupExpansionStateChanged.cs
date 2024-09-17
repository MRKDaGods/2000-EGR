using MRK.UI.MapInterface;

namespace MRK.Events
{
    public class UIMapButtonGroupExpansionStateChanged : Event
    {
        public override EventType EventType
        {
            get
            {
                return EventType.UIMapButtonExpansionStateChanged;
            }
        }

        public MapButtonGroup Group
        {
            get; private set;
        }

        public bool Expanded
        {
            get; private set;
        }

        public UIMapButtonGroupExpansionStateChanged()
        {
        }

        public UIMapButtonGroupExpansionStateChanged(MapButtonGroup group, bool expanded)
        {
            Group = group;
            Expanded = expanded;
        }
    }
}

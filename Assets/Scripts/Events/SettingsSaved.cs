namespace MRK.Events
{
    public class SettingsSaved : Event
    {
        public override EventType EventType
        {
            get
            {
                return EventType.SettingsSaved;
            }
        }

        public SettingsSaved()
        {
        }
    }
}

namespace MRK.Events
{
    public class GraphicsApplied : Event
    {
        public override EventType EventType
        {
            get
            {
                return EventType.GraphicsApplied;
            }
        }

        public SettingsQuality Quality
        {
            get; private set;
        }

        public SettingsFPS FPS
        {
            get; private set;
        }

        public bool IsInit
        {
            get; private set;
        }

        public GraphicsApplied()
        {
        }

        public GraphicsApplied(SettingsQuality quality, SettingsFPS fps, bool isInit)
        {
            Quality = quality;
            FPS = fps;
            IsInit = isInit;
        }
    }
}

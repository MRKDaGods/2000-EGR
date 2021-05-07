namespace MRK {
    public class EGREventGraphicsApplied : EGREvent {
        public override EGREventType EventType => EGREventType.GraphicsApplied;
        public EGRSettingsQuality Quality { get; private set; }
        public EGRSettingsFPS FPS { get; private set; }
        public bool IsInit { get; private set; }

        public EGREventGraphicsApplied() {
        }

        public EGREventGraphicsApplied(EGRSettingsQuality quality, EGRSettingsFPS fps, bool isInit) {
            Quality = quality;
            FPS = fps;
            IsInit = isInit;
        }
    }
}

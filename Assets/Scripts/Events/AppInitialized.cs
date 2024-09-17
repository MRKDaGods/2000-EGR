namespace MRK.Events
{
    public class AppInitialized : Event
    {
        public override EventType EventType => EventType.AppInitialized;

        public AppInitialized()
        {
        }
    }
}

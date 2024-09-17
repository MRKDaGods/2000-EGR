namespace MRK.Events
{
    public class TileDestroyed : Event
    {
        public override EventType EventType
        {
            get
            {
                return EventType.TileDestroyed;
            }
        }

        public Tile Tile
        {
            get; private set;
        }

        public bool ZoomChanged
        {
            get; private set;
        }

        public TileDestroyed()
        {
        }

        public TileDestroyed(Tile tile, bool zoomChanged)
        {
            Tile = tile;
            ZoomChanged = zoomChanged;
        }
    }
}

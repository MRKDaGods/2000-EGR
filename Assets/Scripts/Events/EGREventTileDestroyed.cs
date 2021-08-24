namespace MRK {
    public class EGREventTileDestroyed : EGREvent {
        public override EGREventType EventType => EGREventType.TileDestroyed;
        public MRKTile Tile { get; private set; }
        public bool ZoomChanged { get; private set; }

        public EGREventTileDestroyed() {
        }

        public EGREventTileDestroyed(MRKTile tile, bool zoomChanged) {
            Tile = tile;
            ZoomChanged = zoomChanged;
        }
    }
}

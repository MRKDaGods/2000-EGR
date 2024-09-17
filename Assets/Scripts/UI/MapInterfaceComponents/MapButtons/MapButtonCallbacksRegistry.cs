using System;

namespace MRK.UI.MapInterface
{
    public class MapButtonCallbacksRegistry : Registry<MapButtonID, Action>
    {
        private static MapButtonCallbacksRegistry _global;

        public static new MapButtonCallbacksRegistry Global
        {
            get
            {
                return _global ??= new MapButtonCallbacksRegistry();
            }
        }
    }
}

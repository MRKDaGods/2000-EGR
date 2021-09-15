using System;

namespace MRK.UI.MapInterface {
    public class MRKRegistryUIMapButtonCallbacks : MRKRegistry<EGRUIMapButtonID, Action> {
        static MRKRegistryUIMapButtonCallbacks ms_Global;

        public static new MRKRegistryUIMapButtonCallbacks Global => ms_Global ??= new MRKRegistryUIMapButtonCallbacks();
    }
}

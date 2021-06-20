using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRK.Navigation {
    public class EGRNavigationLive : EGRNavigation {
        void OnRequestLocation(bool success, Vector2d? coords, float? bearing) {

        }

        public override void Update() {
            //get current step
            Client.LocationService.GetCurrentLocation(OnRequestLocation);
        }
    }
}

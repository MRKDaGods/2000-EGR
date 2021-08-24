using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MRK.Networking;

namespace MRK {
    public class EGREventNetworkDownloadRequest : EGREvent {
        public override EGREventType EventType => EGREventType.NetworkDownloadRequest;
        public EGRDownloadContext Context { get; private set; }
        public bool IsAccepted { get; set; }

        public EGREventNetworkDownloadRequest() {
        }

        public EGREventNetworkDownloadRequest(EGRDownloadContext context) {
            Context = context;
            IsAccepted = false;
        }
    }
}

using MRK.Networking;

namespace MRK.Events
{
    public class NetworkDownloadRequest : Event
    {
        public override EventType EventType
        {
            get
            {
                return EventType.NetworkDownloadRequest;
            }
        }

        public EGRDownloadContext Context
        {
            get; private set;
        }

        public bool IsAccepted
        {
            get; set;
        }

        public NetworkDownloadRequest()
        {
        }

        public NetworkDownloadRequest(EGRDownloadContext context)
        {
            Context = context;
            IsAccepted = false;
        }
    }
}

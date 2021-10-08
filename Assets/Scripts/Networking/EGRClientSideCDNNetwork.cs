using System.Threading;
using MRK.Networking.Packets;

namespace MRK.Networking {
    public class EGRClientSideCDNNetwork : IEGRNetworkExternal {
        EGRNetwork m_Network;
        Thread m_Thread;
        bool m_IsLocalCDNRunning;

        bool IsRunning => EGRMain.Instance.IsRunning && m_IsLocalCDNRunning;

        public void SetNetwork(EGRNetwork network) {
        }

        public void StartLocalCDN(int port, string key) {
            MRKLogger.Log($"Started local cdn, port={port} key={key}");

            if (m_Network != null) {
                MRKLogger.Log("Destroying old cdn network");

                m_Network.Stop();
                m_IsLocalCDNRunning = false;
                m_Thread.Abort();
            }

            m_Network = new EGRNetwork(EGRMain.Instance.NetworkingClient.MainNetwork.Endpoint.Address.ToString(),
                port, key, this, () => MRKTime.Time);
            m_Network.Connect();

            m_IsLocalCDNRunning = true;
            m_Thread = new Thread(CDNThread);
            m_Thread.Start();
        }

        public void StopLocalCDN() {
            if (!m_IsLocalCDNRunning)
                return;

            m_IsLocalCDNRunning = false;
            m_Network.Stop();
        }

        void CDNThread() {
            int threadInterval = EGRConstants.EGR_CDN_NETWORK_THREAD_INTERVAL;
            MRKLogger.Log($"Starting local cdn thread, threadInterval={threadInterval}");

            while (IsRunning) {
                m_Network.UpdateNetwork();
                Thread.Sleep(threadInterval);
            }

            MRKLogger.Log("Exiting local cdn thread");
        }

        public bool RequestCDNResource(string resource, byte[] sig, EGRPacketReceivedCallback<PacketInRequestCDNResource> callback) {
            if (m_Network == null)
                return false;

            return m_Network.SendPacket(new PacketOutRequestCDNResource(resource, sig), DeliveryMethod.ReliableOrdered, callback);
        }
    }
}

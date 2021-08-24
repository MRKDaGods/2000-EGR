#define MRK_LOCAL_SERVER

using MRK.Networking.Packets;
using System;
using System.Reflection;

namespace MRK.Networking {
    public class EGRNetworkingClient {
        public EGRNetwork MainNetwork { get; private set; }
        public EGRMainNetworkExternal MainNetworkExternal => (EGRMainNetworkExternal)MainNetwork.External;
        public EGRClientSideCDNNetwork ClientSideCDNNetwork { get; private set; }

        public void Initialize() {
            EGRMain.Log("Initializing networking client");

            //we need to manually get all packets and register them in our packet registry
            //get all types in the current assembly
            foreach (Type type in Assembly.GetExecutingAssembly().GetLoadedModules()[0].GetTypes()) {
                //skip type if it does not belong to MRK.Networking.Packets
                if (type.Namespace != "MRK.Networking.Packets")
                    continue;

                //get packet registration info
                PacketRegInfo regInfo = type.GetCustomAttribute<PacketRegInfo>();
                if (regInfo != null) {
                    //type is a packet, assign to the appropriate registry depending on the nature
                    if (regInfo.PacketNature == PacketNature.Out)
                        Packet.RegisterOut(regInfo.PacketType, type);
                    else
                        Packet.RegisterIn(regInfo.PacketType, type);
                }
            }

#if MRK_LOCAL_SERVER
            //initialize network, and let it connect to localhost
            MainNetwork = new EGRNetwork("127.0.0.1", EGRConstants.EGR_MAIN_NETWORK_PORT, 
                EGRConstants.EGR_MAIN_NETWORK_KEY, new EGRMainNetworkExternal());
#else
            MainNetwork = new EGRNetwork("37.58.62.171", EGRConstants.EGR_MAIN_NETWORK_PORT, EGRConstants.EGR_MAIN_NETWORK_KEY);
#endif

            //connect to the server
            MainNetwork.Connect();
            MainNetwork.PacketWatchdog.Register<PacketInLoginAccount>(OnNetLogin);

            //assign cdn
            ClientSideCDNNetwork = new EGRClientSideCDNNetwork();

            EGREventManager.Instance.Register<EGREventNetworkConnected>(OnNetworkConnected);
            EGREventManager.Instance.Register<EGREventNetworkDisconnected>(OnNetworkDisconnected);
        }

        public void Update() {
            if (MainNetwork != null) {
                MainNetwork.UpdateNetwork();
            }
        }

        public void Shutdown() {
            MainNetwork.Stop();
            ClientSideCDNNetwork.StopLocalCDN();

            EGREventManager.Instance.Unregister<EGREventNetworkConnected>(OnNetworkConnected);
            EGREventManager.Instance.Unregister<EGREventNetworkDisconnected>(OnNetworkDisconnected);
        }

        void OnNetworkConnected(EGREventNetworkConnected evt) {
            if (evt.Network == MainNetwork) {
                //ask for CDN?
                MainNetwork.PacketWatchdog.Start();

                //if we're logged in already, server must know
                if (EGRLocalUser.Instance != null) {
                    MainNetworkExternal.LoginAccountToken(EGRLocalUser.Instance.Token, null);
                }
            }
        }

        void OnNetworkDisconnected(EGREventNetworkDisconnected evt) {
            if (evt.Network == MainNetwork) {
                MainNetwork.PacketWatchdog.Stop();
            }
        }

        void OnNetLogin(PacketInLoginAccount packet) {
            EGRMain.Log($"Late login result = {packet.Response}");
            if (packet.Response == EGRStandardResponse.SUCCESS) {
                //request cdn
                EGRMain.Instance.Runnable.RunLater(() => MainNetworkExternal.RetrieveNetInfo(OnNetRetrieveNetInfo), 1f);
            }
        }

        void OnNetRetrieveNetInfo(PacketInRetrieveNetInfo response) {
            ClientSideCDNNetwork.StartLocalCDN(response.CDNPort, response.CDNKey);
        }
    }
}

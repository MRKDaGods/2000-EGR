using MRK.Networking;
using MRK.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MRK {
    public class TestNetworkClients : MonoBehaviour {
        [SerializeField]
        Button m_Button;
        List<EGRNetwork> m_Networks;

        void Start() {
            m_Button.onClick.AddListener(OnConnectClick);

            foreach (Type type in Assembly.GetExecutingAssembly().ManifestModule.GetTypes()) {
                if (type.Namespace != "MRK.Networking.Packets")
                    continue;

                PacketRegInfo regInfo = type.GetCustomAttribute<PacketRegInfo>();
                if (regInfo != null) {
                    if (regInfo.PacketNature == PacketNature.Out)
                        Packet.RegisterOut(regInfo.PacketType, type);
                    else
                        Packet.RegisterIn(regInfo.PacketType, type);
                }
            }

            m_Networks = new List<EGRNetwork>();
        }

        void OnConnectClick() {
            EGRNetwork network = new EGRNetwork("127.0.0.1", EGRConstants.EGR_MAIN_NETWORK_PORT, EGRConstants.EGR_MAIN_NETWORK_KEY);
            network.Connect();
            m_Networks.Add(network);
        }
    }
}

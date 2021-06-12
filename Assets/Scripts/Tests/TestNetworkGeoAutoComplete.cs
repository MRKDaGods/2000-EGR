using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MRK.Networking;
using UnityEngine.UI;
using MRK.Networking.Packets;
using System;
using System.Reflection;
using TMPro;

namespace MRK {
    public class TestNetworkGeoAutoComplete : MonoBehaviour {
        EGRNetwork m_Network;
        [SerializeField]
        Button m_Button;
        [SerializeField]
        TMP_InputField m_Input;
        [SerializeField]
        TMP_InputField m_Out;

        void Start() {
            m_Button.onClick.AddListener(OnSearchClick);

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

            m_Network = new EGRNetwork("127.0.0.1", EGRConstants.EGR_MAIN_NETWORK_PORT, EGRConstants.EGR_MAIN_NETWORK_KEY);
            m_Network.Connect();
        }

        bool NetGeoAutoComplete(string query, Vector2d proximity, EGRPacketReceivedCallback<PacketInGeoAutoComplete> callback) {
            if (query.Length > 256)
                return false;

            return m_Network.SendPacket(new PacketOutGeoAutoComplete(query, proximity), DeliveryMethod.ReliableOrdered, callback);
        }

        void OnSearchClick() {
            NetGeoAutoComplete(m_Input.text, new Vector2d(30.04778d, 30.99137d), (res) => {
                m_Out.text = res.Response;
            });
        }

        void Update() {
            m_Network.UpdateNetwork();
        }
    }
}

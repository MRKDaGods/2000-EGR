﻿using System;
using System.Net;
using System.Collections.Generic;
using MRK.Networking.Packets;
using UnityEngine;

namespace MRK.Networking {
    public delegate void EGRPacketReceivedCallback<T>(T packet) where T : Packet;

    public enum EGRStandardResponse : byte {
        NONE = 0x0,
        TIMED_OUT,
        FAILED,
        SUCCESS
    }

    public class EGRNetwork {
        struct BufferedRequest {
            public EGRPacketReceivedCallback<Packet> Callback { get; private set; }
            public float StartTime { get; private set; }
            public Type PacketType { get; private set; }

            public BufferedRequest(EGRPacketReceivedCallback<Packet> callback, Type packetType) {
                Callback = callback;
                StartTime = Time.time;
                PacketType = packetType;
            }
        }

        const int INVALID_BUFFERED_REQUEST = -1;
        const float PACKET_TIMEOUT = 5f; //secs

        NetManager m_Network;
        EventBasedNetListener m_Listener;
        IPEndPoint m_Endpoint;
        string m_Key;
        bool m_InitiatedConnection;
        bool m_RaisedDisconnectEvent;
        bool m_HasConnected;
        Dictionary<int, BufferedRequest> m_BufferedRequests;
        List<int> m_RequestClearBuffer;
        string m_XorKey;
        readonly Dictionary<ulong, EGRDownloadContext> m_ActiveDownloads;

        NetPeer Remote => m_Network.FirstPeer;
        public bool IsConnected => Remote != null && Remote.ConnectionState == ConnectionState.Connected;
        public IPEndPoint Endpoint => m_Endpoint;

        public EGRNetwork(string ip, int port, string key) {
            m_BufferedRequests = new Dictionary<int, BufferedRequest>();
            m_RequestClearBuffer = new List<int>();
            m_ActiveDownloads = new Dictionary<ulong, EGRDownloadContext>();
            m_Key = key;

            m_Listener = new EventBasedNetListener();
            m_Network = new NetManager(m_Listener);

            m_Endpoint = new IPEndPoint(IPAddress.Parse(ip), port);

            m_Listener.PeerConnectedEvent += OnConnected;
            m_Listener.PeerDisconnectedEvent += OnDisconnected;
            m_Listener.NetworkReceiveEvent += OnReceive;

            EGREventManager.Instance.Register<EGREventPacketReceived>(OnPacketReceived);
        }

        ~EGRNetwork() {
            EGREventManager.Instance.Unregister<EGREventPacketReceived>(OnPacketReceived);
        }

        void OnReceive(NetPeer server, NetPacketReader reader, DeliveryMethod method) {
            PacketNature nature = (PacketNature)reader.GetByte();
            PacketType type = (PacketType)reader.GetUShort();

            int bufferedReq = reader.GetInt();

            Packet packet = Packet.CreatePacketInstance(nature, type);
            if (packet == null) {
                EGRMain.Log(LogType.Error, $"Cannot create packet, n={nature}, t={type}");
                return;
            }

            PacketDataStream dataStream = new PacketDataStream(reader.GetRemainingBytes(), nature);
            dataStream.Prepare();

            packet.Read(dataStream);

            dataStream.Clean();

            if (bufferedReq != INVALID_BUFFERED_REQUEST) {
                if (!m_BufferedRequests.ContainsKey(bufferedReq)) {
                    EGRMain.Log(LogType.Error, $"Unknown buffered request, req={bufferedReq}");
                }
                else {
                    m_BufferedRequests[bufferedReq].Callback(packet);
                    m_BufferedRequests.Remove(bufferedReq);
                }
            }

            EGREventManager.Instance.BroadcastEvent(new EGREventPacketReceived(this, packet));
        }

        void OnPacketReceived(EGREventPacketReceived evt) {
            switch (evt.Packet.PacketType) {

                case PacketType.XKEY: {
                        m_XorKey = ((PacketInXorKey)evt.Packet).XorKey;
                        EGRMain.Log($"Set xor key to {m_XorKey}");
                    }
                    break;

                case PacketType.DWNLDREQ: {
                        PacketInDownloadRequest packet = (PacketInDownloadRequest)evt.Packet;
                        EGRDownloadContext downloadContext = new EGRDownloadContext(packet.ID, packet.Sections);
                        EGREventNetworkDownloadRequest downloadEvt = new EGREventNetworkDownloadRequest(downloadContext);
                        EGREventManager.Instance.BroadcastEvent(downloadEvt);

                        if (downloadEvt.IsAccepted) {
                            downloadContext.AllocateBuffer();
                            m_ActiveDownloads[downloadContext.ID] = downloadContext;
                        }

                        //reply to server to start download
                        SendStationaryPacket<Packet>(PacketType.DWNLDREQ, DeliveryMethod.ReliableOrdered, null, x => {
                            x.WriteUInt64(downloadContext.ID);
                            x.WriteBool(downloadEvt.IsAccepted);
                        });
                    }
                    break;

                case PacketType.DWNLD: {
                        PacketInDownload packet = (PacketInDownload)evt.Packet;
                        EGRDownloadContext downloadContext = m_ActiveDownloads[packet.ID];
                        downloadContext.SetData(packet.Progress, packet.Data);

                        //received!
                        SendStationaryPacket<Packet>(PacketType.DWNLD, DeliveryMethod.ReliableOrdered, null, x => {
                            x.WriteUInt64(downloadContext.ID);
                        });

                        if (!packet.Incomplete) {
                            lock (m_ActiveDownloads) {
                                m_ActiveDownloads.Remove(packet.ID);
                            }
                        }
                    }
                    break;

            }
        }

        void OnConnected(NetPeer server) {
            EGREventManager.Instance.BroadcastEvent(new EGREventNetworkConnected(this));
            m_HasConnected = true;
            m_RaisedDisconnectEvent = false;

            //send device info (auto)
            SendPacket(new PacketOutDeviceInfo(SystemInfo.deviceUniqueIdentifier));
        }

        void OnDisconnected(NetPeer server, DisconnectInfo info) {
            if (m_RaisedDisconnectEvent)
                return;

            EGREventManager.Instance.BroadcastEvent(new EGREventNetworkDisconnected(this, info));
            m_RaisedDisconnectEvent = true;
            m_HasConnected = false;
        }

        public void AlterConnection(string newIp, string newPort) {
            Disconnect();
            m_Endpoint = new IPEndPoint(IPAddress.Parse(newIp), int.Parse(newPort));
            Connect();
        }
        
        public void Connect() {
            if (!m_Network.IsRunning)
                m_Network.Start();

            m_InitiatedConnection = true;
            m_Network.Connect(m_Endpoint, m_Key);
        }

        public void UpdateNetwork() {
            if (!IsConnected) {
                if (m_InitiatedConnection)
                    Connect();

                if (m_HasConnected)
                    OnDisconnected(null, new DisconnectInfo());

                return;
            }

            m_RequestClearBuffer.Clear();
            foreach (KeyValuePair<int, BufferedRequest> pair in m_BufferedRequests) {
                if (Time.time - pair.Value.StartTime >= PACKET_TIMEOUT) {
                    //force callback
                    if (pair.Value.PacketType == typeof(PacketInStandardResponse)) {
                        pair.Value.Callback(new PacketInStandardResponse(EGRStandardResponse.TIMED_OUT));
                    }

                    m_RequestClearBuffer.Add(pair.Key);
                }
            }

            foreach (int req in m_RequestClearBuffer)
                m_BufferedRequests.Remove(req);

            m_Network.PollEvents();
        }

        public void Disconnect() {
            if (IsConnected) {
                m_Network.DisconnectAll();
                m_InitiatedConnection = false;
            }
        }

        int GetEmptyBufferRequest() {
            int req;

            do
                req = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            while (m_BufferedRequests.ContainsKey(req) || req == INVALID_BUFFERED_REQUEST);

            return req;
        }

        public bool SendPacket<T>(Packet packet, DeliveryMethod deliveryMethod, EGRPacketReceivedCallback<T> receivedCallback, Action<PacketDataStream> customWrite = null) where T : Packet {
            //Packet must be out
            if (packet.PacketNature != PacketNature.Out || !IsConnected)
                return false;

            PacketDataStream dataStream = new PacketDataStream(null, PacketNature.Out);
            dataStream.Prepare();

            dataStream.WriteUInt16((ushort)packet.PacketType);

            if (receivedCallback != null) {
                int req = GetEmptyBufferRequest();
                m_BufferedRequests[req] = new BufferedRequest(x => receivedCallback((T)x), typeof(T));
                dataStream.WriteInt32(req);
                //EGRMain.Log($"BUF={req}");
            }
            else
                dataStream.WriteInt32(INVALID_BUFFERED_REQUEST);

            packet.Write(dataStream);

            //write custom data
            if (customWrite != null)
                customWrite(dataStream);

            m_Network.SendToAll(dataStream.Data, deliveryMethod);

            dataStream.Clean();
            return true;
        }

        public bool SendPacket(Packet packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered) {
            return SendPacket<Packet>(packet, deliveryMethod, null);
        }

        public bool SendStationaryPacket<T>(PacketType type, DeliveryMethod deliveryMethod, EGRPacketReceivedCallback<T> receivedCallback, Action<PacketDataStream> customWrite = null) where T : Packet {
            return SendPacket(new Packet(PacketNature.Out, type), deliveryMethod, receivedCallback, customWrite);
        }
    }
}
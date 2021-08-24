using MRK.Networking.Packets;
using System;
using System.Collections.Generic;

namespace MRK.Networking {
    public class EGRPacketWatchdog {
        EGRNetwork m_Network;
        readonly Dictionary<Type, List<Action<Packet>>> m_Listeners;
        readonly Dictionary<object, Action<Packet>> m_AnonymousStore;

        public bool IsRunning { get; private set; }

        public EGRPacketWatchdog(EGRNetwork network) {
            m_Network = network;
            m_Listeners = new Dictionary<Type, List<Action<Packet>>>();
            m_AnonymousStore = new Dictionary<object, Action<Packet>>();
        }

        public void Start() {
            if (IsRunning)
                return;

            EGREventManager.Instance.Register<EGREventPacketReceived>(OnPacketReceived);
            IsRunning = true;
        }

        public void Stop() {
            if (!IsRunning)
                return;

            EGREventManager.Instance.Unregister<EGREventPacketReceived>(OnPacketReceived);
            IsRunning = false;
        }

        void OnPacketReceived(EGREventPacketReceived evt) {
            if (evt.Network != m_Network)
                return;

            GetListeners(evt.Packet.GetType()).ForEach(x => {
                x(evt.Packet);
            });
        }

        List<Action<Packet>> GetListeners(Type t) {
            if (!m_Listeners.ContainsKey(t)) {
                m_Listeners[t] = new List<Action<Packet>>();
            }

            return m_Listeners[t];
        }

        List<Action<Packet>> GetListeners<T>() {
            return GetListeners(typeof(T));
        }

        public void Register<T>(Action<T> action) where T : Packet {
            Action<Packet> anonAct = (packet) => action((T)packet);
            GetListeners<T>().Add(anonAct);

            m_AnonymousStore[action] = anonAct;
        }

        public void Unregister<T>(Action<T> action) where T : Packet {
            Action<Packet> anonAct = m_AnonymousStore[action];
            GetListeners<T>().Remove(anonAct);

            m_AnonymousStore.Remove(action);
        }
    }
}

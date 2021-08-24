using System;
using System.Collections.Generic;

namespace MRK {
    public enum EGREventType {
        None,
        NetworkConnected,
        NetworkDisconnected,
        PacketReceived,
        ScreenShown,
        ScreenHidden,
        GraphicsApplied,
        NetworkDownloadRequest,
        TileDestroyed,
        SettingsSaved
    }

    public abstract class EGREvent {
        public abstract EGREventType EventType { get; }
    }

    public delegate void EGREventCallback<T>(T czEvent) where T : EGREvent;

    public class EGREventManager {
        readonly Dictionary<EGREventType, List<EGREventCallback<EGREvent>>> m_Callbacks;
        readonly Dictionary<Type, EGREventType> m_ActivatorBuffers;
        readonly Dictionary<object, EGREventCallback<EGREvent>> m_AnonymousStore;

        static EGREventManager ms_Instance;

        public static EGREventManager Instance {
            get {
                if (ms_Instance == null)
                    ms_Instance = new EGREventManager();

                return ms_Instance;
            }
        }

        public EGREventManager() {
            m_Callbacks = new Dictionary<EGREventType, List<EGREventCallback<EGREvent>>>();
            m_ActivatorBuffers = new Dictionary<Type, EGREventType>();
            m_AnonymousStore = new Dictionary<object, EGREventCallback<EGREvent>>();
        }

        void CreateIfMissing(EGREventType type) {
            if (!m_Callbacks.ContainsKey(type))
                m_Callbacks[type] = new List<EGREventCallback<EGREvent>>();
        }

        EGREventType GetFromActivator<T>() where T : EGREvent {
            Type type = typeof(T);
            if (m_ActivatorBuffers.ContainsKey(type))
                return m_ActivatorBuffers[type];

            T local = Activator.CreateInstance<T>();
            m_ActivatorBuffers[type] = local.EventType;

            return local.EventType;
        }

        void EGREventWrapper<T>(EGREvent czEvent, EGREventCallback<T> callback) where T : EGREvent {
            callback((T)czEvent);
        }

        public void Register<T>(EGREventCallback<T> callback) where T : EGREvent {
            if (callback == null)
                return;

            EGREventType eventType = GetFromActivator<T>();
            CreateIfMissing(eventType);

            EGREventCallback<EGREvent> anonAction = (evt) => EGREventWrapper<T>(evt, callback);
            m_Callbacks[eventType].Add(anonAction);
            m_AnonymousStore[callback] = anonAction;
        }

        public void Unregister<T>(EGREventCallback<T> callback) where T : EGREvent {
            if (callback == null)
                return;

            EGREventType eventType = GetFromActivator<T>();
            CreateIfMissing(eventType);

            EGREventCallback<EGREvent> anonAction;
            if (m_AnonymousStore.TryGetValue(callback, out anonAction)) {
                m_Callbacks[eventType].Remove(anonAction);
                m_AnonymousStore.Remove(callback);
            }
        }

        public void UnregisterAll<T>() where T : EGREvent {
            EGREventType eventType = GetFromActivator<T>();
            CreateIfMissing(eventType);

            m_Callbacks[eventType].Clear();
        }

        public void BroadcastEvent<T>(T _event) where T : EGREvent {
            CreateIfMissing(_event.EventType);

            foreach (EGREventCallback<EGREvent> callback in m_Callbacks[_event.EventType])
                callback(_event);
        }
    }
}
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
        ScreenHideRequest,
        GraphicsApplied,
        NetworkDownloadRequest,
        TileDestroyed,
        SettingsSaved,
        AppInitialized,
        UIMapButtonExpansionStateChanged
    }

    public abstract class EGREvent {
        public abstract EGREventType EventType { get; }
    }

    public delegate void EGREventCallback<T>(T czEvent) where T : EGREvent;

    public class EGREventManager {
        struct PendingRequest {
            public bool IsRemoval;
            public EGREventType EventType;
            public EGREventCallback<EGREvent> AnonAction;
            public object Callback;
        }

        readonly Dictionary<EGREventType, List<EGREventCallback<EGREvent>>> m_Callbacks;
        readonly Dictionary<Type, EGREventType> m_ActivatorBuffers;
        readonly Dictionary<object, EGREventCallback<EGREvent>> m_AnonymousStore;
        int m_BroadcastDepth;
        readonly List<PendingRequest> m_PendingRequests;

        static EGREventManager ms_Instance;

        public static EGREventManager Instance => ms_Instance ??= new EGREventManager();

        public EGREventManager() {
            m_Callbacks = new Dictionary<EGREventType, List<EGREventCallback<EGREvent>>>();
            m_ActivatorBuffers = new Dictionary<Type, EGREventType>();
            m_AnonymousStore = new Dictionary<object, EGREventCallback<EGREvent>>();
            m_PendingRequests = new List<PendingRequest>();
            m_BroadcastDepth = 0;
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

            if (m_BroadcastDepth > 0) {
                m_PendingRequests.Add(new PendingRequest {
                    IsRemoval = false,
                    EventType = eventType,
                    AnonAction = anonAction,
                    Callback = callback
                });
                return;
            }

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
                if (m_BroadcastDepth > 0) {
                    m_PendingRequests.Add(new PendingRequest {
                        IsRemoval = true,
                        EventType = eventType,
                        AnonAction = anonAction,
                        Callback = callback
                    });
                    return;
                }

                m_Callbacks[eventType].Remove(anonAction);
                m_AnonymousStore.Remove(callback);
            }
        }

        [Obsolete("Currently unsupported due to addition of AnonStore", true)]
        /// <summary>
        /// UNSUPPORTED
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void UnregisterAll<T>() where T : EGREvent {
            EGREventType eventType = GetFromActivator<T>();
            CreateIfMissing(eventType);

            m_Callbacks[eventType].Clear();
        }

        public void BroadcastEvent<T>(T _event) where T : EGREvent {
            CreateIfMissing(_event.EventType);

            m_BroadcastDepth++;

            foreach (EGREventCallback<EGREvent> callback in m_Callbacks[_event.EventType])
                callback(_event);

            m_BroadcastDepth--;

            if (m_BroadcastDepth == 0) {
                foreach (PendingRequest request in m_PendingRequests) {
                    List<EGREventCallback<EGREvent>> callbacks = m_Callbacks[request.EventType];
                    if (request.IsRemoval) {
                        callbacks.Remove(request.AnonAction);
                        m_AnonymousStore.Remove(request.Callback);
                    }
                    else {
                        callbacks.Add(request.AnonAction);
                        m_AnonymousStore[request.Callback] = request.AnonAction;
                    }
                }

                m_PendingRequests.Clear();
            }
        }
    }
}
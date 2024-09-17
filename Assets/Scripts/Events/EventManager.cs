using System;
using System.Collections.Generic;

namespace MRK.Events
{
    public enum EventType
    {
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

    public abstract class Event
    {
        public abstract EventType EventType
        {
            get;
        }
    }

    public delegate void EventCallback<T>(T czEvent) where T : Event;

    public class EventManager
    {
        private struct PendingRequest
        {
            public bool IsRemoval;
            public EventType EventType;
            public EventCallback<Event> AnonAction;
            public object Callback;
        }

        private readonly Dictionary<EventType, List<EventCallback<Event>>> _callbacks;
        private readonly Dictionary<Type, EventType> _activatorBuffers;
        private readonly Dictionary<object, EventCallback<Event>> _anonymousStore;
        private int _broadcastDepth;
        private readonly List<PendingRequest> _pendingRequests;

        private static EventManager _instance;

        public static EventManager Instance
        {
            get
            {
                return _instance ??= new EventManager();
            }
        }

        public EventManager()
        {
            _callbacks = new Dictionary<EventType, List<EventCallback<Event>>>();
            _activatorBuffers = new Dictionary<Type, EventType>();
            _anonymousStore = new Dictionary<object, EventCallback<Event>>();
            _pendingRequests = new List<PendingRequest>();
            _broadcastDepth = 0;
        }

        private void CreateIfMissing(EventType type)
        {
            if (!_callbacks.ContainsKey(type))
            {
                _callbacks[type] = new List<EventCallback<Event>>();
            }
        }

        private EventType GetFromActivator<T>() where T : Event
        {
            Type type = typeof(T);
            if (_activatorBuffers.ContainsKey(type))
            {
                return _activatorBuffers[type];
            }

            T local = Activator.CreateInstance<T>();
            _activatorBuffers[type] = local.EventType;

            return local.EventType;
        }

        private void EGREventWrapper<T>(Event czEvent, EventCallback<T> callback) where T : Event
        {
            callback((T)czEvent);
        }

        public void Register<T>(EventCallback<T> callback) where T : Event
        {
            if (callback == null)
                return;

            EventType eventType = GetFromActivator<T>();
            CreateIfMissing(eventType);

            EventCallback<Event> anonAction = (evt) => EGREventWrapper<T>(evt, callback);

            if (_broadcastDepth > 0)
            {
                _pendingRequests.Add(new PendingRequest
                {
                    IsRemoval = false,
                    EventType = eventType,
                    AnonAction = anonAction,
                    Callback = callback
                });
                return;
            }

            _callbacks[eventType].Add(anonAction);
            _anonymousStore[callback] = anonAction;
        }

        public void Unregister<T>(EventCallback<T> callback) where T : Event
        {
            if (callback == null)
            {
                return;
            }

            EventType eventType = GetFromActivator<T>();
            CreateIfMissing(eventType);

            EventCallback<Event> anonAction;
            if (_anonymousStore.TryGetValue(callback, out anonAction))
            {
                if (_broadcastDepth > 0)
                {
                    _pendingRequests.Add(new PendingRequest
                    {
                        IsRemoval = true,
                        EventType = eventType,
                        AnonAction = anonAction,
                        Callback = callback
                    });
                    return;
                }

                _callbacks[eventType].Remove(anonAction);
                _anonymousStore.Remove(callback);
            }
        }

        [Obsolete("Currently unsupported due to addition of AnonStore", true)]
        /// <summary>
        /// UNSUPPORTED
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void UnregisterAll<T>() where T : Event
        {
            EventType eventType = GetFromActivator<T>();
            CreateIfMissing(eventType);

            _callbacks[eventType].Clear();
        }

        public void BroadcastEvent<T>(T _event) where T : Event
        {
            CreateIfMissing(_event.EventType);

            _broadcastDepth++;

            foreach (EventCallback<Event> callback in _callbacks[_event.EventType])
                callback(_event);

            _broadcastDepth--;

            if (_broadcastDepth == 0)
            {
                foreach (PendingRequest request in _pendingRequests)
                {
                    List<EventCallback<Event>> callbacks = _callbacks[request.EventType];
                    if (request.IsRemoval)
                    {
                        callbacks.Remove(request.AnonAction);
                        _anonymousStore.Remove(request.Callback);
                    }
                    else
                    {
                        callbacks.Add(request.AnonAction);
                        _anonymousStore[request.Callback] = request.AnonAction;
                    }
                }

                _pendingRequests.Clear();
            }
        }
    }
}
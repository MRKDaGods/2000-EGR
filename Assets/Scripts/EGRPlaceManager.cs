using MRK.Networking.Packets;
using System;
using System.Collections.Generic;

namespace MRK {
    public delegate void EGRFetchPlacesCallback(EGRPlace place);

    public class EGRPlaceManager : EGRBehaviour {
        struct ContextInfo {
            public ulong Context;
            public ulong CID;
        }

        readonly Dictionary<ulong, RectD> m_CachedRects;
        readonly Dictionary<ulong, List<ContextInfo>> m_CachedIDs;
        readonly Dictionary<ulong, EGRPlace> m_CachedPlaces;
        readonly Dictionary<ulong, EGRFetchPlacesCallback> m_ActiveCallbacks;
        readonly Dictionary<ulong, ContextInfo> m_CIDCtxIndex;
        readonly Random m_Rand;

        public EGRPlaceManager() {
            m_CachedRects = new Dictionary<ulong, RectD>();
            m_CachedIDs = new Dictionary<ulong, List<ContextInfo>>();
            m_CachedPlaces = new Dictionary<ulong, EGRPlace>();
            m_ActiveCallbacks = new Dictionary<ulong, EGRFetchPlacesCallback>();
            m_CIDCtxIndex = new Dictionary<ulong, ContextInfo>();
            m_Rand = new Random();
        }

        public void FetchPlacesInRegion(double minLat, double minLng, double maxLat, double maxLng, EGRFetchPlacesCallback callback) {
            RectD rect = new RectD(new Vector2d(minLat, minLng), new Vector2d(maxLat - minLat, maxLng - minLng));
            foreach (KeyValuePair<ulong, RectD> cachedRect in m_CachedRects) {
                if (cachedRect.Value.Contains(rect.Min) && cachedRect.Value.Contains(rect.Max)) {
                    OnRegionPlacesFetched(m_CachedIDs[cachedRect.Key], callback);
                    return;
                }
            }

            if (Client.Network.IsConnected) {
                ulong ctx = m_Rand.NextULong();
                m_CachedRects[ctx] = rect;
                m_ActiveCallbacks[ctx] = callback;
                Client.NetFetchPlacesIDs(ctx, minLat, minLng, maxLat, maxLng, Client.FlatMap.AbsoluteZoom, OnFetchPlacesIDs);
            }
        }

        void OnFetchPlacesIDs(PacketInFetchPlacesIDs response) {
            List<ContextInfo> cInfo = new List<ContextInfo>(response.IDs.Count);
            int idx = 0;
            foreach (ulong cid in response.IDs) {
                ContextInfo info = new ContextInfo {
                    Context = response.Ctx,
                    CID = response.IDs[idx++]
                };

                cInfo.Add(info);
                m_CIDCtxIndex[cid] = info;
            }

            m_CachedIDs[response.Ctx] = cInfo;

            foreach (ulong cid in response.IDs) {
                Client.NetFetchPlace(cid, OnFetchPlace);
            }
        }

        void OnFetchPlace(PacketInFetchPlaces response) {
            m_CachedPlaces[response.Place.CIDNum] = response.Place;

            ulong ctx = m_CIDCtxIndex[response.Place.CIDNum].Context;
            if (m_ActiveCallbacks.ContainsKey(ctx)) {
                m_ActiveCallbacks[ctx](response.Place);
                m_ActiveCallbacks.Remove(ctx);
            }
        }

        void OnRegionPlacesFetched(List<ContextInfo> ids, EGRFetchPlacesCallback callback) {
            if (callback == null)
                return;

            //brought from cache
            foreach (ContextInfo info in ids) {
                callback(m_CachedPlaces[info.CID]);
            }
        }
    }
}

using MRK.Networking.Packets;
using System;
using System.Collections.Generic;

namespace MRK {
    public delegate void EGRFetchPlacesCallback(EGRPlace place);
    public delegate void EGRFetchPlacesV2Callback(List<EGRPlace> places);

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

        readonly Dictionary<int, List<EGRPlace>> m_Places;
        readonly Dictionary<int, EGRFetchPlacesV2Callback> m_ActiveRequests;

        public EGRPlaceManager() {
            m_Places = new Dictionary<int, List<EGRPlace>>();
            m_ActiveRequests = new Dictionary<int, EGRFetchPlacesV2Callback>();

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

        public void FetchPlacesInTile(MRKTileID tileID, EGRFetchPlacesV2Callback callback) {
            int hash = tileID.GetHashCode();
            if (m_Places.ContainsKey(hash)) {
                if (callback != null)
                    callback(m_Places[hash]);

                return;
            }

            if (!Client.Network.IsConnected) {
                //no internet
                return;
            }

            if (m_ActiveRequests.ContainsKey(hash)) {
                //already requested
                return;
            }

            RectD rect = MRKMapUtils.TileBounds(tileID);
            Vector2d min = MRKMapUtils.MetersToLatLon(rect.Min);
            Vector2d max = MRKMapUtils.MetersToLatLon(rect.Max);
            //for some reason min-max lat is inversed?
            if (Client.NetFetchPlacesV2(hash, max.x, min.y, min.x, max.y, Client.FlatMap.AbsoluteZoom, OnFetchPlacesV2)) {
                if (callback != null)
                    m_ActiveRequests[hash] = callback;
            }
        }

        void OnFetchPlacesV2(PacketInFetchPlacesV2 response) {
            if (m_ActiveRequests.ContainsKey(response.Hash)) {
                m_ActiveRequests[response.Hash](response.Places);
                m_ActiveRequests.Remove(response.Hash);
            }

            m_Places[response.Hash] = response.Places;

            foreach (EGRPlace p in response.Places) {
                string t = p.Types.Length > 1 ? p.Types[1].ToString() : "";
                EGRMain.Log($"Received {t} - {p}");
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

using MRK.Networking.Packets;
using System;
using System.Collections.Generic;

namespace MRK {
    public delegate void EGRFetchPlacesCallback(EGRPlace place);
    public delegate void EGRFetchPlacesV2Callback(List<EGRPlace> places, int tileHash);

    public class EGRPlaceManager : EGRBehaviour {
        struct ContextInfo {
            public ulong Context;
            public ulong CID;
        }

        class TileInfo {
            public int Hash;
            public MRKTileID ID;
            public List<EGRPlace> Places;
            public EGRFetchPlacesV2Callback ActiveCallback;
            public RectD Bounds;
            public Vector2d Minimum;
            public Vector2d Maximum;
        }

        readonly Dictionary<ulong, RectD> m_CachedRects;
        readonly Dictionary<ulong, List<ContextInfo>> m_CachedIDs;
        readonly Dictionary<ulong, EGRPlace> m_CachedPlaces;
        readonly Dictionary<ulong, EGRFetchPlacesCallback> m_ActiveCallbacks;
        readonly Dictionary<ulong, ContextInfo> m_CIDCtxIndex;
        readonly Random m_Rand;

        readonly Dictionary<int, TileInfo> m_TileInfos;
        readonly Dictionary<int, List<EGRPlace>> m_Places;
        readonly Dictionary<int, EGRFetchPlacesV2Callback> m_ActiveRequests;
        readonly Dictionary<int, string> m_TileHashes;

        public EGRPlaceManager() {
            m_TileInfos = new Dictionary<int, TileInfo>();
            m_Places = new Dictionary<int, List<EGRPlace>>();
            m_ActiveRequests = new Dictionary<int, EGRFetchPlacesV2Callback>();
            m_TileHashes = new Dictionary<int, string>();

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

        TileInfo GetTileInfo(MRKTileID id) {
            int hash = id.GetHashCode();
            if (!m_TileInfos.ContainsKey(hash)) {
                RectD bounds = MRKMapUtils.TileBounds(id);
                Vector2d min = MRKMapUtils.MetersToLatLon(bounds.Min);
                Vector2d max = MRKMapUtils.MetersToLatLon(bounds.Max);

                m_TileInfos[hash] = new TileInfo {
                    Hash = hash,
                    ID = id,
                    Places = null,
                    ActiveCallback = null,
                    Bounds = bounds,
                    Minimum = new Vector2d(max.x, min.y), //Lats are reversed
                    Maximum = new Vector2d(min.x, max.y)
                };
            }

            return m_TileInfos[hash];
        }

        public void FetchPlacesInTile(MRKTileID tileID, EGRFetchPlacesV2Callback callback) {
            TileInfo tileInfo = GetTileInfo(tileID);

            if (tileInfo.Places != null) {
                if (callback != null)
                    callback(tileInfo.Places, tileInfo.Hash);

                return;
            }

            if (!Client.Network.IsConnected) {
                //no internet
                return;
            }

            if (tileInfo.ActiveCallback != null) {
                //already requested
                return;
            }

            if (Client.NetFetchPlacesV2(tileInfo.Hash, tileInfo.Minimum.x, tileInfo.Minimum.y, tileInfo.Maximum.x, tileInfo.Maximum.y, Client.FlatMap.AbsoluteZoom, OnFetchPlacesV2)) {
                if (callback != null)
                    tileInfo.ActiveCallback = callback;
            }
        }

        void OnFetchPlacesV2(PacketInFetchPlacesV2 response) {
            TileInfo tileInfo = m_TileInfos[response.Hash];

            //nothing, we may have been discarded as you know
            //search fo2, maybe we got some CIDs pre fetched earlier or something?
            //basically carry out the EXACT server sided place check but here in our client
            //carry out a memory search
            foreach (KeyValuePair<int, TileInfo> pair in m_TileInfos) {
                //skip ourselves
                if (pair.Key == tileInfo.Hash)
                    continue;

                //skip tiles with a greater or equal zoom value
                if (pair.Value.ID.Z >= tileInfo.ID.Z)
                    continue;

                //useless shit
                if (pair.Value.Places == null || pair.Value.Places.Count == 0)
                    continue;

                Vector2d min = pair.Value.Minimum;
                Vector2d max = pair.Value.Maximum;
                //do they contain us?
                if (tileInfo.Minimum.x >= min.x && tileInfo.Minimum.y >= min.y && tileInfo.Maximum.x <= max.x && tileInfo.Maximum.y <= max.y) {
                    //yep!
                    response.Places.AddRange(pair.Value.Places);
                    //tada now we have em
                }
            }

            //TODO file search

            if (tileInfo.ActiveCallback != null) {
                tileInfo.ActiveCallback(response.Places, tileInfo.Hash);
                tileInfo.ActiveCallback = null;
            }

            tileInfo.Places = response.Places;
            //EGRMain.Log($"Tile[{response.Hash}] Hash -> " + response.TileHash);

            foreach (EGRPlace p in response.Places) {
                string t = p.Types.Length > 1 ? p.Types[1].ToString() : "";
                EGRMain.Log($"Received {t} - {p}");
            }
        }

        public List<EGRPlace> GetPlacesInTile(int tileHash) {
            TileInfo tileInfo;
            if (!m_TileInfos.TryGetValue(tileHash, out tileInfo))
                return null;

            return tileInfo.Places;
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

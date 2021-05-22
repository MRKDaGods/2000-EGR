using MRK.Networking.Packets;
using MRK.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MRK {
    public delegate void EGRFetchPlacesCallback(EGRPlace place);
    public delegate void EGRFetchPlacesV2Callback(HashSet<EGRPlace> places, int tileHash);

    public class EGRPlaceManager : EGRBehaviour {
        struct ContextInfo {
            public ulong Context;
            public ulong CID;
        }

        class TileInfo {
            public int Hash;
            public MRKTileID ID;
            public HashSet<EGRPlace> Places;
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
        readonly System.Random m_Rand;

        readonly Dictionary<int, TileInfo> m_TileInfos;

        public EGRPlaceManager() {
            m_TileInfos = new Dictionary<int, TileInfo>();

            m_CachedRects = new Dictionary<ulong, RectD>();
            m_CachedIDs = new Dictionary<ulong, List<ContextInfo>>();
            m_CachedPlaces = new Dictionary<ulong, EGRPlace>();
            m_ActiveCallbacks = new Dictionary<ulong, EGRFetchPlacesCallback>();
            m_CIDCtxIndex = new Dictionary<ulong, ContextInfo>();
            m_Rand = new System.Random();
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

            if (Client.NetFetchPlacesV2(tileInfo.Hash, tileInfo.Minimum.x, tileInfo.Minimum.y, tileInfo.Maximum.x, 
                tileInfo.Maximum.y, Client.FlatMap.AbsoluteZoom, OnFetchPlacesV2)) {
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

            //TODO optimize, use local tile zoom and search for tiles with less zoom lvls having minLatLng and max of our local

            for (int z = tileInfo.ID.Z - 1; z > 7; z--) {
                MRKTileID upperTile = MRKMapUtils.CoordinateToTileId((tileInfo.Minimum + tileInfo.Maximum) / 2f, z);
                TileInfo upperInfo;
                if (!m_TileInfos.TryGetValue(upperTile.GetHashCode(), out upperInfo))
                    continue;

                if (upperInfo.Places != null && upperInfo.Places.Count > 0) {
                    foreach (EGRPlace place in upperInfo.Places) {
                        response.Places.Add(place);
                    }
                }
            }

            /*foreach (KeyValuePair<int, TileInfo> pair in m_TileInfos) {
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
                    //response.Places.AddRange(pair.Value.Places);
                    //tada now we have em

                    for (int i = 10; i > -1; i--) {
                        if (response.Places.Count % (10 * (i + 1)) == 0) {
                            yield return new WaitForSeconds(0.5f * (i + 1));
                            break;
                        }
                    }
                }
            }*/

            //TODO file search

            if (tileInfo.ActiveCallback != null) {
                tileInfo.ActiveCallback(response.Places, tileInfo.Hash);
                tileInfo.ActiveCallback = null;
            }

            tileInfo.Places = response.Places;
            //EGRMain.Log($"Tile[{response.Hash}] Hash -> " + response.TileHash);

            //EGRMain.Log($"Processed {response.Places.Count} places");
        }

        public HashSet<EGRPlace> GetPlacesInTile(int tileHash) {
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

        public bool ShouldIncludeMarker(EGRPlaceMarker marker) {
            if (marker == null || marker.Place == null)
                return false;

            //screen space check
            Vector3 spos = marker.ScreenPoint;
            if (spos.x < 0f || spos.x > Screen.width || spos.y < 0f || spos.y > Screen.height)
                return false;

            //this should be same as server
            int zMin = 7, zMax = 21;
            //manual matching for now?
            EGRPlace place = marker.Place;
            EGRPlaceType primaryType = place.Types.Length > 1 ? place.Types[1] : EGRPlaceType.None;
            switch (primaryType) {

                default:
                case EGRPlaceType.None:
                    zMin = 15;
                    break;

                case EGRPlaceType.Mall:
                    zMin = 5;
                    zMax = 21;
                    break;

            }

            int desiredZoom = Client.FlatMap.AbsoluteZoom;
            return desiredZoom >= zMin && desiredZoom <= zMax;
        }

        void OnGUI() {
            /*EGRScreenMapInterface mi = ScreenManager.GetScreen<EGRScreenMapInterface>();
            if (mi == null || !mi.Visible)
                return;

            foreach (EGRPlaceMarker marker in mi.ActiveMarkers) {
                if (marker.Previous == null) {
                    if (marker.Next == null) //lonely ass child
                        continue;

                    EGRPlaceMarker next = marker;
                    while ((next = next.Next) != null) {
                        EGRPlaceMarker previous = next.Previous;
                        EGRGL.DrawLine(previous.ScreenPoint, next.ScreenPoint, Color.blue, 1.4f);
                    }
                }
            }*/
        }
    }
}

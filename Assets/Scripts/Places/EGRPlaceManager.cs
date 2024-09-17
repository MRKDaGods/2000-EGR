using MRK.Maps;
using MRK.Networking.Packets;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MRK {
    public delegate void EGRFetchPlacesCallback(EGRPlace place);
    public delegate void EGRFetchPlacesV2Callback(HashSet<EGRPlace> places, int tileHash);

    public class EGRPlaceManager : BaseBehaviour {
        class TileInfo {
            public int Hash;
            public TileID ID;
            public HashSet<EGRPlace> Places;
            public EGRFetchPlacesV2Callback ActiveCallback;
            public Rectd Bounds;
            public Vector2d Minimum;
            public Vector2d Maximum;
        }

        readonly Dictionary<int, TileInfo> m_TileInfos;
        readonly Dictionary<ulong, EGRPlace> m_PlaceCache;

        public EGRPlaceManager() {
            m_TileInfos = new Dictionary<int, TileInfo>();
            m_PlaceCache = new Dictionary<ulong, EGRPlace>();
        }

        TileInfo GetTileInfo(TileID id) {
            int hash = id.GetHashCode();
            if (!m_TileInfos.ContainsKey(hash)) {
                Rectd bounds = MapUtils.TileBounds(id);
                Vector2d min = MapUtils.MetersToLatLon(bounds.Min);
                Vector2d max = MapUtils.MetersToLatLon(bounds.Max);

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

        public void FetchPlacesInTile(TileID tileID, EGRFetchPlacesV2Callback callback) {
            TileInfo tileInfo = GetTileInfo(tileID);

            if (tileInfo.Places != null) {
                if (callback != null)
                    callback(tileInfo.Places, tileInfo.Hash);

                return;
            }

            if (!NetworkingClient.MainNetwork.IsConnected) {
                //no internet
                return;
            }

            if (tileInfo.ActiveCallback != null) {
                //already requested
                return;
            }

            if (NetworkingClient.MainNetworkExternal.FetchPlacesV2(tileInfo.Hash, tileInfo.Minimum.x, tileInfo.Minimum.y, tileInfo.Maximum.x,
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
                TileID upperTile = MapUtils.CoordinateToTileId((tileInfo.Minimum + tileInfo.Maximum) / 2f, z);
                TileInfo upperInfo;
                if (!m_TileInfos.TryGetValue(upperTile.GetHashCode(), out upperInfo))
                    continue;

                if (upperInfo.Places != null && upperInfo.Places.Count > 0) {
                    foreach (EGRPlace place in upperInfo.Places) {
                        if (ShouldIncludePlace(place))
                            response.Places.Add(place);
                    }
                }
            }

            //TODO file search

            if (tileInfo.ActiveCallback != null) {
                tileInfo.ActiveCallback(response.Places, tileInfo.Hash);
                tileInfo.ActiveCallback = null;
            }

            tileInfo.Places = response.Places;
        }

        public HashSet<EGRPlace> GetPlacesInTile(int tileHash) {
            TileInfo tileInfo;
            if (!m_TileInfos.TryGetValue(tileHash, out tileInfo))
                return null;

            return tileInfo.Places;
        }

        public (int, int) GetPlaceZoomBoundaries(EGRPlace place) {
            int zMin, zMax;

            EGRPlaceType primaryType = place.Types.Length > 1 ? place.Types[1] : EGRPlaceType.None;
            switch (primaryType) {

                case EGRPlaceType.Restaurant:
                    zMin = 13;
                    zMax = 20;
                    break;

                case EGRPlaceType.Delivery:
                    zMin = 16;
                    zMax = 21;
                    break;

                case EGRPlaceType.Gym:
                    zMin = 14;
                    zMax = 21;
                    break;

                case EGRPlaceType.Smoking:
                    zMin = 15;
                    zMax = 21;
                    break;

                case EGRPlaceType.Religion:
                    zMin = 12;
                    zMax = 16;
                    break;

                case EGRPlaceType.Cinema:
                    zMin = 15;
                    zMax = 21;
                    break;

                case EGRPlaceType.Park:
                    zMin = 12;
                    zMax = 21;
                    break;

                case EGRPlaceType.Mall:
                    zMin = 12;
                    zMax = 16;
                    break;

                case EGRPlaceType.Museum:
                    zMin = 11;
                    zMax = 13;
                    break;

                case EGRPlaceType.Library:
                    zMin = 11;
                    zMax = 13;
                    break;

                case EGRPlaceType.Grocery:
                    zMin = 14;
                    zMax = 21;
                    break;

                case EGRPlaceType.Apparel:
                    zMin = 14;
                    zMax = 20;
                    break;

                case EGRPlaceType.Electronics:
                    zMin = 17;
                    zMax = 21;
                    break;

                case EGRPlaceType.Sport:
                    zMin = 17;
                    zMax = 21;
                    break;

                case EGRPlaceType.BeautySupply:
                    zMin = 17;
                    zMax = 21;
                    break;

                case EGRPlaceType.Home:
                    zMin = 17;
                    zMax = 21;
                    break;

                case EGRPlaceType.CarDealer:
                    zMin = 17;
                    zMax = 21;
                    break;

                case EGRPlaceType.Convenience:
                    zMin = 18;
                    zMax = 21;
                    break;

                case EGRPlaceType.Hotel:
                    zMin = 11;
                    zMax = 15;
                    break;

                case EGRPlaceType.ATM:
                    zMin = 13;
                    zMax = 17;
                    break;

                case EGRPlaceType.Gas:
                    zMin = 11;
                    zMax = 15;
                    break;

                case EGRPlaceType.Hospital:
                    zMin = 11;
                    zMax = 15;
                    break;

                case EGRPlaceType.Pharmacy:
                    zMin = 12;
                    zMax = 16;
                    break;

                case EGRPlaceType.CarWash:
                    zMin = 18;
                    zMax = 21;
                    break;

                case EGRPlaceType.Parking:
                    zMin = 19;
                    zMax = 21;
                    break;

                case EGRPlaceType.CarRental:
                    zMin = 17;
                    zMax = 21;
                    break;

                case EGRPlaceType.BeautySalons:
                    zMin = 17;
                    zMax = 21;
                    break;

                case EGRPlaceType.EVC:
                    zMin = 18;
                    zMax = 21;
                    break;

                default:
                    zMin = 15;
                    zMax = 21;
                    break;

            }

            return (zMin, zMax);
        }

        bool ShouldIncludePlace(EGRPlace place) {
            (int, int) bounds = GetPlaceZoomBoundaries(place);

            int desiredZoom = Client.FlatMap.AbsoluteZoom;
            return desiredZoom >= bounds.Item1; // && desiredZoom <= bounds.Item2;
        }

        public bool ShouldIncludeMarker(EGRPlaceMarker marker) {
            if (marker == null || marker.Place == null)
                return false;

            //screen space check
            //Vector3 spos = marker.ScreenPoint;
            //if (spos.x < 0f || spos.x > Screen.width || spos.y < 0f || spos.y > Screen.height)
            //    return false;

            return ShouldIncludePlace(marker.Place);
        }

        public Vector2 GetOverlapCenter(EGRPlaceMarker master) {
            if (!master.IsOverlapMaster || master.Overlappers == null || master.Overlappers.Count == 0)
                return Vector2.zero;

            Vector2 avgPos = master.ScreenPoint;
            foreach (EGRPlaceMarker child in master.Overlappers) {
                avgPos += (Vector2)child.ScreenPoint; //ignore z
            }

            return avgPos / (master.Overlappers.Count + 1);
        }

        public EGRPlace GetPlaceCached(ulong cid) {
            EGRPlace place;
            m_PlaceCache.TryGetValue(cid, out place);
            return place;
        }

        public void FetchPlace(ulong cid, Action<EGRPlace> callback) {
            if (callback == null)
                return;

            if (!NetworkingClient.MainNetworkExternal.FetchPlace(cid, (response) => OnFetchPlace(response, callback))) {
                callback(null);
            }
        }

        void OnFetchPlace(PacketInFetchPlaces response, Action<EGRPlace> callback) {
            if (response.Place != null) {
                m_PlaceCache[response.Place.CIDNum] = response.Place;
            }

            callback(response.Place);
        }
    }
}

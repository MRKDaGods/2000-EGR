using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MRK.UI {
    [System.Serializable]
    public class EGRMapInterfacePlaceMarkersResources {
        public GameObject Marker;
        public GameObject Group;
    }

    public class EGRMapInterfaceComponentPlaceMarkers : EGRMapInterfaceComponent {
        Dictionary<string, EGRPlaceMarker> m_ActiveMarkers;
        ObjectPool<EGRPlaceMarker> m_MarkerPool;
        ObjectPool<EGRPlaceGroup> m_GroupPool;
        Dictionary<int, bool> m_TilePlaceFetchStates;
        HashSet<int> m_PendingDestroyedTiles;
        float m_LastOverlapSearchTime;
        EGRMapInterfacePlaceMarkersResources m_Resources;
        Dictionary<ulong, EGRPlaceGroup> m_Groups;

        public override EGRMapInterfaceComponentType ComponentType => EGRMapInterfaceComponentType.PlaceMarkers;
        public Dictionary<string, EGRPlaceMarker>.ValueCollection ActiveMarkers => m_ActiveMarkers.Values;

        public override void OnComponentInit(EGRScreenMapInterface mapInterface) {
            base.OnComponentInit(mapInterface);

            m_Resources = mapInterface.PlaceMarkersResources;
            m_ActiveMarkers = new Dictionary<string, EGRPlaceMarker>();

            m_MarkerPool = new ObjectPool<EGRPlaceMarker>(() => {
                return Object.Instantiate(m_Resources.Marker, m_Resources.Marker.transform.parent).AddComponent<EGRPlaceMarker>();
            });

            m_GroupPool = new ObjectPool<EGRPlaceGroup>(() => {
                return Object.Instantiate(m_Resources.Group, m_Resources.Group.transform.parent).AddComponent<EGRPlaceGroup>();
            });

            m_TilePlaceFetchStates = new Dictionary<int, bool>();
            m_PendingDestroyedTiles = new HashSet<int>();

            m_Resources.Marker.SetActive(false);
            m_Resources.Group.SetActive(false);

            m_Groups = new Dictionary<ulong, EGRPlaceGroup>();
        }

        public override void OnComponentShow() {
            EventManager.Register<EGREventTileDestroyed>(OnTileDestroyed);
        }

        public override void OnComponentHide() {
            EventManager.Unregister<EGREventTileDestroyed>(OnTileDestroyed);
        }

        public override void OnMapUpdated() {
            //MRKProfile.Push("groups");
            //UpdateGroups();
            //MRKProfile.Pop();
        }

        void UpdateGroups() {
            GetOverlappingMarkers();

            foreach (EGRPlaceMarker marker in ActiveMarkers) {
                marker.OverlapCheckFlag = true;

                if (!marker.IsOverlapMaster) {
                    EGRPlaceGroup group;
                    if (m_Groups.TryGetValue(marker.Place.CIDNum, out group)) {
                        FreeGroup(group);
                    }

                    continue;
                }

                ulong cid = marker.Place.CIDNum;
                if (m_Groups.ContainsKey(cid))
                    continue;

                EGRPlaceGroup _group = m_GroupPool.Rent();
                _group.SetOwner(marker);
                m_Groups[cid] = _group;
            }
        }

        public override void OnMapFullyUpdated() {
            if (Map.Zoom < 10f) {
                List<EGRPlaceMarker> buffer = new List<EGRPlaceMarker>();
                foreach (EGRPlaceMarker marker in m_ActiveMarkers.Values)
                    buffer.Add(marker);

                foreach (EGRPlaceMarker marker in buffer)
                    FreeMarker(marker);

                return;
            }

            foreach (KeyValuePair<int, bool> pair in m_TilePlaceFetchStates) {
                if (!pair.Value) {
                    return;
                }
            }

            m_TilePlaceFetchStates.Clear();

            foreach (MRKTile tile in Map.Tiles) {
                if (tile.SiblingIndex > 4)
                    continue;

                m_TilePlaceFetchStates[tile.ID.GetHashCode()] = false;
                Client.Runnable.RunLater(() => Client.PlaceManager.FetchPlacesInTile(tile.ID, OnPlacesFetched), 0.2f);
            }
        }

        public override void OnWarmup() {
            m_MarkerPool.Reserve(100);
            m_GroupPool.Reserve(100);
        }

        void OnPlacesFetched(HashSet<EGRPlace> places, int tileHash) {
            foreach (EGRPlace place in places) {
                AddMarker(place, tileHash);
            }

            m_TilePlaceFetchStates[tileHash] = true;
            foreach (KeyValuePair<int, bool> pair in m_TilePlaceFetchStates) {
                if (!pair.Value && !m_PendingDestroyedTiles.Contains(pair.Key)) {
                    return;
                }
            }

            //All places have been fetched
            //process pending destroy stuff
            lock (m_PendingDestroyedTiles) {
                foreach (int hash in m_PendingDestroyedTiles) {
                    HashSet<EGRPlace> _places = Client.PlaceManager.GetPlacesInTile(hash);
                    //no places?
                    if (_places == null || _places.Count == 0)
                        continue;

                    foreach (EGRPlace place in _places) {
                        //check if place is actually owned by tile and not superceeded by another
                        EGRPlaceMarker marker;
                        if (!m_ActiveMarkers.TryGetValue(place.CID, out marker))
                            continue;

                        //if (marker.TileHash == hash) {
                        //   FreeMarker(marker);
                        //}

                        if (!Client.PlaceManager.ShouldIncludeMarker(marker)
                            || Map.Tiles.Find(x => x.ID == MRKMapUtils.CoordinateToTileId(new Vector2d(place.Latitude, place.Longitude), Map.AbsoluteZoom)) == null) {
                            FreeMarker(marker);
                        }
                    }
                }

                /*List<EGRPlaceMarker> markers = ActiveMarkers.ToList();
                for (int i = markers.Count - 1; i > -1; i--) {
                    if (!Client.PlaceManager.ShouldIncludeMarker(markers[i])) {
                        //FreeMarker(markers[i]);
                    }
                }*/

                m_PendingDestroyedTiles.Clear();
            }

            //send updated event
            Client.Runnable.Run(UpdateMapLater(0.2f));
        }

        IEnumerator UpdateMapLater(float time) {
            yield return new WaitForSeconds(time);
            Client.FlatMap.InvokeUpdateEvent();

            UpdateGroups();
        }

        void AddMarker(EGRPlace place, int tileHash) {
            EGRPlaceMarker _marker;
            if (m_ActiveMarkers.TryGetValue(place.CID, out _marker)) {
                //we must associate each marker to a tile hash
                _marker.TileHash = tileHash;
                return;
            }

            EGRPlaceMarker marker = m_MarkerPool.Rent();
            marker.TileHash = tileHash;
            marker.SetPlace(place);
            m_ActiveMarkers[place.CID] = marker;
        }

        void FreeMarker(EGRPlaceMarker marker) {
            m_ActiveMarkers.Remove(marker.Place.CID);
            marker.TileHash = -1;

            EGRPlaceGroup group;
            if (m_Groups.TryGetValue(marker.Place.CIDNum, out group)) {
                FreeGroup(group);
            }

            marker.SetPlace(null);
            m_MarkerPool.Free(marker);
        }

        void FreeGroup(EGRPlaceGroup group) {
            m_Groups.Remove(group.Owner.Place.CIDNum);

            group.Free(() => {
                group.SetOwner(null);
                m_GroupPool.Free(group);
            });
        }

        void OnTileDestroyed(EGREventTileDestroyed evt) {
            lock (m_PendingDestroyedTiles) {
                m_PendingDestroyedTiles.Add(evt.Tile.ID.GetHashCode());
            }
        }

        void GetOverlappingMarkers() {
            if (Time.time - m_LastOverlapSearchTime < 0.1f)
                return;

            m_LastOverlapSearchTime = Time.time;

            foreach (EGRPlaceMarker marker in ActiveMarkers)
                marker.ClearOverlaps();

            foreach (EGRPlaceMarker marker in ActiveMarkers) {
                foreach (EGRPlaceMarker other in ActiveMarkers) {
                    if (marker == other)
                        continue;

                    //if (marker.RectTransform.RectOverlaps(other.RectTransform)) {
                    if (marker.RectTransform.RectOverlaps2(other.RectTransform)) {
                        marker.Overlappers.Add(other);
                        if (marker.OverlapOwner == null) {
                            marker.IsOverlapMaster = true;
                        }

                        other.OverlapOwner = marker.IsOverlapMaster ? marker : marker.OverlapOwner;
                    }
                }
            }
        }
    }
}

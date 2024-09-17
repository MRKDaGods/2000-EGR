using MRK.Events;
using MRK.Maps;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MRK.UI.MapInterface
{
    [System.Serializable]
    public class PlaceMarkersResources
    {
        public GameObject Marker;
        public GameObject Group;
    }

    public class PlaceMarkers : Component
    {
        private Dictionary<string, EGRPlaceMarker> _activeMarkers;
        private ObjectPool<EGRPlaceMarker> _markerPool;
        private ObjectPool<EGRPlaceGroup> _groupPool;
        private Dictionary<int, bool> _tilePlaceFetchStates;
        private HashSet<int> _pendingDestroyedTiles;
        private float _lastOverlapSearchTime;
        private PlaceMarkersResources _resources;
        private Dictionary<ulong, EGRPlaceGroup> _groups;

        public override ComponentType ComponentType
        {
            get
            {
                return ComponentType.PlaceMarkers;
            }
        }

        public Dictionary<string, EGRPlaceMarker>.ValueCollection ActiveMarkers
        {
            get
            {
                return _activeMarkers.Values;
            }
        }

        public override void OnComponentInit(MapInterface mapInterface)
        {
            base.OnComponentInit(mapInterface);

            _resources = mapInterface.PlaceMarkersResources;
            _activeMarkers = new Dictionary<string, EGRPlaceMarker>();

            _markerPool = new ObjectPool<EGRPlaceMarker>(() =>
            {
                return Object.Instantiate(_resources.Marker, _resources.Marker.transform.parent).AddComponent<EGRPlaceMarker>();
            });

            _groupPool = new ObjectPool<EGRPlaceGroup>(() =>
            {
                return Object.Instantiate(_resources.Group, _resources.Group.transform.parent).AddComponent<EGRPlaceGroup>();
            });

            _tilePlaceFetchStates = new Dictionary<int, bool>();
            _pendingDestroyedTiles = new HashSet<int>();

            _resources.Marker.SetActive(false);
            _resources.Group.SetActive(false);

            _groups = new Dictionary<ulong, EGRPlaceGroup>();
        }

        public override void OnComponentShow()
        {
            EventManager.Register<TileDestroyed>(OnTileDestroyed);
        }

        public override void OnComponentHide()
        {
            EventManager.Unregister<TileDestroyed>(OnTileDestroyed);
        }

        public override void OnMapUpdated()
        {
            //MRKProfile.Push("groups");
            //UpdateGroups();
            //MRKProfile.Pop();
        }

        private void UpdateGroups()
        {
            GetOverlappingMarkers();

            foreach (EGRPlaceMarker marker in ActiveMarkers)
            {
                marker.OverlapCheckFlag = true;

                if (!marker.IsOverlapMaster)
                {
                    EGRPlaceGroup group;
                    if (_groups.TryGetValue(marker.Place.CIDNum, out group))
                    {
                        FreeGroup(group);
                    }

                    continue;
                }

                ulong cid = marker.Place.CIDNum;
                if (_groups.ContainsKey(cid))
                    continue;

                EGRPlaceGroup _group = _groupPool.Rent();
                _group.SetOwner(marker);
                _groups[cid] = _group;
            }
        }

        public override void OnMapFullyUpdated()
        {
            if (Map.Zoom < 10f)
            {
                List<EGRPlaceMarker> buffer = new List<EGRPlaceMarker>();
                foreach (EGRPlaceMarker marker in _activeMarkers.Values)
                    buffer.Add(marker);

                foreach (EGRPlaceMarker marker in buffer)
                    FreeMarker(marker);

                return;
            }

            foreach (KeyValuePair<int, bool> pair in _tilePlaceFetchStates)
            {
                if (!pair.Value)
                {
                    return;
                }
            }

            _tilePlaceFetchStates.Clear();

            foreach (Tile tile in Map.Tiles)
            {
                if (tile.SiblingIndex > 4)
                {
                    continue;
                }

                _tilePlaceFetchStates[tile.ID.GetHashCode()] = false;
                Client.Runnable.RunLater(() => Client.PlaceManager.FetchPlacesInTile(tile.ID, OnPlacesFetched), 0.2f);
            }
        }

        public override void OnWarmup()
        {
            _markerPool.Reserve(100);
            _groupPool.Reserve(50);
        }

        private void OnPlacesFetched(HashSet<EGRPlace> places, int tileHash)
        {
            foreach (EGRPlace place in places)
            {
                AddMarker(place, tileHash);
            }

            _tilePlaceFetchStates[tileHash] = true;
            foreach (KeyValuePair<int, bool> pair in _tilePlaceFetchStates)
            {
                if (!pair.Value && !_pendingDestroyedTiles.Contains(pair.Key))
                {
                    return;
                }
            }

            //All places have been fetched
            //process pending destroy stuff
            lock (_pendingDestroyedTiles)
            {
                foreach (int hash in _pendingDestroyedTiles)
                {
                    HashSet<EGRPlace> _places = Client.PlaceManager.GetPlacesInTile(hash);
                    //no places?
                    if (_places == null || _places.Count == 0)
                        continue;

                    foreach (EGRPlace place in _places)
                    {
                        //check if place is actually owned by tile and not superceeded by another
                        EGRPlaceMarker marker;
                        if (!_activeMarkers.TryGetValue(place.CID, out marker))
                            continue;

                        //if (marker.TileHash == hash) {
                        //   FreeMarker(marker);
                        //}

                        if (!Client.PlaceManager.ShouldIncludeMarker(marker)
                            || Map.Tiles.Find(x => x.ID == MapUtils.CoordinateToTileId(new Vector2d(place.Latitude, place.Longitude), Map.AbsoluteZoom)) == null)
                        {
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

                _pendingDestroyedTiles.Clear();
            }

            UpdateGroups();
            //send updated event
            Client.Runnable.Run(UpdateMapLater(0.2f));
        }

        private IEnumerator UpdateMapLater(float time)
        {
            yield return new WaitForSeconds(time);
            Client.FlatMap.InvokeUpdateEvent();

            //UpdateGroups();
        }

        private void AddMarker(EGRPlace place, int tileHash)
        {
            EGRPlaceMarker _marker;
            if (_activeMarkers.TryGetValue(place.CID, out _marker))
            {
                //we must associate each marker to a tile hash
                _marker.TileHash = tileHash;
                return;
            }

            EGRPlaceMarker marker = _markerPool.Rent();
            marker.TileHash = tileHash;
            marker.SetPlace(place);
            _activeMarkers[place.CID] = marker;
        }

        private void FreeMarker(EGRPlaceMarker marker)
        {
            _activeMarkers.Remove(marker.Place.CID);
            marker.TileHash = -1;

            EGRPlaceGroup group;
            if (_groups.TryGetValue(marker.Place.CIDNum, out group))
            {
                FreeGroup(group);
            }

            marker.SetPlace(null);
            _markerPool.Free(marker);
        }

        private void FreeGroup(EGRPlaceGroup group)
        {
            _groups.Remove(group.Owner.Place.CIDNum);

            group.Free(() =>
            {
                group.SetOwner(null);
                _groupPool.Free(group);
            });
        }

        private void OnTileDestroyed(TileDestroyed evt)
        {
            lock (_pendingDestroyedTiles)
            {
                _pendingDestroyedTiles.Add(evt.Tile.ID.GetHashCode());
            }
        }

        private void GetOverlappingMarkers()
        {
            if (Time.time - _lastOverlapSearchTime < 0.1f)
                return;

            _lastOverlapSearchTime = Time.time;

            foreach (EGRPlaceMarker marker in ActiveMarkers)
                marker.ClearOverlaps();

            foreach (EGRPlaceMarker marker in ActiveMarkers)
            {
                foreach (EGRPlaceMarker other in ActiveMarkers)
                {
                    if (marker == other)
                        continue;

                    //if (marker.RectTransform.RectOverlaps(other.RectTransform)) {
                    if (marker.RectTransform.RectOverlaps2(other.RectTransform))
                    {
                        marker.Overlappers.Add(other);
                        if (marker.OverlapOwner == null)
                        {
                            marker.IsOverlapMaster = true;
                        }

                        other.OverlapOwner = marker.IsOverlapMaster ? marker : marker.OverlapOwner;
                    }
                }
            }
        }
    }
}

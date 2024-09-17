using MRK.Events;
using System.Collections.Generic;

namespace MRK.UI.MapInterface
{
    public class MapButtons : Component
    {
        private readonly Dictionary<MapButtonGroupAlignment, MapButtonGroup> _groups;
        private readonly Dictionary<MapButtonID, MapButtonInfo> _buttonsInfo;

        public override ComponentType ComponentType
        {
            get
            {
                return ComponentType.MapButtons;
            }
        }

        public MapButtons(MapButtonInfo[] buttons)
        {
            _groups = new Dictionary<MapButtonGroupAlignment, MapButtonGroup>();

            _buttonsInfo = new Dictionary<MapButtonID, MapButtonInfo>();
            foreach (MapButtonInfo info in buttons)
            {
                _buttonsInfo[info.ID] = info;
            }

            //register callbacks
            MapButtonCallbacksRegistry registry = MapButtonCallbacksRegistry.Global;
            registry[MapButtonID.Settings] = OnSettingsClick;
            registry[MapButtonID.Trending] = OnHottestTrendsClick;
            registry[MapButtonID.CurrentLocation] = OnCurrentLocationClick;
            registry[MapButtonID.Navigation] = OnNavigationClick;
            registry[MapButtonID.BackToEarth] = OnBackToEarthClick;
            registry[MapButtonID.FieldOfView] = OnSpaceFOVClick;
        }

        public override void OnComponentInit(MapInterface mapInterface)
        {
            base.OnComponentInit(mapInterface);

            foreach (MapButtonGroup group in mapInterface.GetComponentsInChildren<MapButtonGroup>())
            {
                _groups[group.GroupAlignment] = group;
            }
        }

        public override void OnComponentShow()
        {
            foreach (MapButtonGroup group in _groups.Values)
            {
                group.OnParentComponentShow();
            }

            EventManager.Register<UIMapButtonGroupExpansionStateChanged>(OnGroupExpansionStateChanged);
        }

        public override void OnComponentHide()
        {
            foreach (MapButtonGroup group in _groups.Values)
            {
                group.OnParentComponentHide();
            }

            EventManager.Unregister<UIMapButtonGroupExpansionStateChanged>(OnGroupExpansionStateChanged);
        }

        public void SetButtons(MapButtonGroupAlignment groupAlignment, HashSet<MapButtonID> ids)
        {
            MapButtonGroup group;
            if (!_groups.TryGetValue(groupAlignment, out group))
            {
                MRKLogger.LogError($"Group with alignment {groupAlignment} does not exist !!");
                return;
            }

            group.SetButtons(ids);
        }

        public void RemoveButton(MapButtonGroupAlignment groupAlignment, MapButtonID id)
        {
            MapButtonGroup group;
            if (!_groups.TryGetValue(groupAlignment, out group))
            {
                MRKLogger.LogError($"Group with alignment {groupAlignment} does not exist !!");
                return;
            }

            group.RemoveButton(id);
        }

        public void AddButton(MapButtonGroupAlignment groupAlignment, MapButtonID id, bool checkState = false, bool expand = false)
        {
            MapButtonGroup group;
            if (!_groups.TryGetValue(groupAlignment, out group))
            {
                MRKLogger.LogError($"Group with alignment {groupAlignment} does not exist !!");
                return;
            }

            group.AddButton(id, checkState: checkState, expand: expand);
        }

        public bool HasButton(MapButtonGroupAlignment groupAlignment, MapButtonID id, out MapButton button)
        {
            button = null;

            MapButtonGroup group;
            if (!_groups.TryGetValue(groupAlignment, out group))
            {
                MRKLogger.LogError($"Group with alignment {groupAlignment} does not exist !!");
                return false;
            }

            return group.HasButton(id, out button);
        }

        public MapButtonInfo GetButtonInfo(MapButtonID id)
        {
            return _buttonsInfo[id];
        }

        public void ShrinkOtherGroups(MapButtonGroup requestor)
        {
            foreach (MapButtonGroup group in _groups.Values)
            {
                if (group != requestor)
                {
                    group.SetExpanded(false);
                }
            }
        }

        public void RemoveAllButtons()
        {
            foreach (MapButtonGroup group in _groups.Values)
            {
                group.SetButtons(null);
            }
        }

        public void SetGroupExpansionState(MapButtonGroupAlignment groupAlignment, bool expanded)
        {
            MapButtonGroup group;
            if (!_groups.TryGetValue(groupAlignment, out group))
            {
                MRKLogger.LogError($"Group with alignment {groupAlignment} does not exist !!");
                return;
            }

            group.SetExpanded(expanded);
        }

        private void OnGroupExpansionStateChanged(UIMapButtonGroupExpansionStateChanged evt)
        {
            if (Client.MapMode != EGRMapMode.Globe)
                return;

            if (evt.Group.GroupAlignment == MapButtonGroupAlignment.BottomRight)
            {
                if (evt.Expanded)
                {
                    if (HasButton(MapButtonGroupAlignment.BottomCenter, MapButtonID.BackToEarth, out _))
                    {
                        RemoveButton(MapButtonGroupAlignment.BottomCenter, MapButtonID.BackToEarth);
                        AddButton(MapButtonGroupAlignment.BottomRight, MapButtonID.BackToEarth, true);
                    }
                }
                else
                {
                    if (HasButton(MapButtonGroupAlignment.BottomRight, MapButtonID.BackToEarth, out _))
                    {
                        RemoveButton(MapButtonGroupAlignment.BottomRight, MapButtonID.BackToEarth);

                        if (!MapInterface.IsObservedTransformEarth())
                        {
                            AddButton(MapButtonGroupAlignment.BottomCenter, MapButtonID.BackToEarth);
                        }
                    }
                }
            }
        }

        private void OnHottestTrendsClick()
        {
            ScreenManager.GetScreen<HottestTrends>().ShowScreen((Screen)MapInterface);
        }

        private void OnSettingsClick()
        {
            Screen screen = Client.MapMode == EGRMapMode.Globe ? ScreenManager.GetScreen<GlobeSettings>()
                : (Screen)ScreenManager.GetScreen<MapSettings>();

            screen.ShowScreen((Screen)MapInterface);
        }

        private void OnNavigationClick()
        {
            void EnterNavigation()
            {
                Client.FlatCamera.EnterNavigation();
                MapInterface.Components.Navigation.Show();
            }

            if (Client.MapMode == EGRMapMode.Globe)
            {
                Client.GlobeCamera.SwitchToFlatMapExternal(() =>
                {
                    Client.Runnable.RunLater(EnterNavigation, 0.2f);
                });
            }
            else
            {
                EnterNavigation();
            }
        }

        private void OnCurrentLocationClick()
        {
            if (Client.MapMode == EGRMapMode.Flat)
            {
                Client.LocationManager.RequestCurrentLocation(false, true, true);
            }
            else if (Client.MapMode == EGRMapMode.Globe)
            {
                void action()
                {
                    Client.GlobeCamera.SwitchToFlatMapExternal(() =>
                    {
                        Client.Runnable.RunLater(() =>
                        {
                            Client.LocationManager.RequestCurrentLocation(false, true, true);
                        }, 0.2f);
                    });
                }

                if (!MapInterface.IsObservedTransformEarth())
                {
                    MapInterface.SetObservedTransformToEarth(action);
                }
                else
                {
                    action();
                }
            }
        }

        private void OnBackToEarthClick()
        {
            if (Client.MapMode == EGRMapMode.Globe)
            {
                if (MapInterface.ObservedTransform != Client.GlobalMap.transform)
                {
                    MapInterface.SetObservedTransformNameState(false);

                    //set back to earth
                    MapInterface.ObservedTransform = Client.GlobalMap.transform;
                    MapInterface.ObservedTransformDirty = true;
                    MapInterface.OnObservedTransformChanged();
                }
            }
            else if (Client.MapMode == EGRMapMode.Flat)
            {
                Client.FlatCamera.SwitchToGlobe();
            }
        }

        private void OnSpaceFOVClick()
        {
            ScreenManager.GetScreen<SpaceFOV>().ShowScreen();
        }
    }
}

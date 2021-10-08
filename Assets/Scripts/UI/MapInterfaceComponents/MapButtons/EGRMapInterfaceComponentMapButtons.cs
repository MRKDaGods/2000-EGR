using System;
using System.Collections.Generic;

namespace MRK.UI.MapInterface {
    public class EGRMapInterfaceComponentMapButtons : EGRMapInterfaceComponent {
        readonly Dictionary<EGRUIMapButtonsGroupAlignment, EGRUIMapButtonsGroup> m_Groups;
        readonly Dictionary<EGRUIMapButtonID, EGRUIMapButtonInfo> m_ButtonsInfo;

        public override EGRMapInterfaceComponentType ComponentType => EGRMapInterfaceComponentType.MapButtons;

        public EGRMapInterfaceComponentMapButtons(EGRUIMapButtonInfo[] buttons) {
            m_Groups = new Dictionary<EGRUIMapButtonsGroupAlignment, EGRUIMapButtonsGroup>();

            m_ButtonsInfo = new Dictionary<EGRUIMapButtonID, EGRUIMapButtonInfo>();
            foreach (EGRUIMapButtonInfo info in buttons) {
                m_ButtonsInfo[info.ID] = info;
            }

            //register callbacks
            MRKRegistryUIMapButtonCallbacks registry = MRKRegistryUIMapButtonCallbacks.Global;
            registry[EGRUIMapButtonID.Settings] = OnSettingsClick;
            registry[EGRUIMapButtonID.Trending] = OnHottestTrendsClick;
            registry[EGRUIMapButtonID.CurrentLocation] = OnCurrentLocationClick;
            registry[EGRUIMapButtonID.Navigation] = OnNavigationClick;
            registry[EGRUIMapButtonID.BackToEarth] = OnBackToEarthClick;
            registry[EGRUIMapButtonID.FieldOfView] = OnSpaceFOVClick;
        }

        public override void OnComponentInit(EGRScreenMapInterface mapInterface) {
            base.OnComponentInit(mapInterface);

            foreach (EGRUIMapButtonsGroup group in mapInterface.GetComponentsInChildren<EGRUIMapButtonsGroup>()) {
                m_Groups[group.GroupAlignment] = group;
            }
        }

        public override void OnComponentShow() {
            foreach (EGRUIMapButtonsGroup group in m_Groups.Values) {
                group.OnParentComponentShow();
            }

            EventManager.Register<EGREventUIMapButtonGroupExpansionStateChanged>(OnGroupExpansionStateChanged);
        }

        public override void OnComponentHide() {
            foreach (EGRUIMapButtonsGroup group in m_Groups.Values) {
                group.OnParentComponentHide();
            }

            EventManager.Unregister<EGREventUIMapButtonGroupExpansionStateChanged>(OnGroupExpansionStateChanged);
        }

        public void SetButtons(EGRUIMapButtonsGroupAlignment groupAlignment, HashSet<EGRUIMapButtonID> ids) {
            EGRUIMapButtonsGroup group;
            if (!m_Groups.TryGetValue(groupAlignment, out group)) {
                MRKLogger.LogError($"Group with alignment {groupAlignment} does not exist !!");
                return;
            }

            group.SetButtons(ids);
        }

        public void RemoveButton(EGRUIMapButtonsGroupAlignment groupAlignment, EGRUIMapButtonID id) {
            EGRUIMapButtonsGroup group;
            if (!m_Groups.TryGetValue(groupAlignment, out group)) {
                MRKLogger.LogError($"Group with alignment {groupAlignment} does not exist !!");
                return;
            }

            group.RemoveButton(id);
        }

        public void AddButton(EGRUIMapButtonsGroupAlignment groupAlignment, EGRUIMapButtonID id, bool checkState = false, bool expand = false) {
            EGRUIMapButtonsGroup group;
            if (!m_Groups.TryGetValue(groupAlignment, out group)) {
                MRKLogger.LogError($"Group with alignment {groupAlignment} does not exist !!");
                return;
            }

            group.AddButton(id, checkState: checkState, expand: expand);
        }

        public bool HasButton(EGRUIMapButtonsGroupAlignment groupAlignment, EGRUIMapButtonID id, out EGRUIMapButton button) {
            button = null;

            EGRUIMapButtonsGroup group;
            if (!m_Groups.TryGetValue(groupAlignment, out group)) {
                MRKLogger.LogError($"Group with alignment {groupAlignment} does not exist !!");
                return false;
            }

            return group.HasButton(id, out button);
        }

        public EGRUIMapButtonInfo GetButtonInfo(EGRUIMapButtonID id) {
            return m_ButtonsInfo[id];
        }

        public void ShrinkOtherGroups(EGRUIMapButtonsGroup requestor) {
            foreach (EGRUIMapButtonsGroup group in m_Groups.Values) {
                if (group != requestor) {
                    group.SetExpanded(false);
                }
            }
        }

        public void RemoveAllButtons() {
            foreach (EGRUIMapButtonsGroup group in m_Groups.Values) {
                group.SetButtons(null);
            }
        }

        public void SetGroupExpansionState(EGRUIMapButtonsGroupAlignment groupAlignment, bool expanded) {
            EGRUIMapButtonsGroup group;
            if (!m_Groups.TryGetValue(groupAlignment, out group)) {
                MRKLogger.LogError($"Group with alignment {groupAlignment} does not exist !!");
                return;
            }

            group.SetExpanded(expanded);
        }

        void OnGroupExpansionStateChanged(EGREventUIMapButtonGroupExpansionStateChanged evt) {
            if (Client.MapMode != EGRMapMode.Globe)
                return;

            if (evt.Group.GroupAlignment == EGRUIMapButtonsGroupAlignment.BottomRight) {
                if (evt.Expanded) {
                    if (HasButton(EGRUIMapButtonsGroupAlignment.BottomCenter, EGRUIMapButtonID.BackToEarth, out _)) {
                        RemoveButton(EGRUIMapButtonsGroupAlignment.BottomCenter, EGRUIMapButtonID.BackToEarth);
                        AddButton(EGRUIMapButtonsGroupAlignment.BottomRight, EGRUIMapButtonID.BackToEarth, true);
                    }
                }
                else {
                    if (HasButton(EGRUIMapButtonsGroupAlignment.BottomRight, EGRUIMapButtonID.BackToEarth, out _)) {
                        RemoveButton(EGRUIMapButtonsGroupAlignment.BottomRight, EGRUIMapButtonID.BackToEarth);

                        if (!MapInterface.IsObservedTransformEarth()) {
                            AddButton(EGRUIMapButtonsGroupAlignment.BottomCenter, EGRUIMapButtonID.BackToEarth);
                        }
                    }
                }
            }
        }

        void OnHottestTrendsClick() {
            ScreenManager.GetScreen<EGRScreenHottestTrends>().ShowScreen(MapInterface);
        }

        void OnSettingsClick() {
            EGRScreen screen = Client.MapMode == EGRMapMode.Globe ? ScreenManager.GetScreen<EGRScreenOptionsGlobeSettings>() 
                : (EGRScreen)ScreenManager.GetScreen<EGRScreenOptionsMapSettings>();

            screen.ShowScreen(MapInterface);
        }

        void OnNavigationClick() {
            void EnterNavigation() {
                Client.FlatCamera.EnterNavigation();
                MapInterface.Components.Navigation.Show();
            }

            if (Client.MapMode == EGRMapMode.Globe) {
                Client.GlobeCamera.SwitchToFlatMapExternal(() => {
                    Client.Runnable.RunLater(EnterNavigation, 0.2f);
                });
            }
            else {
                EnterNavigation();
            }
        }

        void OnCurrentLocationClick() {
            if (Client.MapMode == EGRMapMode.Flat) {
                Client.LocationManager.RequestCurrentLocation(false, true, true);
            }
            else if (Client.MapMode == EGRMapMode.Globe) {
                void action() => Client.GlobeCamera.SwitchToFlatMapExternal(() => {
                    Client.Runnable.RunLater(() => {
                        Client.LocationManager.RequestCurrentLocation(false, true, true);
                    }, 0.2f);
                });

                if (!MapInterface.IsObservedTransformEarth()) {
                    MapInterface.SetObservedTransformToEarth(action);
                }
                else {
                    action();
                }
            }
        }

        void OnBackToEarthClick() {
            if (Client.MapMode == EGRMapMode.Globe) {
                if (MapInterface.ObservedTransform != Client.GlobalMap.transform) {
                    MapInterface.SetObservedTransformNameState(false);

                    //set back to earth
                    MapInterface.ObservedTransform = Client.GlobalMap.transform;
                    MapInterface.ObservedTransformDirty = true;
                    MapInterface.OnObservedTransformChanged();
                }
            }
            else if (Client.MapMode == EGRMapMode.Flat) {
                Client.FlatCamera.SwitchToGlobe();
            }
        }

        void OnSpaceFOVClick() {
            ScreenManager.GetScreen<EGRScreenSpaceFOV>().ShowScreen();
        }
    }
}

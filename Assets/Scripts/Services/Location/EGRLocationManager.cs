using UnityEngine;
using UnityEngine.UI;

namespace MRK {
    public class EGRLocationManager : EGRBehaviour {
        const float LOCATION_REQUEST_DELAY = 0f; //0.5f

        Image m_CurrentLocationSprite;
        bool m_IsActive;
        bool m_RequestingLocation;
        float m_LastLocationRequestTime;
        Vector2d? m_LastFetchedCoords;
        float? m_LastFetchedBearing;

        public bool AllowMapRotation { get; set; }

        void Start() {
            m_CurrentLocationSprite = ScreenManager.MapInterface.MapInterfaceResources.CurrentLocationSprite;
            m_CurrentLocationSprite.gameObject.SetActive(false);

            Client.RegisterMapModeDelegate(OnMapModeChanged);
            Client.FlatMap.OnMapUpdated += OnMapUpdated;
        }

        void OnDestroy() {
            Client.UnregisterMapModeDelegate(OnMapModeChanged);
            Client.FlatMap.OnMapUpdated -= OnMapUpdated;
        }

        void OnMapUpdated() {
            if (m_IsActive && m_LastFetchedCoords.HasValue) {
                Vector3 pos = Client.FlatMap.GeoToWorldPosition(m_LastFetchedCoords.Value);
                Vector3 spos = Client.ActiveCamera.WorldToScreenPoint(pos);
                m_CurrentLocationSprite.transform.position = EGRPlaceMarker.ScreenToMarkerSpace(spos);
                m_CurrentLocationSprite.transform.localScale = Vector3.one *
                    ScreenManager.MapInterface.MapInterfaceResources.CurrentLocationScaleCurve.Evaluate(Client.FlatMap.Zoom / 21f);

                m_CurrentLocationSprite.transform.rotation = Quaternion.Euler(Quaternion.Euler(0f, 0f, m_LastFetchedBearing.Value).eulerAngles 
                    - Quaternion.Euler(-90f, 0f, -Client.FlatCamera.MapRotation.y).eulerAngles);

                //float show = 0f;
            }
        }

        void OnMapModeChanged(EGRMapMode mode) {
            if (mode != EGRMapMode.Flat) {
                DeActivate();
            }
            else {
                RequestCurrentLocation(true);
            }
        }

        void DeActivate() {
            if (!m_IsActive)
                return;

            m_IsActive = false;
            m_CurrentLocationSprite.gameObject.SetActive(false);
        }

        void ActivateIfNeeded() {
            if (m_IsActive)
                return;

            m_IsActive = true;
            m_CurrentLocationSprite.gameObject.SetActive(true);
        }

        void OnReceiveLocation(bool success, Vector2d? coords, float? bearing) {
            m_RequestingLocation = false;

            if (!success) {
                DeActivate();
                return;
            }

            ActivateIfNeeded();

            m_LastFetchedCoords = coords.Value;
            m_LastFetchedBearing = bearing.Value;

            OnMapUpdated(); //position marker

            if (AllowMapRotation) {
                Client.FlatCamera.SetRotation(new Vector2(0f, bearing.Value));
            }

            /*if (Client.NavigationManager.CurrentDirections.HasValue) {
                TestLineSegs();
            } */
        }

        void Update() {
            if (!m_IsActive)
                return;

            if (Client.NavigationManager.IsNavigating) {
                DeActivate();
                return;
            }

            RequestCurrentLocation();

            //assuming MRK
            if (Client.InputModel.NeedsUpdate) {
                OnMapUpdated();
            }
        }

        public void RequestCurrentLocation(bool silent = false, bool force = false, bool teleport = false) {
            if (!force && (Time.time - m_LastLocationRequestTime < LOCATION_REQUEST_DELAY || m_RequestingLocation)) {
                goto __teleport;
            }

            m_RequestingLocation = true;
            m_LastLocationRequestTime = Time.time;
            Client.LocationService.GetCurrentLocation(OnReceiveLocation, silent);

        __teleport:
            if (teleport && m_LastFetchedCoords.HasValue) {
                Client.FlatCamera.SetCenterAndZoom(m_LastFetchedCoords.Value, 17f);
            }
        }

        /*Vector2d? _interLatLng;

        void TestLineSegs() {
            Vector2d current = MRKMapUtils.LatLonToMeters(m_LastFetchedCoords.Value);

            Vector2d start = MRKMapUtils.LatLonToMeters(Client.NavigationManager.CurrentRoute.Geometry.Coordinates[0]);
            Vector2d end = MRKMapUtils.LatLonToMeters(Client.NavigationManager.CurrentRoute.Geometry.Coordinates[1]);

            //slope of line segment
            double m = (end.y - start.y) / (end.x - start.x);

            //y = mx + c
            //c = y - mx
            double c = start.y - m * start.x;

            //neg reciprocal (normal slope) of line segment
            //double normalM = -1d / m;

            //y - y'  -1
            //      =
            //x - x'   m
            //-x + x'= m(y - y')
            //(x' - x) / m + y' = y
            //(x' / m) - (x / m) + y' = y
            //c = (x' / m) + y'
            double normalC = current.x / m + current.y;
            //y = (-1 / m)x + normalC
            //(-1 / m)x + normalC = mx + c
            //(-1 / m)x - mx = c - normalC
            //x(-1 / m - m) = c - normalC
            //x = (c - normalC) / (-1 / m - m)
            double x = (c - normalC) / (-1d / m - m); //intersection x
            double y = m * x + c; //intersection y

            _interLatLng = MRKMapUtils.MetersToLatLon(new Vector2d(x, y));
            Debug.Log($"intersection={_interLatLng}");
        }

        void OnGUI() {
           if (_interLatLng.HasValue) {
                MRKMap map = Client.FlatMap;
                Vector3 worldPos = map.GeoToWorldPosition(_interLatLng.Value);
                Vector3 worldPosCur = map.GeoToWorldPosition(m_LastFetchedCoords.Value);

                Vector3 sPos = Client.ActiveCamera.WorldToScreenPoint(worldPos);
                Vector3 sPosCur = Client.ActiveCamera.WorldToScreenPoint(worldPosCur);

                sPos.y = Screen.height - sPos.y;
                sPosCur.y = Screen.height - sPosCur.y;

                EGRGL.DrawLine(sPos, sPosCur, Color.blue, 2f);
            }
        }*/
    }
}

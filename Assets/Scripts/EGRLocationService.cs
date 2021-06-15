using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Android;
using MRK.UI;
using static MRK.EGRLanguageManager;

namespace MRK {
    public enum EGRLocationError {
        None,
        NotEnabled,
        Denied,
        Failed,
        TimeOut
    }

    public class MRKAngleSmoother {
        readonly CircularBuffer<double> m_Angles;
        double m_Alpha;

        public MRKAngleSmoother(int measurements) {
            m_Angles = new CircularBuffer<double>(measurements);
            m_Alpha = 2d / (double)(measurements + 1);
        }

        public void Add(double angle) {
            angle = angle < 0 ? angle + 360 : angle >= 360 ? angle - 360 : angle;
            m_Angles.Add(angle);
        }

        public double GetAngle() {
            double[] angles = m_Angles.Reverse().ToArray();

            double sin = Math.Sin(angles[0] * Mathd.Deg2Rad);
            double cos = Math.Cos(angles[0] * Mathd.Deg2Rad);

            for (int i = 1; i < angles.Length; i++) {
                sin = (Math.Sin(angles[i] * Mathd.Deg2Rad) - sin) * m_Alpha + sin;
                cos = (Math.Cos(angles[i] * Mathd.Deg2Rad) - cos) * m_Alpha + cos;
            }

            double finalAngle = Math.Round(Math.Atan2(sin, cos) * Mathd.Rad2Deg, 2);
            finalAngle = finalAngle < 0 ? finalAngle + 360 : finalAngle >= 360 ? finalAngle - 360 : finalAngle;
            return finalAngle;
        }
    }

    public class EGRLocationService : EGRBehaviour {
        class PermissionAwaiter {
            int m_Count;
            int m_Value;

            public int Count {
                get => m_Count;
                set {
                    m_Value = 0;
                    m_Count = value;
                }
            }
            public bool IsWaiting => m_Count > m_Value;

            public IEnumerator Await() {
                while (IsWaiting)
                    yield return new WaitForSeconds(0.2f);
            }

            public void Increment() {
                m_Value++;
            }
        }

        const int MAX_POSITION_COUNT = 5;
        const int ANGLE_MEASUREMENT_COUNT = 5;

        bool m_Initialized;
        readonly static string[] ms_Permissions;
        readonly PermissionCallbacks m_PermissionCallbacks;
        readonly PermissionAwaiter m_PermissionAwaiter;
        bool m_PermissionDenied;
        MRKRunnable m_Runnable;
        EGRLocationServiceSimulator m_Simulator;
        readonly CircularBuffer<Vector2d> m_LastPositions;
        readonly MRKAngleSmoother m_RawCompassSmoothing;
        readonly MRKAngleSmoother m_HighPrecisionCompassSmoothing;
        double? m_LastUncertainDistance;

        public EGRLocationError LastError { get; private set; }

        static EGRLocationService() {
            ms_Permissions = new string[] {
                Permission.CoarseLocation,
                Permission.FineLocation
            };
        }

        public EGRLocationService() {
            m_PermissionCallbacks = new PermissionCallbacks();
            m_PermissionCallbacks.PermissionGranted += OnPermissionGranted;
            m_PermissionCallbacks.PermissionDenied += OnPermissionDenied;
            m_PermissionCallbacks.PermissionDeniedAndDontAskAgain += OnPermissionDeniedAndDontAskAgain;

            m_PermissionAwaiter = new PermissionAwaiter();

            m_LastPositions = new CircularBuffer<Vector2d>(MAX_POSITION_COUNT);
            m_RawCompassSmoothing = new MRKAngleSmoother(ANGLE_MEASUREMENT_COUNT);
            m_HighPrecisionCompassSmoothing = new MRKAngleSmoother(ANGLE_MEASUREMENT_COUNT);
        }

        void Start() {
            m_Runnable = gameObject.AddComponent<MRKRunnable>();
        }

        void OnPermissionGranted(string perm) {
            m_PermissionAwaiter.Increment();
        }

        void OnPermissionDenied(string perm) {
            m_PermissionDenied = true;
            m_PermissionAwaiter.Increment();
        }

        void OnPermissionDeniedAndDontAskAgain(string perm) {
            m_PermissionDenied = true;
            PlayerPrefs.SetInt($"EGR_PERM_{perm}", 1);
            m_PermissionAwaiter.Increment();
        }

        bool IsPermissionRestricted(string perm) {
            return PlayerPrefs.GetInt($"EGR_PERM_{perm}", 0) == 1;
        }

        IEnumerator Initialize(Action callback, bool silent) {
            Debug.Log("Initializing...");
            LastError = EGRLocationError.None;

            List<string> perms = null;

            if (Application.platform == RuntimePlatform.Android) {
            __request:
                if (perms == null)
                    perms = ListPool<string>.Default.Rent();
                else
                    perms.Clear();

                foreach (string perm in ms_Permissions) {
                    if (!Permission.HasUserAuthorizedPermission(perm)) {
                        if (IsPermissionRestricted(perm)) {
                            if (!silent) {
                                EGRPopupConfirmation popup = Client.ScreenManager.GetPopup<EGRPopupConfirmation>();
                                popup.SetYesButtonText(Localize(EGRLanguageData.SETTINGS));
                                popup.SetNoButtonText(Localize(EGRLanguageData.CANCEL));
                                popup.ShowPopup(
                                    Localize(EGRLanguageData.EGR),
                                    Localize(EGRLanguageData.LOCATION_PERMISSION_MUST_BE_ENABLED_TO_BE_ABLE_TO_USE_CURRENT_LOCATION),
                                    (p, res) => {
                                        if (res == EGRPopupResult.YES) {
                                            AndroidRuntimePermissions.OpenSettings();
                                        }
                                    },
                                    Client.ActiveScreens[0]);
                            }
                            else {
                                LastError = EGRLocationError.Denied;
                            }

                            goto __exit;
                        }

                        perms.Add(perm);
                    }
                }

                if (perms.Count > 0) {
                    if (!silent) {
                        m_PermissionDenied = false;
                        m_PermissionAwaiter.Count = perms.Count;
                        Permission.RequestUserPermissions(perms.ToArray(), m_PermissionCallbacks);

                        yield return m_PermissionAwaiter.Await();

                        if (m_PermissionDenied) {
                            Reference<EGRPopupResult?> result = new Reference<EGRPopupResult?>();

                            EGRPopupConfirmation popup = Client.ScreenManager.GetPopup<EGRPopupConfirmation>();
                            popup.SetYesButtonText(Localize(EGRLanguageData.ENABLE));
                            popup.SetNoButtonText(Localize(EGRLanguageData.CANCEL));
                            popup.ShowPopup(
                                Localize(EGRLanguageData.EGR),
                                Localize(EGRLanguageData.LOCATION_PERMISSION_MUST_BE_ENABLED_TO_BE_ABLE_TO_USE_CURRENT_LOCATION),
                                (p, res) => {
                                    result.Value = res;
                                },
                                Client.ActiveScreens[0]);

                            while (!result.Value.HasValue)
                                yield return new WaitForSeconds(0.2f);

                            if (result.Value == EGRPopupResult.YES)
                                goto __request;

                            LastError = EGRLocationError.Denied;
                            goto __exit;
                        }
                    }
                    else {
                        LastError = EGRLocationError.Denied;
                        goto __exit;
                    }
                }
            }

            if (!Input.location.isEnabledByUser) {
                LastError = EGRLocationError.NotEnabled;

                if (!silent) {
                    Client.ScreenManager.GetPopup<EGRPopupMessageBox>().ShowPopup(
                        Localize(EGRLanguageData.EGR), 
                        Localize(EGRLanguageData.LOCATION_MUST_BE_ENABLED_TO_BE_ABLE_TO_USE_CURRENT_LOCATION), 
                        null, Client.ActiveScreens[0]);
                }

                goto __exit;
            }

            Debug.Log("Starting");

            Input.location.Start();
            float elapsed = 0f;
            while (Input.location.status == LocationServiceStatus.Initializing) {
                yield return new WaitForSeconds(0.2f);

                elapsed += 0.2f;
                if (elapsed > 10f) {
                    LastError = EGRLocationError.TimeOut;
                    goto __exit;
                }
            }

            if (Input.location.status == LocationServiceStatus.Failed) {
                LastError = EGRLocationError.Failed;
                goto __exit;
            }

            Debug.Log("SUCCESS");

            Debug.Log("Starting NT location");
            NativeToolkit.StartLocation();

            elapsed = 0f;
            bool ntSuccess = false;
            while (true) {
                bool error = false;

                try {
                    NativeToolkit.GetLatitude();
                }
                catch {
                    error = true;
                }
                finally {
                    if (!error)
                        ntSuccess = true;
                }

                if (ntSuccess) {
                    break;
                }

                yield return new WaitForSeconds(0.2f);

                elapsed += 0.2f;
                if (elapsed > 10f) {
                    LastError = EGRLocationError.TimeOut;
                    goto __exit;
                }
            }

            Debug.Log("SUCCESS NT");

            Input.compass.enabled = true;
            m_Initialized = true;

        __exit:
            if (perms != null) {
                ListPool<string>.Default.Free(perms);
            }

            callback();
        }

        public void GetCurrentLocation(Action<bool, Vector2d?, float?> callback, bool silent = false) {
#if UNITY_EDITOR
            if (m_Simulator == null) {
                m_Simulator = gameObject.AddComponent<EGRLocationServiceSimulator>();
            }

            callback(true, m_Simulator.Coords, m_Simulator.Bearing);
            return;
#endif

            if (m_Runnable.Count > 0 && !m_Initialized)
                return;

            if (m_Initialized && (!Input.location.isEnabledByUser || Input.location.status != LocationServiceStatus.Running)) {
                m_Initialized = false;
                m_Runnable.StopAll();
            }

            if (!m_Initialized) {
                m_Runnable.Run(Initialize(() => {
                    if (LastError == EGRLocationError.None)
                        GetCurrentLocation(callback);
                    else
                        callback(false, null, null);
                }, silent));
                return;
            }

            float bearing = Input.compass.trueHeading;
            double lat = NativeToolkit.GetLatitude();
            double lng = NativeToolkit.GetLongitude();

            if (m_LastPositions.Count > 0) {
                CheapRuler cheapRuler = new CheapRuler(lat, CheapRulerUnits.Meters);
                double distance = cheapRuler.Distance(
                    new double[] { lng, lat },
                    new double[] { m_LastPositions[0].y, m_LastPositions[0].x }
                );

                if (distance > 1d) {
                    if (distance > 10d) {
                        if (m_LastUncertainDistance.HasValue) {
                            m_LastUncertainDistance = null;
                            m_LastPositions.Add(new Vector2d(lat, lng));
                        }
                        else {
                            m_LastUncertainDistance = distance;
                            lat = m_LastPositions[0].x;
                            lng = m_LastPositions[0].y;
                        }
                    }
                    else
                        m_LastPositions.Add(new Vector2d(lat, lng));
                }
            }
            else {
                m_LastPositions.Add(new Vector2d(lat, lng));
            }
            
            if (m_LastPositions.Count == MAX_POSITION_COUNT) {
                Vector2d newestPos = m_LastPositions[0];
                Vector2d oldestPos = m_LastPositions[MAX_POSITION_COUNT - 1];
                CheapRuler cheapRuler = new CheapRuler(newestPos.x, CheapRulerUnits.Meters);

                double distance = cheapRuler.Distance(
                    new double[] { newestPos.y, newestPos.x },
                    new double[] { oldestPos.y, oldestPos.x }
                );

                if (distance >= 1.5d) {
                    float[] lastHeadings = new float[MAX_POSITION_COUNT - 1];

                    for (int i = 1; i < MAX_POSITION_COUNT; i++) {
                        // atan2 increases angle CCW, flip sign of latDiff to get CW
                        double latDiff = -(m_LastPositions[i].x - m_LastPositions[i - 1].x);
                        double lngDiff = m_LastPositions[i].y - m_LastPositions[i - 1].y;
                        // +90.0 to make top (north) 0°
                        double heading = (Math.Atan2(latDiff, lngDiff) * 180d / Math.PI) + 90d;
                        // stay within [0..360]° range
                        if (heading < 0) { 
                            heading += 360; 
                        }
                        if (heading >= 360) {
                            heading -= 360; 
                        }

                        lastHeadings[i - 1] = (float)heading;
                    }

                    m_HighPrecisionCompassSmoothing.Add(lastHeadings[0]);
                    float finalHeading = (float)m_HighPrecisionCompassSmoothing.GetAngle();

                    //fix heading to have 0° for north, 90° for east, 180° for south and 270° for west
                    finalHeading = finalHeading >= 180f ? finalHeading - 180f : finalHeading + 180f;
                    bearing = finalHeading;
                }
            }
            else {
                m_RawCompassSmoothing.Add(bearing);
                bearing = (float)m_RawCompassSmoothing.GetAngle();
            }

            callback(true, new Vector2d(lat, lng), bearing);
        }
    }
}

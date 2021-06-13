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

        bool m_Initialized;
        readonly static string[] ms_Permissions;
        readonly PermissionCallbacks m_PermissionCallbacks;
        readonly PermissionAwaiter m_PermissionAwaiter;
        bool m_PermissionDenied;
        MRKRunnable m_Runnable;

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

        IEnumerator Initialize(Action callback) {
            Debug.Log("Initializing...");
            LastError = EGRLocationError.None;

            if (Application.platform == RuntimePlatform.Android) {
            __request:
                List<string> perms = new List<string>();

                foreach (string perm in ms_Permissions) {
                    if (!Permission.HasUserAuthorizedPermission(perm)) {
                        if (IsPermissionRestricted(perm)) {
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

                            goto __exit;
                        }

                        perms.Add(perm);
                    }
                }

                if (perms.Count > 0) {
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
            }

            if (!Input.location.isEnabledByUser) {
                LastError = EGRLocationError.NotEnabled;
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
            callback();
        }

        public void GetCurrentLocation(Action<bool, Vector2d?, float?> callback) {
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
                }));
                return;
            }

            float bearing = Input.compass.trueHeading;
            double lat = NativeToolkit.GetLatitude();
            double lng = NativeToolkit.GetLongitude();
            Debug.Log($"Found loc {lat}, {lng} @{bearing}");
            callback(true, new Vector2d(lat, lng), bearing);
        }
    }
}

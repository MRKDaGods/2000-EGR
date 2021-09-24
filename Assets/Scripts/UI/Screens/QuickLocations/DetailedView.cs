using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MRK.EGRLanguageManager;

namespace MRK.UI {
    public partial class EGRScreenQuickLocations : EGRScreen {
        class DetailedView : MRKBehaviourPlain {
            RectTransform m_Transform;
            TMP_InputField m_Name;
            TMP_Dropdown m_Type;
            TextMeshProUGUI m_Date;
            TextMeshProUGUI m_Distance;
            Button m_Save;
            Button m_Delete;
            EGRQuickLocation m_Location;
            CanvasGroup m_CanvasGroup;

            public DetailedView(RectTransform transform) {
                m_Transform = transform;
                m_Name = transform.GetElement<TMP_InputField>("Layout/Name/Input");
                m_Name.onValueChanged.AddListener((_) => UpdateSaveButtonInteractibility());

                m_Type = transform.GetElement<TMP_Dropdown>("Layout/Type/Dropdown");
                m_Type.onValueChanged.AddListener((_) => UpdateSaveButtonInteractibility());

                m_Date = transform.GetElement<TextMeshProUGUI>("Layout/Date/Val");
                m_Distance = transform.GetElement<TextMeshProUGUI>("Layout/Dist/Val");

                m_Save = transform.GetElement<Button>("Layout/Save/Button");
                m_Save.onClick.AddListener(OnSaveClick);

                m_Delete = transform.GetElement<Button>("Layout/Delete/Button");
                m_Delete.onClick.AddListener(OnDeleteClick);

                m_CanvasGroup = transform.GetComponent<CanvasGroup>();

                transform.GetElement<Button>("Layout/Top/Close").onClick.AddListener(OnCloseClick);
                transform.GetElement<Button>("Layout/Goto/Button").onClick.AddListener(OnGotoClick);
            }

            public void SetActive(bool active) {
                m_Transform.gameObject.SetActive(active);
            }

            public void SetLocation(EGRQuickLocation loc) {
                m_Location = loc;
                m_Name.text = loc.Name;
                m_Type.value = (int)loc.Type;
                TimeSpan period = loc.Period();

                string str;
                if (period.TotalHours < 1d) {
                    str = string.Format(Localize(EGRLanguageData._0__MINUTES_AGO), (int)period.TotalMinutes);
                }
                else if (period.TotalDays < 1d) {
                    str = string.Format(Localize(EGRLanguageData._0__HOURS_AGO), (int)period.TotalHours);
                }
                else {
                    str = string.Format(Localize(EGRLanguageData._0__DAYS_AGO), (int)period.TotalDays);
                }

                m_Date.text = str;

                //distance
                Client.LocationService.GetCurrentLocation((success, coords, bearing) => {
                    if (!success) {
                        m_Distance.text = Localize(EGRLanguageData.N_A);
                        return;
                    }

                    Vector2d delta = coords.Value - loc.Coords;
                    float distance = (float)MRKMapUtils.LatLonToMeters(delta).magnitude;
                    if (distance > 1000f) {
                        distance /= 1000f;
                        m_Distance.text = string.Format(Localize(EGRLanguageData._0__KM_AWAY), (int)distance);
                    }
                    else {
                        m_Distance.text = string.Format(Localize(EGRLanguageData._0__M_AWAY), (int)distance);
                    }
                }, 
                true);

                m_Save.interactable = false;
            }

            void OnCloseClick() {
                ms_Instance.CloseDetailedView();
            }

            void OnSaveClick() {
                m_Location.Name = m_Name.text;
                m_Location.Type = (EGRQuickLocationType)m_Type.value;

                EGRQuickLocation.SaveLocalLocations(() => {
                    ms_Instance.MessageBox.ShowPopup(
                        Localize(EGRLanguageData.QUICK_LOCATIONS),
                        Localize(EGRLanguageData.SAVED),
                        null,
                        ms_Instance
                    );

                    ms_Instance.UpdateLocationListFromLocal();
                });

                UpdateSaveButtonInteractibility();
            }

            void UpdateSaveButtonInteractibility() {
                m_Save.interactable = m_Name.text != m_Location.Name 
                    || m_Type.value != (int)m_Location.Type;
            }

            void OnDeleteClick() {
                EGRPopupConfirmation popup = ScreenManager.GetPopup<EGRPopupConfirmation>();
                popup.SetYesButtonText(Localize(EGRLanguageData.DELETE));
                popup.SetNoButtonText(Localize(EGRLanguageData.CANCEL));
                popup.ShowPopup(
                    Localize(EGRLanguageData.QUICK_LOCATIONS),
                    string.Format(Localize(EGRLanguageData.ARE_YOU_SURE_THAT_YOU_WANT_TO_DELETE__0__), m_Location.Name),
                    (_, res) => {
                        if (res == EGRPopupResult.YES) {
                            EGRQuickLocation.Delete(m_Location);
                            OnCloseClick();

                            ms_Instance.UpdateLocationListFromLocal();
                        }
                    },
                    ms_Instance
                );
            }

            void OnGotoClick() {
                OnCloseClick();

                //hide everything
                ms_Instance.UpdateMainView(false);

                Client.FlatCamera.TeleportToLocationTweened(m_Location.Coords);
            }

            public void AnimateIn() {
                DOTween.To(
                    () => m_CanvasGroup.alpha,
                    x => m_CanvasGroup.alpha = x,
                    1f,
                    0.3f
                ).ChangeStartValue(0f)
                .SetEase(Ease.OutSine);
            }

            public void AnimateOut(Action callback) {
                DOTween.To(
                    () => m_CanvasGroup.alpha,
                    x => m_CanvasGroup.alpha = x,
                    0f,
                    0.3f
                )
                .SetEase(Ease.OutSine)
                .OnComplete(() => callback());
            }
        }
    }
}

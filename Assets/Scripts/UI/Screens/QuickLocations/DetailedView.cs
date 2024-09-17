using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MRK.Localization;
using static MRK.Localization.LanguageManager;

namespace MRK.UI
{
    public partial class QuickLocations
    {
        private class DetailedView : BaseBehaviourPlain
        {
            private RectTransform _transform;
            private TMP_InputField _name;
            private TMP_Dropdown _type;
            private TextMeshProUGUI _date;
            private TextMeshProUGUI _distance;
            private Button _save;
            private Button _delete;
            private EGRQuickLocation _location;
            private CanvasGroup _canvasGroup;

            public bool IsActive
            {
                get
                {
                    return _transform.gameObject.activeInHierarchy;
                }
            }

            public DetailedView(RectTransform transform)
            {
                _transform = transform;
                _name = transform.GetElement<TMP_InputField>("Layout/Name/Input");
                _name.onValueChanged.AddListener((_) => UpdateSaveButtonInteractibility());

                _type = transform.GetElement<TMP_Dropdown>("Layout/Type/Dropdown");
                _type.onValueChanged.AddListener((_) => UpdateSaveButtonInteractibility());

                _date = transform.GetElement<TextMeshProUGUI>("Layout/Date/Val");
                _distance = transform.GetElement<TextMeshProUGUI>("Layout/Dist/Val");

                _save = transform.GetElement<Button>("Layout/Save/Button");
                _save.onClick.AddListener(() => OnSaveClick());

                _delete = transform.GetElement<Button>("Layout/Delete/Button");
                _delete.onClick.AddListener(OnDeleteClick);

                _canvasGroup = transform.GetComponent<CanvasGroup>();

                transform.GetElement<Button>("Layout/Top/Close").onClick.AddListener(OnCloseClick);
                transform.GetElement<Button>("Layout/Goto/Button").onClick.AddListener(OnGotoClick);
            }

            public void SetActive(bool active)
            {
                _transform.gameObject.SetActive(active);
            }

            public void SetLocation(EGRQuickLocation loc)
            {
                _location = loc;
                _name.text = loc.Name;
                _type.value = (int)loc.Type;
                TimeSpan period = loc.Period();

                string str;
                if (period.TotalHours < 1d)
                {
                    str = string.Format(Localize(LanguageData._0__MINUTES_AGO), (int)period.TotalMinutes);
                }
                else if (period.TotalDays < 1d)
                {
                    str = string.Format(Localize(LanguageData._0__HOURS_AGO), (int)period.TotalHours);
                }
                else
                {
                    str = string.Format(Localize(LanguageData._0__DAYS_AGO), (int)period.TotalDays);
                }

                _date.text = str;

                //distance
                Client.LocationService.GetCurrentLocation((success, coords, bearing) =>
                {
                    if (!success)
                    {
                        _distance.text = Localize(LanguageData.N_A);
                        return;
                    }

                    Vector2d delta = coords.Value - loc.Coords;
                    float distance = (float)MapUtils.LatLonToMeters(delta).magnitude;
                    if (distance > 1000f)
                    {
                        distance /= 1000f;
                        _distance.text = string.Format(Localize(LanguageData._0__KM_AWAY), (int)distance);
                    }
                    else
                    {
                        _distance.text = string.Format(Localize(LanguageData._0__M_AWAY), (int)distance);
                    }
                },
                true);

                _save.interactable = false;
            }

            private void OnCloseClick()
            {
                if (_save.interactable)
                {
                    Confirmation popup = ScreenManager.GetPopup<Confirmation>();
                    popup.SetYesButtonText(Localize(LanguageData.SAVE));
                    popup.SetNoButtonText(Localize(LanguageData.CANCEL));
                    popup.ShowPopup(
                        Localize(LanguageData.QUICK_LOCATIONS),
                        Localize(LanguageData.YOU_HAVE_UNSAVED_CHANGES_nWOULD_YOU_LIKE_TO_SAVE_YOUR_CHANGES_),
                        OnUnsavedClose,
                        null
                    );
                }
                else
                {
                    _instance.CloseDetailedView();
                }
            }

            private void OnUnsavedClose(Popup popup, PopupResult result)
            {
                if (result == PopupResult.YES)
                {
                    OnSaveClick(true);
                    return;
                }

                _instance.CloseDetailedView();
            }

            private void OnSaveClick(bool hideAfter = false)
            {
                _location.Name = _name.text;
                _location.Type = (EGRQuickLocationType)_type.value;

                EGRQuickLocation.SaveLocalLocations(() =>
                {
                    _instance.MessageBox.ShowPopup(
                        Localize(LanguageData.QUICK_LOCATIONS),
                        Localize(LanguageData.SAVED),
                        null,
                        _instance
                    );

                    if (hideAfter)
                    {
                        _instance.CloseDetailedView();
                    }

                    _instance.UpdateLocationListFromLocal();
                });

                UpdateSaveButtonInteractibility();
            }

            private void UpdateSaveButtonInteractibility()
            {
                _save.interactable = _name.text != _location.Name
                    || _type.value != (int)_location.Type;
            }

            private void OnDeleteClick()
            {
                Confirmation popup = ScreenManager.GetPopup<Confirmation>();
                popup.SetYesButtonText(Localize(LanguageData.DELETE));
                popup.SetNoButtonText(Localize(LanguageData.CANCEL));
                popup.ShowPopup(
                    Localize(LanguageData.QUICK_LOCATIONS),
                    string.Format(Localize(LanguageData.ARE_YOU_SURE_THAT_YOU_WANT_TO_DELETE__0__), _location.Name),
                    (_, res) =>
                    {
                        if (res == PopupResult.YES)
                        {
                            EGRQuickLocation.Delete(_location);
                            OnCloseClick();

                            _instance.UpdateLocationListFromLocal();
                        }
                    },
                    _instance
                );
            }

            private void OnGotoClick()
            {
                OnCloseClick();

                //hide everything
                _instance.UpdateMainView(false);

                Client.FlatCamera.TeleportToLocationTweened(_location.Coords);
            }

            public void AnimateIn()
            {
                DOTween.To(
                    () => _canvasGroup.alpha,
                    x => _canvasGroup.alpha = x,
                    1f,
                    0.3f
                ).ChangeStartValue(0f)
                .SetEase(Ease.OutSine);
            }

            public void AnimateOut(Action callback)
            {
                DOTween.To(
                    () => _canvasGroup.alpha,
                    x => _canvasGroup.alpha = x,
                    0f,
                    0.3f
                )
                .SetEase(Ease.OutSine)
                .OnComplete(() => callback());
            }

            public void Close()
            {
                OnCloseClick();
            }
        }
    }
}

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MRK.UI.EGRUI_Main.EGRScreen_Loading;

namespace MRK.UI.Screens
{
    public class Loading : Screen
    {
        private EGRFiniteStateMachine _stateMachine;
        private TextMeshProUGUI _egrText;
        private TextMeshProUGUI _numText;
        private Image _egrBg;
        private EGRColorFade _colorFade;

        public override bool CanChangeBar
        {
            get
            {
                return true;
            }
        }

        public override uint BarColor
        {
            get
            {
                return 0x00000000u;
            }
        }

        protected override void OnScreenInit()
        {
            _egrText = GetElement<TextMeshProUGUI>(Labels.Egr);
            _egrText.color = Color.clear;

            _numText = GetElement<TextMeshProUGUI>(Labels.Num);
            _numText.color = Color.clear;

            _egrBg = GetElement<Image>(Images.EgrBg);

            float targetY = 0f;
            float deltaY = 0f;

            _stateMachine = new EGRFiniteStateMachine(new Tuple<Func<bool>, Action, Action>[] {
                new Tuple<Func<bool>, Action, Action>(() => {
                    return _colorFade.Done;
                },
                () => {
                    _colorFade.Update();
                    _egrText.color = _colorFade.Current;
                },
                () => {
                    _colorFade = new EGRColorFade(Color.clear, Color.white, 1.5f);
                }),

                new Tuple<Func<bool>, Action, Action>(() => {
                    return deltaY >= 1f;
                },
                () => {
                    deltaY += Time.deltaTime * 5f;
                    _egrText.rectTransform.anchoredPosition = new Vector2(_egrText.rectTransform.anchoredPosition.x, Mathf.Lerp(0f, targetY, deltaY));
                },
                () => {
                    targetY = -_egrText.rectTransform.sizeDelta.y / 2f;
                }),

                new Tuple<Func<bool>, Action, Action>(() => {
                    return _colorFade.Done;
                },
                () => {
                    _colorFade.Update();
                    _numText.color = _colorFade.Current;
                },
                () => {
                    _colorFade = new EGRColorFade(Color.clear, Color.white, 1.5f);
                }),

                new Tuple<Func<bool>, Action, Action>(() => {
                    return _colorFade.Done;
                },
                () => {
                    _colorFade.Update();
                    _egrText.color = _colorFade.Current;
                    _egrBg.color = _colorFade.Current.Inverse();
                },
                () => {
                    _colorFade = new EGRColorFade(Color.white, Color.black, 2f);
                }),

                new Tuple<Func<bool>, Action, Action>(() => {
                    return true;
                },
                () => { },
                () => {
                    StartCoroutine(Load());
                    Client.InitializeMaps();
                    Client.SetPostProcessState(true);
                    ScreenManager.GetScreen<MapInterface>().Warmup();

                    //SO MUCH TIME, USE WISELY
                    //Client.FixInvalidTiles();
                })
            });
        }

        private IEnumerator Load()
        {
            for (int i = 0; i < 10; i++)
                yield return new WaitForEndOfFrame();

            FadeManager.Fade(1f, 0.5f, () =>
            {
                Client.Initialize();

                ScreenManager.GetScreen<Login>().ShowScreen();
                HideScreen();
            });
        }

        protected override void OnScreenUpdate()
        {
            _stateMachine.UpdateFSM();
        }
    }
}

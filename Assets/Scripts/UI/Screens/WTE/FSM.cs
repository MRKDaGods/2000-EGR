using System;
using UnityEngine;

namespace MRK.UI
{
    public partial class WTE
    {
        private void InitTransitionFSM()
        {
            _animFSM = new EGRFiniteStateMachine(new Tuple<Func<bool>, Action, Action>[]{
                new Tuple<Func<bool>, Action, Action>(() => {
                    return _transitionFade.Done;
                }, () => {
                    _transitionFade.Update();

                    _wteTextBg.color = _transitionFade.Current;
                    _wteText.color = _transitionFade.Current.Inverse().AlterAlpha(1f);

                }, () => {
                    _transitionFade.Reset();

                    _stripFade.Reset();
                    _stripFade.SetColors(_transitionFade.Final, Color.clear, 0.3f);

                    _wteTextBgDissolve.effectFactor = 0f;
                }),

                new Tuple<Func<bool>, Action, Action>(() => {
                    return _wteTextBgDissolve.effectFactor >= 1f;
                }, () => {
                }, () => {
                    _wteTextBgDissolve.effectPlayer.duration = 0.5f;
                    Client.Runnable.RunLater(() => _wteTextBgDissolve.effectPlayer.Play(false), 1f);
                }),

                //exit
                new Tuple<Func<bool>, Action, Action>(() => {
                    return true;
                }, () => {
                }, OnWTETransitionEnd)
            });
        }
    }
}
using System;
using UnityEngine;

namespace MRK.UI {
    public partial class EGRScreenWTE {
        void InitTransitionFSM() {
            m_AnimFSM = new EGRFiniteStateMachine(new Tuple<Func<bool>, Action, Action>[]{
                new Tuple<Func<bool>, Action, Action>(() => {
                    return m_TransitionFade.Done;
                }, () => {
                    m_TransitionFade.Update();

                    m_WTETextBg.color = m_TransitionFade.Current;
                    m_WTEText.color = m_TransitionFade.Current.Inverse().AlterAlpha(1f);

                }, () => {
                    m_TransitionFade.Reset();

                    m_StripFade.Reset();
                    m_StripFade.SetColors(m_TransitionFade.Final, Color.clear, 0.3f);

                    m_WTETextBgDissolve.effectFactor = 0f;
                }),

                new Tuple<Func<bool>, Action, Action>(() => {
                    return m_WTETextBgDissolve.effectFactor >= 1f;
                }, () => {
                }, () => {
                    m_WTETextBgDissolve.effectPlayer.duration = 0.5f;
                    Client.Runnable.RunLater(() => m_WTETextBgDissolve.effectPlayer.Play(false), 1f);
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
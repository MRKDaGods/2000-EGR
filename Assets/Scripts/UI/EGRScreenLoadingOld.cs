using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using static MRK.UI.EGRUI_Main.EGRScreen_LoadingOLD;

namespace MRK.UI {
    public class EGRScreenLoadingOld : EGRScreen {
        class LoadingFSM {
            Tuple<Func<bool>, Action, Action>[] m_States;
            int m_CurrentState;
            bool m_Dirty;

            public LoadingFSM(Tuple<Func<bool>, Action, Action>[] states) {
                m_States = states;
                m_CurrentState = 0;
                m_Dirty = true;
            }

            public void UpdateFSM() {
                if (m_CurrentState >= m_States.Length)
                    return;

                if (m_Dirty) {
                    m_Dirty = false;
                    m_States[m_CurrentState].Item3();
                }

                if (m_States[m_CurrentState].Item1()) {
                    Debug.Log("Switching states");
                    m_CurrentState++;
                    m_Dirty = true;
                    return;
                }

                m_States[m_CurrentState].Item2();
            }
        }

        TextMeshProUGUI m_InitialLabel;
        EGRColorFade m_InitialLabelFade;
        TextMeshProUGUI[] m_FSM2Labels;
        EGRColorFade m_FSMFade;
        Rect m_FSMSection;
        TextMeshProUGUI[] m_FSM4Labels;
        LoadingFSM m_StateMachine;
        readonly string[] m_FSMDetailedLabels;
        [SerializeField]
        TMP_FontAsset m_LightFont;

        public EGRScreenLoadingOld() {
            m_FSMDetailedLabels = new string[3] {
                "gyptian",
                "rowth",
                "evolution"
            };
        }

        protected override void OnScreenInit() {
            m_InitialLabel = GetElement<TextMeshProUGUI>(Labels.Initial);
            m_InitialLabelFade = new EGRColorFade(Color.clear, m_InitialLabel.color, 1.2f);

            m_FSM2Labels = new TextMeshProUGUI[3];
            for (int i = 0; i < m_FSM2Labels.Length; i++) {
                TextMeshProUGUI txt = Instantiate(m_InitialLabel, m_InitialLabel.transform.parent);
                txt.text = "EGR"[i].ToString();
                txt.rectTransform.anchoredPosition -= new Vector2(txt.rectTransform.sizeDelta.x / 3f, 0f);
                txt.gameObject.SetActive(false);

                m_FSM2Labels[i] = txt;
            }

            m_FSMFade = new EGRColorFade(Color.clear, m_InitialLabel.color, 2f);

            m_StateMachine = new LoadingFSM(new Tuple<Func<bool>, Action, Action>[] {
                //loading
                new Tuple<Func<bool>, Action, Action>(() => {
                    return m_InitialLabelFade.Done;
                }, 
                () => {
                    m_InitialLabelFade.Update();
                    m_InitialLabel.color = m_InitialLabelFade.Current;
                }, 
                () => { }),

                new Tuple<Func<bool>, Action, Action>(() => {
                    return m_InitialLabelFade.Done;
                }, 
                () => {
                    m_InitialLabelFade.Update();
                    m_InitialLabel.color = m_InitialLabelFade.Current;

                    m_FSMFade.Update();
                    m_FSM2Labels[0].color = m_FSMFade.Current;
                }, 
                () => {
                    //show the E
                    m_FSM2Labels[0].gameObject.SetActive(true);
                    m_FSM2Labels[0].color = Color.clear;

                    m_InitialLabelFade = new EGRColorFade(m_InitialLabel.color, Color.clear, 1.3f);
                }),

                new Tuple<Func<bool>, Action, Action>(() => {
                    return m_FSM2Labels[0].rectTransform.anchoredPosition.Approx(m_FSMSection.position);
                }, 
                () => {
                    m_FSM2Labels[0].rectTransform.anchoredPosition = Vector2.Lerp(m_FSM2Labels[0].rectTransform.anchoredPosition, 
                        m_FSMSection.position, Time.deltaTime * 2f);
                }, 
                () => {
                    m_InitialLabel.gameObject.SetActive(false);

                    float y = m_InitialLabel.rectTransform.sizeDelta.y * 6f;
                    m_FSMSection = new Rect(m_InitialLabel.rectTransform.anchoredPosition.x - m_InitialLabel.rectTransform.sizeDelta.x,
                        Screen.height / 2f - y / 2f, m_InitialLabel.rectTransform.sizeDelta.x / 3f * 10f, y);
                }),

                new Tuple<Func<bool>, Action, Action>(() => {
                    return m_FSMFade.Done;
                },
                () => {
                    m_FSMFade.Update();

                    for (int i = 1; i < m_FSM2Labels.Length; i++) {
                        m_FSM2Labels[i].color = m_FSMFade.Current;
                    }
                },
                () => {
                    m_FSMFade = new EGRColorFade(Color.clear, m_FSM2Labels[0].color, 1.5f);

                    for (int i = 1; i < m_FSM2Labels.Length; i++) {
                        m_FSM2Labels[i].color = Color.clear;
                        m_FSM2Labels[i].rectTransform.anchoredPosition = new Vector2(m_FSMSection.x, m_InitialLabel.rectTransform.anchoredPosition.y 
                            + (i - 1) * (m_InitialLabel.rectTransform.anchoredPosition.y - m_FSMSection.y));

                        m_FSM2Labels[i].gameObject.SetActive(true);
                    }
                }),

                new Tuple<Func<bool>, Action, Action>(() => {
                    return m_FSMFade.Done;
                }, 
                () => {
                    m_FSMFade.Update();

                    for (int i = 0; i < m_FSM4Labels.Length; i++) {
                        m_FSM4Labels[i].color = m_FSMFade.Current;
                    }
                },
                () => {
                    m_FSMFade = new EGRColorFade(Color.clear, new Color32(200, 200, 200, 255), 1.2f);

                    m_FSM4Labels = new TextMeshProUGUI[3];
                    for (int i = 0; i < m_FSM4Labels.Length; i++) {
                        TextMeshProUGUI txt = Instantiate(m_FSM2Labels[i], m_FSM2Labels[i].transform.parent);
                        txt.text = m_FSMDetailedLabels[i];
                        txt.color = Color.clear;
                        txt.font = m_LightFont;
                        txt.fontSize = 130f;

                        float w = txt.GetPreferredValues().x;
                        txt.rectTransform.sizeDelta += new Vector2(w - txt.rectTransform.sizeDelta.x, 0f);
                        txt.rectTransform.anchoredPosition += new Vector2(w / (i == 1 ? 1.57f : i == 0 ? 1.65f : 1.7f), 0f);

                        txt.gameObject.SetActive(true);

                        m_FSM4Labels[i] = txt;
                    }
                }),

                new Tuple<Func<bool>, Action, Action>(() => {
                    return true;
                },
                () => { },
                () => {
                    /*EGRFadeManager.Fade(1f, 2f, () => {
                        HideScreen();
                        Manager.GetScreen(EGRUI_Main.EGRScreen_Main.SCREEN_NAME).ShowScreen();
                    });*/
                })
            });
        }

        protected override void OnScreenShow() {
            m_InitialLabel.color = Color.clear;
        }

        protected override void OnScreenUpdate() {
            m_StateMachine.UpdateFSM();
        }
    }
}

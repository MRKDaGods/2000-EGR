using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MRK {
    public class EGRVirtualController : EGRController {
        class TouchState {
            public Vector2 DownPos;
            public int Id;
            public Vector3 Velocity;
        }

        TouchState[] m_States;
        EGRControllerMouseData[] m_MouseData;

        public override EGRControllerMessageKind MessageKind => EGRControllerMessageKind.Virtual;
        public override Vector3 Velocity => m_States[0].Velocity;
        public override Vector3 LookVelocity => m_States[1].Velocity;
        public override Vector2 Sensitivity => new Vector2(20f, 20f);

        public override void UpdateController() {
            foreach (EGRControllerMouseData data in m_MouseData) {
                if (Input.touchCount <= data.Index) {
                    if (data.MouseDown) {
                        data.Handle = true;
                        data.MouseDown = false;
                        m_ReceivedDelegate?.Invoke(new EGRControllerMessage {
                            Kind = EGRControllerMessageKind.Virtual,
                            ContextualKind = EGRControllerMessageContextualKind.Mouse,
                            Proposer = data,
                            ObjectIndex = 1,
                            Payload = new object[]
                            {
                                EGRControllerMouseEventKind.Up, data.LastPosition
                            }
                        });
                    }

                    continue;
                }

                Touch touch = Input.GetTouch(data.Index);
                bool mouseDown = touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
                Vector3 mousePos = touch.position;

                if (!mouseDown) {
                    if (data.MouseDown) {
                        data.Handle = true;
                        data.MouseDown = mouseDown;
                        m_ReceivedDelegate?.Invoke(new EGRControllerMessage {
                            Kind = EGRControllerMessageKind.Virtual,
                            ContextualKind = EGRControllerMessageContextualKind.Mouse,
                            Proposer = data,
                            ObjectIndex = 1,
                            Payload = new object[]
                            {
                                EGRControllerMouseEventKind.Up, mousePos
                            }
                        });
                    }
                }
                else {
                    if (data.Handle) {
                        bool mouseState = data.MouseDown; //old ks
                        data.MouseDown = mouseDown;
                        data.LastPosition = mousePos;
                        EGRControllerMessage message = new EGRControllerMessage {
                            Kind = EGRControllerMessageKind.Virtual,
                            ContextualKind = EGRControllerMessageContextualKind.Mouse,
                            Proposer = data,
                            ObjectIndex = 3,
                            Payload = new object[]
                            {
                                EGRControllerMouseEventKind.Down, mouseState, false, mousePos
                            }
                        };
                        m_ReceivedDelegate?.Invoke(message);
                        data.Handle = !(bool)message.Payload[2];
                    }
                }
                if (data.LastPosition != mousePos) {
                    Vector3 lastPos = data.LastPosition;
                    data.LastPosition = mousePos;
                    m_ReceivedDelegate?.Invoke(new EGRControllerMessage {
                        Kind = EGRControllerMessageKind.Virtual,
                        ContextualKind = EGRControllerMessageContextualKind.Mouse,
                        Proposer = data,
                        ObjectIndex = 1,
                        Payload = new object[]
                        {
                            EGRControllerMouseEventKind.Drag, mousePos, mousePos - lastPos /*delta*/, m_MouseData
                        }
                    });
                }
            }
        }

        public override void InitController() {
            m_States = new TouchState[2];
            for (int i = 0; i < 2; i++)
                m_States[i] = new TouchState();

            m_MouseData = new EGRControllerMouseData[2];
            for (int i = 0; i < 2; i++)
                m_MouseData[i] = new EGRControllerMouseData { Index = i, Handle = true };
        }

        public override void RenderController() {
            foreach (TouchState state in m_States) {
                //if (state.Id != -1)
                //    GLDraw.DrawLine(state.DownPos, Input.GetTouch(state.Id).position, Color.red, 2f);
            }
        }
    }
}

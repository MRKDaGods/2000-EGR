using UnityEngine;

namespace MRK {
    public class MRKInputVirtualController : MRKInputController {
        class TouchState {
            public Vector2 DownPos;
            public int Id;
            public Vector3 Velocity;
        }

        TouchState[] m_States;
        MRKInputControllerMouseData[] m_MouseData;

        public override MRKInputControllerMessageKind MessageKind => MRKInputControllerMessageKind.Virtual;
        public override Vector3 Velocity => m_States[0].Velocity;
        public override Vector3 LookVelocity => m_States[1].Velocity;
        public override Vector2 Sensitivity => new Vector2(20f, 20f);

        public override void UpdateController() {
            foreach (MRKInputControllerMouseData data in m_MouseData) {
                if (Input.touchCount <= data.Index) {
                    if (data.MouseDown) {
                        data.Handle = true;
                        data.MouseDown = false;
                        m_ReceivedDelegate?.Invoke(new MRKInputControllerMessage {
                            Kind = MRKInputControllerMessageKind.Virtual,
                            ContextualKind = MRKInputControllerMessageContextualKind.Mouse,
                            Proposer = data,
                            ObjectIndex = 1,
                            Payload = new object[]
                            {
                                MRKInputControllerMouseEventKind.Up, data.LastPosition
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
                        m_ReceivedDelegate?.Invoke(new MRKInputControllerMessage {
                            Kind = MRKInputControllerMessageKind.Virtual,
                            ContextualKind = MRKInputControllerMessageContextualKind.Mouse,
                            Proposer = data,
                            ObjectIndex = 1,
                            Payload = new object[]
                            {
                                MRKInputControllerMouseEventKind.Up, mousePos
                            }
                        });
                    }
                }
                else {
                    if (data.Handle) {
                        bool mouseState = data.MouseDown; //old ks
                        data.MouseDown = mouseDown;
                        data.LastPosition = mousePos;
                        MRKInputControllerMessage message = new MRKInputControllerMessage {
                            Kind = MRKInputControllerMessageKind.Virtual,
                            ContextualKind = MRKInputControllerMessageContextualKind.Mouse,
                            Proposer = data,
                            ObjectIndex = 3,
                            Payload = new object[]
                            {
                                MRKInputControllerMouseEventKind.Down, mouseState, false, mousePos
                            }
                        };
                        m_ReceivedDelegate?.Invoke(message);
                        data.Handle = !(bool)message.Payload[2];
                    }
                }
                if (data.LastPosition != mousePos) {
                    Vector3 lastPos = data.LastPosition;
                    data.LastPosition = mousePos;
                    m_ReceivedDelegate?.Invoke(new MRKInputControllerMessage {
                        Kind = MRKInputControllerMessageKind.Virtual,
                        ContextualKind = MRKInputControllerMessageContextualKind.Mouse,
                        Proposer = data,
                        ObjectIndex = 1,
                        Payload = new object[]
                        {
                            MRKInputControllerMouseEventKind.Drag, mousePos, mousePos - lastPos /*delta*/, m_MouseData
                        }
                    });
                }
            }
        }

        public override void InitController() {
            m_States = new TouchState[2];
            for (int i = 0; i < 2; i++)
                m_States[i] = new TouchState();

            m_MouseData = new MRKInputControllerMouseData[2];
            for (int i = 0; i < 2; i++)
                m_MouseData[i] = new MRKInputControllerMouseData { Index = i, Handle = true };
        }

        public override void RenderController() {
            foreach (TouchState state in m_States) {
                //if (state.Id != -1)
                //    GLDraw.DrawLine(state.DownPos, Input.GetTouch(state.Id).position, Color.red, 2f);
            }
        }
    }
}

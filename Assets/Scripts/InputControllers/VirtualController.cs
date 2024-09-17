using UnityEngine;

namespace MRK.InputControllers
{
    public class VirtualController : InputController
    {
        private class TouchState
        {
            public Vector2 DownPos;
            public int Id;
            public Vector3 Velocity;
        }

        private TouchState[] m_States;
        private MouseData[] m_MouseData;

        public override MessageKind MessageKind
        {
            get
            {
                return MessageKind.Virtual;
            }
        }

        public override Vector3 Velocity
        {
            get
            {
                return m_States[0].Velocity;
            }
        }

        public override Vector3 LookVelocity
        {
            get
            {
                return m_States[1].Velocity;
            }
        }

        public override Vector2 Sensitivity
        {
            get
            {
                return new Vector2(20f, 20f);
            }
        }

        public override void InitController()
        {
            m_States = new TouchState[2];
            for (int i = 0; i < 2; i++)
            {
                m_States[i] = new TouchState();
            }

            m_MouseData = new MouseData[2];
            for (int i = 0; i < 2; i++)
            {
                m_MouseData[i] = new MouseData { Index = i, Handle = true };
            }
        }

        public override void UpdateController()
        {
            foreach (MouseData data in m_MouseData)
            {
                if (Input.touchCount <= data.Index)
                {
                    if (data.MouseDown)
                    {
                        data.Handle = true;
                        data.MouseDown = false;
                        _receivedDelegate?.Invoke(new Message
                        {
                            Kind = MessageKind.Virtual,
                            ContextualKind = MessageContextualKind.Mouse,
                            Proposer = data,
                            ObjectIndex = 1,
                            Payload = new object[]
                            {
                                MouseEventKind.Up, data.LastPosition
                            }
                        });
                    }

                    continue;
                }

                Touch touch = Input.GetTouch(data.Index);
                bool mouseDown = touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
                Vector3 mousePos = touch.position;

                if (!mouseDown)
                {
                    if (data.MouseDown)
                    {
                        data.Handle = true;
                        data.MouseDown = mouseDown;
                        _receivedDelegate?.Invoke(new Message
                        {
                            Kind = MessageKind.Virtual,
                            ContextualKind = MessageContextualKind.Mouse,
                            Proposer = data,
                            ObjectIndex = 1,
                            Payload = new object[]
                            {
                                MouseEventKind.Up, mousePos
                            }
                        });
                    }
                }
                else
                {
                    if (data.Handle)
                    {
                        bool mouseState = data.MouseDown; //old ks
                        data.MouseDown = mouseDown;
                        data.LastPosition = mousePos;
                        Message message = new Message
                        {
                            Kind = MessageKind.Virtual,
                            ContextualKind = MessageContextualKind.Mouse,
                            Proposer = data,
                            ObjectIndex = 3,
                            Payload = new object[]
                            {
                                MouseEventKind.Down, mouseState, false, mousePos
                            }
                        };
                        _receivedDelegate?.Invoke(message);
                        data.Handle = !(bool)message.Payload[2];
                    }
                }

                if (data.LastPosition != mousePos)
                {
                    Vector3 lastPos = data.LastPosition;
                    data.LastPosition = mousePos;
                    _receivedDelegate?.Invoke(new Message
                    {
                        Kind = MessageKind.Virtual,
                        ContextualKind = MessageContextualKind.Mouse,
                        Proposer = data,
                        ObjectIndex = 1,
                        Payload = new object[]
                        {
                            MouseEventKind.Drag, mousePos, mousePos - lastPos /*delta*/, m_MouseData
                        }
                    });
                }
            }
        }
    }
}

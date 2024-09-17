using System.Collections.Generic;
using UnityEngine;

namespace MRK.InputControllers
{
    public class PhysicalController : InputController
    {
        private KeyData[] _keyData;
        private MouseData[] _mouseData;

        public override MessageKind MessageKind
        {
            get
            {
                return MessageKind.Physical;
            }
        }

        public override Vector3 Velocity
        {
            get
            {
                return new Vector3(Mathf.Clamp(Input.GetAxis("Horizontal"), -1f, 1f), 0f, Mathf.Clamp(Input.GetAxis("Vertical"), -1f, 1f));
            }
        }

        public override Vector3 LookVelocity
        {
            get
            {
                return new Vector3(Mathf.Clamp(Input.GetAxis("Mouse X"), -1f, 1f), 0f, Mathf.Clamp(Input.GetAxis("Mouse Y"), -1f, 1f));
            }
        }

        public override Vector2 Sensitivity
        {
            get
            {
                return new Vector2(30f, 30f);
            }
        }

        public override void InitController()
        {
            //we don't know the exact count of keys, so rather than reallocating n times, we can just use an automated list
#if MRK_USE_KEYBOARD
            List<KeyData> keys = new List<KeyData>();
            for (KeyCode key = KeyCode.Backspace; key < KeyCode.JoystickButton0; key++)
            {
                keys.Add(new KeyData { KeyCode = key, Handle = true });
            }
            _keyData = keys.ToArray();
#endif

            _mouseData = new MouseData[2];
            for (int i = 0; i < _mouseData.Length; i++)
            {
                _mouseData[i] = new MouseData { Index = i, Handle = true };
            }
        }

        public override void UpdateController()
        {
            foreach (MouseData data in _mouseData)
            {
                Vector3 mousePos = Input.mousePosition;

                bool mouseDown = Input.GetMouseButton(data.Index);
                if (!mouseDown)
                {
                    if (data.MouseDown)
                    {
                        data.Handle = true;
                        data.MouseDown = mouseDown;
                        _receivedDelegate?.Invoke(new Message
                        {
                            Kind = MessageKind.Physical,
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
                        Message message = new Message
                        {
                            Kind = MessageKind.Physical,
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
                        Kind = MessageKind.Physical,
                        ContextualKind = MessageContextualKind.Mouse,
                        Proposer = data,
                        ObjectIndex = 1,
                        Payload = new object[]
                        {
                            MouseEventKind.Drag, mousePos, mousePos - lastPos /*delta*/, _mouseData
                        }
                    });
                }
            }

#if MRK_USE_KEYBOARD
            foreach (EGRControllerKeyData data in m_KeyData) {
                bool keyDown = Input.GetKey(data.KeyCode);
                if (!keyDown) {
                    if (data.KeyDown) {
                        _receivedDelegate?.Invoke(new EGRControllerMessage {
                            Kind = EGRControllerMessageKind.Physical,
                            ContextualKind = EGRControllerMessageContextualKind.Keyboard,
                            Proposer = data,
                            Payload = new object[]
                            {
                                EGRControllerKeyEventKind.Up
                            }
                        });
                    }
                    data.Handle = true;
                    data.KeyDown = keyDown;
                }
                else {
                    if (data.Handle) {
                        bool keyState = data.KeyDown; //old ks
                        data.KeyDown = keyDown;
                        EGRControllerMessage message = new EGRControllerMessage {
                            Kind = EGRControllerMessageKind.Physical,
                            ContextualKind = EGRControllerMessageContextualKind.Keyboard,
                            Proposer = data,
                            Payload = new object[]
                            {
                                EGRControllerKeyEventKind.Down, keyState, false
                            }
                        };
                        _receivedDelegate?.Invoke(message);
                        data.Handle = !(bool)message.Payload[2];
                    }
                }
            }
#endif
        }
    }
}

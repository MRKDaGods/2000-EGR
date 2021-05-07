using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MRK
{
    public class EGRPhysicalController : EGRController
    {
        EGRControllerKeyData[] m_KeyData;
        EGRControllerMouseData[] m_MouseData;

        public override EGRControllerMessageKind MessageKind => EGRControllerMessageKind.Physical;
        public override Vector3 Velocity => new Vector3(Mathf.Clamp(Input.GetAxis("Horizontal"), -1f, 1f), 0f, Mathf.Clamp(Input.GetAxis("Vertical"), -1f, 1f));
        public override Vector3 LookVelocity => new Vector3(Mathf.Clamp(Input.GetAxis("Mouse X"), -1f, 1f), 0f, Mathf.Clamp(Input.GetAxis("Mouse Y"), -1f, 1f));
        public override Vector2 Sensitivity => new Vector2(30f, 30f);

        public override void InitController()
        {
            //we don't know the exact count of keys, so rather than reallocating n times, we can just use an automated list
            List<EGRControllerKeyData> keys = new List<EGRControllerKeyData>();
            for (KeyCode key = KeyCode.Backspace; key < KeyCode.JoystickButton0; key++)
                keys.Add(new EGRControllerKeyData { KeyCode = key, Handle = true });
            m_KeyData = keys.ToArray();

            m_MouseData = new EGRControllerMouseData[2];
            for (int i = 0; i < m_MouseData.Length; i++)
                m_MouseData[i] = new EGRControllerMouseData { Index = i, Handle = true };
        }

        public override void UpdateController()
        {
            foreach (EGRControllerMouseData data in m_MouseData)
            {
                bool mouseDown = Input.GetMouseButton(data.Index);
                Vector3 mousePos = Input.mousePosition;//GUIUtility.ScreenToGUIPoint(Input.mousePosition);
                //mousePos.y = Screen.height - mousePos.y;
                if (!mouseDown)
                {
                    if (data.MouseDown)
                    {
                        data.Handle = true;
                        data.MouseDown = mouseDown;
                        m_ReceivedDelegate?.Invoke(new EGRControllerMessage
                        {
                            Kind = EGRControllerMessageKind.Physical,
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
                else
                {
                    if (data.Handle)
                    {
                        bool mouseState = data.MouseDown; //old ks
                        data.MouseDown = mouseDown;
                        EGRControllerMessage message = new EGRControllerMessage
                        {
                            Kind = EGRControllerMessageKind.Physical,
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
                if (data.LastPosition != mousePos)
                {
                    Vector3 lastPos = data.LastPosition;
                    data.LastPosition = mousePos;
                    m_ReceivedDelegate?.Invoke(new EGRControllerMessage
                    {
                        Kind = EGRControllerMessageKind.Physical,
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
            foreach (EGRControllerKeyData data in m_KeyData)
            {
                bool keyDown = Input.GetKey(data.KeyCode);
                if (!keyDown)
                {
                    if (data.KeyDown)
                    {
                        m_ReceivedDelegate?.Invoke(new EGRControllerMessage
                        {
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
                else
                {
                    if (data.Handle)
                    {
                        bool keyState = data.KeyDown; //old ks
                        data.KeyDown = keyDown;
                        EGRControllerMessage message = new EGRControllerMessage
                        {
                            Kind = EGRControllerMessageKind.Physical,
                            ContextualKind = EGRControllerMessageContextualKind.Keyboard,
                            Proposer = data,
                            Payload = new object[]
                            {
                                EGRControllerKeyEventKind.Down, keyState, false
                            }
                        };
                        m_ReceivedDelegate?.Invoke(message);
                        data.Handle = !(bool)message.Payload[2];
                    }
                }
            }
        }
    }
}

using UnityEngine;

namespace MRK.InputControllers
{
    public enum KeyEventKind
    {
        None,
        Down,
        Up
    }

    public class KeyData : IProposer
    {
        public KeyCode KeyCode;
        public bool KeyDown;
        public bool Handle;

        public MessageContextualKind MessengerKind
        {
            get
            {
                return MessageContextualKind.Keyboard;
            }
        }
    }
}

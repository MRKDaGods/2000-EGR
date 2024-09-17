using UnityEngine;

namespace MRK.InputControllers
{
    public enum MouseEventKind
    {
        None,
        Down,
        Up,
        Scroll,
        Drag
    }

    public class MouseData : IProposer
    {
        public int Index;
        public bool MouseDown;
        public bool Handle;
        public Vector3 LastPosition;

        public MessageContextualKind MessengerKind
        {
            get
            {
                return MessageContextualKind.Mouse;
            }
        }
    }
}
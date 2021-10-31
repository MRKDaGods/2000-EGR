using UnityEngine;

namespace MRK {
    public enum MRKInputControllerKeyEventKind {
        None,
        Down,
        Up
    }

    public class MRKInputControllerKeyData : MRKInputControllerProposer {
        public KeyCode KeyCode;
        public bool KeyDown;
        public bool Handle;

        public MRKInputControllerMessageContextualKind MessengerKind => MRKInputControllerMessageContextualKind.Keyboard;
    }
}

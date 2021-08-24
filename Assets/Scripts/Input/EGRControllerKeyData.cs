using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MRK {
    public enum EGRControllerKeyEventKind {
        None,
        Down,
        Up
    }

    public class EGRControllerKeyData : EGRControllerProposer {
        public KeyCode KeyCode;
        public bool KeyDown;
        public bool Handle;

        public EGRControllerMessageContextualKind MessengerKind => EGRControllerMessageContextualKind.Keyboard;
    }
}

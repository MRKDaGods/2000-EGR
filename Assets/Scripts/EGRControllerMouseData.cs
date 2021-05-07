using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MRK {
    public enum EGRControllerMouseEventKind {
        None,
        Down,
        Up,
        Scroll,
        Drag
    }

    public class EGRControllerMouseData : EGRControllerProposer {
        public int Index;
        public bool MouseDown;
        public bool Handle;
        public Vector3 LastPosition;

        public EGRControllerMessageContextualKind MessengerKind => EGRControllerMessageContextualKind.Mouse;
    }
}
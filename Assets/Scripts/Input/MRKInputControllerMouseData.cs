using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MRK {
    public enum MRKInputControllerMouseEventKind {
        None,
        Down,
        Up,
        Scroll,
        Drag
    }

    public class MRKInputControllerMouseData : MRKInputControllerProposer {
        public int Index;
        public bool MouseDown;
        public bool Handle;
        public Vector3 LastPosition;

        public MRKInputControllerMessageContextualKind MessengerKind => MRKInputControllerMessageContextualKind.Mouse;
    }
}
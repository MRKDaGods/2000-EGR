using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MRK {
    public enum EGRControllerMessageKind {
        None,
        Virtual,
        Physical
    }

    public enum EGRControllerMessageContextualKind {
        None,
        Mouse,
        Keyboard,
        Touch
    }

    public interface EGRControllerProposer {
        string ToString();
        EGRControllerMessageContextualKind MessengerKind { get; }
    }

    public class EGRControllerMessage {
        public EGRControllerMessageKind Kind;
        public EGRControllerMessageContextualKind ContextualKind;
        public EGRControllerProposer Proposer;
        public object[] Payload;
        public int ObjectIndex;
    }

    public delegate void EGRControllerMessageReceivedDelegate(EGRControllerMessage msg);

    public abstract class EGRController {
        protected EGRControllerMessageReceivedDelegate m_ReceivedDelegate;

        public abstract EGRControllerMessageKind MessageKind { get; }

        public abstract Vector3 Velocity { get; }

        public abstract Vector3 LookVelocity { get; }

        public abstract Vector2 Sensitivity { get; }

        public void RegisterReceiver(EGRControllerMessageReceivedDelegate receivedDelegate) {
            m_ReceivedDelegate += receivedDelegate;
        }

        public void UnregisterReceiver(EGRControllerMessageReceivedDelegate receivedDelegate) {
            m_ReceivedDelegate -= receivedDelegate;
        }

        public abstract void InitController();

        public abstract void UpdateController();

        public virtual void RenderController() {
        }
    }
}

using UnityEngine;

namespace MRK {
    public enum MRKInputControllerMessageKind {
        None,
        Virtual,
        Physical
    }

    public enum MRKInputControllerMessageContextualKind {
        None,
        Mouse,
        Keyboard,
        Touch
    }

    public interface MRKInputControllerProposer {
        string ToString();
        MRKInputControllerMessageContextualKind MessengerKind { get; }
    }

    public class MRKInputControllerMessage {
        public MRKInputControllerMessageKind Kind;
        public MRKInputControllerMessageContextualKind ContextualKind;
        public MRKInputControllerProposer Proposer;
        public object[] Payload;
        public int ObjectIndex;
    }

    public delegate void MRKInputControllerMessageReceivedDelegate(MRKInputControllerMessage msg);

    public abstract class MRKInputController {
        protected MRKInputControllerMessageReceivedDelegate m_ReceivedDelegate;

        public abstract MRKInputControllerMessageKind MessageKind { get; }

        public abstract Vector3 Velocity { get; }

        public abstract Vector3 LookVelocity { get; }

        public abstract Vector2 Sensitivity { get; }

        public void RegisterReceiver(MRKInputControllerMessageReceivedDelegate receivedDelegate) {
            m_ReceivedDelegate += receivedDelegate;
        }

        public void UnregisterReceiver(MRKInputControllerMessageReceivedDelegate receivedDelegate) {
            m_ReceivedDelegate -= receivedDelegate;
        }

        public abstract void InitController();

        public abstract void UpdateController();

        public virtual void RenderController() {
        }
    }
}

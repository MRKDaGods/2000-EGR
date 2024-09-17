using UnityEngine;

namespace MRK.InputControllers
{
    public enum MessageKind
    {
        None,
        Virtual,
        Physical
    }

    public enum MessageContextualKind
    {
        None,
        Mouse,
        Keyboard,
        Touch
    }

    public interface IProposer
    {
        public MessageContextualKind MessengerKind
        {
            get;
        }

        public string ToString();
    }

    public class Message
    {
        public MessageKind Kind;
        public MessageContextualKind ContextualKind;
        public IProposer Proposer;
        public object[] Payload;
        public int ObjectIndex;
    }

    public delegate void MessageReceivedDelegate(Message msg);

    public abstract class InputController
    {
        protected MessageReceivedDelegate _receivedDelegate;

        public abstract MessageKind MessageKind
        {
            get;
        }

        public abstract Vector3 Velocity
        {
            get;
        }

        public abstract Vector3 LookVelocity
        {
            get;
        }

        public abstract Vector2 Sensitivity
        {
            get;
        }

        public void RegisterReceiver(MessageReceivedDelegate receivedDelegate)
        {
            _receivedDelegate += receivedDelegate;
        }

        public void UnregisterReceiver(MessageReceivedDelegate receivedDelegate)
        {
            _receivedDelegate -= receivedDelegate;
        }

        public abstract void InitController();

        public abstract void UpdateController();

        public virtual void RenderController()
        {
        }
    }
}

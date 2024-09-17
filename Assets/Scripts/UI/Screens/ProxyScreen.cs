namespace MRK.UI
{
    public enum ProxyTask
    {
        None = 0,
        Show = 1,
        Hide = 2,
        Move = 4
    }

    public delegate void ProxyAction(Screen proxiedInstance);

    public class ProxyScreen
    {
        public ProxyAction ProxyAction
        {
            get; private set;
        }

        public ProxyAction ProxyOnShow
        {
            get; private set;
        }

        public string Name
        {
            get; private set;
        }

        public uint RequestIndex
        {
            get; private set;
        }

        public ProxyTask Tasks
        {
            get; private set;
        }

        public ProxyScreen(string name, uint request)
        {
            Name = name;
            RequestIndex = request;
        }

        public void ShowScreen()
        {
            Tasks |= ProxyTask.Show;
        }

        public void HideScreen()
        {
            Tasks |= ProxyTask.Hide;
        }

        public void MoveToFront()
        {
            Tasks |= ProxyTask.Move;
        }

        public void ExecuteProxyAction(ProxyAction proxyAction)
        {
            if (proxyAction != null)
                ProxyAction += proxyAction;
        }

        public void OnProxyScreenShow(ProxyAction proxyAction)
        {
            if (proxyAction != null)
                ProxyOnShow += proxyAction;
        }
    }
}
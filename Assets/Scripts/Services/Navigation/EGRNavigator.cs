using MRK.Navigation;
using MRK.UI.MapInterface;

namespace MRK.Navigation {
    public abstract class EGRNavigator : MRKBehaviourPlain {
        readonly MRKSelfContainedPtr<EGRMapInterfaceComponentNavigation> m_NavigationUI;

        protected EGRNavigationRoute Route { get; private set; }
        protected EGRNavigationManager NavigationManager => Client.NavigationManager;
        protected EGRMapInterfaceComponentNavigation NavigationUI => m_NavigationUI;

        public EGRNavigator() {
            m_NavigationUI = new MRKSelfContainedPtr<EGRMapInterfaceComponentNavigation>(() => Client.ScreenManager.MapInterface.Components.Navigation);
        }

        protected virtual void Prepare() {
        }

        public void SetRoute(EGRNavigationRoute route) {
            Route = route;

            Prepare();
        }

        public abstract void Update();
    }
}
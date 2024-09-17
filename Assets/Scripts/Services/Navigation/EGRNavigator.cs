using MRK.Navigation;
using MRK.UI.MapInterface;

namespace MRK.Navigation {
    public abstract class EGRNavigator : BaseBehaviourPlain {
        readonly MRKSelfContainedPtr<UI.MapInterface.Navigation> m_NavigationUI;

        protected EGRNavigationRoute Route { get; private set; }
        protected EGRNavigationManager NavigationManager => Client.NavigationManager;
        protected UI.MapInterface.Navigation NavigationUI => m_NavigationUI;
        public virtual Vector2d LastKnownCenter { get; }
        public virtual float LastKnownBearing { get; }

        public EGRNavigator() {
            m_NavigationUI = new MRKSelfContainedPtr<UI.MapInterface.Navigation>(() => Client.ScreenManager.MapInterface.Components.Navigation);
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
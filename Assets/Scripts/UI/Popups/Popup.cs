using System.Collections.Generic;

namespace MRK.UI
{
    public enum PopupResult
    {
        OK,
        YES,
        NO,
        CANCEL
    }

    public delegate void PopupCallback(Popup popup, PopupResult result);

    public class Popup : Screen
    {
        private struct ShowInfo
        {
            public string Name;
            public string Title;
            public string Text;
            public PopupCallback Callback;
            public Screen Owner;
            public int RequestIdx;
        }

        private PopupCallback _callback;
        protected PopupResult _result;
        private ShowInfo _showInfo;

        private static readonly Queue<ShowInfo> _queuedPopups;
        private static Popup _current;

        static Popup()
        {
            _queuedPopups = new Queue<ShowInfo>();
        }

        protected override void OnScreenHide()
        {
            if (_callback != null)
            {
                _callback(this, _result);
                _callback = null;
            }

            _current = null;
            bool shown = false;
            while (_queuedPopups.Count > 0)
            {
                ShowInfo info = _queuedPopups.Peek();
                if (info.RequestIdx == ScreenManager.SceneChangeIndex)
                {
                    Popup target = ScreenManager.Instance.GetScreen(info.Name) as Popup;

                    if (target != null)
                    {
                        target.InternalShow(info);

                        shown = true;
                        break;
                    }
                }

                _queuedPopups.Dequeue();
            }

            if (!shown && _showInfo.Owner != null)
                _showInfo.Owner.ShowScreen();
        }

        private void InternalShow(ShowInfo info)
        {
            _showInfo = info;

            SetTitle(info.Title);
            SetText(info.Text);

            _callback = info.Callback;

            MoveToFront();
            ShowScreen();

            _current = this;

            if (_queuedPopups.Count > 0)
                _queuedPopups.Dequeue();
        }

        public bool ShowPopup(string title, string text, PopupCallback callback, Screen owner)
        {
            ShowInfo showInfo = new ShowInfo
            {
                Name = ScreenName,
                Title = title,
                Text = text,
                Callback = callback,
                Owner = owner,
                RequestIdx = ScreenManager.SceneChangeIndex
            };

            if (_queuedPopups.Count == 0 && _current == null)
            {
                InternalShow(showInfo);
                return true;
            }

            _queuedPopups.Enqueue(showInfo);
            return false;
        }

        protected virtual void SetTitle(string title)
        {
        }

        protected virtual void SetText(string txt)
        {
        }

        public void SetResult(PopupResult res)
        {
            _result = res;
        }
    }
}

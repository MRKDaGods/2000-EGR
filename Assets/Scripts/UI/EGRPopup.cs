using System.Collections.Generic;

namespace MRK.UI {
    public enum EGRPopupResult {
        OK,
        YES,
        NO,
        CANCEL
    }

    public delegate void EGRPopupCallback(EGRPopup popup, EGRPopupResult result);

    public class EGRPopup : EGRScreen {
        struct PopupShowInfo {
            public string name;
            public string title;
            public string text;
            public EGRPopupCallback callback;
            public EGRScreen owner;
            public int requestIdx;
        }

        static Queue<PopupShowInfo> ms_QueuedPopups = new Queue<PopupShowInfo>();
        static EGRPopup ms_Current;

        EGRPopupCallback m_Callback;
        protected EGRPopupResult m_Result;
        PopupShowInfo m_ShowInfo;

        protected override void OnScreenHide() {
            if (m_Callback != null) {
                m_Callback(this, m_Result);
                m_Callback = null;
            }

            ms_Current = null;
            bool shown = false;
            while (ms_QueuedPopups.Count > 0) {
                PopupShowInfo info = ms_QueuedPopups.Peek();
                if (info.requestIdx == EGRScreenManager.SceneChangeIndex) {
                    EGRPopup target = EGRScreenManager.Instance.GetScreen(info.name) as EGRPopup;

                    if (target != null) {
                        target.InternalShow(info);

                        shown = true;
                        break;
                    }
                }

                ms_QueuedPopups.Dequeue();
            }

            if (!shown && m_ShowInfo.owner != null)
                m_ShowInfo.owner.ShowScreen();
        }

        void InternalShow(PopupShowInfo info) {
            m_ShowInfo = info;

            SetTitle(info.title);
            SetText(info.text);

            m_Callback = info.callback;

            MoveToFront();
            ShowScreen();

            ms_Current = this;

            if (ms_QueuedPopups.Count > 0)
                ms_QueuedPopups.Dequeue();
        }

        public bool ShowPopup(string title, string text, EGRPopupCallback callback, EGRScreen owner) {
            PopupShowInfo showInfo = new PopupShowInfo {
                name = ScreenName,
                title = title,
                text = text,
                callback = callback,
                owner = owner,
                requestIdx = EGRScreenManager.SceneChangeIndex
            };

            if (ms_QueuedPopups.Count == 0 && ms_Current == null) {
                InternalShow(showInfo);
                return true;
            }

            ms_QueuedPopups.Enqueue(showInfo);
            return false;
        }

        protected virtual void SetTitle(string title) {
        }

        protected virtual void SetText(string txt) {
        }

        public void SetResult(EGRPopupResult res) {
            m_Result = res;
        }
    }
}

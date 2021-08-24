using System.Collections.Generic;
using UnityEngine;

namespace MRK.UI {
    public enum TransitionType {
        None,
        Scale,
        MAX
    }

    public class EGRTransitionFactory {
        static Dictionary<TransitionType, List<EGRTransition>> ms_Transitions;
        static Dictionary<TransitionType, List<EGRTransition>> ms_RunningTransitions;
        static List<EGRTransition> ms_RemovingTransitions;

        static EGRTransitionFactory() {
            ms_Transitions = new Dictionary<TransitionType, List<EGRTransition>>();
            for (TransitionType type = TransitionType.None; type < TransitionType.MAX; type++)
                ms_Transitions[type] = new List<EGRTransition>();

            ms_RunningTransitions = new Dictionary<TransitionType, List<EGRTransition>>();
            for (TransitionType type = TransitionType.None; type < TransitionType.MAX; type++)
                ms_RunningTransitions[type] = new List<EGRTransition>();

            ms_RemovingTransitions = new List<EGRTransition>();
        }

        static EGRTransition ForType(TransitionType type) {
            switch (type) {

                case TransitionType.None:
                    return new EGRTransitionScreenNone();

                case TransitionType.Scale:
                    return new EGRTransitionScreenScale();

            }

            return null;
        }

        public static T GetFreeTransition<T>(TransitionType type) where T : EGRTransition {
            if (ms_Transitions[type].Count > 0) {
                T t = (T)ms_Transitions[type][0];
                ms_Transitions[type].RemoveAt(0);
                ms_RunningTransitions[type].Add(t);
                t.Free = false;
                return t;
            }

            T nt = (T)ForType(type);
            nt.Free = false;
            ms_RunningTransitions[type].Add(nt);
            return nt;
        }

        public static void Update() {
            for (TransitionType type = TransitionType.Scale; type < TransitionType.MAX; type++) {
                foreach (EGRTransition trns in ms_RunningTransitions[type]) {
                    if (trns.Free) {
                        ms_RemovingTransitions.Add(trns);
                    }
                }
            }

            if (ms_RemovingTransitions.Count > 0) {
                foreach (EGRTransition trns in ms_RemovingTransitions) {
                    ms_RunningTransitions[trns.Type].Remove(trns);
                    ms_Transitions[trns.Type].Add(trns);
                }

                ms_RemovingTransitions.Clear();
            }
        }
    }

    public abstract class EGRTransition {
        protected Transform m_Screen;
        public bool Free = true;

        public abstract TransitionType Type { get; }

        public void SetTarget(Transform target) {
            m_Screen = target;
        }

        public abstract void OnShow();

        public abstract void OnUpdate();
    }

    public class EGRTransitionScreenNone : EGRTransition {
        public override TransitionType Type {
            get {
                return TransitionType.None;
            }
        }

        public override void OnShow() {
        }

        public override void OnUpdate() {
        }
    }

    public class EGRTransitionScreenScale : EGRTransition {
        static Vector3 ms_Target = new Vector3(1f, 1f, 1f);

        public override TransitionType Type {
            get {
                return TransitionType.Scale;
            }
        }

        public override void OnShow() {
            m_Screen.transform.localScale = new Vector3(0f, 0f, 1f); //dont alter the z
        }

        public override void OnUpdate() {
            if (m_Screen.transform.localScale == ms_Target) {
                Free = true;
                return;
            }

            m_Screen.localScale = new Vector3 {
                x = Mathf.MoveTowards(m_Screen.localScale.x, 1f, Time.deltaTime * 2f),
                y = Mathf.MoveTowards(m_Screen.localScale.y, 1f, Time.deltaTime * 2f),
                z = 1f
            };
        }
    }
}
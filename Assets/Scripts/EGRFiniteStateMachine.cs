using System;

namespace MRK {
    public class EGRFiniteStateMachine {
        Tuple<Func<bool>, Action, Action>[] m_States;
        int m_CurrentState;
        bool m_Dirty;

        public EGRFiniteStateMachine(Tuple<Func<bool>, Action, Action>[] states) {
            m_States = states;
            m_CurrentState = 0;
            m_Dirty = true;
        }

        public void UpdateFSM() {
            if (m_CurrentState >= m_States.Length)
                return;

            if (m_Dirty) {
                m_Dirty = false;
                m_States[m_CurrentState].Item3();
            }

            if (m_States[m_CurrentState].Item1()) {
                m_CurrentState++;
                m_Dirty = true;
                return;
            }

            m_States[m_CurrentState].Item2();
        }
    }
}

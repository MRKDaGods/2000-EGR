using System;

namespace MRK {
    public class MRKSelfContainedPtr<T> where T : new() {
        T m_Value;
        readonly Func<T> m_Func;

        static MRKSelfContainedPtr<T> ms_Global;

        public T Value => GetValue();
        public static MRKSelfContainedPtr<T> Global => ms_Global ??= new MRKSelfContainedPtr<T>(() => new T());

        public MRKSelfContainedPtr(Func<T> func) {
            m_Func = func;
        }

        T GetValue() {
            if (m_Value == null) {
                if (m_Func == null) {
                    return default;
                }

                m_Value = m_Func();
            }

            return m_Value;
        }

        public static implicit operator T(MRKSelfContainedPtr<T> ptr) => ptr.GetValue();
    }
}

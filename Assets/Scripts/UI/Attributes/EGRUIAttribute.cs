using System;

namespace MRK.UI.Attributes {
    public enum EGRUIAttributes {
        None,
        ContentType
    }

    public partial class EGRUIAttribute : MRKBehaviour {
        [Serializable]
        public class Attribute<T> {
            public EGRUIAttributes Attr;
            public T Value;
        }
    }
}

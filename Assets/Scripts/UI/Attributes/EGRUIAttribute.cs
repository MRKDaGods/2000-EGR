using System;

namespace MRK.UI.Attributes
{
    public enum UIAttributes
    {
        None,
        ContentType
    }

    public partial class UIAttribute : BaseBehaviour
    {
        [Serializable]
        public class Attribute<T>
        {
            public UIAttributes Attr;
            public T Value;
        }
    }
}

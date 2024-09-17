using System.Collections.Generic;
using UnityEngine;

namespace MRK.UI.Attributes
{
    public enum ContentType
    {
        None,
        Body
    }

    public partial class UIAttribute
    {
        [SerializeField]
        private List<Attribute<ContentType>> _contentTypeAttributes;

        public Attribute<ContentType> Get(UIAttributes attr)
        {
            return _contentTypeAttributes.Find(x => x.Attr == attr);
        }
    }
}

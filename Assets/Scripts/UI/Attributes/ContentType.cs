using System.Collections.Generic;
using UnityEngine;

namespace MRK.UI.Attributes {
    public enum EGRUIContentType {
        None,
        Body
    }

    public partial class EGRUIAttribute {
        [SerializeField]
        List<Attribute<EGRUIContentType>> m_ContentTypeAttributes;

        public Attribute<EGRUIContentType> Get(EGRUIAttributes attr) {
            return m_ContentTypeAttributes.Find(x => x.Attr == attr);
        }
    }
}

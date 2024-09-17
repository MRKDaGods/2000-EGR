using System.Collections.Generic;
using UnityEngine;

namespace MRK.UI
{
    public class FancyScrollViewContent : BaseBehaviour
    {
        [SerializeField]
        private List<FancyScrollViewItemData> _data;

        private void Update()
        {
            GetComponent<FancyScrollView>().UpdateData(_data);
        }
    }
}

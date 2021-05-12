using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI {
    public class EGRScreenOptionsAudioSettings : EGRScreenAnimatedLayout {
        protected override void OnScreenInit() {
            base.OnScreenInit();

            GetElement<Button>("bBack").onClick.AddListener(() => HideScreen());
        }
    }
}

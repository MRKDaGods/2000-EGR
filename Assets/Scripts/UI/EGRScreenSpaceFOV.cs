using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using MRK;
using UnityEngine.UI.Extensions;

namespace MRK.UI {
    public class EGRScreenSpaceFOV : EGRScreen {
        readonly static float[] ms_FOVs;

        ScrollSnap m_HorizontalSnap;

        static EGRScreenSpaceFOV() {
            ms_FOVs = new float[4] {
                65f, 75f, 90f, 120f
            };
        }

        protected override void OnScreenInit() {
            m_HorizontalSnap = GetElement<ScrollSnap>("Values");
        }
        
        protected override void OnScreenShow() {
            m_HorizontalSnap.onPageChange += OnPageChanged;
        }

        protected override void OnScreenHide() {
            m_HorizontalSnap.onPageChange -= OnPageChanged;
        }

        void OnPageChanged(int page) {
            Client.GlobeCamera.TargetFOV = ms_FOVs[page];
        }
    }
}

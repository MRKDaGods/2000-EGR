﻿using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI {
    public class EGRScreenOptionsAdvancedSettings : EGRScreenAnimatedLayout {
        EGRUIMultiSelectorSettings m_InputModelSelector;

        public override bool CanChangeBar => true;
        public override uint BarColor => 0xFF000000;

        protected override void OnScreenInit() {
            base.OnScreenInit();

            GetElement<Button>("bBack").onClick.AddListener(() => HideScreen());

            m_InputModelSelector = GetElement<EGRUIMultiSelectorSettings>("InputModelSelector");
        }

        protected override void OnScreenShow() {
            m_InputModelSelector.SelectedIndex = (int)EGRSettings.InputModel;
        }

        protected override void OnScreenHide() {
            EGRSettings.InputModel = (EGRSettingsInputModel)m_InputModelSelector.SelectedIndex;
            EGRSettings.Save();
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MRK {
    public class TestARCamera : MonoBehaviour {
        RawImage m_Image;
        WebCamTexture web;

        void Start() {
            m_Image = GetComponent<RawImage>();
            web = new WebCamTexture();
            web.Play();

            m_Image.texture = web;
        }

        void Update() {
        }
    }
}
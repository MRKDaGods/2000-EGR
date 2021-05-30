using DG.Tweening;
using MRK.UI;
using System.Collections.Generic;
using UnityEngine;

namespace MRK {
    public class EGRCameraFlat : EGRCamera, IMRKMapController {
        struct RelativeTransform {
            public Transform Transform;
            public Vector3Int Position;
        }

        struct TouchContext {
            public float LastDownTime;
            public float LastValidDownTime;
        }

        Vector2d m_CurrentLatLong;
        Vector2d m_TargetLatLong;
        float m_CurrentZoom;
        float m_TargetZoom;
        float m_LastZoomTime;
        object m_PanTweenLat;
        object m_PanTweenLng;
        object m_ZoomTween;
        EGRScreenMapInterface m_MapInterface;
        TouchContext m_TouchCtx0;
        Vector3 m_LastZoomPosition;

        MRKMap m_Map => EGRMain.Instance.FlatMap;

        public EGRCameraFlat() : base() {
            m_CurrentZoom = m_TargetZoom = 2f; //default zoom
        }

        void Start() {
            Client.RegisterControllerReceiver(OnReceiveControllerMessage);

            m_MapInterface = EGRScreenManager.Instance.GetScreen<EGRScreenMapInterface>();
        }

        void OnDestroy() {
            Client.UnregisterControllerReceiver(OnReceiveControllerMessage);
        }

        public void SetInitialSetup(Vector2d latlng, float zoom) {
            m_CurrentLatLong = m_TargetLatLong = latlng;
            m_CurrentZoom = m_TargetZoom = zoom;
        }

        void OnReceiveControllerMessage(EGRControllerMessage msg) {
            if (!m_InterfaceActive || !gameObject.activeSelf)
                return;

            if (!ShouldProcessControllerMessage(msg)) {
                ResetStates();
                return;
            }

            if (msg.ContextualKind == EGRControllerMessageContextualKind.Mouse) {
                EGRControllerMouseEventKind kind = (EGRControllerMouseEventKind)msg.Payload[0];
                EGRControllerMouseData data = (EGRControllerMouseData)msg.Proposer;

                switch (kind) {

                    case EGRControllerMouseEventKind.Down:
                        m_Down[data.Index] = true;
                        msg.Payload[2] = true;

                        if (data.Index == 0) {
                            m_TouchCtx0.LastDownTime = Time.time;
                        }
                        break;

                    case EGRControllerMouseEventKind.Drag:
                        //store delta for zoom
                        m_Deltas[data.Index] = (Vector3)msg.Payload[2];

                        int touchCount = 0;
                        for (int i = 0; i < 2; i++) {
                            if (m_Down[i])
                                touchCount++;
                        }

                        switch (touchCount) {

                            case 0:
                                break;

                            case 1:

                                if (m_Down[0]) {
                                    ProcessPan((Vector3)msg.Payload[2]);
                                }

                                break;

                            case 2:

                                if (data.Index == 1) { //handle 2nd touch
                                    ProcessZoom((EGRControllerMouseData[])msg.Payload[3]);
                                }

                                break;

                        }
                        break;

                    case EGRControllerMouseEventKind.Up:
                        if (m_Down[0]) {
                            if (Time.time - m_LastZoomTime > 0.5f && Time.time - m_TouchCtx0.LastValidDownTime < 0.2f) {
                                ProcessDoubleClick((Vector3)msg.Payload[1]);
                            }
                            else if (Time.time - m_TouchCtx0.LastDownTime < 0.1f) {
                                m_TouchCtx0.LastValidDownTime = Time.time;
                            }
                        }

                        m_Down[data.Index] = false;
                        break;

                }
            }
        }

        void ProcessDoubleClick(Vector3 pos) {
            m_TargetZoom += 2f;
            m_TargetZoom = Mathf.Clamp(m_TargetZoom, 0f, 21f);

            pos.z = m_Camera.transform.localPosition.y;
            Vector3 wPos = m_Camera.ScreenToWorldPoint(pos);
            m_TargetLatLong = m_Map.WorldToGeoPosition(wPos);

            if (m_PanTweenLat != null) {
                DOTween.Kill(m_PanTweenLat);
            }

            if (m_PanTweenLng != null) {
                DOTween.Kill(m_PanTweenLng);
            }

            m_PanTweenLat = DOTween.To(() => m_CurrentLatLong.x, x => m_CurrentLatLong.x = x, m_TargetLatLong.x, 0.5f)
                .SetEase(Ease.OutSine);

            m_PanTweenLng = DOTween.To(() => m_CurrentLatLong.y, x => m_CurrentLatLong.y = x, m_TargetLatLong.y, 0.5f)
                .SetEase(Ease.OutSine);

            if (m_ZoomTween != null) {
                DOTween.Kill(m_ZoomTween);
            }

            m_ZoomTween = DOTween.To(() => m_CurrentZoom, x => m_CurrentZoom = x, m_TargetZoom, 0.4f)
                .SetEase(Ease.OutSine);
        }

        public void KillAllTweens() {
            if (m_PanTweenLat != null) {
                DOTween.Kill(m_PanTweenLat);
            }

            if (m_PanTweenLng != null) {
                DOTween.Kill(m_PanTweenLng);
            }

            if (m_ZoomTween != null) {
                DOTween.Kill(m_ZoomTween);
            }
        }

        void ProcessPan(Vector3 delta) {
            if (m_LastController == null)
                return;

            if (Time.time - m_LastZoomTime < 0.2f)
                return;

            m_Delta[0] = 0f;

            Vector2d offset2D = new Vector2d(-delta.x, -delta.y) * 3f * EGRSettings.GetMapSensitivity();
            float gameobjectScalingMultiplier = m_Map.transform.localScale.x * (Mathf.Pow(2, (m_Map.InitialZoom - m_Map.AbsoluteZoom)));
            Vector2d newLatLong = MRKMapUtils.MetersToLatLon(
                MRKMapUtils.LatLonToMeters(m_Map.CenterLatLng) + (offset2D / m_Map.WorldRelativeScale) / gameobjectScalingMultiplier);

            //float factor = 2f * Conversions.GetTileScaleInMeters((float)m_Map.CenterLatitudeLongitude.x, m_Map.AbsoluteZoom) / m_Map.UnityTileSize;
            //Vector2d latlongDelta = Conversions.MetersToLatLon(new Vector2d(-delta.x * factor * m_LastController.Sensitivity.x, -delta.y * factor * m_LastController.Sensitivity.y));
            m_TargetLatLong = newLatLong; //m_Map.CenterLatitudeLongitude + latlongDelta;
            m_TargetLatLong.x = Mathd.Clamp(m_TargetLatLong.x, -MRKMapUtils.LATITUDE_MAX, MRKMapUtils.LATITUDE_MAX);
            m_TargetLatLong.y = Mathd.Clamp(m_TargetLatLong.y, -MRKMapUtils.LONGITUDE_MAX, MRKMapUtils.LONGITUDE_MAX);

            Client.InputModel.ProcessPan(ref m_CurrentLatLong, ref m_TargetLatLong, () => m_CurrentLatLong, x => m_CurrentLatLong = x);
        }

        void ProcessZoomInternal(float rawDelta) {
            m_TargetZoom += rawDelta * Time.deltaTime * EGRSettings.GetMapSensitivity();

            if (m_TargetZoom < 0.5f) {
                m_MapInterface.SetTransitionTex(Client.CaptureScreenBuffer());
                (Client.GlobalMap.GetComponent<EGRCameraGlobe>()).SetDistance(7000f);
                Client.SetMapMode(EGRMapMode.Globe);
            }

            m_TargetZoom = Mathf.Clamp(m_TargetZoom, 0f, 21f);

            Client.InputModel.ProcessZoom(ref m_CurrentZoom, ref m_TargetZoom, () => m_CurrentZoom, x => m_CurrentZoom = x);

            m_LastZoomTime = Time.time;
        }

        void ProcessZoom(EGRControllerMouseData[] data) {
            m_Delta[1] = 0f;
            Vector3 prevPos0 = data[0].LastPosition - m_Deltas[0];
            Vector3 prevPos1 = data[1].LastPosition - m_Deltas[1];

            float olddeltaMag = (prevPos0 - prevPos1).magnitude;
            float newdeltaMag = (data[0].LastPosition - data[1].LastPosition).magnitude;

            m_LastZoomPosition = (data[0].LastPosition + data[1].LastPosition) * 0.5f;
            ProcessZoomInternal(newdeltaMag - olddeltaMag);
        }

        void ProcessZoomScroll(float delta) {
            m_LastZoomPosition = Input.mousePosition;
            ProcessZoomInternal(delta * 100f);
        }

        void UpdateTransform() {
            /* if (Client.InputModel is EGRInputModelMRK) {
                if (((EGRInputModelMRK)Client.InputModel).ZoomContext.CanUpdate) {
                    Vector3 mousePosScreen = m_LastZoomPosition;
                    mousePosScreen.z = m_Camera.transform.localPosition.y;
                    Vector3 _mousePosition = m_Camera.ScreenToWorldPoint(mousePosScreen);
                    Vector2d geo = m_Map.WorldToGeoPosition(_mousePosition);
                    Vector2d pos1 = MRKMapUtils.LatLonToMeters(geo);

                    m_Map.UpdateMap(m_Map.CenterLatLng, m_CurrentZoom);
                    geo = m_Map.WorldToGeoPosition(_mousePosition);

                    Vector2d pos2 = MRKMapUtils.LatLonToMeters(geo);
                    Vector2d delta = pos2 - pos1;
                    m_CurrentLatLong = m_TargetLatLong = MRKMapUtils.MetersToLatLon(m_Map.CenterMercator - delta);
                }
            } */

            m_Map.UpdateMap(m_CurrentLatLong, m_CurrentZoom);
        }

        void Update() {
            UpdateTransform();

#if UNITY_EDITOR
            if (Input.mouseScrollDelta != Vector2.zero) {
                ProcessZoomScroll(Input.GetAxis("Mouse ScrollWheel") * 10f);
            }
#endif
        }

        public Vector3 GetMapVelocity() {
            return new Vector3((float)(m_TargetLatLong.x - m_CurrentLatLong.x), (float)(m_TargetLatLong.y - m_CurrentLatLong.y), m_TargetZoom - m_CurrentZoom) * 5f;
        }
    }
}

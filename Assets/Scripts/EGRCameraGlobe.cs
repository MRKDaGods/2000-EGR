using DG.Tweening;
using MRK.UI;
using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace MRK {
    public class EGRCameraGlobe : EGRCamera {
        Vector2 m_TargetRotation;
        Vector2 m_CurrentRotation;
        float m_CurrentDistance;
        [SerializeField, Range(2100f, 20000f)]
        float m_TargetDistance;
        Vector2 m_BackupRotation;
        float m_RotationSpeed;
        float m_BackupDistance;
        float m_DistanceSpeed;
        object m_RotTween;
        object m_DistTween;
        Material m_EarthMat;
        [SerializeField]
        AnimationCurve m_CloudTransparencyCurve;
        EGRScreenMapInterface m_MapInterface;
        bool m_IsSwitching;
        GameObject m_DummyRaycastObject;
        float m_TimeOfDayRotation;

        public EGRCameraGlobe() : base() {
            m_CurrentDistance = m_TargetDistance = m_BackupDistance = 9000f;
            m_RotationSpeed = 8f;
            m_DistanceSpeed = 8f;
        }

        void Start() {
            Client.RegisterControllerReceiver(OnReceiveControllerMessage);
            m_EarthMat = Client.GlobalMap.GetComponent<MeshRenderer>().material;

            //update light pos
            //based on day
            //24->360
            //1->15
            //off=-70
            DateTime time = DateTime.UtcNow.AddHours(2d); //convert to GMT+2 (CLT)
            float hrs = time.Hour;
            hrs += time.Minute / 60f;
            //GameObject.Find("SunPivot/SunPlane/Directional Light").transform.rotation = Quaternion.Euler(0f, hrs * 15f + 50f, 0f);

            m_TimeOfDayRotation = hrs * -15f + 50f - 270f + 50f;
            transform.rotation = Quaternion.Euler(0f, m_TimeOfDayRotation + 270f, 0f);
            m_DummyRaycastObject = new GameObject("Dummy Raycast Object");
        }

        void OnDestroy() {
            Client.UnregisterControllerReceiver(OnReceiveControllerMessage);
        }

        void OnReceiveControllerMessage(EGRControllerMessage msg) {
            if (!m_InterfaceActive || !gameObject.activeSelf)
                return;

            if (!ShouldProcessControllerMessage(msg))
                return;

            if (msg.ContextualKind == EGRControllerMessageContextualKind.Mouse) {
                EGRControllerMouseEventKind kind = (EGRControllerMouseEventKind)msg.Payload[0];
                EGRControllerMouseData data = (EGRControllerMouseData)msg.Proposer;

                switch (kind) {

                    case EGRControllerMouseEventKind.Down:

                        m_Down[data.Index] = true;
                        msg.Payload[2] = true;

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
                                    ProcessRotation((Vector3)msg.Payload[2]);
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

                        m_Down[data.Index] = false;

                        break;

                }
            }
        }

        void ProcessZoom(EGRControllerMouseData[] data) {
            if (m_IsSwitching)
                return;

            //m_Delta[1] = 0f;
            Vector3 prevPos0 = data[0].LastPosition - m_Deltas[0];
            Vector3 prevPos1 = data[1].LastPosition - m_Deltas[1];
            float olddeltaMag = (prevPos0 - prevPos1).magnitude;
            float newdeltaMag = (data[0].LastPosition - data[1].LastPosition).magnitude;

            m_TargetDistance -= (newdeltaMag - olddeltaMag) * Time.deltaTime * 400f * EGRSettings.SensitivityGlobeZ;

            if (m_TargetDistance < 3500f) {
                if (!m_IsSwitching) {
                    m_IsSwitching = true;

                    if (m_DistTween != null) {
                        DOTween.Kill(m_DistTween);
                    }

                    //enable my post processing?
                    //vignette
                    Vignette vig = Client.GetActivePostProcessEffect<Vignette>();
                    vig.active = true;

                    ChromaticAberration aberration = Client.GetActivePostProcessEffect<ChromaticAberration>();

                    Quaternion initialRot = transform.rotation;
                    transform.DORotate(new Vector3(0f, initialRot.eulerAngles.y + 720f), 1.8f, RotateMode.FastBeyond360);

                    m_TargetDistance = 10000f;
                    m_DistTween = DOTween.To(() => m_CurrentDistance, x => m_CurrentDistance = x, m_TargetDistance, 0.5f)
                        .SetEase(Ease.OutSine)
                        .OnUpdate(() => {
                            vig.intensity.value = ((Tween)m_DistTween).ElapsedPercentage() * 0.65f;
                        })
                        .OnComplete(() => {
                            aberration.active = true;
                            aberration.intensity.value = 0f;

                            m_TargetDistance = 3000f;

                            m_DistTween = DOTween.To(() => m_CurrentDistance, x => m_CurrentDistance = x, m_TargetDistance, 1.2f)
                            .SetEase(Ease.InOutExpo)
                            .OnComplete(() => {
                                m_IsSwitching = false;
                                aberration.active = false;
                                vig.active = false;

                                transform.rotation = initialRot;

                                m_TargetDistance = 5000f;

                                m_MapInterface.SetTransitionTex(Client.CaptureScreenBuffer());

                                SwitchToFlatMap();
                            })
                            .OnUpdate(() => {
                                float perc = ((Tween)m_DistTween).ElapsedPercentage();
                                aberration.intensity.value = Mathf.Min(perc * 2f, 1f);
                            })
                            .SetDelay(0.3f);
                        });

                    return;
                }
            }

            m_TargetDistance = Mathf.Clamp(m_TargetDistance, 3100f, 20000f);

            if (m_DistTween != null) {
                DOTween.Kill(m_DistTween);
            }

            m_DistTween = DOTween.To(() => m_CurrentDistance, x => m_CurrentDistance = x, m_TargetDistance, 0.3f)
                .SetEase(Ease.OutBack);

            m_DistanceSpeed = 8f;
        }

        Vector2d GetCurrentGeoPos() {
            RaycastHit hit;
            //if (Physics.Raycast(Client.ActiveCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f)), out hit)) {

            m_DummyRaycastObject.transform.position = Client.ActiveCamera.transform.position;
            m_DummyRaycastObject.transform.rotation = Client.ActiveCamera.transform.rotation;
            m_DummyRaycastObject.transform.RotateAround(transform.position, Vector3.up, -m_TimeOfDayRotation);
            m_DummyRaycastObject.transform.LookAt(transform);
            //m_DummyRaycastObject.transform.rotation *= Quaternion.AngleAxis(-m_TimeOfDayRotation, Vector3.up);

            if (Physics.Raycast(m_DummyRaycastObject.transform.position, transform.position - m_DummyRaycastObject.transform.position, out hit)) {    
                return MRKMapUtils.GeoFromGlobePosition(new Vector3(hit.point.x - transform.position.x, hit.point.y, hit.point.z), transform.localScale.x);
            }

            return Vector2d.zero;
        }

        void SwitchToFlatMap() {
            Vector2d latlng = GetCurrentGeoPos();
            Debug.Log($"Centre latlng={latlng}");
            Client.SetMapMode(EGRMapMode.Flat);
            (Client.ActiveEGRCamera as EGRCameraFlat).SetInitialSetup(latlng, 3f);
        }

        void ProcessZoomScroll(float delta) {
            if (m_IsSwitching)
                return;

            m_TargetDistance -= delta * Time.deltaTime * 400f * EGRSettings.SensitivityGlobeZ;

            if (m_TargetDistance < 3500f) {
                if (!m_IsSwitching) {
                    m_IsSwitching = true;

                    if (m_DistTween != null) {
                        DOTween.Kill(m_DistTween);
                    }

                    //enable my post processing?
                    //vignette
                    Vignette vig = Client.GetActivePostProcessEffect<Vignette>();
                    vig.active = true;

                    ChromaticAberration aberration = Client.GetActivePostProcessEffect<ChromaticAberration>();

                    Quaternion initialRot = transform.rotation;
                    transform.DORotate(new Vector3(0f, initialRot.eulerAngles.y + 720f), 1.8f, RotateMode.FastBeyond360);

                    m_TargetDistance = 10000f;
                    m_DistTween = DOTween.To(() => m_CurrentDistance, x => m_CurrentDistance = x, m_TargetDistance, 0.5f)
                        .SetEase(Ease.OutSine)
                        .OnUpdate(() => {
                            vig.intensity.value = ((Tween)m_DistTween).ElapsedPercentage() * 0.65f;
                        })
                        .OnComplete(() => {
                            aberration.active = true;
                            aberration.intensity.value = 0f;

                            m_TargetDistance = 3000f;

                            m_DistTween = DOTween.To(() => m_CurrentDistance, x => m_CurrentDistance = x, m_TargetDistance, 1.2f)
                            .SetEase(Ease.InOutExpo)
                            .OnComplete(() => {
                                m_IsSwitching = false;
                                aberration.active = false;
                                vig.active = false;

                                transform.rotation = initialRot;

                                m_TargetDistance = 5000f;

                                m_MapInterface.SetTransitionTex(Client.CaptureScreenBuffer());

                                SwitchToFlatMap();
                            })
                            .OnUpdate(() => {
                                float perc = ((Tween)m_DistTween).ElapsedPercentage();
                                aberration.intensity.value = Mathf.Min(perc * 2f, 1f);
                            })
                            .SetDelay(0.3f);
                        });

                    return;
                }
            }

            m_TargetDistance = Mathf.Clamp(m_TargetDistance, 3100f, 20000f);

            if (m_DistTween != null) {
                DOTween.Kill(m_DistTween);
            }

            m_DistTween = DOTween.To(() => m_CurrentDistance, x => m_CurrentDistance = x, m_TargetDistance, 0.3f)
                .SetEase(Ease.OutBack);

            m_DistanceSpeed = 8f;
        }

        void ProcessRotation(Vector3 delta, bool withTween = true, bool withDelta = true) {
            if (m_LastController == null)
                return;

            float factor = Mathf.Clamp01(m_CurrentDistance / 10000f + 0.5f);
            m_TargetRotation.x += delta.x * (withDelta ? Time.deltaTime : 1f) * m_LastController.Sensitivity.x * EGRSettings.SensitivityGlobeX * factor;
            m_TargetRotation.y -= delta.y * (withDelta ? Time.deltaTime : 1f) * m_LastController.Sensitivity.y * EGRSettings.SensitivityGlobeY * factor;

            //m_TargetRotation.x = ClampAngle(m_TargetRotation.x, float.NegativeInfinity, float.PositiveInfinity);
            m_TargetRotation.y = ClampAngle(m_TargetRotation.y, -80f, 80f);

            if (m_RotTween != null) {
                DOTween.Kill(m_RotTween);
            }

            if (withTween) {
                m_RotTween = DOTween.To(() => m_CurrentRotation, x => m_CurrentRotation = x, m_TargetRotation, 0.7f)
                    .SetEase(Ease.OutBack);

                m_Delta[0] = 1f;
            }
            else {
                m_Delta[0] = 0f;
            }

            m_RotationSpeed = 2f;
        }

        void ProcessRotationIdle(Vector3 delta, bool withTween = true, bool withDelta = true) {
            m_TargetRotation.x += delta.x * (withDelta ? Time.deltaTime : 1f);
            m_TargetRotation.y -= delta.y * (withDelta ? Time.deltaTime : 1f);

            //m_TargetRotation.x = ClampAngle(m_TargetRotation.x, float.NegativeInfinity, float.PositiveInfinity);
            m_TargetRotation.y = ClampAngle(m_TargetRotation.y, -80f, 80f);

            m_CurrentRotation = m_TargetRotation;
            UpdateTransform();
        }

        public void SetDistance(float dist) {
            m_CurrentDistance = m_BackupDistance = m_TargetDistance = dist;
        }

        public void UpdateTransform() {
            Quaternion rotation = Quaternion.Euler(m_CurrentRotation.y, m_CurrentRotation.x, 0);

            Vector3 negDistance = new Vector3(0f, 0f, -m_CurrentDistance);
            Vector3 position = rotation * negDistance + transform.position;

            m_Camera.transform.rotation = rotation;
            m_Camera.transform.position = position;

            if (m_MapInterface == null) {
                m_MapInterface = EGRScreenManager.Instance.GetScreen<EGRScreenMapInterface>(EGRUI_Main.EGRScreen_MapInterface.SCREEN_NAME);
            }

            if (m_MapInterface != null && m_MapInterface.Visible) {
                m_MapInterface.SetDistanceText($"{(int)(m_CurrentDistance - transform.localScale.x)}m");
            }

            float transparency = Mathf.Clamp01((Mathf.Min(4200f, m_CurrentDistance) - 3300f) / 3300f);
            float val = m_CloudTransparencyCurve.Evaluate(transparency);
            m_EarthMat.SetColor("_CloudColor", new Color(val, val, val));
        }

        public (Vector3, Vector3) GetSamplePosRot() {
            Quaternion rotation = Quaternion.Euler(m_CurrentRotation.y, m_CurrentRotation.x, 0);

            Vector3 negDistance = new Vector3(0f, 0f, -m_CurrentDistance);
            Vector3 position = rotation * negDistance + transform.position;

            return (position, rotation.eulerAngles);
        }

        void Update() {
            if (Client.MapMode != EGRMapMode.Globe || Client.CamDirty)
                return;

            if (m_DistTween != null || m_RotTween != null)
                UpdateTransform();

            if (m_Delta[0] < 1f) {
                m_Delta[0] += Time.deltaTime * m_RotationSpeed;
                m_Delta[0] = Mathf.Clamp01(m_Delta[0]);
                m_CurrentRotation = Vector2.Lerp(m_CurrentRotation, m_TargetRotation, m_Delta[0]);

                UpdateTransform();
            }

            if (m_Delta[1] < 1f) {
                m_Delta[1] += Time.deltaTime * m_DistanceSpeed;
                m_Delta[1] = Mathf.Clamp01(m_Delta[1]);
                m_CurrentDistance = Mathf.Lerp(m_CurrentDistance, m_TargetDistance, m_Delta[1]);

                UpdateTransform();
            }

#if UNITY_EDITOR
            if (Input.mouseScrollDelta != Vector2.zero) {
                ProcessZoomScroll(Input.GetAxis("Mouse ScrollWheel") * 500f);
            }
#endif

            if (!m_InterfaceActive)
                ProcessRotationIdle(new Vector3(30f, 0f), false);
        }

        float ClampAngle(float angle, float min = 0f, float max = 0f) {
            if (angle < -360f)
                angle += 360f;
            if (angle > 360f)
                angle -= 360f;

            return angle;
            //return Mathf.Clamp(angle, min, max);
        }

        public override void SetInterfaceState(bool active) {
            if (active == m_InterfaceActive)
                return;

            base.SetInterfaceState(active);

            if (m_InterfaceActive) {
                m_CurrentRotation = new Vector2(ClampAngle(m_CurrentRotation.x), ClampAngle(m_CurrentRotation.y));

                //go back to old pos before interface inactivity?
                m_TargetRotation = m_BackupRotation;
                m_TargetDistance = m_BackupDistance;

                for (int i = 0; i < m_Delta.Length; i++)
                    m_Delta[i] = 0f;

                m_RotationSpeed = 1f;
                m_DistanceSpeed = 3f;

                for (int i = 0; i < m_Down.Length; i++)
                    m_Down[i] = false;
            }
            else {
                m_BackupRotation = m_TargetRotation;
                m_BackupDistance = m_TargetDistance;

                m_DistanceSpeed = 1f;
                m_RotationSpeed = 5f;
                m_Delta[0] = 1f;
                m_Delta[1] = 0f;
                m_TargetDistance = 6000f;
            }
        }
    }
}

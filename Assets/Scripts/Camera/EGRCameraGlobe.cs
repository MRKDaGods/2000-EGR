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
        Tween m_DistTween;
        Material m_EarthMat;
        [SerializeField]
        AnimationCurve m_CloudTransparencyCurve;
        EGRScreenMapInterface m_MapInterface;
        bool m_IsSwitching;
        GameObject m_DummyRaycastObject;
        float m_TimeOfDayRotation;
        bool m_PositionLocked;
        bool m_RotationLocked;
        Transform m_Light;
        Vector3 m_OriginalLightRotation;

        public bool IsLocked => m_PositionLocked || m_RotationLocked;
        public float TargetFOV { get; set; }

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

            m_Light = Client.Sun.parent.GetChild(0); //Directional Light
            m_OriginalLightRotation = m_Light.transform.rotation.eulerAngles; //0,180,0

            TargetFOV = m_Camera.fieldOfView; //init fov
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
            Client.SetMapMode(EGRMapMode.Flat);
            Client.FlatCamera.SetInitialSetup(GetCurrentGeoPos(), 3f);

            m_Camera.fieldOfView = EGRConstants.EGR_CAMERA_DEFAULT_FOV; //default fov

            if (!MRKPlayerPrefs.Get<bool>(EGRConstants.EGR_LOCALPREFS_RUNS_FLAT_MAP, false)) {
                MRKPlayerPrefs.Set<bool>(EGRConstants.EGR_LOCALPREFS_RUNS_FLAT_MAP, true);
                MRKPlayerPrefs.Save();

                EGRScreenManager.Instance.GetScreen<EGRScreenMapChooser>().ShowScreen();
            }
        }

        void StartTransitionToFlat(Action callback = null) {
            if (!m_IsSwitching) {
                m_IsSwitching = true;

                if (m_DistTween != null) {
                    DOTween.Kill(m_DistTween.id);
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
                        vig.intensity.value = m_DistTween.ElapsedPercentage() * 0.65f;
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

                            if (callback != null)
                                callback();
                        })
                        .OnUpdate(() => {
                            float perc = m_DistTween.ElapsedPercentage();
                            aberration.intensity.value = Mathf.Min(perc * 2f, 1f);
                        })
                        .SetDelay(0.3f);
                    });
            }
        }

        public void SwitchToFlatMapExternal(Action callback) {
            StartTransitionToFlat(callback);
        }

        void ProcessZoomInternal(float rawDelta) {
            if (m_IsSwitching)
                return;

            m_TargetDistance -= rawDelta * Time.deltaTime * 400f * EGRSettings.GetGlobeSensitivity();

            if (m_TargetDistance < 3500f && m_MapInterface.ObservedTransform == transform) {
                StartTransitionToFlat();
                return;
            }

            m_TargetDistance = Mathf.Clamp(m_TargetDistance, 3100f, 20000f);

            if (m_DistTween != null) {
                DOTween.Kill(m_DistTween.id);
            }

            m_DistTween = DOTween.To(() => m_CurrentDistance, x => m_CurrentDistance = x, m_TargetDistance, 0.2f).SetEase(Ease.OutBack);
            m_DistTween.intId = EGRTweenIDs.IntId;

            m_DistanceSpeed = 8f;
        }

        void ProcessZoomScroll(float delta) {
            ProcessZoomInternal(delta);
        }

        void ProcessZoom(EGRControllerMouseData[] data) {
            Vector3 prevPos0 = data[0].LastPosition - m_Deltas[0];
            Vector3 prevPos1 = data[1].LastPosition - m_Deltas[1];
            float olddeltaMag = (prevPos0 - prevPos1).magnitude;
            float newdeltaMag = (data[0].LastPosition - data[1].LastPosition).magnitude;

            ProcessZoomInternal(newdeltaMag - olddeltaMag);
        }

        void ProcessRotation(Vector3 delta, bool withTween = true, bool withDelta = true) {
            if (m_LastController == null || IsLocked)
                return;

            float factor = Mathf.Clamp01(m_CurrentDistance / 10000f + 0.5f);
            m_TargetRotation.x += delta.x * (withDelta ? Time.deltaTime : 1f) * m_LastController.Sensitivity.x * EGRSettings.GetGlobeSensitivity() * factor;
            m_TargetRotation.y -= delta.y * (withDelta ? Time.deltaTime : 1f) * m_LastController.Sensitivity.y * EGRSettings.GetGlobeSensitivity() * factor;

            //m_TargetRotation.x = ClampAngle(m_TargetRotation.x, float.NegativeInfinity, float.PositiveInfinity);
            m_TargetRotation.y = ClampAngle(m_TargetRotation.y, -80f, 80f);

            if (m_RotTween != null) {
                DOTween.Kill(m_RotTween);
            }

            if (withTween) {
                m_RotTween = DOTween.To(() => m_CurrentRotation, x => m_CurrentRotation = x, m_TargetRotation, 0.4f)
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
            if (m_MapInterface == null) {
                m_MapInterface = EGRScreenManager.Instance.GetScreen<EGRScreenMapInterface>();

                if (m_MapInterface == null)
                    return;
            }

            if (m_MapInterface.ObservedTransformDirty) {
                m_CurrentDistance = m_TargetDistance = m_MapInterface.ObservedTransform.localScale.x * 6.5f;
            }

            Quaternion rotation = Quaternion.Euler(m_CurrentRotation.y, m_CurrentRotation.x, 0);

            Vector3 negDistance = new Vector3(0f, 0f, -m_CurrentDistance);
            Vector3 position = rotation * negDistance + m_MapInterface.ObservedTransform.position;

            if (m_MapInterface.ObservedTransformDirty) {
                m_MapInterface.ObservedTransformDirty = false;
                m_PositionLocked = m_RotationLocked = true;

                m_Camera.transform.DOMove(position, 1f).SetEase(Ease.OutQuad).OnComplete(() => {
                    m_PositionLocked = false;

                    if (!m_InterfaceActive && !m_PositionLocked && !m_RotationLocked) {
                        SetInterfaceState(false, true);
                    }
                });

                m_Camera.transform.DORotate(rotation.eulerAngles, 0.3f).SetEase(Ease.OutSine).OnComplete(() => m_RotationLocked = false);
                m_MapInterface.SetDistanceText($"{(int)(m_CurrentDistance - m_MapInterface.ObservedTransform.localScale.x)}m", true);

                //light
                Vector3 targetRot;
                if (m_MapInterface.ObservedTransform == this)
                    targetRot = m_OriginalLightRotation;
                else
                    targetRot = Quaternion.LookRotation(m_MapInterface.ObservedTransform.position - m_Light.position).eulerAngles;

                m_Light.DORotate(targetRot, 0.3f).SetEase(Ease.OutSine);
            }

            if (m_PositionLocked || m_RotationLocked)
                return;

            m_Camera.transform.rotation = rotation;
            m_Camera.transform.position = position;

            if (m_MapInterface.Visible) {
                m_MapInterface.SetDistanceText($"{(int)(m_CurrentDistance - m_MapInterface.ObservedTransform.localScale.x)}m");
            }

            float transparency = Mathf.Clamp01((Mathf.Min(4200f, m_CurrentDistance) - 3300f) / 3300f);
            float val = m_CloudTransparencyCurve.Evaluate(transparency);
            m_EarthMat.SetColor("_CloudColor", new Color(val, val, val));
        }

        public (Vector3, Vector3) GetSamplePosRot() {
            Quaternion rotation = Quaternion.Euler(m_CurrentRotation.y, m_CurrentRotation.x, 0);

            Vector3 negDistance = new Vector3(0f, 0f, -m_CurrentDistance);
            Vector3 position = rotation * negDistance + m_MapInterface.ObservedTransform.position;

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
                ProcessRotationIdle(new Vector3(10f, 0f), false);

            float targetFov = Client.FOVStabilizer != null ? Client.FOVStabilizer.TargetFOV : TargetFOV;
            m_Camera.fieldOfView += (targetFov - m_Camera.fieldOfView) * Time.deltaTime * 7f;
        }

        float ClampAngle(float angle, float min = 0f, float max = 0f) {
            if (angle < -360f)
                angle += 360f;
            if (angle > 360f)
                angle -= 360f;

            return angle;
            //return Mathf.Clamp(angle, min, max);
        }

        public override void SetInterfaceState(bool active, bool force = false) {
            if (active == m_InterfaceActive && !force)
                return;

            base.SetInterfaceState(active, force);

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

                TargetFOV = EGRScreenSpaceFOV.GetFOV(EGRSettings.SpaceFOV);
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

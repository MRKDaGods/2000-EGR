using DG.Tweening;
using MRK.UI;
using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace MRK.Cameras
{
    public class CameraGlobe : BaseCamera
    {
        private Vector2 _targetRotation;
        private Vector2 _currentRotation;
        private float _currentDistance;
        [SerializeField, Range(100f, 20000f)]
        private float _targetDistance;
        private Vector2 _backupRotation;
        private float _rotationSpeed;
        private float _backupDistance;
        private float _distanceSpeed;
        private object _rotTween;
        private Tween _distTween;
        private Material _earthMat;
        [SerializeField]
        private AnimationCurve _cloudTransparencyCurve;
        private bool _isSwitching;
        private GameObject _dummyRaycastObject;
        private float _timeOfDayRotation;
        private bool _positionLocked;
        private bool _rotationLocked;
        private Transform _light;
        private Vector3 _originalLightRotation;
        [SerializeField]
        private float _minimumDistance = 100f;
        [SerializeField]
        private float _maximumDistance = 10000f;
        [SerializeField]
        private float _thresholdDistance = 110f;
        [SerializeField]
        private float _gestureSpeed = 400f;

        public bool IsLocked
        {
            get { return _positionLocked || _rotationLocked; }
        }

        public float TargetFOV
        {
            get; set;
        }

        public CameraGlobe() : base()
        {
            _rotationSpeed = 8f;
            _distanceSpeed = 8f;
        }

        private void Start()
        {
            _earthMat = Client.GlobalMap.GetComponent<MeshRenderer>().material;

            Client.RegisterControllerReceiver(OnReceiveControllerMessage);

            //update light pos
            //based on day
            //24->360
            //1->15
            //off=-70
            DateTime time = DateTime.UtcNow.AddHours(2d); //convert to GMT+2 (CLT)
            float hrs = time.Hour;
            hrs += time.Minute / 60f;

            _timeOfDayRotation = (hrs * -15f) + 50f - 270f + 50f;
            transform.rotation = Quaternion.Euler(0f, _timeOfDayRotation + 270f, 0f);
            _dummyRaycastObject = new GameObject("Dummy Raycast Object");

            _light = Client.Sun.GetChild(0); //Directional Light
            _originalLightRotation = _light.transform.rotation.eulerAngles; //0,180,0

            TargetFOV = Camera.fieldOfView; //init fov
        }

        private void OnDestroy()
        {
            Client.UnregisterControllerReceiver(OnReceiveControllerMessage);
        }

        private void OnReceiveControllerMessage(InputControllerMessage msg)
        {
            if (!_interfaceActive
                || !gameObject.activeSelf
                || !ShouldProcessControllerMessage(msg))
            {
                return;
            }

            if (msg.ContextualKind == InputControllerMessageContextualKind.Mouse)
            {
                MouseEventKind kind = (MouseEventKind)msg.Payload[0];
                MouseData data = (MouseData)msg.Proposer;

                switch (kind)
                {
                    case MouseEventKind.Down:
                        _down[data.Index] = true;
                        msg.Payload[2] = true;
                        break;

                    case MouseEventKind.Drag:
                        //store delta for zoom
                        _deltas[data.Index] = (Vector3)msg.Payload[2];

                        int touchCount = 0;
                        for (int i = 0; i < 2; i++)
                        {
                            if (_down[i])
                            {
                                touchCount++;
                            }
                        }

                        switch (touchCount)
                        {

                            case 0:
                                break;

                            case 1:
                                if (_down[0])
                                {
                                    ProcessRotation((Vector3)msg.Payload[2]);
                                }
                                break;

                            case 2:
                                if (data.Index == 1)
                                { //handle 2nd touch
                                    ProcessZoom((MouseData[])msg.Payload[3]);
                                }
                                break;

                        }
                        break;

                    case MouseEventKind.Up:
                        _down[data.Index] = false;
                        break;
                }
            }
        }

        private Vector2d GetCurrentGeoPos()
        {
            RaycastHit hit;
            //if (Physics.Raycast(Client.ActiveCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f)), out hit)) {

            _dummyRaycastObject.transform.position = Client.ActiveCamera.transform.position;
            _dummyRaycastObject.transform.rotation = Client.ActiveCamera.transform.rotation;
            _dummyRaycastObject.transform.RotateAround(transform.position, Vector3.up, -_timeOfDayRotation);
            _dummyRaycastObject.transform.LookAt(transform);
            //m_DummyRaycastObject.transform.rotation *= Quaternion.AngleAxis(-m_TimeOfDayRotation, Vector3.up);

            if (Physics.Raycast(_dummyRaycastObject.transform.position, transform.position - _dummyRaycastObject.transform.position, out hit))
            {
                return MapUtils.GeoFromGlobePosition(new Vector3(hit.point.x - transform.position.x, hit.point.y, hit.point.z), transform.localScale.x);
            }

            return Vector2d.zero;
        }

        public void SwitchToFlatMap()
        {
            Client.SetMapMode(EGRMapMode.Flat);
            Client.FlatCamera.SetInitialSetup(GetCurrentGeoPos(), 3f);

            Camera.fieldOfView = EGRConstants.EGR_CAMERA_DEFAULT_FOV; //default fov

            if (!CryptoPlayerPrefs.Get<bool>(EGRConstants.EGR_LOCALPREFS_RUNS_FLAT_MAP, false))
            {
                CryptoPlayerPrefs.Set<bool>(EGRConstants.EGR_LOCALPREFS_RUNS_FLAT_MAP, true);
                CryptoPlayerPrefs.Save();

                ScreenManager.Instance.GetScreen<MapChooser>().ShowScreen();
            }
        }

        private void StartTransitionToFlat(Action callback = null)
        {
            if (!_isSwitching)
            {
                _isSwitching = true;

                if (_distTween != null)
                {
                    DOTween.Kill(_distTween.id);
                }

                //enable my post processing?
                //vignette
                Vignette vig = Client.GetActivePostProcessEffect<Vignette>();
                vig.active = true;

                ChromaticAberration aberration = Client.GetActivePostProcessEffect<ChromaticAberration>();

                RuntimeConfiguration.GlobeSetup setup = Client.RuntimeConfiguration.GlobeSettings;
                Quaternion initialRot = transform.rotation;
                transform.DORotate(new Vector3(0f, initialRot.eulerAngles.y + 720f), setup.TransitionRotationLength, RotateMode.FastBeyond360);

                _targetDistance = _maximumDistance;
                _distTween = DOTween.To(() => _currentDistance, x => _currentDistance = x, _targetDistance, setup.TransitionZoomInLength)
                    .SetEase(Ease.OutSine)
                    .OnUpdate(() => {
                        vig.intensity.value = _distTween.ElapsedPercentage() * 0.65f;
                    })
                    .OnComplete(() => {
                        aberration.active = true;
                        aberration.intensity.value = 0f;

                        _targetDistance = _minimumDistance;

                        _distTween = DOTween.To(() => _currentDistance, x => _currentDistance = x, _targetDistance, setup.TransitionZoomInLength)
                        .SetEase(Ease.InOutExpo)
                        .OnComplete(() => {
                            _isSwitching = false;
                            aberration.active = false;
                            vig.active = false;

                            transform.rotation = initialRot;

                            ScreenManager.MapInterface.SetTransitionTex(Client.CaptureScreenBuffer());

                            SwitchToFlatMap();

                            if (callback != null)
                                callback();
                        })
                        .OnUpdate(() => {
                            float perc = _distTween.ElapsedPercentage();
                            aberration.intensity.value = Mathf.Min(perc * 2f, 1f);
                        })
                        .SetDelay(0.3f);
                    });
            }
        }

        public void SwitchToFlatMapExternal(Action callback)
        {
            StartTransitionToFlat(callback);
        }

        private void ProcessZoomInternal(float rawDelta)
        {
            if (_isSwitching)
                return;

            _targetDistance -= rawDelta * Time.deltaTime * _gestureSpeed * Settings.GetGlobeSensitivity();

            if (_targetDistance < _thresholdDistance && ScreenManager.MapInterface.ObservedTransform == transform)
            {
                StartTransitionToFlat();
                return;
            }

            _targetDistance = Mathf.Clamp(_targetDistance, _minimumDistance, _maximumDistance);

            if (_distTween != null)
            {
                DOTween.Kill(_distTween.id);
            }

            _distTween = DOTween.To(() => _currentDistance, x => _currentDistance = x, _targetDistance, 0.2f).SetEase(Ease.OutQuint);
            _distTween.intId = EGRTweenIDs.IntId;

            _distanceSpeed = 8f;
        }

        public void SetDistanceEased(float dist)
        {
            _targetDistance = dist;
        }

        private void ProcessZoomScroll(float delta)
        {
            ProcessZoomInternal(delta);
        }

        private void ProcessZoom(MouseData[] data)
        {
            Vector3 prevPos0 = data[0].LastPosition - _deltas[0];
            Vector3 prevPos1 = data[1].LastPosition - _deltas[1];
            float olddeltaMag = (prevPos0 - prevPos1).magnitude;
            float newdeltaMag = (data[0].LastPosition - data[1].LastPosition).magnitude;

            ProcessZoomInternal(newdeltaMag - olddeltaMag);
        }

        private void ProcessRotation(Vector3 delta, bool withTween = true, bool withDelta = true)
        {
            if (_lastController == null || IsLocked)
            {
                return;
            }

            float factor = Mathf.Clamp01((_currentDistance / _maximumDistance) + 0.5f);
            _targetRotation.x += delta.x * (withDelta ? Time.deltaTime : 1f) * _lastController.Sensitivity.x * Settings.GetGlobeSensitivity() * factor;
            _targetRotation.y -= delta.y * (withDelta ? Time.deltaTime : 1f) * _lastController.Sensitivity.y * Settings.GetGlobeSensitivity() * factor;

            _targetRotation.y = ClampAngle(_targetRotation.y, -80f, 80f);

            if (_rotTween != null)
            {
                DOTween.Kill(_rotTween);
            }

            if (withTween)
            {
                _rotTween = DOTween.To(() => _currentRotation, x => _currentRotation = x, _targetRotation, 0.4f)
                    .SetEase(Ease.OutQuint);

                _delta[0] = 1f;
            }
            else
            {
                _delta[0] = 0f;
            }

            _rotationSpeed = 2f;
        }

        private void ProcessRotationIdle(Vector3 delta, bool withTween = true, bool withDelta = true)
        {
            _targetRotation.x += delta.x * (withDelta ? Time.deltaTime : 1f);
            _targetRotation.y -= delta.y * (withDelta ? Time.deltaTime : 1f);

            _targetRotation.y = ClampAngle(_targetRotation.y, -80f, 80f);

            _currentRotation = _targetRotation;
            UpdateTransform();
        }

        public void SetDistance(float dist)
        {
            _currentDistance = _backupDistance = _targetDistance = dist;
        }

        public void UpdateTransform()
        {
            if (ScreenManager.MapInterface.ObservedTransformDirty)
            {
                _currentDistance = _targetDistance = ScreenManager.MapInterface.ObservedTransform.lossyScale.x * 6.5f;
            }

            Quaternion rotation = Quaternion.Euler(_currentRotation.y, _currentRotation.x, 0);

            Vector3 negDistance = new Vector3(0f, 0f, -_currentDistance);
            Vector3 position = (rotation * negDistance) + ScreenManager.MapInterface.ObservedTransform.position;

            if (ScreenManager.MapInterface.ObservedTransformDirty)
            {
                ScreenManager.MapInterface.ObservedTransformDirty = false;
                _positionLocked = _rotationLocked = true;

                Camera.transform.DOMove(position, 1f).SetEase(Ease.OutQuad).OnComplete(() => {
                    _positionLocked = false;

                    if (!_interfaceActive && !_positionLocked && !_rotationLocked)
                    {
                        SetInterfaceState(false, true);
                    }
                });

                Camera.transform.DORotate(rotation.eulerAngles, 0.3f).SetEase(Ease.OutSine).OnComplete(() => _rotationLocked = false);
                ScreenManager.MapInterface.SetDistanceText($"{(int)(_currentDistance - ScreenManager.MapInterface.ObservedTransform.localScale.x)}m", true);

                //light
                Vector3 targetRot;
                if (ScreenManager.MapInterface.ObservedTransform == this)
                    targetRot = _originalLightRotation;
                else
                    targetRot = Quaternion.LookRotation(ScreenManager.MapInterface.ObservedTransform.position - _light.position).eulerAngles;

                _light.DORotate(targetRot, 0.3f).SetEase(Ease.OutSine);
            }

            if (_positionLocked || _rotationLocked)
                return;

            Camera.transform.rotation = rotation;
            Camera.transform.position = position;

            if (ScreenManager.MapInterface.Visible)
            {
                ScreenManager.MapInterface.SetDistanceText($"{(int)(_currentDistance - ScreenManager.MapInterface.ObservedTransform.localScale.x)}m");
            }

            float transparency = Mathf.Clamp01((Mathf.Min(4200f, _currentDistance) - 3300f) / 3300f);
            float val = _cloudTransparencyCurve.Evaluate(transparency);
            _earthMat.SetColor("_CloudColor", new Color(val, val, val));
        }

        public (Vector3, Vector3) GetSamplePosRot()
        {
            Quaternion rotation = Quaternion.Euler(_currentRotation.y, _currentRotation.x, 0);

            Vector3 negDistance = new Vector3(0f, 0f, -_currentDistance);
            Vector3 position = (rotation * negDistance) + ScreenManager.MapInterface.ObservedTransform.position;

            return (position, rotation.eulerAngles);
        }

        private void Update()
        {
            if (Client.MapMode != EGRMapMode.Globe || Client.CamDirty)
            {
                return;
            }

            bool updateTransform = _distTween != null || _rotTween != null;

            if (_delta[0] < 1f)
            {
                _delta[0] += Time.deltaTime * _rotationSpeed;
                _delta[0] = Mathf.Clamp01(_delta[0]);
                _currentRotation = Vector2.Lerp(_currentRotation, _targetRotation, _delta[0]);

                updateTransform |= true;
            }

            if (_delta[1] < 1f)
            {
                _delta[1] += Time.deltaTime * _distanceSpeed;
                _delta[1] = Mathf.Clamp01(_delta[1]);
                _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, _delta[1]);

                updateTransform |= true;
            }

#if UNITY_EDITOR
            if (Input.mouseScrollDelta != Vector2.zero)
            {
                ProcessZoomScroll(Input.GetAxis("Mouse ScrollWheel") * 500f);
                updateTransform |= true;
            }
#endif

            if (!_interfaceActive)
            {
                ProcessRotationIdle(new Vector3(10f, 0f), false);
            }

            float targetFov = Client.FOVStabilizer != null ? Client.FOVStabilizer.TargetFOV : TargetFOV;
            Camera.fieldOfView += (targetFov - Camera.fieldOfView) * Time.deltaTime * 7f;

            if (updateTransform)
            {
                UpdateTransform();
            }
        }

        private float ClampAngle(float angle, float min = 0f, float max = 0f)
        {
            if (angle < -360f)
            {
                angle += 360f;
            }

            if (angle > 360f)
            {
                angle -= 360f;
            }

            return angle;
            //return Mathf.Clamp(angle, min, max);
        }

        public override void SetInterfaceState(bool active, bool force = false)
        {
            if (active == _interfaceActive && !force)
            {
                return;
            }

            base.SetInterfaceState(active, force);

            if (_interfaceActive)
            {
                _currentRotation = new Vector2(ClampAngle(_currentRotation.x), ClampAngle(_currentRotation.y));

                //go back to old pos before interface inactivity?
                _targetRotation = _backupRotation;
                _targetDistance = _backupDistance;

                for (int i = 0; i < _delta.Length; i++)
                {
                    _delta[i] = 0f;
                }

                _rotationSpeed = 1f;
                _distanceSpeed = 3f;

                for (int i = 0; i < _down.Length; i++)
                {
                    _down[i] = false;
                }

                TargetFOV = SpaceFOV.GetFOV(Settings.SpaceFOV);
            }
            else
            {
                _backupRotation = _targetRotation;
                _backupDistance = _targetDistance;

                _distanceSpeed = 1f;
                _rotationSpeed = 5f;
                _delta[0] = 1f;
                _delta[1] = 0f;
                _targetDistance = Client.RuntimeConfiguration.GlobeSettings.UnfocusedOffset;
            }
        }
    }
}

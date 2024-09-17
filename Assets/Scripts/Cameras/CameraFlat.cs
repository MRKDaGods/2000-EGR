using DG.Tweening;
using MRK.Maps;
using MRK.UI;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace MRK.Cameras
{
    public class CameraFlat : BaseCamera, IMapController
    {
        private struct RelativeTransform
        {
            public Transform Transform;
            public Vector3Int Position;
        }

        private struct TouchContext
        {
            public float LastDownTime;
            public float LastValidDownTime;
        }

        private Vector2d _currentLatLong;
        private Vector2d _targetLatLong;
        private float _currentZoom;
        private float _targetZoom;
        private float _lastZoomTime;
        private object _panTweenLat;
        private object _panTweenLng;
        private object _zoomTween;
        private HUD _mapInterface;
        private TouchContext _touchCtx0;
        private Vector3 _lastZoomPosition;
        private bool _isInNavigation;
        private float _minViewportZoomLevel;
        private Vector3 _currentRotation;
        private Vector3 _targetRotation;

        private Map Map
        {
            get { return EGR.Instance.FlatMap; }
        }

        public Vector3 MapRotation
        {
            get { return _currentRotation; }
        }

        public CameraFlat() : base()
        {
            _currentZoom = _targetZoom = 2f; //default zoom
        }

        private void Start()
        {
            _mapInterface = ScreenManager.Instance.GetScreen<HUD>();
            Client.RegisterControllerReceiver(OnReceiveControllerMessage);
        }

        private void OnDestroy()
        {
            Client.UnregisterControllerReceiver(OnReceiveControllerMessage);
        }

        public void SetInitialSetup(Vector2d latlng, float zoom)
        {
            _currentLatLong = _targetLatLong = latlng;
            _currentZoom = _targetZoom = zoom;

            /*var pool = ObjectPool<Reference<float>>.Default;

            Reference<float> zoomRef = pool.Rent();
            _Map.FitToBounds(new Vector2d(-MRKMapUtils.LATITUDE_MAX, -MRKMapUtils.LONGITUDE_MAX), 
                new Vector2d(MRKMapUtils.LATITUDE_MAX, MRKMapUtils.LONGITUDE_MAX), 0f, false, zoomRef);

            _MinViewportZoomLevel = zoomRef.Value;
            pool.Free(zoomRef); */

            _minViewportZoomLevel = 0f;
        }

        private void OnReceiveControllerMessage(InputControllerMessage msg)
        {
            if (!_interfaceActive || !gameObject.activeSelf)
                return;

            if (!ShouldProcessControllerMessage(msg, _down[0]))
            {
                ResetStates();

                if (((MouseEventKind)msg.Payload[0]) == MouseEventKind.Down)
                {
                    msg.Payload[2] = true;
                }

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

                        if (data.Index == 0)
                        {
                            _touchCtx0.LastDownTime = Time.time;
                        }

                        _passedThreshold[data.Index] = false;
                        break;

                    case MouseEventKind.Drag:
                        //store delta for zoom
                        _deltas[data.Index] = (Vector3)msg.Payload[2];

                        Vector3 delta = (Vector3)msg.Payload[2];
                        if (!_passedThreshold[data.Index] && delta.sqrMagnitude > 8f)
                        {
                            _passedThreshold[data.Index] = true;
                        }

                        int touchCount = 0;
                        for (int i = 0; i < 2; i++)
                        {
                            if (_down[i])
                                touchCount++;
                        }

                        switch (touchCount)
                        {
                            case 0:
                                break;

                            case 1:

                                if (_down[0] && _passedThreshold[data.Index])
                                {
                                    ProcessPan(delta);
                                }

                                break;

                            case 2:

                                if (data.Index == 1 && _passedThreshold[0] && _passedThreshold[1])
                                { //handle 2nd touch
                                    ProcessZoom((MouseData[])msg.Payload[3]);
                                }

                                break;
                        }
                        break;

                    case MouseEventKind.Up:
                        if (_down[0] && !_passedThreshold[0])
                        {
                            if (Time.time - _lastZoomTime > 0.5f && Time.time - _touchCtx0.LastValidDownTime < 0.2f)
                            {
                                ProcessDoubleClick((Vector3)msg.Payload[1]);
                            }
                            else if (Time.time - _touchCtx0.LastDownTime < 0.1f)
                            {
                                _touchCtx0.LastValidDownTime = Time.time;
                            }
                        }

                        _down[data.Index] = false;
                        break;
                }
            }
        }

        private void ProcessDoubleClick(Vector3 pos)
        {
            _targetZoom += 2f;
            _targetZoom = Mathf.Clamp(_targetZoom, 0f, 21f);

            Client.InputModel.ProcessZoom(ref _currentZoom, ref _targetZoom, () => _currentZoom, x => _currentZoom = x);

            pos.z = Camera.transform.localPosition.y;
            Vector3 wPos = Camera.ScreenToWorldPoint(pos);
            _targetLatLong = Map.WorldToGeoPosition(wPos);

            Client.InputModel.ProcessPan(ref _currentLatLong, ref _targetLatLong, () => _currentLatLong, x => _currentLatLong = x);
        }

        public void KillAllTweens()
        {
            if (_panTweenLat != null)
            {
                DOTween.Kill(_panTweenLat);
            }

            if (_panTweenLng != null)
            {
                DOTween.Kill(_panTweenLng);
            }

            if (_zoomTween != null)
            {
                DOTween.Kill(_zoomTween);
            }
        }

        private void ProcessPan(Vector3 delta)
        {
            if (_lastController == null)
                return;

            if (Time.time - _lastZoomTime < 0.2f)
                return;

            _delta[0] = 0f;

            Vector2d offset2D = new Vector2d(-delta.x, -delta.y) * 3f * Settings.GetMapSensitivity();
            offset2D = Map.ProjectVector(offset2D); //apply necessary map rotation

            float gameobjectScalingMultiplier = Map.transform.localScale.x * Mathf.Pow(2, Map.InitialZoom - Map.AbsoluteZoom);
            Vector2d newLatLong = MapUtils.MetersToLatLon(
                MapUtils.LatLonToMeters(Map.CenterLatLng) + (offset2D / Map.WorldRelativeScale / gameobjectScalingMultiplier)
            );

            _targetLatLong = newLatLong;
            _targetLatLong.x = Mathd.Clamp(_targetLatLong.x, -MapUtils.LatitudeMax, MapUtils.LatitudeMax);
            _targetLatLong.y = Mathd.Clamp(_targetLatLong.y, -MapUtils.LongitudeMax, MapUtils.LongitudeMax);

            Client.InputModel.ProcessPan(ref _currentLatLong, ref _targetLatLong, () => _currentLatLong, x => _currentLatLong = x);
        }

        public void SwitchToGlobe()
        {
            _mapInterface.SetTransitionTex(Client.CaptureScreenBuffer(), null);
            Client.GlobeCamera.SetDistance(Client.RuntimeConfiguration.GlobeSettings.FlatTransitionOffset);
            Client.SetMapMode(EGRMapMode.Globe);
        }

        private void ProcessZoomInternal(float rawDelta)
        {
            _targetZoom += rawDelta * Time.deltaTime * Settings.GetMapSensitivity();

            if (_targetZoom < 0.5f)
            {
                SwitchToGlobe();
                return;
            }

            _targetZoom = Mathf.Clamp(_targetZoom, _minViewportZoomLevel, 21f);

            Client.InputModel.ProcessZoom(ref _currentZoom, ref _targetZoom, () => _currentZoom, x => _currentZoom = x);

            _lastZoomTime = Time.time;
        }

        private void ProcessZoom(MouseData[] data)
        {
            _delta[1] = 0f;
            Vector3 prevPos0 = data[0].LastPosition - _deltas[0];
            Vector3 prevPos1 = data[1].LastPosition - _deltas[1];

            float olddeltaMag = (prevPos0 - prevPos1).magnitude;
            float newdeltaMag = (data[0].LastPosition - data[1].LastPosition).magnitude;

            _lastZoomPosition = (data[0].LastPosition + data[1].LastPosition) * 0.5f;
            ProcessZoomInternal(newdeltaMag - olddeltaMag);
        }

        private void ProcessZoomScroll(float delta)
        {
            _lastZoomPosition = Input.mousePosition;
            ProcessZoomInternal(delta * 100f);
        }

        private void UpdateTransform()
        {
            /* if (Client.InputModel is EGRInputModelMRK) {
                if (((EGRInputModelMRK)Client.InputModel).ZoomContext.CanUpdate) {
                    Vector3 mousePosScreen = _LastZoomPosition;
                    mousePosScreen.z = _Camera.transform.localPosition.y;
                    Vector3 _mousePosition = _Camera.ScreenToWorldPoint(mousePosScreen);
                    Vector2d geo = _Map.WorldToGeoPosition(_mousePosition);
                    Vector2d pos1 = MRKMapUtils.LatLonToMeters(geo);

                    _Map.UpdateMap(_Map.CenterLatLng, _CurrentZoom);
                    geo = _Map.WorldToGeoPosition(_mousePosition);

                    Vector2d pos2 = MRKMapUtils.LatLonToMeters(geo);
                    Vector2d delta = pos2 - pos1;
                    _CurrentLatLong = _TargetLatLong = MRKMapUtils.MetersToLatLon(_Map.CenterMercator - delta);
                }
            } */

            Map.UpdateMap(_currentLatLong, _currentZoom);
            Map.transform.rotation = Quaternion.Euler(_currentRotation.x, _currentRotation.y, _currentRotation.z);
        }

        private void Update()
        {
            UpdateTransform();

#if UNITY_EDITOR
            if (Input.mouseScrollDelta != Vector2.zero)
            {
                ProcessZoomScroll(Input.GetAxis("Mouse ScrollWheel") * 10f);
            }
#endif
        }

        public Vector3 GetMapVelocity()
        {
            return new Vector3((float)(_targetLatLong.x - _currentLatLong.x), (float)(_targetLatLong.y - _currentLatLong.y), _targetZoom - _currentZoom) * 5f;
        }

        public void EnterNavigation()
        {
            if (!_isInNavigation)
            {
                _isInNavigation = true;

                //_Camera.transform.DORotate(new Vector3(50f, 0f), 1f).SetEase(Ease.OutSine).OnUpdate(() => ProcessPan(new Vector3(0f, 100f) * Time.deltaTime));

                //UpdateMapViewingAngles(40f);
            }
        }

        public void ExitNavigation()
        {
            if (_isInNavigation)
            {
                _isInNavigation = false;
                UpdateMapViewingAngles();
            }
        }

        public void SetCenterAndZoom(Vector2d? targetCenter = null, float? targetZoom = null)
        {
            if (targetCenter.HasValue)
            {
                _targetLatLong = targetCenter.Value;
                Client.InputModel.ProcessPan(ref _currentLatLong, ref _targetLatLong, () => _currentLatLong, x => _currentLatLong = x);
            }

            if (targetZoom.HasValue)
            {
                _targetZoom = targetZoom.Value;
                Client.InputModel.ProcessZoom(ref _currentZoom, ref _targetZoom, () => _currentZoom, x => _currentZoom = x);
            }
        }

        public void SetRotation(Vector3 rotation)
        {
            _targetRotation = rotation;
            Client.InputModel.ProcessRotation(ref _currentRotation, ref _targetRotation, () => _currentRotation, x => _currentRotation = x);
        }

        public void UpdateMapViewingAngles(float? target = null, float? startValue = null)
        {
            LensDistortion lens = Client.GetActivePostProcessEffect<LensDistortion>();
            DOTween.To(() => lens.intensity.value, x => lens.intensity.value = x, target ?? Settings.GetCurrentMapViewingAngle(), 1f)
                .ChangeStartValue(startValue ?? lens.intensity.value)
                .SetEase(Ease.OutBack);
        }

        public void TeleportToLocationTweened(Vector2d target)
        {
            //zoom from z to 4
            DOTween.To(
                () => _currentZoom,
                x => _currentZoom = x,
                4f,
                1f
            ).SetEase(Ease.OutSine)
            .OnComplete(() => {
                _targetZoom = _currentZoom;

                //center to target
                DOTween.To(
                    () => _currentLatLong.x,
                    x => _currentLatLong.x = x,
                    target.x,
                    1f
                );

                DOTween.To(
                    () => _currentLatLong.y,
                    x => _currentLatLong.y = x,
                    target.y,
                    1f
                ).OnComplete(() => {
                    _targetLatLong = _currentLatLong;

                    //zoom to 17
                    DOTween.To(() => _currentZoom,
                        x => _currentZoom = x,
                        17f,
                        1f
                    ).OnComplete(() => {
                        _targetZoom = _currentZoom;
                    });
                });
            });
        }
    }
}

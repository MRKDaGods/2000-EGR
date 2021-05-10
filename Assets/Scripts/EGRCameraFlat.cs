﻿using DG.Tweening;
using MRK.UI;
using System.Collections.Generic;
using UnityEngine;

namespace MRK {
    public class EGRCameraFlat : EGRCamera {
        struct RelativeTransform {
            public Transform Transform;
            public Vector3Int Position;
        }

        Vector2d m_CurrentLatLong;
        Vector2d m_TargetLatLong;
        float m_CurrentZoom;
        float m_TargetZoom;
        float m_LastZoomTime;
        readonly List<Transform> m_ExTiles;
        GameObject m_ExContainer;
        RelativeTransform[] m_RelativeGrid;
        Vector3 m_OldMapScale;
        float m_MapDelta;
        bool m_MapDirty;
        object m_PanTweenLat;
        object m_PanTweenLng;
        object m_ZoomTween;
        EGRScreenMapInterface m_MapInterface;

        MRKMap m_Map => EGRMain.Instance.FlatMap;

        public EGRCameraFlat() : base() {
            m_CurrentZoom = m_TargetZoom = 2f; //default zoom

            m_ExTiles = new List<Transform>();
        }

        void Start() {
            Client.RegisterControllerReceiver(OnReceiveControllerMessage);
            //m_Map.OnEGRMapZoomUpdated += OnMapZoomUpdated;

            m_ExContainer = new GameObject("EGRMapExx");
            m_MapInterface = EGRScreenManager.Instance.GetScreen<EGRScreenMapInterface>();
        }

        void OnDestroy() {
            Client.UnregisterControllerReceiver(OnReceiveControllerMessage);
            //m_Map.OnEGRMapZoomUpdated -= OnMapZoomUpdated;
        }

        public void SetInitialSetup(Vector2d latlng, float zoom) {
            m_CurrentLatLong = m_TargetLatLong = latlng;
            m_CurrentZoom = m_TargetZoom = zoom;
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

                        m_Down[data.Index] = false;

                        break;

                }
            }
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

            var offset2D = new Vector2d(-delta.x * 3f * EGRSettings.SensitivityMapX, -delta.y * 3f * EGRSettings.SensitivityMapY);
            var gameobjectScalingMultiplier = m_Map.transform.localScale.x * (Mathf.Pow(2, (m_Map.InitialZoom - m_Map.AbsoluteZoom)));
            var newLatLong = MRKMapUtils.MetersToLatLon(
                MRKMapUtils.LatLonToMeters(m_Map.CenterLatLng) + (offset2D / m_Map.WorldRelativeScale) / gameobjectScalingMultiplier);

            //float factor = 2f * Conversions.GetTileScaleInMeters((float)m_Map.CenterLatitudeLongitude.x, m_Map.AbsoluteZoom) / m_Map.UnityTileSize;
            //Vector2d latlongDelta = Conversions.MetersToLatLon(new Vector2d(-delta.x * factor * m_LastController.Sensitivity.x, -delta.y * factor * m_LastController.Sensitivity.y));
            m_TargetLatLong = newLatLong; //m_Map.CenterLatitudeLongitude + latlongDelta;

            if (m_PanTweenLat != null) {
                DOTween.Kill(m_PanTweenLat);
            }

            if (m_PanTweenLng != null) {
                DOTween.Kill(m_PanTweenLng);
            }

            m_PanTweenLat = DOTween.To(() => m_CurrentLatLong.x, x => m_CurrentLatLong.x = x, m_TargetLatLong.x, 0.7f)
                .SetEase(Ease.OutBack);

            m_PanTweenLng = DOTween.To(() => m_CurrentLatLong.y, x => m_CurrentLatLong.y = x, m_TargetLatLong.y, 0.7f)
                .SetEase(Ease.OutBack);
        }

        void ProcessZoom(EGRControllerMouseData[] data) {
            //if (m_MapInterface.IsInTransition)
            //    return;

            m_Delta[1] = 0f;
            Vector3 prevPos0 = data[0].LastPosition - m_Deltas[0];
            Vector3 prevPos1 = data[1].LastPosition - m_Deltas[1];
            float olddeltaMag = (prevPos0 - prevPos1).magnitude;
            float newdeltaMag = (data[0].LastPosition - data[1].LastPosition).magnitude;

            m_TargetZoom += (newdeltaMag - olddeltaMag) * Time.deltaTime * 2f * EGRSettings.SensitivityMapZ;

            if (m_TargetZoom < 0.5f) {
                m_MapInterface.SetTransitionTex(Client.CaptureScreenBuffer());
                (Client.GlobalMap.GetComponent<EGRCameraGlobe>()).SetDistance(7000f);
                Client.SetMapMode(EGRMapMode.Globe);

                /* if (!EGRFadeManager.IsFading) {
                    EGRUITextRenderer.Render("EGR\nUNIVERSE", 1f, 1);

                    EGRFadeManager.Fade(1f, 2f, () => {
                        (Client.GlobalMap.GetComponent<EGRCameraGlobe>()).SetDistance(7000f);
                        Client.SetMapMode(EGRMapMode.Globe);
                    });
                } */
            }

            m_TargetZoom = Mathf.Clamp(m_TargetZoom, 0f, 21f);

            m_LastZoomTime = Time.time;

            if (m_ZoomTween != null) {
                DOTween.Kill(m_ZoomTween);
            }

            m_ZoomTween = DOTween.To(() => m_CurrentZoom, x => m_CurrentZoom = x, m_TargetZoom, 0.7f)
                .SetEase(Ease.OutBack);
        }

        void ProcessZoomScroll(float delta) {
            //if (m_MapInterface.IsInTransition)
            //    return;

            m_TargetZoom += delta * Time.deltaTime * 100f * EGRSettings.SensitivityMapZ;

            if (m_TargetZoom < 0.5f) {
                m_MapInterface.SetTransitionTex(Client.CaptureScreenBuffer());
                (Client.GlobalMap.GetComponent<EGRCameraGlobe>()).SetDistance(7000f);
                Client.SetMapMode(EGRMapMode.Globe);

                /* if (!EGRFadeManager.IsFading) {
                    EGRUITextRenderer.Render("EGR\nUNIVERSE", 1f, 1);

                    EGRFadeManager.Fade(1f, 2f, () => {
                        (Client.GlobalMap.GetComponent<EGRCameraGlobe>()).SetDistance(7000f);
                        Client.SetMapMode(EGRMapMode.Globe);
                    });
                } */
            }

            m_TargetZoom = Mathf.Clamp(m_TargetZoom, 0f, 21f);
            m_LastZoomTime = Time.time;

            if (m_ZoomTween != null) {
                DOTween.Kill(m_ZoomTween);
            }

            m_ZoomTween = DOTween.To(() => m_CurrentZoom, x => m_CurrentZoom = x, m_TargetZoom, 0.7f)
                .SetEase(Ease.OutSine);
        }

        void UpdateTransform() {
            m_Map.UpdateMap(m_CurrentLatLong, m_CurrentZoom);
        }

        void Update() {
            /*if (m_Delta[0] < 1f) {
                m_Delta[0] += Time.deltaTime * 4f;
                m_CurrentLatLong = Vector2d.Lerp(m_CurrentLatLong, m_TargetLatLong, m_Delta[0]);

                UpdateTransform();
            }

            if (m_Delta[1] < 1f) {
                m_Delta[1] += Time.deltaTime * 4f;
                m_CurrentZoom = Mathf.Lerp(m_CurrentZoom, m_TargetZoom, m_Delta[1]);

                UpdateTransform();
            }*/

            UpdateTransform();

            if (m_MapDirty) {
                m_MapDelta += Time.deltaTime * 2f;

                m_MapDelta = Mathf.Clamp01(m_MapDelta);

                foreach (Transform trans in m_ExTiles) {
                    trans.GetComponent<MeshRenderer>().material.SetFloat("_Alpha", 1f - m_MapDelta);
                }

                if (m_MapDelta >= 1f) {
                    m_MapDirty = false;
                }
            }

#if UNITY_EDITOR
            if (Input.mouseScrollDelta != Vector2.zero) {
                ProcessZoomScroll(Input.GetAxis("Mouse ScrollWheel") * 10f);
            }
#endif
        }

        void OnMapZoomUpdated(int z1, int z2) {
            Debug.Log($"Zoom updated from {z1} to {z2}");

            m_OldMapScale = m_Map.transform.localScale;
            //StartCoroutine(UpdateExTiles());
        }

        void OnGUI() {
            if (m_RelativeGrid == null)
                return;

            for (int i = 0; i < m_RelativeGrid.Length; i++) {
                Transform t = m_RelativeGrid[i].Transform;
                if (t == null)
                    continue;

                Vector3 spos = m_Camera.WorldToScreenPoint(t.position);
                if (spos.z > 0f) {
                    spos.y = Screen.height - spos.y;

                    GUI.Label(new Rect(spos.x, spos.y, 100f, 130f), $"<b><color=white><size=50>{i}</size></color></b>");
                }
            }
        }

        /*IEnumerator UpdateExTiles() {
            yield return new WaitForEndOfFrame();

            m_ExContainer.transform.position = m_Map.transform.position;
            m_ExContainer.transform.localScale = m_Map.transform.localScale;

            int tileCount = m_Map.transform.childCount;

            List<Transform> realTiles = new List<Transform>();
            for (int i = 0; i < tileCount; i++) {
                Transform t = m_Map.transform.GetChild(i);
                if (!t.name.Contains("Provider"))
                    realTiles.Add(t);
            }

            tileCount = realTiles.Count;

            int tileCountDif = tileCount - m_ExTiles.Count;
            if (tileCountDif > 0) {
                int countPre = m_ExTiles.Count;
                for (int i = 0; i < tileCountDif; i++) {
                    GameObject exTrans = Instantiate(realTiles[countPre + i].gameObject, m_ExContainer.transform, true);
                    Destroy(exTrans.GetComponent<UnityTile>());

                    exTrans.GetComponent<MeshRenderer>().material = new Material(realTiles[countPre + i].GetComponent<MeshRenderer>().material);

                    m_ExTiles.Add(exTrans.transform);
                }
            }

            if (tileCountDif < 0) {
                int countPre = m_ExTiles.Count;
                for (int i = -tileCountDif; i > 0; i--) {
                    int idx = countPre - i;
                    Destroy(m_ExTiles[idx]);
                    m_ExTiles.RemoveAt(idx);
                }
            }

            int minX = 10000, minY = 10000, minZ = 10000, maxY = 0;

            for (int i = 0; i < tileCount; i++) {
                UnityTile tile = realTiles[i].GetComponent<UnityTile>();
                minX = Mathf.Min(minX, tile.OldUnwrappedTileID.X);
                minY = Mathf.Min(minY, tile.OldUnwrappedTileID.Y);
                minZ = Mathf.Min(minZ, tile.OldUnwrappedTileID.Z);
                maxY = Mathf.Max(maxY, tile.OldUnwrappedTileID.Y);
            }

            m_RelativeGrid = new RelativeTransform[tileCount];

            for (int i = 0; i < tileCount; i++) {
                UnityTile tile = realTiles[i].GetComponent<UnityTile>();
                
                Vector3Int relPos = new Vector3Int(tile.OldUnwrappedTileID.X - minX, tile.OldUnwrappedTileID.Y - minY, tile.OldUnwrappedTileID.Z - minZ);
                int relIdx = relPos.y * (maxY - minY + 1) + relPos.x;
                m_RelativeGrid[relIdx] = new RelativeTransform { Transform = tile.transform, Position = relPos };
            }

            for (int i = 0; i < tileCount; i++) {
                RelativeTransform relativeTransform = m_RelativeGrid[i];
                UnityTile uTile = relativeTransform.Transform.GetComponent<UnityTile>();
                UnityTile.EGRTransformProperties tProps = uTile.OldTransformProperties;

                Transform extileTrans = m_ExTiles[i];

                extileTrans.name = tProps.Name;
                extileTrans.position = tProps.Position + Vector3.up * 5f;
                extileTrans.localScale = tProps.Scale;
                extileTrans.SetSiblingIndex(i);

                MeshRenderer meshRenderer = extileTrans.GetComponent<MeshRenderer>();
                meshRenderer.material.mainTexture = Instantiate(uTile.m_OldTex);
                meshRenderer.material.SetFloat("_Alpha", 1f);
            }

            m_MapDirty = true;
            m_MapDelta = 0f;
        }*/
    }
}
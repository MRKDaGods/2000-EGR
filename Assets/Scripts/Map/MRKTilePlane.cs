using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

namespace MRK {
    public class MRKTilePlane : EGRBehaviour {
        static Mesh ms_TileMesh;
        static float ms_LastAssignedTileMeshSize;
        readonly static ObjectPool<Material> ms_MaterialPool;
        MeshFilter m_MeshFilter;
        MeshRenderer m_MeshRenderer;
        float m_DissolveValue;
        Material m_Material;
        float m_WorldRelativeScale;
        Rectd m_Rect;
        int m_AbsoluteZoom;
        int m_Tween;
        int m_SiblingIdx;
        bool m_MapHasHigherZoom;

        static MRKTilePlane() {
            ms_MaterialPool = new ObjectPool<Material>(() => {
                return Instantiate(EGRMain.Instance.FlatMap.TilePlaneMaterial);
            });
        }

        public void InitPlane(Texture2D tex, float size, Rectd rect, int zoom, Func<bool> killPredicate, int siblingIdx) {
            if (tex == null) {
                RecyclePlane();
                return;
            }

            m_SiblingIdx = -1;

            Material stolenMaterial = null;
            MRKTilePlane plane = Client.FlatMap.ActivePlanes.Find(x => x.m_SiblingIdx == siblingIdx);
            if (plane != null) {
                stolenMaterial = plane.m_Material; //steal their mat
                plane.RecyclePlane(false);
            }

            m_SiblingIdx = siblingIdx;

            gameObject.SetActive(true);

            m_WorldRelativeScale = size / (float)rect.Size.x;
            m_Rect = rect;
            m_AbsoluteZoom = zoom;

            UpdatePlane();

            if (ms_TileMesh == null || ms_LastAssignedTileMeshSize != size)
                CreateTileMesh(size);

            if (m_MeshFilter == null) {
                m_MeshFilter = gameObject.AddComponent<MeshFilter>();
                m_MeshFilter.mesh = ms_TileMesh;
            }

            m_Material = stolenMaterial ?? ms_MaterialPool.Rent();
            if (stolenMaterial == null)
                m_Material.mainTexture = tex;

            m_DissolveValue = 0f;
            m_Material.SetFloat("_Emission", Client.FlatMap.GetDesiredTilesetEmission());
            m_Material.SetFloat("_Amount", 0f);

            if (m_MeshRenderer == null) {
                m_MeshRenderer = gameObject.AddComponent<MeshRenderer>();
            }
            m_MeshRenderer.material = m_Material;

            StartCoroutine(KillPlane(killPredicate));

            m_Tween = -999;
            m_MapHasHigherZoom = Client.FlatMap.AbsoluteZoom > m_AbsoluteZoom;
        }

        IEnumerator KillPlane(Func<bool> killPredicate) {
            while (!killPredicate()) {
                UpdatePlane();
                yield return new WaitForEndOfFrame();
            }

            m_Tween = DOTween.To(() => m_DissolveValue, x => m_DissolveValue = x, 1f, m_MapHasHigherZoom ? 0.6f : 0.3f)
                .OnUpdate(() => {
                    if (m_Material != null) m_Material.SetFloat("_Amount", m_DissolveValue);
                })
                .OnComplete(() => RecyclePlane())
                .SetEase(Ease.OutSine)
                .intId = EGRTweenIDs.IntId;
        }

        public void UpdatePlane() {
            MRKTile.PlaneContainer.transform.localScale = Client.FlatMap.transform.localScale;
            MRKTile.PlaneContainer.transform.rotation = Client.FlatMap.transform.rotation;

            float scaleFactor = Mathf.Pow(2, Client.FlatMap.InitialZoom - m_AbsoluteZoom);
            transform.localScale = Vector3.one * scaleFactor;

            Vector2d mercator = Client.FlatMap.CenterMercator;
            transform.localPosition = new Vector3((float)(m_Rect.Center.x - mercator.x) * m_WorldRelativeScale * scaleFactor, 0f,
                     (float)(m_Rect.Center.y - mercator.y) * m_WorldRelativeScale * scaleFactor);
        }

        public void RecyclePlane(bool destroyMat = true) {
            if (m_Tween.IsValidTween())
                DOTween.Kill(m_Tween);

            if (m_MeshRenderer != null) {
                m_MeshRenderer.material = null;
            }

            if (m_Material != null && destroyMat) {
                m_Material.mainTexture = null;
                ms_MaterialPool.Free(m_Material);
                m_Material = null;
            }

            StopAllCoroutines();
            gameObject.SetActive(false);
            MRKTile.PlanePool.Free(this);
        }

        static void CreateTileMesh(float size) {
            float halfSize = size / 2;
            ms_TileMesh = new Mesh {
                vertices = new Vector3[4] { new Vector3(-halfSize, 0f, -halfSize), new Vector3(halfSize, 0f, -halfSize),
                    new Vector3(halfSize, 0, halfSize), new Vector3(-halfSize, 0, halfSize) },
                normals = new Vector3[4] { Vector3.up, Vector3.up, Vector3.up, Vector3.up },
                triangles = new int[6] { 0, 2, 1, 0, 3, 2 },
                uv = new Vector2[4] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) }
            };
        }
    }
}

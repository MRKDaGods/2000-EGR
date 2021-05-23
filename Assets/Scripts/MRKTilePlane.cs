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

        static MRKTilePlane() {
            ms_MaterialPool = new ObjectPool<Material>(() => {
                return Instantiate(EGRMain.Instance.FlatMap.TilePlaneMaterial);
            });
        }

        public void InitPlane(Vector3 position, Vector3 scale, Texture2D tex, float size, Func<bool> killPredicate) {
            if (tex == null) {
                RecyclePlane();
                return;
            }

            gameObject.SetActive(true);

            transform.position = position;
            transform.localScale = scale;

            if (ms_TileMesh == null || ms_LastAssignedTileMeshSize != size)
                CreateTileMesh(size);

            if (m_MeshFilter == null) {
                m_MeshFilter = gameObject.AddComponent<MeshFilter>();
                m_MeshFilter.mesh = ms_TileMesh;
            }

            m_Material = ms_MaterialPool.Rent();
            m_Material.mainTexture = tex;
            m_DissolveValue = 0f;
            m_Material.SetFloat("_Emission", Client.FlatMap.GetDesiredTilesetEmission());
            m_Material.SetFloat("_Amount", 0f);

            if (m_MeshRenderer == null) {
                m_MeshRenderer = gameObject.AddComponent<MeshRenderer>();
            }
            m_MeshRenderer.material = m_Material;

            StartCoroutine(KillPlane(killPredicate));
        }

        IEnumerator KillPlane(Func<bool> killPredicate) {
            while (!killPredicate())
                yield return new WaitForSeconds(0.04f);

            DOTween.To(() => m_DissolveValue, x => m_DissolveValue = x, 1f, 0.7f)
                .OnUpdate(() => m_Material.SetFloat("_Amount", m_DissolveValue))
                .OnComplete(() => RecyclePlane())
                .SetEase(Ease.OutSine);
        }

        public void RecyclePlane(bool remove = true) {
            if (m_MeshRenderer != null) {
                m_MeshRenderer.material = null;
            }

            if (m_Material != null) {
                m_Material.mainTexture = null;
                ms_MaterialPool.Free(m_Material);
                m_Material = null;
            }

            StopAllCoroutines();
            gameObject.SetActive(false);
            MRKTile.PlanePool.Free(this);

            if (remove) {
                //Client.FlatMap.ActivePlanes.Remove(this);
            }
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

using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

namespace MRK.Maps
{
    public class TilePlane : BaseBehaviour
    {
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private float _dissolveValue;
        private Material _material;
        private float _worldRelativeScale;
        private Rectd _rect;
        private int _absoluteZoom;
        private int _tween;
        private int _siblingIdx;
        private bool _mapHasHigherZoom;

        private static Mesh _tileMesh;
        private static float _lastAssignedTileMeshSize;
        private static readonly ObjectPool<Material> _materialPool;

        static TilePlane()
        {
            _materialPool = new ObjectPool<Material>(() =>
            {
                return Instantiate(EGR.Instance.FlatMap.TilePlaneMaterial);
            });
        }

        public void InitPlane(Texture2D tex, float size, Rectd rect, int zoom, Func<bool> killPredicate, int siblingIdx)
        {
            if (tex == null)
            {
                RecyclePlane();
                return;
            }

            _siblingIdx = -1;

            Material stolenMaterial = null;
            TilePlane plane = Client.FlatMap.ActivePlanes.Find(x => x._siblingIdx == siblingIdx);
            if (plane != null)
            {
                stolenMaterial = plane._material; //steal their mat
                plane.RecyclePlane(false);
            }

            _siblingIdx = siblingIdx;

            gameObject.SetActive(true);

            _worldRelativeScale = size / (float)rect.Size.x;
            _rect = rect;
            _absoluteZoom = zoom;

            UpdatePlane();

            if (_tileMesh == null || _lastAssignedTileMeshSize != size)
                CreateTileMesh(size);

            if (_meshFilter == null)
            {
                _meshFilter = gameObject.AddComponent<MeshFilter>();
                _meshFilter.mesh = _tileMesh;
            }

            _material = stolenMaterial ?? _materialPool.Rent();
            if (stolenMaterial == null)
            {
                _material.mainTexture = tex;
            }

            _dissolveValue = 0f;
            _material.SetFloat("_Emission", Client.FlatMap.GetDesiredTilesetEmission());
            _material.SetFloat("_Amount", 0f);

            if (_meshRenderer == null)
            {
                _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            _meshRenderer.material = _material;

            StartCoroutine(KillPlane(killPredicate));

            _tween = -999;
            _mapHasHigherZoom = Client.FlatMap.AbsoluteZoom > _absoluteZoom;
        }

        private IEnumerator KillPlane(Func<bool> killPredicate)
        {
            while (!killPredicate())
            {
                UpdatePlane();
                yield return new WaitForEndOfFrame();
            }

            _tween = DOTween.To(
                () => _dissolveValue,
                x => _dissolveValue = x,
                1f,
                _mapHasHigherZoom ? 0.6f : 0.3f)
            .OnUpdate(
                () =>
                {
                    if (_material != null)
                    {
                        _material.SetFloat("_Amount", _dissolveValue);
                    }
                })
            .OnComplete(() => RecyclePlane())
            .SetEase(Ease.OutSine)
            .intId = EGRTweenIDs.IntId;
        }

        public void UpdatePlane()
        {
            Tile.PlaneContainer.transform.localScale = Client.FlatMap.transform.localScale;
            Tile.PlaneContainer.transform.rotation = Client.FlatMap.transform.rotation;

            float scaleFactor = Mathf.Pow(2, Client.FlatMap.InitialZoom - _absoluteZoom);
            transform.localScale = Vector3.one * scaleFactor;

            Vector2d mercator = Client.FlatMap.CenterMercator;
            transform.localPosition = new Vector3((float)(_rect.Center.x - mercator.x) * _worldRelativeScale * scaleFactor, 0f,
                     (float)(_rect.Center.y - mercator.y) * _worldRelativeScale * scaleFactor);

            transform.localEulerAngles = Vector3.zero;
        }

        public void RecyclePlane(bool destroyMat = true)
        {
            if (_tween.IsValidTween())
            {
                DOTween.Kill(_tween);
            }

            if (_meshRenderer != null)
            {
                _meshRenderer.material = null;
            }

            if (_material != null && destroyMat)
            {
                _material.mainTexture = null;
                _materialPool.Free(_material);
                _material = null;
            }

            StopAllCoroutines();
            gameObject.SetActive(false);
            Tile.PlanePool.Free(this);
        }

        private static void CreateTileMesh(float size)
        {
            float halfSize = size / 2;
            _tileMesh = new Mesh
            {
                vertices = new Vector3[4] { new Vector3(-halfSize, 0f, -halfSize), new Vector3(halfSize, 0f, -halfSize),
                    new Vector3(halfSize, 0, halfSize), new Vector3(-halfSize, 0, halfSize) },
                normals = new Vector3[4] { Vector3.up, Vector3.up, Vector3.up, Vector3.up },
                triangles = new int[6] { 0, 2, 1, 0, 3, 2 },
                uv = new Vector2[4] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) }
            };
        }
    }
}

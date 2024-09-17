using System.Collections.Generic;
using UnityEngine;
using Vectrosity;

namespace MRK
{
    public class ARManager : BaseBehaviour
    {
        [SerializeField]
        private bool _isListening;
        [SerializeField]
        private List<Vector2d> _coords;
        [SerializeField]
        private Texture2D _lineTex;
        [SerializeField]
        private Material _lineMaterial;
        private VectorLine _vL;
        private bool _fixedCanvas;

        private void Start()
        {
            _vL = new VectorLine("LR", new List<Vector3>(), _lineTex, 14f, LineType.Continuous, Joins.Weld);
            _vL.material = _lineMaterial;
            Client.FlatMap.MapUpdated += OnMapUpdated;
        }

        private void Update()
        {
            if (_isListening && Input.GetMouseButtonUp(0))
            {
                if (!_fixedCanvas)
                {
                    _fixedCanvas = true;
                    VectorLine.canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    VectorLine.canvas.worldCamera = Client.ActiveCamera;
                }

                Vector3 mousePos = Input.mousePosition;
                mousePos.z = Client.ActiveCamera.transform.position.y;

                Vector3 wPos = Client.ActiveCamera.ScreenToWorldPoint(mousePos);
                Vector2d coord = Client.FlatMap.WorldToGeoPosition(wPos);

                _coords.Add(coord);

                Debug.Log($"Added coord {coord}");

                _vL.points3.Clear();
                foreach (Vector2d geoLoc in _coords)
                {
                    Vector3 worldPos = Client.FlatMap.GeoToWorldPosition(geoLoc);
                    worldPos.y = 0.1f;
                    _vL.points3.Add(worldPos);
                }

                _vL.Draw();
            }
        }

        private void OnMapUpdated()
        {
            _vL.points3.Clear();
            foreach (Vector2d geoLoc in _coords)
            {
                Vector3 worldPos = Client.FlatMap.GeoToWorldPosition(geoLoc);
                worldPos.y = 0.1f;
                _vL.points3.Add(worldPos);
            }

            _vL.Draw();
        }
    }
}

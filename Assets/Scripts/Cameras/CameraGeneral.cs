using UnityEngine;

namespace MRK.Cameras
{
    public class CameraGeneral : BaseCamera
    {
        [SerializeField]
        private float _radiusX;
        [SerializeField]
        private float _radiusY;
        [SerializeField]
        private float _radiusZ;
        [SerializeField]
        private int _pointCount;
        private readonly Vector3[] _bounds;
        private Vector3[] m_Points;
        private int _pointIdx;

        public CameraGeneral() : base()
        {
            _bounds = new Vector3[8] {
                new Vector3(0f, 0f, 0f),
                new Vector3(1, 0f, 0f),
                new Vector3(1f, 0f, 1f),
                new Vector3(0f, 0f, 1f),

                new Vector3(0f, 1f, 0f),
                new Vector3(1f, 1f, 0f),
                new Vector3(1f, 1f, 1f),
                new Vector3(0f, 1f, 1f)
            };
        }

        private void Start()
        {
            //position cam!
            transform.position = new Vector3(-_radiusX / 2f, -_radiusY / 2f, -_radiusZ / 2f);

            m_Points = new Vector3[_pointCount];

            //generate points

            //0, 0, 0
            Vector3 minPoint = GetRealPosition(_bounds[0]);
            //1, 1, 1
            Vector3 maxPoint = GetRealPosition(_bounds[6]);

            for (int i = 0; i < _pointCount; i++)
            {
                m_Points[i] = new Vector3(Random.Range(minPoint.x, maxPoint.x), Random.Range(minPoint.y, maxPoint.y), Random.Range(minPoint.z, maxPoint.z));

                //if (Application.isPlaying && i % 2 == 0)
                //    Instantiate(obj, m_Points[i], Quaternion.identity);
            }
        }

        private Vector3 GetRealPosition(Vector3 rel)
        {
            return transform.position + new Vector3(rel.x * _radiusX, rel.y * _radiusY, rel.z * _radiusZ);
        }

        private void RelDrawLine(int i1, int i2)
        {
            Gizmos.DrawLine(GetRealPosition(_bounds[i1]), GetRealPosition(_bounds[i2]));
        }

        private void OnDrawGizmos()
        {
            if (m_Points == null || m_Points.Length == 0)
            {
                Start();
            }

            for (int i = 0; i < m_Points.Length; i++)
            {
                Vector3 curPoint = m_Points[i];

                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(curPoint, 50f);

                int nextPointIdx = (i + 1) % m_Points.Length;
                Vector3 nextPointPos = m_Points[nextPointIdx];

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(curPoint, nextPointPos);
            }

            Gizmos.color = Color.red;

            RelDrawLine(0, 1);
            RelDrawLine(1, 2);
            RelDrawLine(2, 3);
            RelDrawLine(3, 0);

            RelDrawLine(4, 5);
            RelDrawLine(5, 6);
            RelDrawLine(6, 7);
            RelDrawLine(7, 4);

            RelDrawLine(0, 4);
            RelDrawLine(3, 7);
            RelDrawLine(1, 5);
            RelDrawLine(2, 6);
        }

        private void Update()
        {
            if (_delta[0] < 1f)
            {
                int nextPointIdx = (_pointIdx + 1) % m_Points.Length;
                int prevPointIdx = _pointIdx - 1;
                if (prevPointIdx == -1)
                    prevPointIdx = m_Points.Length - 1;

                Vector3 nextPointPos = m_Points[nextPointIdx];
                Vector3 curPointPos = m_Points[_pointIdx];
                Vector3 prevPointPos = m_Points[prevPointIdx];

                _delta[0] += Time.deltaTime / (Vector3.Distance(curPointPos, nextPointPos) / 30f);

                Camera.transform.position = Vector3.Lerp(curPointPos, nextPointPos, _delta[0]);

                Quaternion lookRot = Quaternion.LookRotation(nextPointPos - curPointPos);
                Quaternion oldLookRot = Quaternion.LookRotation(curPointPos - prevPointPos);
                Camera.transform.rotation = Quaternion.Slerp(oldLookRot, lookRot, _delta[0]);

                //m_Camera.transform.Rotate(m_Rotation * Time.deltaTime * 0.5f);
            }
            else
            {
                _delta[0] = 0f;
                _pointIdx = (_pointIdx + 1) % m_Points.Length;

                Debug.Log($"Update point, newidx={_pointIdx}");
            }
        }
    }
}

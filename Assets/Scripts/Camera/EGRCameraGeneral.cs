using UnityEngine;

namespace MRK {
    public class EGRCameraGeneral : EGRCamera {
        [SerializeField]
        float m_RadiusX;
        [SerializeField]
        float m_RadiusY;
        [SerializeField]
        float m_RadiusZ;
        [SerializeField]
        int m_PointCount;
        readonly Vector3[] m_Bounds;
        Vector3[] m_Points;
        int m_PointIdx;
        [SerializeField]
        GameObject obj;

        public EGRCameraGeneral() : base() {
            m_Bounds = new Vector3[8] {
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

        void Start() {
            //position cam!
            transform.position = new Vector3(-m_RadiusX / 2f, -m_RadiusY / 2f, -m_RadiusZ / 2f);

            m_Points = new Vector3[m_PointCount];

            //generate points

            //0, 0, 0
            Vector3 minPoint = GetRealPosition(m_Bounds[0]);
            //1, 1, 1
            Vector3 maxPoint = GetRealPosition(m_Bounds[6]);

            for (int i = 0; i < m_PointCount; i++) {
                m_Points[i] = new Vector3(Random.Range(minPoint.x, maxPoint.x), Random.Range(minPoint.y, maxPoint.y), Random.Range(minPoint.z, maxPoint.z));

                //if (Application.isPlaying && i % 2 == 0)
                //    Instantiate(obj, m_Points[i], Quaternion.identity);
            }
        }

        Vector3 GetRealPosition(Vector3 rel) {
            return transform.position + new Vector3(rel.x * m_RadiusX, rel.y * m_RadiusY, rel.z * m_RadiusZ);
        }

        void RelDrawLine(int i1, int i2) {
            Gizmos.DrawLine(GetRealPosition(m_Bounds[i1]), GetRealPosition(m_Bounds[i2]));
        }

        void OnDrawGizmos() {
            if (m_Points == null || m_Points.Length == 0) {
                Start();
            }

            for (int i = 0; i < m_Points.Length; i++) {
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

        void Update() {
            if (m_Delta[0] < 1f) {
                int nextPointIdx = (m_PointIdx + 1) % m_Points.Length;
                int prevPointIdx = m_PointIdx - 1;
                if (prevPointIdx == -1)
                    prevPointIdx = m_Points.Length - 1;

                Vector3 nextPointPos = m_Points[nextPointIdx];
                Vector3 curPointPos = m_Points[m_PointIdx];
                Vector3 prevPointPos = m_Points[prevPointIdx];

                m_Delta[0] += Time.deltaTime / (Vector3.Distance(curPointPos, nextPointPos) / 30f);

                m_Camera.transform.position = Vector3.Lerp(curPointPos, nextPointPos, m_Delta[0]);

                Quaternion lookRot = Quaternion.LookRotation(nextPointPos - curPointPos);
                Quaternion oldLookRot = Quaternion.LookRotation(curPointPos - prevPointPos);
                m_Camera.transform.rotation = Quaternion.Slerp(oldLookRot, lookRot, m_Delta[0]);

                //m_Camera.transform.Rotate(m_Rotation * Time.deltaTime * 0.5f);
            }
            else {
                m_Delta[0] = 0f;
                m_PointIdx = (m_PointIdx + 1) % m_Points.Length;

                Debug.Log($"Update point, newidx={m_PointIdx}");
            }
        }
    }
}

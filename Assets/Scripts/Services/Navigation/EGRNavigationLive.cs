using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MRK.Navigation {
    public class EGRNavigationLive : EGRNavigator {
        const double TOLERANCE = 8d;
        const float REROUTE_WAIT_TIME = 3f;

        struct LineSegmentPointRank {
            public int Index;
            public double SqrDistance;
            public Vector2d? Intersection;
        }

        int m_FailCount;
        int m_LastStepIndex;
        int m_StepIndex;
        Vector2d? m_LastKnownCoords;
        float? m_LastKnownBearing;
        bool m_IsRerouting;
        EGRNavigationStep? m_LastStep;

        protected override void Prepare() {
            m_FailCount = 0;
            m_StepIndex = 0;
            m_LastStepIndex = -1;
            m_LastKnownCoords = null;
            m_LastKnownBearing = null;
        }

        int IsPointOnLine(Vector2d p, List<Vector2d> points, double tolerance, Reference<Vector2d> intersection = null, bool rank = false) {
            int pointIdx = 0;
            Vector2d current = MRKMapUtils.LatLonToMeters(p);

            List<LineSegmentPointRank> rankedList = null;
            if (rank) {
                rankedList = ListPool<LineSegmentPointRank>.Default.Rent();
            }

            while (pointIdx + 1 < points.Count) {
                Vector2d start = MRKMapUtils.LatLonToMeters(points[pointIdx]);
                Vector2d end = MRKMapUtils.LatLonToMeters(points[pointIdx + 1]);

                //slope of line segment
                double m = (end.y - start.y) / (end.x - start.x);

                //y = mx + c
                //c = y - mx
                double c = start.y - m * start.x;

                //neg reciprocal (normal slope) of line segment
                //double normalM = -1d / m;

                //y - y'  -1
                //      =
                //x - x'   m
                //-x + x'= m(y - y')
                //(x' - x) / m + y' = y
                //(x' / m) - (x / m) + y' = y
                //c = (x' / m) + y'
                double normalC = current.x / m + current.y;
                //y = (-1 / m)x + normalC
                //(-1 / m)x + normalC = mx + c
                //(-1 / m)x - mx = c - normalC
                //x(-1 / m - m) = c - normalC
                //x = (c - normalC) / (-1 / m - m)
                double x = (c - normalC) / (-1d / m - m); //intersection x
                double y = m * x + c; //intersection y

                //are we outside the line segment?
                if (x > Mathd.Max(start.x, end.x) + tolerance || y > Mathd.Max(start.y, end.y) + tolerance
                    || x < Mathd.Min(start.x, end.x) - tolerance || y < Mathd.Min(start.y, end.y) - tolerance)
                    goto __continue;

                //are we too far?
                double sqrDist = Mathd.Pow(current.x - x, 2d) + Mathd.Pow(current.y - y, 2d);
                if (sqrDist > tolerance * tolerance)
                    goto __continue;

                if (rank) {
                    rankedList.Add(new LineSegmentPointRank {
                        Index = pointIdx,
                        SqrDistance = sqrDist,
                        Intersection = intersection != null ? new Vector2d?(MRKMapUtils.MetersToLatLon(new Vector2d(x, y))) : null
                    });

                    goto __continue;
                }
                else {
                    if (intersection != null) {
                        Vector2d intersectionMeters = new Vector2d(x, y);
                        intersection.Value = MRKMapUtils.MetersToLatLon(intersectionMeters);
                    }

                    return pointIdx;
                }

            __continue:
                pointIdx++;
            }

            if (rank && rankedList.Count > 0) {
                int minIdx;
                int rankIdx = 0;

                if (rankedList.Count == 1) {
                    minIdx = rankedList[0].Index;
                    goto __rankedExit;
                }

                //find the one with the min distance, list count must be >= 2
                double minDist = rankedList[0].SqrDistance;
                minIdx = rankedList[0].Index;

                for (int i = 1; i < rankedList.Count; i++) {
                    LineSegmentPointRank pointRank = rankedList[i];
                    if (pointRank.SqrDistance < minDist) {
                        minDist = pointRank.SqrDistance;
                        minIdx = pointRank.Index;
                        rankIdx = i;
                    }
                }

            __rankedExit:
                if (intersection != null) {
                    intersection.Value = rankedList[rankIdx].Intersection.Value;
                }

                ListPool<LineSegmentPointRank>.Default.Free(rankedList);
                return minIdx;
            }

            return -1;
        }

        int GetNeighbouringStep(int idx, Vector2d p) {
            List<EGRNavigationStep> steps = Route.Legs[0].Steps;

            for (int i = 0; i < 2; i++) {
                int realIdx = idx + (i == 0 ? 1 : -1);
                if (realIdx >= steps.Count || realIdx < 0)
                    continue;

                int subIdx = IsPointOnLine(p, steps[realIdx].Geometry.Coordinates, TOLERANCE, null, false);
                if (subIdx != -1)
                    return realIdx;
            }

            return -1;
        }

        void OnReceiveLocation(bool success, Vector2d? coords, float? bearing) {
            if (!success) {
                m_FailCount++;

                if (m_FailCount >= 10) {
                    Debug.Log("Cannot retrieve location");
                }
                return;
            }

            if (m_StepIndex >= Route.Legs[0].Steps.Count) {
                Debug.Log("Live NAV ended");
                return;
            }

            //current step
            EGRNavigationStep step = m_StepIndex == -1 ? m_LastStep.Value : Route.Legs[0].Steps[m_StepIndex];
            if (m_StepIndex != -1) {
                m_LastStep = step;
                m_LastStepIndex = m_StepIndex;

                NavigationUI.NavigationInterface.CurrentStep.SetInstruction(step.Maneuver.Instruction, null);
            }

            ObjectPool<Reference<Vector2d>> refPool = ObjectPool<Reference<Vector2d>>.Default;
            Reference<Vector2d> intersection = refPool.Rent();

            m_LastKnownBearing = bearing.Value;
            m_LastKnownCoords = coords.Value;

            int subIdx = IsPointOnLine(coords.Value, step.Geometry.Coordinates, TOLERANCE, intersection, false);
            if (subIdx == -1) {
                //m_LastKnownCoords = coords.Value;

                //backup purposes to ignore rerouting
                //int b = m_StepIndex;
                m_StepIndex = GetNeighbouringStep(m_StepIndex, coords.Value);
                if (m_StepIndex == -1) {
                    if (!m_IsRerouting) {
                        Client.Runnable.Run(StartRerouting());
                    }

                    Debug.Log("RE ROUTE REQUIRED");
                    goto __exit;
                }

                m_LastStepIndex = m_StepIndex;
            }
            else {
                //snap nav sprite to route
                //m_LastKnownCoords = intersection.Value;
                m_StepIndex = m_LastStepIndex;

                //no need to re-route if an existing re-route queue has been initiated
                m_IsRerouting = false;
            }

            Debug.Log(m_StepIndex + step.Maneuver.Instruction);

        __exit:
            refPool.Free(intersection);
        }

        IEnumerator StartRerouting() {
            m_IsRerouting = true;

            //so I guess lets say we should we for 2-3 secs (about 6 location updates) before we re route
            float totalWaitTime = 0f;
            while (totalWaitTime < REROUTE_WAIT_TIME) {
                yield return new WaitForSeconds(0.2f);
                totalWaitTime += 0.2f;
            }

            if (m_IsRerouting) {
                //ASK FOR RE-ROUTE
            }

            yield break;
        }

        public override void Update() {
            //get current step
            Client.LocationService.GetCurrentLocation(OnReceiveLocation);

            if (m_LastKnownCoords.HasValue) {
                Vector3 pos = Client.FlatMap.GeoToWorldPosition(m_LastKnownCoords.Value);
                Vector3 spos = Client.ActiveCamera.WorldToScreenPoint(pos);
                NavigationManager.NavigationSprite.transform.position = EGRPlaceMarker.ScreenToMarkerSpace(spos);

                NavigationManager.NavigationSprite.transform.rotation = Quaternion.Euler(Quaternion.Euler(0f, 0f, m_LastKnownBearing.Value).eulerAngles
                    - Quaternion.Euler(-90f, 0f, -Client.FlatCamera.MapRotation.y).eulerAngles);
            }
        }
    }
}

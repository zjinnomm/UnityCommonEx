using UnityEngine;


namespace UnityCommonEx
{

    public static partial class GeometryUtil
    {

        public static float Cross(Vector2 v1, Vector2 v2) => v1.x * v2.y - v1.y * v2.x;

        public static bool IsVectorInTriangle2D(Vector2 p, Vector2 t1, Vector2 t2, Vector2 t3)
        {
            Vector2 v0 = t2 - t1;
            Vector2 v1 = t3 - t1;
            Vector2 v2 = p - t1;
            float d00 = Vector2.Dot(v0, v0);
            float d01 = Vector2.Dot(v0, v1);
            float d11 = Vector2.Dot(v1, v1);
            float d20 = Vector2.Dot(v2, v0);
            float d21 = Vector2.Dot(v2, v1);
            float denom = d00 * d11 - d01 * d01;
            float u = (d11 * d20 - d01 * d21) / denom;
            float v = (d00 * d21 - d01 * d20) / denom;
            return u >= 0 && u <= 1 && v >= 0 && v <= 1 && u + v <= 1;
        }

        public static Vector2 GetPointLinePerpendicular2D(Vector2 point, Vector2 linePoint1, Vector2 linePoint2)
        {
            Vector2 point2 = new Vector2(linePoint2.y - linePoint1.y + point.x, linePoint1.x - linePoint2.x + point.y);
            if (GetLineLineIntersection2D(point, point2, linePoint1, linePoint2, out Vector2 intersection))
            {
                return intersection;
            }
            return Vector2.zero;
        }

        public static Vector2 GetMirrorPoint2D(Vector2 point, Vector2 linePoint1, Vector2 linePoint2)
        {
            Vector2 perpendicular = GetPointLinePerpendicular2D(point, linePoint1, linePoint2);
            return perpendicular * 2 - point;
        }

        public static bool GetSegmentCircleIntersection2D(Vector2 start, Vector2 end, Vector2 center, float radius, out float t1, out float t2)
        {
            t1 = t2 = 0;
            if (start == end)
            {
                return false;
            }
            Vector2 dir = end - start;
            Vector2 dirNorm = dir.normalized;
            Vector2 c2s = start - center;
            Vector2 perpendicular = start - dirNorm * Vector2.Dot(dir, c2s);
            float h2 = radius * radius - (perpendicular - center).sqrMagnitude;
            if (h2 < 0)
            {
                return false;
            }
            float h = Mathf.Sqrt(h2);
            Vector2 p1 = perpendicular + h * dirNorm;
            Vector2 p2 = perpendicular - h * dirNorm;
            t1 = (p1.x - start.x) / dir.x;
            t2 = (p2.x - start.x) / dir.x;
            if (t2 < t1)
            {
                float swap = t1;
                t1 = t2;
                t2 = swap;
            }
            return true;
        }

        public static bool GetCircleIntersection2D(Vector2 center1, float radius1, Vector2 center2, float radius2, out Vector2 p1, out Vector2 p2)
        {
            p1 = p2 = Vector2.zero;
            Vector2 dir = center2 - center1;
            float dirMag = dir.magnitude;
            if (dirMag >= radius1 + radius2)
            {
                return false;
            }
            float a = (radius1 * radius1 - radius2 * radius2 + dirMag * dirMag) / (2 * dirMag);
            float h = Mathf.Sqrt(radius1 * radius1 - a * a);
            Vector2 perpendicular = center1 + a * dir / dirMag;
            Vector2 perDir = new Vector2(dir.y, -dir.x) * h / dirMag;
            p1 = perDir + perpendicular;
            p2 = perDir - perpendicular;
            return true;
        }

        public static bool GetLineLineIntersection2D(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2, out Vector2 intersection)
        {
            intersection = Vector2.zero;
            Vector2 r = p2 - p1;
            Vector2 s = q2 - q1;
            float cross = Cross(r, s);
            if (cross == 0)
            {
                return false;
            }
            float t = Cross(q1 - p1, s) / cross;
            float u = Cross(q1 - p1, r) / cross;
            intersection = p1 + r * t;
            return true;
        }

        public static bool GetSegmentSegmentIntersection2D(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2, out Vector2 intersection)
        {
            intersection = Vector2.zero;
            Vector2 r = p2 - p1;
            Vector2 s = q2 - q1;
            float cross = Cross(r, s);
            if (cross == 0)
            {
                return false;
            }
            float t = Cross(q1 - p1, s) / cross;
            float u = Cross(q1 - p1, r) / cross;
            if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
            {
                intersection = p1 + r * t;
                return true;
            }
            return false;
        }

        public static bool CheckCircleSectorOverlapped2D(Vector2 center1, float radius1, Vector2 center2, float radius2, Vector2 dir, float halfAngle)
        {
            Vector2 centerDir = center1 - center2;
            float centerDirDist = centerDir.magnitude;
            if (centerDirDist >= radius1 + radius2)
            {
                return false;
            }
            if (centerDirDist < radius1)
            {
                return true;
            }
            dir = dir.normalized;
            if (Vector2.Dot(centerDir, dir) > Mathf.Cos(Mathf.Deg2Rad * halfAngle) * centerDirDist)
            {
                return true;
            }
            Vector3 sideExtent = Quaternion.AngleAxis(halfAngle, Vector3.forward) * dir * radius2;
            if (GetSegmentCircleIntersection2D(center2, center2 + new Vector2(sideExtent.x, sideExtent.y), center1, radius1, out float t1, out float t2) && t2 > 0 && t1 < 1)
            {
                return true;
            }
            sideExtent = Quaternion.AngleAxis(-halfAngle, Vector3.forward) * dir * radius2;
            if (GetSegmentCircleIntersection2D(center2, center2 + new Vector2(sideExtent.x, sideExtent.y), center1, radius1, out t1, out t2) && t2 > 0 && t1 < 1)
            {
                return true;
            }
            return false;
        }

    }

}
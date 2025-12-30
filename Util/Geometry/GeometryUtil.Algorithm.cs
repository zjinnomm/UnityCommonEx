using System.Collections.Generic;
using UnityEngine;


namespace UnityCommonEx
{

    public static partial class GeometryUtil
    {

        public static bool EarClipping(Vector2[] vertices, out int[] triangles)
        {
            triangles = null;
            if (vertices == null || vertices.Length <= 2)
            {
                return false;
            }
            if (vertices.Length == 3)
            {
                triangles = new int[] { 0, 1, 2 };
                return true;
            }
            for (int i = 0; i < vertices.Length - 1; i++)
            {
                Vector2 p1 = vertices[i];
                Vector2 p2 = vertices[i + 1];
                for (int j = i + 2; j < vertices.Length; j++)
                {
                    if ((j + 1) % vertices.Length == i)
                    {
                        continue;
                    }
                    Vector2 q1 = vertices[j];
                    Vector2 q2 = vertices[(j + 1) % vertices.Length];
                    if (GetSegmentSegmentIntersection2D(p1, p2, q1, q2, out var _))
                    {
                        // this is not a valid common polygon: it has holes or intersections
                        return false;
                    }
                }
            }
            List<int> verticeIndexes = new List<int>();
            List<int> triangleIndexes = new List<int>();
            for (int i = 0; i < vertices.Length; i++)
            {
                verticeIndexes.Add(i);
            }
        outer: while (verticeIndexes.Count > 3)
            {
                int count = verticeIndexes.Count;
                for (int i = 0; i < count; i++)
                {
                    int i0 = verticeIndexes[i];
                    int i1 = verticeIndexes[(i + 1) % count];
                    int i2 = verticeIndexes[(i + 2) % count];
                    Vector2 p0 = vertices[i0];
                    Vector2 p1 = vertices[i1];
                    Vector2 p2 = vertices[i2];
                    float cross = Cross(p0 - p1, p2 - p1);
                    if (cross == 0)
                    {
                        verticeIndexes.RemoveAt((i + 1) % count);
                        goto outer;
                    }
                    else if (cross > 0)
                    {
                        bool isEar = true;
                        for (int j = 0; j < count; j++)
                        {
                            if (j == i || j == (i + 1) % count || j == (i + 2) % count)
                            {
                                continue;
                            }
                            if (IsVectorInTriangle2D(vertices[verticeIndexes[j]], p0, p1, p2))
                            {
                                isEar = false;
                                break;
                            }
                        }
                        if (!isEar)
                        {
                            continue;
                        }
                        triangleIndexes.Add(i0);
                        triangleIndexes.Add(i1);
                        triangleIndexes.Add(i2);
                        verticeIndexes.RemoveAt((i + 1) % count);
                        goto outer;
                    }
                }
                return false;
            }
            triangleIndexes.AddRange(verticeIndexes);
            triangles = triangleIndexes.ToArray();
            return true;
        }

    }

}
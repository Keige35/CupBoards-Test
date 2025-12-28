using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class BuildingMeshGenerator
{
    public static Mesh CreateBuildingMesh(List<Vector3> polygonPoints, float height = 10.0f, bool hasRoof = true, bool hasFloor = true)
    {
        if (polygonPoints == null || polygonPoints.Count < 3)
        {
            Debug.LogWarning("BuildingMeshGenerator: ƒл€ создани€ меша здани€ нужно как минимум 3 точки.");
            return null;
        }

        List<Vector3> points = new List<Vector3>(polygonPoints);

        if (points.First() != points.Last())
        {
            points.Add(points[0]);
        }

        if (points.Count < 4) return null;

        int polygonVertexCount = points.Count - 1;
        int totalVertexCount = polygonVertexCount * 4;
        if (hasRoof) totalVertexCount += polygonVertexCount;
        if (hasFloor) totalVertexCount += polygonVertexCount;

        Vector3[] vertices = new Vector3[totalVertexCount];
        Vector2[] uvs = new Vector2[totalVertexCount];
        Vector3[] normals = new Vector3[totalVertexCount];
        List<int> triangles = new List<int>();

        int vertexIndex = 0;

        Vector3 polygonNormal = CalculatePolygonNormal(points);
        Vector3 upDirection = Vector3.up;

        bool isPolygonClockwise = IsPolygonClockwise(points);

        for (int i = 0; i < polygonVertexCount; i++)
        {
            Vector3 currentPoint = points[i];
            Vector3 nextPoint = points[(i + 1) % polygonVertexCount];

            vertices[vertexIndex] = currentPoint;
            vertices[vertexIndex + 1] = currentPoint + upDirection * height;
            vertices[vertexIndex + 2] = nextPoint + upDirection * height;
            vertices[vertexIndex + 3] = nextPoint;

            Vector3 wallNormal = Vector3.Cross(nextPoint - currentPoint, upDirection).normalized;

            if (isPolygonClockwise)
            {
                wallNormal = -wallNormal;
            }

            normals[vertexIndex] = wallNormal;
            normals[vertexIndex + 1] = wallNormal;
            normals[vertexIndex + 2] = wallNormal;
            normals[vertexIndex + 3] = wallNormal;

            float wallHeightUV = height / 10f;
            uvs[vertexIndex] = new Vector2(0, 0);
            uvs[vertexIndex + 1] = new Vector2(0, wallHeightUV);
            uvs[vertexIndex + 2] = new Vector2(1, wallHeightUV);
            uvs[vertexIndex + 3] = new Vector2(1, 0);

            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);

            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 3);

            vertexIndex += 4;
        }

        if (hasRoof)
        {
            int roofStartIndex = vertexIndex;

            for (int i = 0; i < polygonVertexCount; i++)
            {
                vertices[vertexIndex] = points[i] + upDirection * height;
                normals[vertexIndex] = upDirection;
                uvs[vertexIndex] = new Vector2(points[i].x, points[i].z) * 0.1f;
                vertexIndex++;
            }

            List<int> roofTriangles = TriangulatePolygon(points, polygonVertexCount);
            foreach (int triangleIndex in roofTriangles)
            {
                triangles.Add(roofStartIndex + triangleIndex);
            }
        }

        if (hasFloor)
        {
            int floorStartIndex = vertexIndex;

            for (int i = 0; i < polygonVertexCount; i++)
            {
                vertices[vertexIndex] = points[i];
                normals[vertexIndex] = -upDirection;
                uvs[vertexIndex] = new Vector2(points[i].x, points[i].z) * 0.1f;
                vertexIndex++;
            }

            List<int> floorTriangles = TriangulatePolygon(points, polygonVertexCount);
            floorTriangles.Reverse();

            foreach (int triangleIndex in floorTriangles)
            {
                triangles.Add(floorStartIndex + triangleIndex);
            }
        }

        Mesh buildingMesh = new Mesh();
        buildingMesh.name = "BuildingMesh";
        buildingMesh.vertices = vertices;
        buildingMesh.uv = uvs;
        buildingMesh.normals = normals;
        buildingMesh.triangles = triangles.ToArray();

        buildingMesh.RecalculateBounds();
        buildingMesh.RecalculateTangents();

        return buildingMesh;
    }

    private static Vector3 CalculatePolygonNormal(List<Vector3> points)
    {
        Vector3 normal = Vector3.zero;
        int count = points.Count - 1;

        for (int i = 0; i < count; i++)
        {
            Vector3 current = points[i];
            Vector3 next = points[(i + 1) % count];

            normal.x += (current.y - next.y) * (current.z + next.z);
            normal.y += (current.z - next.z) * (current.x + next.x);
            normal.z += (current.x - next.x) * (current.y + next.y);
        }

        return normal.normalized;
    }

    private static bool IsPolygonClockwise(List<Vector3> points)
    {
        float sum = 0f;
        int count = points.Count - 1;

        for (int i = 0; i < count; i++)
        {
            Vector3 current = points[i];
            Vector3 next = points[(i + 1) % count];
            sum += (next.x - current.x) * (next.z + current.z);
        }

        return sum > 0;
    }

    private static List<int> TriangulatePolygon(List<Vector3> points, int vertexCount)
    {
        List<int> triangles = new List<int>();
        List<int> indices = new List<int>();

        for (int i = 0; i < vertexCount; i++)
        {
            indices.Add(i);
        }

        while (indices.Count > 3)
        {
            for (int i = 0; i < indices.Count; i++)
            {
                int a = indices[i];
                int b = indices[(i + 1) % indices.Count];
                int c = indices[(i + 2) % indices.Count];

                Vector3 v1 = points[b] - points[a];
                Vector3 v2 = points[c] - points[b];
                Vector3 normal = Vector3.Cross(v1, v2);

                if (normal.y <= 0) continue;

                bool isEar = true;

                for (int j = 0; j < indices.Count; j++)
                {
                    if (j == i || j == (i + 1) % indices.Count || j == (i + 2) % indices.Count) continue;

                    Vector3 p = points[indices[j]];
                    if (IsPointInTriangle(p, points[a], points[b], points[c]))
                    {
                        isEar = false;
                        break;
                    }
                }

                if (isEar)
                {
                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(c);

                    indices.RemoveAt((i + 1) % indices.Count);
                    break;
                }
            }
        }

        triangles.Add(indices[0]);
        triangles.Add(indices[1]);
        triangles.Add(indices[2]);

        return triangles;
    }

    private static bool IsPointInTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 v0 = c - a;
        Vector3 v1 = b - a;
        Vector3 v2 = p - a;

        float dot00 = Vector3.Dot(v0, v0);
        float dot01 = Vector3.Dot(v0, v1);
        float dot02 = Vector3.Dot(v0, v2);
        float dot11 = Vector3.Dot(v1, v1);
        float dot12 = Vector3.Dot(v1, v2);

        float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
        float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        return (u >= 0) && (v >= 0) && (u + v < 1);
    }

    public static Mesh CreateSimpleBuildingMesh(List<Vector3> polygonPoints, float height = 10.0f)
    {
        if (polygonPoints == null || polygonPoints.Count < 3) return null;

        List<Vector3> points = new List<Vector3>(polygonPoints);

        if (points.First() != points.Last())
        {
            points.Add(points[0]);
        }

        if (points.Count < 4) return null;

        int polygonVertexCount = points.Count - 1;
        int totalVertexCount = polygonVertexCount * 2 * 3;

        Vector3[] vertices = new Vector3[totalVertexCount];
        Vector2[] uvs = new Vector2[totalVertexCount];
        Vector3[] normals = new Vector3[totalVertexCount];
        int[] triangles = new int[totalVertexCount];

        int vertexIndex = 0;
        int triangleIndex = 0;

        for (int i = 0; i < polygonVertexCount; i++)
        {
            Vector3 current = points[i];
            Vector3 next = points[(i + 1) % polygonVertexCount];

            vertices[vertexIndex] = current;
            vertices[vertexIndex + 1] = current + Vector3.up * height;
            vertices[vertexIndex + 2] = next;

            vertices[vertexIndex + 3] = next;
            vertices[vertexIndex + 4] = current + Vector3.up * height;
            vertices[vertexIndex + 5] = next + Vector3.up * height;

            Vector3 normal = Vector3.Cross(next - current, Vector3.up).normalized;

            for (int j = 0; j < 6; j++)
            {
                normals[vertexIndex + j] = normal;
                uvs[vertexIndex + j] = new Vector2(j % 2, j / 2);
                triangles[triangleIndex++] = vertexIndex + j;
            }

            vertexIndex += 6;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.normals = normals;
        mesh.triangles = triangles;

        mesh.RecalculateBounds();

        return mesh;
    }
}
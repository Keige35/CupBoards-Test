using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class RoadMeshGenerator
{
    public static Mesh CreateRoadMesh(List<Vector3> pathPoints, float roadWidth = 5.0f, bool smoothPath = false)
    {
        if (pathPoints == null || pathPoints.Count < 2)
        {
            return null;
        }

        List<Vector3> processedPoints = smoothPath ? CatmullRomSmooth(pathPoints, 2) : new List<Vector3>(pathPoints);

        if (processedPoints.Count > 100)
        {
            processedPoints = SimplifyPath(processedPoints, 0.5f);
        }

        Mesh roadMesh = new Mesh();
        roadMesh.name = "RoadMesh";

        int vertexCount = processedPoints.Count * 2;
        int triangleCount = (processedPoints.Count - 1) * 2;

        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        int[] triangles = new int[triangleCount * 3];

        float totalLength = 0f;
        float[] segmentLengths = new float[processedPoints.Count - 1];

        for (int i = 0; i < processedPoints.Count - 1; i++)
        {
            segmentLengths[i] = Vector3.Distance(processedPoints[i], processedPoints[i + 1]);
            totalLength += segmentLengths[i];
        }

        float currentLength = 0f;
        for (int i = 0; i < processedPoints.Count; i++)
        {
            Vector3 forward = CalculateForwardDirection(processedPoints, i);
            Vector3 left = new Vector3(-forward.z, 0, forward.x).normalized;

            int vertexIndex = i * 2;

            float halfWidth = roadWidth * 0.5f;
            vertices[vertexIndex] = processedPoints[i] + left * halfWidth;
            vertices[vertexIndex + 1] = processedPoints[i] - left * halfWidth;

            float u = totalLength > 0 ? currentLength / totalLength : 0f;
            uvs[vertexIndex] = new Vector2(u, 0f);
            uvs[vertexIndex + 1] = new Vector2(u, 1f);

            normals[vertexIndex] = Vector3.up;
            normals[vertexIndex + 1] = Vector3.up;

            if (i < processedPoints.Count - 1)
            {
                currentLength += segmentLengths[i];
            }
        }

        int triangleIndex = 0;
        for (int i = 0; i < processedPoints.Count - 1; i++)
        {
            int baseVertexIndex = i * 2;

            triangles[triangleIndex++] = baseVertexIndex;
            triangles[triangleIndex++] = baseVertexIndex + 1;
            triangles[triangleIndex++] = baseVertexIndex + 2;

            triangles[triangleIndex++] = baseVertexIndex + 1;
            triangles[triangleIndex++] = baseVertexIndex + 3;
            triangles[triangleIndex++] = baseVertexIndex + 2;
        }

        roadMesh.vertices = vertices;
        roadMesh.uv = uvs;
        roadMesh.normals = normals;
        roadMesh.triangles = triangles;

        roadMesh.RecalculateNormals();
        roadMesh.RecalculateBounds();
        roadMesh.RecalculateTangents();

        return roadMesh;
    }

    private static Vector3 CalculateForwardDirection(List<Vector3> points, int index)
    {
        if (points.Count <= 1) return Vector3.forward;

        if (index == 0)
        {
            return (points[1] - points[0]).normalized;
        }
        else if (index == points.Count - 1)
        {
            return (points[index] - points[index - 1]).normalized;
        }
        else
        {
            Vector3 dirToPrev = (points[index] - points[index - 1]).normalized;
            Vector3 dirToNext = (points[index + 1] - points[index]).normalized;

            if (Vector3.Dot(dirToPrev, dirToNext) < -0.9f)
            {
                return dirToPrev;
            }

            return (dirToPrev + dirToNext).normalized;
        }
    }

    private static List<Vector3> CatmullRomSmooth(List<Vector3> originalPoints, int subdivisions)
    {
        if (originalPoints.Count < 3) return new List<Vector3>(originalPoints);

        List<Vector3> smoothed = new List<Vector3>();

        for (int i = 0; i < originalPoints.Count - 1; i++)
        {
            Vector3 p0 = (i == 0) ? originalPoints[i] : originalPoints[i - 1];
            Vector3 p1 = originalPoints[i];
            Vector3 p2 = originalPoints[i + 1];
            Vector3 p3 = (i == originalPoints.Count - 2) ? originalPoints[i + 1] : originalPoints[i + 2];

            smoothed.Add(p1);

            for (int j = 1; j <= subdivisions; j++)
            {
                float t = j / (float)(subdivisions + 1);
                Vector3 point = CalculateCatmullRomPoint(t, p0, p1, p2, p3);
                smoothed.Add(point);
            }
        }

        smoothed.Add(originalPoints[originalPoints.Count - 1]);
        return smoothed;
    }

    private static Vector3 CalculateCatmullRomPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2 * p1) +
            (-p0 + p2) * t +
            (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 +
            (-p0 + 3 * p1 - 3 * p2 + p3) * t3
        );
    }

    private static List<Vector3> SimplifyPath(List<Vector3> path, float tolerance)
    {
        if (path.Count <= 2) return new List<Vector3>(path);

        List<Vector3> simplified = new List<Vector3>();
        simplified.Add(path[0]);

        for (int i = 1; i < path.Count - 1; i++)
        {
            float distance = PointToLineDistance(path[i], simplified[simplified.Count - 1], path[i + 1]);

            if (distance > tolerance)
            {
                simplified.Add(path[i]);
            }
        }

        simplified.Add(path[path.Count - 1]);
        return simplified;
    }

    private static float PointToLineDistance(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 line = lineEnd - lineStart;
        float lineLength = line.magnitude;

        if (lineLength < 0.0001f) return Vector3.Distance(point, lineStart);

        Vector3 lineDir = line / lineLength;
        Vector3 pointToStart = point - lineStart;

        float t = Vector3.Dot(pointToStart, lineDir);
        t = Mathf.Clamp(t, 0f, lineLength);

        Vector3 closestPoint = lineStart + lineDir * t;
        return Vector3.Distance(point, closestPoint);
    }
}
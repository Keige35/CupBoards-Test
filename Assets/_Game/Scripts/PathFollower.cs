using UnityEngine;
using System.Collections;

public class PathFollower : MonoBehaviour
{
    public Transform movingObject;
    public float speed = 5f;
    public bool loop = false;
    public bool lookForward = true;

    private Vector3[] currentPath;
    private float[] segmentLengths;
    private float totalLength;
    private float distanceTraveled;
    private bool isPaused = false;
    private Coroutine moveRoutine;

    private void Awake()
    {
        if (movingObject == null) movingObject = transform;
    }

    public void SetPath(Vector3[] waypoints)
    {
        StopMove();
        currentPath = waypoints;
        PrecomputePathData();
        distanceTraveled = 0f;
        StartMove();
    }

    private void PrecomputePathData()
    {
        if (currentPath == null || currentPath.Length < 2)
        {
            totalLength = 0f;
            segmentLengths = null;
            return;
        }

        segmentLengths = new float[currentPath.Length - 1];
        totalLength = 0f;
        for (int i = 0; i < currentPath.Length - 1; i++)
        {
            segmentLengths[i] = Vector3.Distance(currentPath[i], currentPath[i + 1]);
            totalLength += segmentLengths[i];
        }
    }

    public void StartMove()
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        if (currentPath == null || currentPath.Length < 2) return;
        moveRoutine = StartCoroutine(MoveAlongPath());
    }

    public void StopMove()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }
    }

    public void Pause() => isPaused = true;
    public void Resume() => isPaused = false;

    private IEnumerator MoveAlongPath()
    {
        if (movingObject == null) yield break;
        movingObject.position = currentPath[0];

        while (distanceTraveled < totalLength)
        {
            if (!isPaused)
            {
                distanceTraveled += speed * Time.deltaTime;
                if (distanceTraveled > totalLength) distanceTraveled = totalLength;
            }

            Vector3 currentPos = GetPointAtDistance(distanceTraveled);
            movingObject.position = currentPos;

            if (lookForward && distanceTraveled < totalLength)
            {
                Vector3 nextPos = GetPointAtDistance(Mathf.Min(distanceTraveled + 0.1f, totalLength));
                Vector3 dir = (nextPos - currentPos).normalized;
                if (dir != Vector3.zero)
                    movingObject.rotation = Quaternion.LookRotation(dir);
            }

            yield return null;
        }

        if (loop)
        {
            distanceTraveled = 0f;
            moveRoutine = StartCoroutine(MoveAlongPath());
        }
        else
        {
            moveRoutine = null;
        }
    }

    private Vector3 GetPointAtDistance(float dist)
    {
        if (totalLength <= 0f || currentPath == null || currentPath.Length == 0)
            return movingObject.position;

        if (dist <= 0f) return currentPath[0];
        if (dist >= totalLength) return currentPath[currentPath.Length - 1];

        float accumulated = 0f;
        for (int i = 0; i < currentPath.Length - 1; i++)
        {
            if (dist <= accumulated + segmentLengths[i])
            {
                float t = (dist - accumulated) / segmentLengths[i];
                return Vector3.Lerp(currentPath[i], currentPath[i + 1], t);
            }
            accumulated += segmentLengths[i];
        }
        return currentPath[currentPath.Length - 1];
    }
}
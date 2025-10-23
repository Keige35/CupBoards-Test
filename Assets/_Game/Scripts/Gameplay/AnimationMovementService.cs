using System.Collections;
using UnityEngine;

public class AnimationMovementService : IGameService
{
    private float _moveSpeed = 40f;
    private bool _isAnimating = false;

    public bool IsAnimating => _isAnimating;

    public void Initialize()
    {
    }

    public void Cleanup()
    {
    }

  

    public IEnumerator AnimateMovement(BasePiece piece, Node targetNode, IPathfinder pathfinder)
    {
        _isAnimating = true;

        var path = pathfinder.FindPath(piece.CurrentNode, targetNode, piece);

        if (path == null || path.Count <= 1)
        {
            piece.transform.position = targetNode.transform.position;
            _isAnimating = false;
            yield break;
        }


        for (int i = 1; i < path.Count; i++)
        {
            Node currentNode = path[i];
            Vector3 startPos = piece.transform.position;
            Vector3 endPos = currentNode.transform.position;

            float distance = Vector3.Distance(startPos, endPos);
            float duration = distance / _moveSpeed;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                piece.transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            piece.transform.position = endPos;
        }

        _isAnimating = false;
    }
}
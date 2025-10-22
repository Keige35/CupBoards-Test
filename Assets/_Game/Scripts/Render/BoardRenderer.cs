using System.Collections.Generic;
using UnityEngine;

public class BoardRenderer : MonoBehaviour, IGameService
{
    [SerializeField] private LineRenderer connectionPrefab;

    public void Initialize() { }
    public void Cleanup() { }

    public void RenderConnections(List<Connection> connections, List<Node> nodes)
    {
        foreach (var connection in connections)
        {
            Node from = nodes[connection.from];
            Node to = nodes[connection.to];

            LineRenderer line = Instantiate(connectionPrefab, transform);
            line.SetPositions(new Vector3[] { from.transform.position, to.transform.position });
        }
    }
}
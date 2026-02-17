using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Renderer))]
public class PickableObject : MonoBehaviour
{
    public int ID { get; private set; } = -1;
    private Renderer rend;

    public static List<PickableObject> ActiveObjects { get; } = new List<PickableObject>();

    private void Awake()
    {
        rend = GetComponent<Renderer>();
    }

    private void OnEnable()
    {
        if (!ActiveObjects.Contains(this))
            ActiveObjects.Add(this);
    }

    private void OnDisable()
    {
        ActiveObjects.Remove(this);
    }

    private void Start()
    {
        if (IDManager.Instance == null)
        {
            Debug.LogError("IDManager not found");
            return;
        }

        ID = IDManager.Instance.GetID(gameObject);
        if (ID == -1)
        {
            Debug.LogError($"Failed to get ID for {name}");
        }
    }

    private void OnDestroy()
    {
        if (ID != -1 && IDManager.Instance != null)
        {
            IDManager.Instance.ReleaseID(ID);
        }
    }
}
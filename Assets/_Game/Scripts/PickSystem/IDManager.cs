using System.Collections.Generic;
using UnityEngine;

public class IDManager : MonoBehaviour
{
    public static IDManager Instance { get; private set; }

    [SerializeField] private int maxID = 16777215;
    private int nextID = 1;
    private Queue<int> freeIDs = new Queue<int>();
    private Dictionary<int, GameObject> idToObject = new Dictionary<int, GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public int GetID(GameObject obj)
    {
        int id;
        if (freeIDs.Count > 0)
        {
            id = freeIDs.Dequeue();
        }
        else if (nextID <= maxID)
        {
            id = nextID;
            nextID++;
        }
        else
        {
            Debug.LogError($"IDManager: all IDs are in use ({maxID})");
            return -1;
        }

        idToObject[id] = obj;
        return id;
    }

    public void ReleaseID(int id)
    {
        if (id <= 0 || id > maxID) return;
        if (idToObject.ContainsKey(id))
        {
            idToObject.Remove(id);
            freeIDs.Enqueue(id);
        }
    }

    public GameObject GetObjectByID(int id)
    {
        idToObject.TryGetValue(id, out GameObject obj);
        return obj;
    }

    public Color IDToColor(int id)
    {
        if (id < 0) id = 0;
        byte r = (byte)(id & 0xFF);
        byte g = (byte)((id >> 8) & 0xFF);
        byte b = (byte)((id >> 16) & 0xFF);
        return new Color32(r, g, b, 255);
    }

    public int ColorToID(Color32 color)
    {
        return color.r | (color.g << 8) | (color.b << 16);
    }
}
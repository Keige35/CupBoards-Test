using System;
using System.Collections.Generic;
using UnityEngine;

public class ServiceLocator : MonoBehaviour
{
    private static ServiceLocator _instance;
    private readonly Dictionary<Type, IGameService> _services = new Dictionary<Type, IGameService>();

    public static ServiceLocator Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ServiceLocator>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("ServiceLocator");
                    _instance = obj.AddComponent<ServiceLocator>();
                    DontDestroyOnLoad(obj);
                }
            }
            return _instance;
        }
    }

    public void Register<T>(T service) where T : IGameService
    {
        Type type = typeof(T);
        if (_services.ContainsKey(type))
        {
            Debug.LogWarning($"Service of type {type} already registered. Overwriting.");
            _services[type] = service;
        }
        else
        {
            _services.Add(type, service);
        }

        service.Initialize();
    }

    public T Get<T>() where T : IGameService
    {
        Type type = typeof(T);
        if (_services.TryGetValue(type, out IGameService service))
        {
            return (T)service;
        }

        throw new InvalidOperationException($"Service of type {type} is not registered.");
    }

    public bool TryGet<T>(out T service) where T : IGameService
    {
        Type type = typeof(T);
        if (_services.TryGetValue(type, out IGameService gameService))
        {
            service = (T)gameService;
            return true;
        }

        service = default(T);
        return false;
    }

    public void Unregister<T>() where T : IGameService
    {
        Type type = typeof(T);
        if (_services.TryGetValue(type, out IGameService service))
        {
            service.Cleanup();
            _services.Remove(type);
        }
    }

    private void OnDestroy()
    {
        foreach (var service in _services.Values)
        {
            service.Cleanup();
        }
        _services.Clear();
    }
}

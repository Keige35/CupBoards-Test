using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class GPUPicker : MonoBehaviour
{
    [Header("Basic settings")]
    public Camera mainCamera;
    public LayerMask pickableLayers = -1;
    [Range(0.1f, 1f)]
    public float downsample = 1f;

    [Header("Shader for pick")]
    public Shader pickerShader;

    [Header("Event")]
    public UnityEvent<GameObject> OnObjectClicked;

    private Camera pickCamera;
    private RenderTexture pickRT;
    private int pickWidth, pickHeight;

    private Material pickerMaterial;

    private Dictionary<Renderer, Material> originalMaterials = new Dictionary<Renderer, Material>();
    private List<Renderer> allRenderers = new List<Renderer>();

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (pickerShader == null)
        {
            Debug.LogError("GPUPicker: not found pickerShader!");
            return;
        }

        pickerMaterial = new Material(pickerShader);

        CreatePickCamera();
    }

    private void CreatePickCamera()
    {
        GameObject go = new GameObject("PickCamera");
        go.transform.SetParent(mainCamera.transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        pickCamera = go.AddComponent<Camera>();
        pickCamera.CopyFrom(mainCamera);
        pickCamera.clearFlags = CameraClearFlags.SolidColor;
        pickCamera.backgroundColor = Color.black;
        pickCamera.cullingMask = pickableLayers;
        pickCamera.enabled = false;

        pickCamera.allowHDR = false;
        pickCamera.allowMSAA = false;
        pickCamera.allowDynamicResolution = false;
        pickCamera.useOcclusionCulling = false;

        pickWidth = Mathf.RoundToInt(Screen.width * downsample);
        pickHeight = Mathf.RoundToInt(Screen.height * downsample);
        if (pickWidth < 1) pickWidth = 1;
        if (pickHeight < 1) pickHeight = 1;

        pickRT = new RenderTexture(pickWidth, pickHeight, 24, RenderTextureFormat.ARGB32);
        pickRT.Create();
        pickCamera.targetTexture = pickRT;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandlePick();
        }
    }

    private void HandlePick()
    {
        if (pickCamera == null || pickRT == null || IDManager.Instance == null)
            return;

        PrepareObjectsForPicking();

        pickCamera.Render();

        RestoreObjectsAfterPicking();

        Vector2 mousePos = Input.mousePosition;
        int x = Mathf.RoundToInt(mousePos.x * ((float)pickWidth / Screen.width));
        int y = Mathf.RoundToInt(mousePos.y * ((float)pickHeight / Screen.height));

        if (x < 0 || x >= pickWidth || y < 0 || y >= pickHeight)
            return;

        RenderTexture.active = pickRT;
        Texture2D tex = new Texture2D(pickWidth, pickHeight, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, pickWidth, pickHeight), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        Color32 pixel = tex.GetPixel(x, y);
        Destroy(tex);

        int id = IDManager.Instance.ColorToID(pixel);
        if (id != 0)
        {
            GameObject clickedObject = IDManager.Instance.GetObjectByID(id);
            if (clickedObject != null)
            {
                Debug.Log($"GPUPicker: click for {clickedObject.name} (ID: {id})");
                OnObjectClicked?.Invoke(clickedObject);
            }
        }
    }

    private void PrepareObjectsForPicking()
    {
        originalMaterials.Clear();
        allRenderers.Clear();

        foreach (var pickable in PickableObject.ActiveObjects)
        {
            if (pickable == null || !pickable.gameObject.activeInHierarchy) continue;

            Renderer rend = pickable.GetComponent<Renderer>();
            if (rend == null) continue;

            allRenderers.Add(rend);
            originalMaterials[rend] = rend.material; 

            rend.material = pickerMaterial;

            Color uniqueColor = IDManager.Instance.IDToColor(pickable.ID);
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_PickColor", uniqueColor);
            rend.SetPropertyBlock(block);
        }
    }

    private void RestoreObjectsAfterPicking()
    {
        foreach (var rend in allRenderers)
        {
            if (rend == null) continue;

            if (originalMaterials.TryGetValue(rend, out Material original))
            {
                rend.material = original;
            }

            rend.SetPropertyBlock(null);
        }
    }

    private void OnDestroy()
    {
        if (pickRT != null)
            pickRT.Release();

        if (pickerMaterial != null)
            DestroyImmediate(pickerMaterial);
    }

    private void OnGUI()
    {
        if (pickRT != null)
        {
            GUI.DrawTexture(new Rect(10, 10, 200, 200), pickRT, ScaleMode.ScaleToFit, false);
        }
    }
}
using UnityEngine;
using OsmSharp;
using OsmSharp.Streams;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Threading;

public class OsmRoadImporter : MonoBehaviour
{
    public string osmFileName = "map.osm";
    public FileFormat fileFormat = FileFormat.AutoDetect;
    public float roadWidth = 6.0f;
    public bool autoCenter = true;
    public Vector2 originLatLon = new Vector2(0, 0);
    public float metersPerDegreeLat = 111319.5f;
    public bool useBoundingBoxFilter = true;
    public double minLatitude = 59.90;
    public double maxLatitude = 60.00;
    public double minLongitude = 29.80;
    public double maxLongitude = 30.00;
    public bool strictBoundingBoxFilter = true;
    public bool showBoundingBoxInScene = true;
    public float baseHeight = 0.01f;
    public bool flattenToBaseHeight = true;
    public List<string> includedHighwayTypes = new List<string>
    {
        "motorway", "trunk", "primary", "secondary",
        "tertiary", "unclassified", "residential"
    };
    public bool generateBuildings = true;
    public List<string> includedBuildingTypes = new List<string>
    {
        "apartments", "house", "commercial", "industrial",
        "retail", "office", "school", "university"
    };
    public float defaultBuildingHeight = 10.0f;
    public Material buildingMaterial;
    public string buildingsParentName = "Buildings";
    public string parentObjectName = "RoadNetwork";
    public bool createSubcategories = true;
    public bool enableTMPSigns = true;
    public TMP_FontAsset signFontAsset;
    public float signFontSize = 3f;
    public float fontSizeMultiplier = 10f;
    public Color signColor = Color.white;
    public float signHeightAboveRoad = 10f;
    public Material signMaterial;
    public List<string> signHighwayTypes = new List<string> { "primary", "secondary", "tertiary" };
    public int minRoadPointsForSign = 10;
    public int maxDisplayNameLength = 30;
    public float textOffsetFromRoad = 2f;
    public int textOrientationType = 0;
    public int maxRoadsToProcess = 5000;
    public int maxBuildingsToProcess = 2000;
    public bool debugMaterials = true;
    public bool enableNodeCacheCleanup = true;
    public int maxNodeCacheSize = 300000;
    public int batchSize = 20;
    public string mainUrpShader = "Universal Render Pipeline/Lit";
    public string fallbackShader = "Sprites/Default";
    public Material roadMaterial;

    private Dictionary<string, Material> roadMaterials = new Dictionary<string, Material>();
    private Transform roadsParent;
    private Transform buildingsParent;
    private Dictionary<string, Transform> categoryParents = new Dictionary<string, Transform>();
    private Vector3 centerOffset = Vector3.zero;
    private Shader urpShader;
    private Shader fallbackShaderObj;
    private Transform tmpsignsParent;
    private Camera mainCamera;
    private CancellationTokenSource cancellationTokenSource;

    public enum FileFormat
    {
        AutoDetect,
        OSM_XML,
        OSM_PBF
    }

    async void Start()
    {
        mainCamera = Camera.main;

        roadsParent = new GameObject(parentObjectName).transform;
        roadsParent.SetParent(this.transform);
        roadsParent.position = Vector3.zero;

        buildingsParent = new GameObject(buildingsParentName).transform;
        buildingsParent.SetParent(this.transform);
        buildingsParent.position = Vector3.zero;

        if (enableTMPSigns)
        {
            tmpsignsParent = new GameObject("TMP_RoadSigns").transform;
            tmpsignsParent.SetParent(roadsParent);
            tmpsignsParent.localPosition = Vector3.zero;
        }

        InitializeMaterials();
        await ImportOSMDataAsync();
    }

    void OnDestroy()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }

    private void InitializeMaterials()
    {
        urpShader = Shader.Find(mainUrpShader);

        if (urpShader == null)
        {
            string[] possibleUrpShaders = {
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Simple Lit",
                "Universal Render Pipeline/Baked Lit",
                "Sprites/Default",
                "Legacy Shaders/Diffuse"
            };

            foreach (var shaderName in possibleUrpShaders)
            {
                urpShader = Shader.Find(shaderName);
                if (urpShader != null) break;
            }
        }

        if (urpShader == null)
        {
            urpShader = Shader.Find(fallbackShader);
        }

        fallbackShaderObj = Shader.Find(fallbackShader) ?? urpShader;

        roadMaterials["motorway"] = roadMaterial;
        roadMaterials["trunk"] = roadMaterial;
        roadMaterials["primary"] = roadMaterial;
        roadMaterials["secondary"] = roadMaterial;
        roadMaterials["tertiary"] = roadMaterial;
        roadMaterials["unclassified"] = roadMaterial;
        roadMaterials["residential"] = roadMaterial;
    }

    public async UniTask ImportOSMDataAsync()
    {
        cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        string filePath = Path.Combine(Application.streamingAssetsPath, osmFileName);

        if (!File.Exists(filePath))
        {
            Debug.LogError($"OSM файл не найден: {filePath}");
            return;
        }

        try
        {
            FileFormat format = DetermineFileFormat(filePath);

            var parseResult = await ParseOsmFileAsync(filePath, format, cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;

            var nodeCache = parseResult.nodes;
            var ways = parseResult.ways;

            var roadWays = ways
                .Where(way => way.Tags != null && way.Tags.ContainsKey("highway"))
                .Where(way => includedHighwayTypes.Contains(way.Tags["highway"]))
                .Where(way =>
                {
                    if (!useBoundingBoxFilter) return true;
                    bool hasNodesInBoundingBox = way.Nodes.Any(nodeId => nodeCache.ContainsKey(nodeId));
                    if (!hasNodesInBoundingBox) return false;

                    if (strictBoundingBoxFilter)
                    {
                        int nodesInside = 0;
                        int totalNodes = 0;

                        foreach (var nodeId in way.Nodes)
                        {
                            if (nodeCache.ContainsKey(nodeId)) nodesInside++;
                            totalNodes++;
                        }

                        if (nodesInside == totalNodes) return true;
                        else if (nodesInside > 0) return true;
                        else return false;
                    }
                    else return true;
                })
                .Take(maxRoadsToProcess)
                .ToList();

            if (autoCenter)
            {
                CalculateCenterOffset(roadWays, nodeCache);
            }

            if (createSubcategories)
            {
                CreateCategoryParents();
            }

            var roadsData = await ProcessRoadWaysAsync(roadWays, nodeCache, cancellationToken);
            await CreateRoadsInBatches(roadsData, cancellationToken);

            if (generateBuildings)
            {
                var buildingWays = ways
                    .Where(way => way.Tags != null && way.Tags.ContainsKey("building"))
                    .Where(way => includedBuildingTypes.Contains(way.Tags["building"]) || includedBuildingTypes.Count == 0)
                    .Take(maxBuildingsToProcess)
                    .ToList();

                var buildingsData = await ProcessBuildingWaysAsync(buildingWays, nodeCache, cancellationToken);
                await CreateBuildingsInBatches(buildingsData, cancellationToken);
            }

            nodeCache.Clear();

            if (showBoundingBoxInScene && useBoundingBoxFilter)
            {
                await UniTask.SwitchToMainThread();
                CreateBoundingBoxVisualization();
            }

            roadsParent.gameObject.SetActive(true);
            buildingsParent.gameObject.SetActive(true);
        }
        catch (System.Exception e)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                Debug.LogError($"Ошибка при импорте OSM данных: {e.Message}");
            }
        }
    }

    private async UniTask<List<RoadData>> ProcessRoadWaysAsync(List<Way> roadWays, Dictionary<long, Node> nodeCache, CancellationToken cancellationToken)
    {
        var result = new List<RoadData>();

        foreach (var way in roadWays)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var points = new List<Vector3>();
            foreach (var nodeId in way.Nodes)
            {
                if (nodeCache.TryGetValue(nodeId, out var node) && node.Latitude.HasValue && node.Longitude.HasValue)
                {
                    Vector3 position = ConvertLatLonToUnityPosition(node.Latitude.Value, node.Longitude.Value);
                    points.Add(position);
                }
            }

            if (points.Count >= 2)
            {
                string roadType = "residential";
                if (way.Tags != null && way.Tags.ContainsKey("highway"))
                {
                    roadType = way.Tags["highway"];
                }

                result.Add(new RoadData
                {
                    way = way,
                    points = points,
                    roadType = roadType
                });
            }
        }

        return result;
    }

    private async UniTask<List<BuildingData>> ProcessBuildingWaysAsync(List<Way> buildingWays, Dictionary<long, Node> nodeCache, CancellationToken cancellationToken)
    {
        var result = new List<BuildingData>();

        foreach (var way in buildingWays)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var points = new List<Vector3>();
            foreach (var nodeId in way.Nodes)
            {
                if (nodeCache.TryGetValue(nodeId, out var node) && node.Latitude.HasValue && node.Longitude.HasValue)
                {
                    Vector3 position = ConvertLatLonToUnityPosition(node.Latitude.Value, node.Longitude.Value);
                    points.Add(position);
                }
            }

            if (points.Count >= 3)
            {
                float buildingHeight = defaultBuildingHeight;

                if (way.Tags != null)
                {
                    if (way.Tags.ContainsKey("height"))
                    {
                        float.TryParse(way.Tags["height"], out buildingHeight);
                    }
                    else if (way.Tags.ContainsKey("building:levels"))
                    {
                        if (float.TryParse(way.Tags["building:levels"], out float levels))
                        {
                            buildingHeight = levels * 3.0f;
                        }
                    }
                }

                result.Add(new BuildingData
                {
                    way = way,
                    points = points,
                    height = buildingHeight
                });
            }
        }

        return result;
    }

    private async UniTask CreateRoadsInBatches(List<RoadData> roadsData, CancellationToken cancellationToken)
    {
        await UniTask.SwitchToMainThread();

        for (int i = 0; i < roadsData.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested) return;

            CreateRoad(roadsData[i]);

            if (i % batchSize == 0)
            {
                await UniTask.Yield();
            }
        }
    }

    private async UniTask CreateBuildingsInBatches(List<BuildingData> buildingsData, CancellationToken cancellationToken)
    {
        await UniTask.SwitchToMainThread();

        for (int i = 0; i < buildingsData.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested) return;

            CreateBuilding(buildingsData[i]);

            if (i % batchSize == 0)
            {
                await UniTask.Yield();
            }
        }
    }

    private void CreateRoad(RoadData roadData)
    {
        var way = roadData.way;
        var roadPoints = roadData.points;
        var roadType = roadData.roadType;

        Mesh roadMesh = RoadMeshGenerator.CreateRoadMesh(roadPoints, roadWidth, false);
        if (roadMesh == null) return;

        long wayId = way.Id.HasValue ? way.Id.Value : 0;
        string roadName = $"Road_{wayId}";

        Transform parentTransform = roadsParent;
        if (createSubcategories)
        {
            if (categoryParents.ContainsKey(roadType))
            {
                parentTransform = categoryParents[roadType];
            }
            else
            {
                string tempCategoryName = "OTHER_Roads";
                if (!categoryParents.ContainsKey("other"))
                {
                    GameObject tempCategory = new GameObject(tempCategoryName);
                    tempCategory.transform.SetParent(roadsParent);
                    tempCategory.transform.localPosition = Vector3.zero;
                    categoryParents["other"] = tempCategory.transform;
                }
                parentTransform = categoryParents["other"];
            }
        }

        GameObject roadObject = new GameObject(roadName);
        roadObject.transform.SetParent(parentTransform);
        roadObject.transform.localPosition = Vector3.zero;

        MeshFilter meshFilter = roadObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = roadObject.AddComponent<MeshRenderer>();

        meshFilter.mesh = roadMesh;

        Material materialToUse = roadMaterials.ContainsKey(roadType) ? roadMaterials[roadType] : roadMaterials["residential"];

        if (materialToUse != null)
        {
            meshRenderer.material = materialToUse;
        }
        else
        {
            Material tempMaterial = new Material(urpShader ?? fallbackShaderObj);
            tempMaterial.color = Color.magenta;
            meshRenderer.material = tempMaterial;
        }

        RoadObjectData roadDataComponent = roadObject.AddComponent<RoadObjectData>();
        roadDataComponent.Initialize(wayId, roadType, roadPoints.Count, roadPoints);

        if (enableTMPSigns && tmpsignsParent != null && signFontAsset != null)
        {
            CreateTMPSign(way, roadPoints, roadType);
        }
    }

    private void CreateBuilding(BuildingData buildingData)
    {
        var way = buildingData.way;
        var buildingPoints = buildingData.points;
        var buildingHeight = buildingData.height;

        Mesh buildingMesh = BuildingMeshGenerator.CreateSimpleBuildingMesh(buildingPoints, buildingHeight);
        if (buildingMesh == null) return;

        long wayId = way.Id.HasValue ? way.Id.Value : 0;
        string buildingName = $"Building_{wayId}";

        GameObject buildingObject = new GameObject(buildingName);
        buildingObject.transform.SetParent(buildingsParent);
        buildingObject.transform.localPosition = Vector3.zero;

        MeshFilter meshFilter = buildingObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = buildingObject.AddComponent<MeshRenderer>();

        meshFilter.mesh = buildingMesh;

        if (buildingMaterial != null)
        {
            meshRenderer.material = buildingMaterial;
        }
        else
        {
            Material tempMaterial = new Material(urpShader ?? fallbackShaderObj);
            tempMaterial.color = new Color(0.8f, 0.8f, 0.8f);
            meshRenderer.material = tempMaterial;
        }

        BuildingObjectData buildingDataComponent = buildingObject.AddComponent<BuildingObjectData>();
        buildingDataComponent.Initialize(wayId, buildingPoints.Count, buildingPoints, buildingHeight);
    }

    private void CreateTMPSign(Way way, List<Vector3> roadPoints, string roadType)
    {
        if (signHighwayTypes != null && signHighwayTypes.Count > 0 && !signHighwayTypes.Contains(roadType)) return;
        if (roadPoints.Count < minRoadPointsForSign) return;

        string displayName = GetDisplayName(way, roadType);
        if (string.IsNullOrEmpty(displayName)) return;

        if (displayName.Length > maxDisplayNameLength)
        {
            displayName = displayName.Substring(0, maxDisplayNameLength) + "...";
        }

        displayName = displayName.Trim().ToUpper();
        if (displayName.Length < 2) return;

        int midPointIndex = roadPoints.Count / 2;
        if (midPointIndex >= roadPoints.Count) midPointIndex = roadPoints.Count - 1;

        Vector3 signPosition = roadPoints[midPointIndex];
        signPosition.y += signHeightAboveRoad;

        Vector3 roadDirection = Vector3.forward;
        if (midPointIndex > 0 && midPointIndex < roadPoints.Count - 1)
        {
            roadDirection = (roadPoints[midPointIndex + 1] - roadPoints[midPointIndex - 1]).normalized;
        }
        else if (roadPoints.Count > 1)
        {
            roadDirection = (roadPoints[1] - roadPoints[0]).normalized;
        }

        Vector3 roadPerpendicular = Vector3.Cross(Vector3.up, roadDirection).normalized;

        GameObject textObject = new GameObject($"Sign_{way.Id}");
        textObject.transform.SetParent(tmpsignsParent);
        textObject.transform.position = signPosition;

        textObject.transform.rotation = Quaternion.LookRotation(Vector3.up, roadDirection);
        textObject.transform.Rotate(0, 0, 90);
        textObject.transform.localScale = new Vector3(1, -1, 1);

        TextMeshPro tmpText = textObject.AddComponent<TextMeshPro>();
        tmpText.font = signFontAsset;
        tmpText.text = displayName;

        float finalFontSize = signFontSize * fontSizeMultiplier;
        tmpText.fontSize = finalFontSize;

        tmpText.color = signColor;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.textWrappingMode = TextWrappingModes.NoWrap;
        tmpText.rectTransform.sizeDelta = new Vector2(100, 20);
        tmpText.fontStyle = FontStyles.Bold;
        tmpText.enableAutoSizing = false;

        if (signFontAsset != null && signFontAsset.material != null)
        {
            Material tmpMaterial = new Material(signFontAsset.material);
            tmpMaterial.shader = Shader.Find("TextMeshPro/Distance Field");
            tmpMaterial.SetFloat("_CullMode", 0);
            tmpMaterial.SetFloat("_FaceDilate", 0.1f);
            tmpMaterial.renderQueue = 4000;
            tmpText.fontMaterial = tmpMaterial;
        }
        else if (signMaterial != null)
        {
            tmpText.fontSharedMaterial = signMaterial;
        }

        tmpText.ForceMeshUpdate();

        float offset = roadWidth / 2f + textOffsetFromRoad;
        textObject.transform.position += roadPerpendicular * offset;

        Bounds textBounds = tmpText.textBounds;
        if (textBounds.size.x > 0)
        {
            textObject.transform.position -= roadDirection * (textBounds.size.x / 2f);
        }
    }

    private string GetDisplayName(Way way, string roadType)
    {
        if (way.Tags != null && way.Tags.ContainsKey("name") && !string.IsNullOrEmpty(way.Tags["name"]))
        {
            return way.Tags["name"];
        }
        else if (way.Tags != null && way.Tags.ContainsKey("ref") && !string.IsNullOrEmpty(way.Tags["ref"]))
        {
            return way.Tags["ref"];
        }
        else
        {
            return "";
        }
    }

    private void CreateBoundingBoxVisualization()
    {
        GameObject bboxObject = new GameObject("BoundingBox_Visualization");
        bboxObject.transform.SetParent(roadsParent);

        Vector3 swCorner = ConvertLatLonToUnityPosition(minLatitude, minLongitude);
        Vector3 seCorner = ConvertLatLonToUnityPosition(minLatitude, maxLongitude);
        Vector3 nwCorner = ConvertLatLonToUnityPosition(maxLatitude, minLongitude);
        Vector3 neCorner = ConvertLatLonToUnityPosition(maxLatitude, maxLongitude);

        LineRenderer lineRenderer = bboxObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 5;
        lineRenderer.SetPositions(new Vector3[] {
            swCorner, seCorner, neCorner, nwCorner, swCorner
        });

        lineRenderer.startWidth = 0.5f;
        lineRenderer.endWidth = 0.5f;

        Material lineMaterial = new Material(urpShader ?? fallbackShaderObj);
        lineMaterial.color = Color.red;
        lineRenderer.material = lineMaterial;
    }

    private FileFormat DetermineFileFormat(string filePath)
    {
        if (fileFormat != FileFormat.AutoDetect) return fileFormat;

        string extension = Path.GetExtension(filePath).ToLower();
        return extension == ".pbf" ? FileFormat.OSM_PBF : FileFormat.OSM_XML;
    }

    private async UniTask<(Dictionary<long, Node> nodes, List<Way> ways, int nodesInsideBoundingBox)> ParseOsmFileAsync(string filePath, FileFormat format, CancellationToken cancellationToken)
    {
        var task = UniTask.RunOnThreadPool(() =>
        {
            int localNodesInsideBoundingBox = 0;
            var nodes = new Dictionary<long, Node>();
            var ways = new List<Way>();

            using (var fileStream = File.OpenRead(filePath))
            {
                OsmStreamSource source = CreateOsmSource(filePath, format);
                int elementCount = 0;

                foreach (var element in source)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    elementCount++;

                    if (element.Type == OsmGeoType.Node)
                    {
                        var node = (Node)element;
                        if (node.Id.HasValue && node.Latitude.HasValue && node.Longitude.HasValue)
                        {
                            bool isInsideBoundingBox = IsPointInsideBoundingBox(node.Latitude.Value, node.Longitude.Value);

                            if (isInsideBoundingBox)
                            {
                                nodes[node.Id.Value] = node;
                                localNodesInsideBoundingBox++;
                            }

                            if (enableNodeCacheCleanup && nodes.Count > maxNodeCacheSize)
                            {
                                var keysToRemove = nodes.Keys.Take(nodes.Count - maxNodeCacheSize / 2).ToList();
                                foreach (var key in keysToRemove) nodes.Remove(key);
                                System.GC.Collect();
                            }
                        }
                    }
                    else if (element.Type == OsmGeoType.Way)
                    {
                        var way = (Way)element;
                        ways.Add(way);
                    }

                    if (elementCount > 50000000) break;
                }
            }

            return (nodes, ways, localNodesInsideBoundingBox);
        });

        return await task;
    }

    private bool IsPointInsideBoundingBox(double lat, double lon)
    {
        if (!useBoundingBoxFilter) return true;
        return lat >= minLatitude && lat <= maxLatitude && lon >= minLongitude && lon <= maxLongitude;
    }

    private Vector3 ConvertLatLonToRawPosition(double lat, double lon)
    {
        float x = (float)((lon - originLatLon.y) * metersPerDegreeLat * Mathf.Cos(originLatLon.x * Mathf.Deg2Rad));
        float z = (float)((lat - originLatLon.x) * metersPerDegreeLat);
        return new Vector3(x, 0f, z);
    }

    private Vector3 ConvertLatLonToUnityPosition(double lat, double lon)
    {
        Vector3 rawPosition = ConvertLatLonToRawPosition(lat, lon);
        Vector3 centeredPosition = rawPosition - centerOffset;
        if (flattenToBaseHeight) centeredPosition.y = baseHeight;
        return centeredPosition;
    }

    private OsmStreamSource CreateOsmSource(string filePath, FileFormat format)
    {
        switch (format)
        {
            case FileFormat.OSM_PBF:
                var pbfStream = File.OpenRead(filePath);
                return new PBFOsmStreamSource(pbfStream);
            case FileFormat.OSM_XML:
                var xmlStream = File.OpenRead(filePath);
                return new XmlOsmStreamSource(xmlStream);
            default:
                throw new System.NotSupportedException($"Формат {format} не поддерживается");
        }
    }

    private void CalculateCenterOffset(List<Way> roadWays, Dictionary<long, Node> nodes)
    {
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;
        int sampleCount = 0;

        int maxSamples = Mathf.Min(1000, roadWays.Count * 10);

        foreach (var way in roadWays)
        {
            if (sampleCount >= maxSamples) break;

            foreach (var nodeId in way.Nodes)
            {
                if (sampleCount >= maxSamples) break;

                if (nodes.TryGetValue(nodeId, out var node) && node.Latitude.HasValue && node.Longitude.HasValue)
                {
                    Vector3 rawPos = ConvertLatLonToRawPosition(node.Latitude.Value, node.Longitude.Value);
                    minX = Mathf.Min(minX, rawPos.x);
                    maxX = Mathf.Max(maxX, rawPos.x);
                    minZ = Mathf.Min(minZ, rawPos.z);
                    maxZ = Mathf.Max(maxZ, rawPos.z);
                    sampleCount++;
                }
            }
        }

        if (sampleCount > 0)
        {
            centerOffset = new Vector3((minX + maxX) / 2f, 0f, (minZ + maxZ) / 2f);
        }
        else
        {
            centerOffset = Vector3.zero;
        }
    }

    private void CreateCategoryParents()
    {
        foreach (var roadType in includedHighwayTypes)
        {
            string categoryName = $"{roadType.ToUpper()}_Roads";
            GameObject categoryGO = new GameObject(categoryName);
            categoryGO.transform.SetParent(roadsParent);
            categoryGO.transform.localPosition = Vector3.zero;
            categoryParents[roadType] = categoryGO.transform;
        }
    }

    void OnDrawGizmos()
    {
        if (centerOffset != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(Vector3.zero, 10f);
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * 20f);
            Gizmos.DrawLine(Vector3.zero, Vector3.right * 20f);
        }
    }

    public struct RoadData
    {
        public Way way;
        public List<Vector3> points;
        public string roadType;
    }

    private struct BuildingData
    {
        public Way way;
        public List<Vector3> points;
        public float height;
    }

    public class RoadObjectData : MonoBehaviour
    {
        public long OsmId { get; private set; }
        public string HighwayType { get; private set; }
        public int SegmentCount { get; private set; }
        public List<Vector3> Points { get; private set; }

        public void Initialize(long id, string type, int segments, List<Vector3> points)
        {
            OsmId = id;
            HighwayType = type;
            SegmentCount = segments;
            Points = new List<Vector3>(points);
        }

        void OnDrawGizmosSelected()
        {
            if (Points != null && Points.Count > 1)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < Points.Count - 1; i++)
                {
                    Gizmos.DrawLine(Points[i], Points[i + 1]);
                }

                Gizmos.color = Color.red;
                foreach (var point in Points)
                {
                    Gizmos.DrawSphere(point, 0.5f);
                }
            }
        }
    }

    public class BuildingObjectData : MonoBehaviour
    {
        public long OsmId { get; private set; }
        public int VertexCount { get; private set; }
        public List<Vector3> Points { get; private set; }
        public float Height { get; private set; }

        public void Initialize(long id, int vertexCount, List<Vector3> points, float height)
        {
            OsmId = id;
            VertexCount = vertexCount;
            Points = new List<Vector3>(points);
            Height = height;
        }

        void OnDrawGizmosSelected()
        {
            if (Points != null && Points.Count > 2)
            {
                Gizmos.color = Color.blue;
                for (int i = 0; i < Points.Count; i++)
                {
                    int next = (i + 1) % Points.Count;
                    Gizmos.DrawLine(Points[i], Points[next]);
                }
            }
        }
    }
}
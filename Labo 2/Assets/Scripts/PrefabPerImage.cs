using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(ARTrackedImageManager))]
public sealed class PrefabPerImage : MonoBehaviour
{
    const string CubeImageName = "cubo";
    const string SphereImageName = "esfera";

    [SerializeField]
    ARTrackedImageManager images;

    [SerializeField]
    GameObject cubePrefab;

    [SerializeField]
    GameObject spherePrefab;

    readonly Dictionary<TrackableId, GameObject> spawned = new();

    public ARTrackedImageManager Images => images;
    public GameObject CubePrefab => cubePrefab;
    public GameObject SpherePrefab => spherePrefab;

    public void Configure(
        ARTrackedImageManager imageManager,
        GameObject trackedCubePrefab,
        GameObject trackedSpherePrefab)
    {
        images = imageManager;
        cubePrefab = trackedCubePrefab;
        spherePrefab = trackedSpherePrefab;
    }

    void Reset()
    {
        images = GetComponent<ARTrackedImageManager>();
    }

    void OnValidate()
    {
        if (images == null)
            images = GetComponent<ARTrackedImageManager>();
    }

    void OnEnable()
    {
        if (images == null)
            images = GetComponent<ARTrackedImageManager>();

        if (images == null || cubePrefab == null || spherePrefab == null)
        {
            Debug.LogError(
                "PrefabPerImage requiere el AR Tracked Image Manager y los prefabs de cubo y esfera.",
                this);
            enabled = false;
            return;
        }

        images.trackablesChanged.AddListener(OnChanged);

        foreach (var image in images.trackables)
            SpawnOrApply(image);
    }

    void OnDisable()
    {
        if (images != null)
            images.trackablesChanged.RemoveListener(OnChanged);
    }

    void OnChanged(ARTrackablesChangedEventArgs<ARTrackedImage> changes)
    {
        foreach (var image in changes.added)
            SpawnOrApply(image);

        foreach (var image in changes.updated)
            SpawnOrApply(image);

        foreach (var pair in changes.removed)
        {
            if (!spawned.Remove(pair.Key, out var instance))
                continue;

            Destroy(instance);
        }
    }

    void SpawnOrApply(ARTrackedImage image)
    {
        if (!spawned.TryGetValue(image.trackableId, out var instance))
        {
            var prefab = PrefabFor(image.referenceImage.name);
            if (prefab == null)
            {
                Debug.LogWarning(
                    $"La imagen '{image.referenceImage.name}' no tiene contenido AR asignado.",
                    image);
                return;
            }

            instance = Instantiate(prefab, image.transform);
            instance.name = prefab.name;
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;
            spawned.Add(image.trackableId, instance);
        }

        instance.SetActive(image.trackingState == TrackingState.Tracking);
    }

    GameObject PrefabFor(string imageName)
    {
        return imageName switch
        {
            CubeImageName => cubePrefab,
            SphereImageName => spherePrefab,
            _ => null
        };
    }
}

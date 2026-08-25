using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(ARTrackedImageManager))]
public sealed class TrackedImageVisibility : MonoBehaviour
{
    [SerializeField]
    ARTrackedImageManager images;

    public void Configure(ARTrackedImageManager manager)
    {
        images = manager;
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

        if (images == null)
        {
            Debug.LogError("TrackedImageVisibility requiere un ARTrackedImageManager asignado.", this);
            enabled = false;
            return;
        }

        images.trackablesChanged.AddListener(OnChanged);

        foreach (var image in images.trackables)
            Apply(image);
    }

    void OnDisable()
    {
        if (images != null)
            images.trackablesChanged.RemoveListener(OnChanged);
    }

    static void OnChanged(ARTrackablesChangedEventArgs<ARTrackedImage> changes)
    {
        foreach (var image in changes.added)
            Apply(image);

        foreach (var image in changes.updated)
            Apply(image);
    }

    static void Apply(ARTrackedImage image)
    {
        var isVisible = image.trackingState == TrackingState.Tracking;

        foreach (var childRenderer in image.GetComponentsInChildren<Renderer>(true))
            childRenderer.enabled = isVisible;
    }
}

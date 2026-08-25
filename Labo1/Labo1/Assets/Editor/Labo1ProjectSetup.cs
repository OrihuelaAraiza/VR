using System;
using System.IO;
using System.Linq;
using System.Xml;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.ARCore;
using UnityEditor.XR.ARKit;
using UnityEditor.XR.ARSubsystems;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARCore;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARKit;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;
using Object = UnityEngine.Object;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

public static class Labo1ProjectSetup
{
    const string ScenePath = "Assets/Scenes/Labo1AR.unity";
    const string LibraryPath = "Assets/Blancos/BibliotecaBlancos.asset";
    const string PrefabPath = "Assets/Prefabs/ContenidoBlanco.prefab";
    const string SabrinaModelPath = "Assets/Models/SabrinaCarpenter/source/Tour Ready Pink.fbx";
    const string SabrinaTextureRoot = "Assets/Models/SabrinaCarpenter/textures";
    const string SabrinaMaterialRoot = "Assets/Materials/SabrinaCarpenter";
    const string AndroidBuildPath = "Builds/Labo1-AR.apk";
    const string IOSBuildPath = "Builds/iOS/Labo1AR-iPad";
    const string ARKitSettingsPath = "Assets/XR/Settings/ARKitSettings.asset";
    const string AutoStageKey = "Labo1.AutoSetupStage.v2";
    const float PrintedTargetWidthMeters = 0.18f;
    const float SabrinaHeightMeters = 0.14f;
    const float SabrinaBaseClearanceMeters = 0.002f;

    [InitializeOnLoadMethod]
    static void ScheduleAutomaticSetup()
    {
        EditorApplication.delayCall += ContinueAutomaticSetup;
    }

    [MenuItem("Labo1/Configurar proyecto AR")]
    public static void ConfigureFromMenu()
    {
        ConfigureProject(rebuildScene: true);
        Debug.Log("Labo1: proyecto AR configurado correctamente.");
    }

    [MenuItem("Labo1/Compilar APK Android")]
    public static void BuildFromMenu()
    {
        ConfigureProject(rebuildScene: false);
        BuildAndroid();
    }

    [MenuItem("Labo1/Compilar proyecto Xcode para iPad")]
    public static void BuildIOSFromMenu()
    {
        ConfigureProject(rebuildScene: false);
        BuildIOS();
    }

    static void ContinueAutomaticSetup()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer)
        {
            EditorApplication.delayCall += ContinueAutomaticSetup;
            return;
        }

        var stage = SessionState.GetString(AutoStageKey, string.Empty);
        if (stage == "complete" || stage == "failed")
            return;

        try
        {
            ConfigureProject(rebuildScene: !File.Exists(ScenePath));

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                SessionState.SetString(AutoStageKey, "waiting-for-android");
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                    throw new InvalidOperationException("Unity no pudo cambiar la plataforma activa a Android.");
            }

            SessionState.SetString(AutoStageKey, "building");
            BuildAndroid();
            SessionState.SetString(AutoStageKey, "complete");
        }
        catch (Exception exception)
        {
            SessionState.SetString(AutoStageKey, "failed");
            Debug.LogException(exception);
            Debug.LogError("Labo1: la configuración o compilación automática falló. Usa Labo1 > Compilar APK Android después de corregir el error indicado.");
        }
    }

    static void ConfigureProject(bool rebuildScene)
    {
        EnsureFolders();
        ConfigureAndroidPlayer();
        ConfigureIOSPlayer();
        ConfigureARCoreLoader();
        ConfigureARKitLoader();
        ConfigureARBackgroundRendererFeatures();
        ConfigureTextures();
        ConfigureSabrinaAssets();

        var library = CreateOrUpdateReferenceLibrary();
        var prefab = CreateOrUpdateTrackedPrefab();

        if (rebuildScene || !File.Exists(ScenePath))
            CreateARScene(library, prefab);

        ConfigureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void EnsureFolders()
    {
        EnsureFolder("Assets/Blancos");
        EnsureFolder("Assets/Materials");
        EnsureFolder(SabrinaMaterialRoot);
        EnsureFolder("Assets/Models");
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/XR");
        EnsureFolder("Assets/XR/Settings");
        EnsureFolder("Builds");
        EnsureFolder("Builds/iOS");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path) || Directory.Exists(path))
            return;

        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        var name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent))
        {
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    static void ConfigureTextures()
    {
        ConfigureTexture("Assets/Blancos/ar1.jpg");
        ConfigureTexture("Assets/Blancos/ar2.jpg");
    }

    static void ConfigureTexture(string path)
    {
        if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            throw new FileNotFoundException($"No se encontró la textura requerida: {path}");

        var changed = importer.textureType != TextureImporterType.Default
            || importer.mipmapEnabled
            || importer.maxTextureSize < 2048
            || !importer.sRGBTexture;

        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 2048;
        importer.sRGBTexture = true;
        importer.alphaSource = TextureImporterAlphaSource.None;
        importer.isReadable = false;

        if (changed)
            importer.SaveAndReimport();
    }

    static XRReferenceImageLibrary CreateOrUpdateReferenceLibrary()
    {
        var library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(LibraryPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<XRReferenceImageLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }

        while (library.count > 0)
            library.RemoveAt(library.count - 1);

        AddReferenceImage(library, "Assets/Blancos/ar1.jpg", "ar1");
        AddReferenceImage(library, "Assets/Blancos/ar2.jpg", "ar2");
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssetIfDirty(library);
        return library;
    }

    static void AddReferenceImage(XRReferenceImageLibrary library, string texturePath, string imageName)
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
            throw new FileNotFoundException($"No se pudo cargar {texturePath} como Texture2D.");

        var height = PrintedTargetWidthMeters * texture.height / texture.width;
        library.Add();
        var index = library.count - 1;
        library.SetName(index, imageName);
        library.SetTexture(index, texture, keepTexture: false);
        library.SetSpecifySize(index, true);
        library.SetSize(index, new Vector2(PrintedTargetWidthMeters, height));
    }

    static void ConfigureSabrinaAssets()
    {
        if (!File.Exists(SabrinaModelPath))
            throw new FileNotFoundException($"No se encontró el modelo de Sabrina Carpenter: {SabrinaModelPath}");

        ConfigureSabrinaTexture("T_DimeBlanketGrace_Body_D.png", false);
        ConfigureSabrinaTexture("T_DimeBlanketGrace_Body_N.tga.png", true);
        ConfigureSabrinaTexture("T_DimeBlanketGrace_Body_S.tga.png", false, false);
        ConfigureSabrinaTexture("T_DimeBlanketGrace_FaceAcc_D.png", false);
        ConfigureSabrinaTexture("T_DimeBlanketGrace_FaceAcc_M.png", false, false);
        ConfigureSabrinaTexture("T_DimeBlanketGrace_FaceAcc_N.png", true);
        ConfigureSabrinaTexture("T_DimeBlanketGrace_FaceAcc_S.png", false, false);
        ConfigureSabrinaTexture("T_DimeBlanketGrace_Head_D.png", false);
        ConfigureSabrinaTexture("T_DimeBlanketGrace_Head_N.png", true);
        ConfigureSabrinaTexture("T_DimeBlanketGrace_Head_S.png", false, false);

        AssetDatabase.ImportAsset(SabrinaModelPath, ImportAssetOptions.ForceSynchronousImport);
        if (AssetImporter.GetAtPath(SabrinaModelPath) is not ModelImporter importer)
            throw new InvalidOperationException($"Unity no pudo importar {SabrinaModelPath} como un modelo 3D.");

        var needsReimport = importer.importAnimation
            || importer.importBlendShapes
            || importer.importCameras
            || importer.importLights
            || importer.isReadable
            || importer.meshCompression != ModelImporterMeshCompression.Medium;

        importer.importAnimation = false;
        importer.importBlendShapes = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.isReadable = false;
        importer.meshCompression = ModelImporterMeshCompression.Medium;
        importer.optimizeMeshPolygons = true;
        importer.optimizeMeshVertices = true;

        if (needsReimport)
            importer.SaveAndReimport();
    }

    static void ConfigureSabrinaTexture(string fileName, bool normalMap, bool sRgb = true)
    {
        var path = $"{SabrinaTextureRoot}/{fileName}";
        if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            throw new FileNotFoundException($"Falta la textura del modelo: {path}");

        var desiredType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
        var changed = importer.textureType != desiredType
            || importer.maxTextureSize != 1024
            || !importer.mipmapEnabled
            || importer.isReadable
            || (!normalMap && importer.sRGBTexture != sRgb);

        importer.textureType = desiredType;
        importer.maxTextureSize = 1024;
        importer.mipmapEnabled = true;
        importer.isReadable = false;
        importer.alphaIsTransparency = !normalMap && sRgb;
        if (!normalMap)
            importer.sRGBTexture = sRgb;

        if (changed)
            importer.SaveAndReimport();
    }

    static GameObject CreateOrUpdateTrackedPrefab()
    {
        var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(SabrinaModelPath);
        if (modelAsset == null)
            throw new InvalidOperationException($"No se pudo cargar el modelo importado: {SabrinaModelPath}");

        var body = CreateOrUpdateSabrinaMaterial(
            "SabrinaBody",
            "T_DimeBlanketGrace_Body_D.png",
            "T_DimeBlanketGrace_Body_N.tga.png",
            alphaClipping: false,
            smoothness: 0.38f);
        var head = CreateOrUpdateSabrinaMaterial(
            "SabrinaHead",
            "T_DimeBlanketGrace_Head_D.png",
            "T_DimeBlanketGrace_Head_N.png",
            alphaClipping: false,
            smoothness: 0.42f);
        var faceAccessories = CreateOrUpdateSabrinaMaterial(
            "SabrinaFaceAccessories",
            "T_DimeBlanketGrace_FaceAcc_D.png",
            "T_DimeBlanketGrace_FaceAcc_N.png",
            alphaClipping: true,
            smoothness: 0.35f);
        var eyes = CreateOrUpdateSabrinaMaterial(
            "SabrinaEyes",
            "T_DimeBlanketGrace_Head_D.png",
            "T_DimeBlanketGrace_Head_N.png",
            alphaClipping: false,
            smoothness: 0.72f);
        var flyawayHair = CreateOrUpdateSabrinaMaterial(
            "SabrinaFlyawayHair",
            "T_DimeBlanketGrace_Head_D.png",
            "T_DimeBlanketGrace_Head_N.png",
            alphaClipping: true,
            smoothness: 0.28f);

        var root = new GameObject("ContenidoBlanco");
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one;

        var model = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
        if (model == null)
        {
            Object.DestroyImmediate(root);
            throw new InvalidOperationException("No se pudo instanciar el modelo de Sabrina Carpenter.");
        }

        model.name = "SabrinaCarpenterAR";
        model.transform.SetParent(root.transform, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            var assigned = renderer.sharedMaterials;
            for (var index = 0; index < assigned.Length; index++)
            {
                var sourceName = assigned[index] != null ? assigned[index].name : string.Empty;
                assigned[index] = ResolveSabrinaMaterial(
                    sourceName,
                    body,
                    head,
                    faceAccessories,
                    eyes,
                    flyawayHair);
            }

            renderer.sharedMaterials = assigned;
        }

        FitModelToTrackedImage(model);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static Material CreateOrUpdateSabrinaMaterial(
        string materialName,
        string baseMapName,
        string normalMapName,
        bool alphaClipping,
        float smoothness)
    {
        var materialPath = $"{SabrinaMaterialRoot}/{materialName}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("No se encontró el shader Universal Render Pipeline/Lit.");

            material = new Material(shader) { name = materialName };
            AssetDatabase.CreateAsset(material, materialPath);
        }

        var baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>($"{SabrinaTextureRoot}/{baseMapName}");
        var normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>($"{SabrinaTextureRoot}/{normalMapName}");
        if (baseMap == null || normalMap == null)
            throw new InvalidOperationException($"No se pudieron cargar las texturas para {materialName}.");

        material.shader = Shader.Find("Universal Render Pipeline/Lit");
        material.SetColor("_BaseColor", Color.white);
        material.SetTexture("_BaseMap", baseMap);
        material.SetTexture("_BumpMap", normalMap);
        material.SetFloat("_BumpScale", 1f);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", smoothness);
        material.EnableKeyword("_NORMALMAP");
        material.SetFloat("_AlphaClip", alphaClipping ? 1f : 0f);
        material.SetFloat("_Cutoff", 0.35f);
        material.SetFloat("_Cull", alphaClipping ? 0f : 2f);

        if (alphaClipping)
        {
            material.EnableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.AlphaTest;
        }
        else
        {
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = -1;
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    static Material ResolveSabrinaMaterial(
        string sourceName,
        Material body,
        Material head,
        Material faceAccessories,
        Material eyes,
        Material flyawayHair)
    {
        if (sourceName.IndexOf("FaceAcc", StringComparison.OrdinalIgnoreCase) >= 0)
            return faceAccessories;
        if (sourceName.IndexOf("Flyaway", StringComparison.OrdinalIgnoreCase) >= 0)
            return flyawayHair;
        if (sourceName.IndexOf("Eye", StringComparison.OrdinalIgnoreCase) >= 0)
            return eyes;
        if (sourceName.IndexOf("Head", StringComparison.OrdinalIgnoreCase) >= 0)
            return head;
        return body;
    }

    static void FitModelToTrackedImage(GameObject model)
    {
        var renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new InvalidOperationException("El modelo de Sabrina Carpenter no contiene renderers visibles.");

        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1))
            bounds.Encapsulate(renderer.bounds);

        if (bounds.size.y <= Mathf.Epsilon)
            throw new InvalidOperationException("El modelo de Sabrina Carpenter tiene una altura inválida.");

        var scale = SabrinaHeightMeters / bounds.size.y;
        model.transform.localScale = Vector3.one * scale;

        bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1))
            bounds.Encapsulate(renderer.bounds);

        model.transform.localPosition = new Vector3(
            -bounds.center.x,
            SabrinaBaseClearanceMeters - bounds.min.y,
            -bounds.center.z);
    }

    static void CreateARScene(XRReferenceImageLibrary library, GameObject prefab)
    {
        EditorSceneManager.SaveOpenScenes();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var sessionObject = new GameObject("AR Session");
        sessionObject.AddComponent<ARSession>();
        sessionObject.AddComponent<ARInputManager>();

        var originObject = new GameObject("XR Origin");
        var origin = originObject.AddComponent<XROrigin>();

        var cameraOffset = new GameObject("Camera Offset");
        cameraOffset.transform.SetParent(originObject.transform, false);

        var cameraObject = new GameObject("Main Camera");
        cameraObject.transform.SetParent(cameraOffset.transform, false);
        cameraObject.tag = "MainCamera";

        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Color;
        camera.backgroundColor = Color.black;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 20f;
        cameraObject.AddComponent<AudioListener>();

        var cameraManager = cameraObject.AddComponent<ARCameraManager>();
        cameraManager.autoFocusRequested = true;
        cameraManager.requestedFacingDirection = CameraFacingDirection.World;
        cameraManager.requestedLightEstimation = LightEstimation.AmbientIntensity | LightEstimation.AmbientColor;
        cameraObject.AddComponent<ARCameraBackground>();

        var trackedPoseDriver = cameraObject.AddComponent<TrackedPoseDriver>();
        var positionAction = new InputAction("Position", binding: "<XRHMD>/centerEyePosition", expectedControlType: "Vector3");
        positionAction.AddBinding("<HandheldARInputDevice>/devicePosition");
        var rotationAction = new InputAction("Rotation", binding: "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion");
        rotationAction.AddBinding("<HandheldARInputDevice>/deviceRotation");
        trackedPoseDriver.positionInput = new InputActionProperty(positionAction);
        trackedPoseDriver.rotationInput = new InputActionProperty(rotationAction);

        origin.CameraFloorOffsetObject = cameraOffset;
        origin.Camera = camera;

        var imageManager = originObject.AddComponent<ARTrackedImageManager>();
        imageManager.referenceLibrary = library;
        imageManager.requestedMaxNumberOfMovingImages = 1;
        imageManager.trackedImagePrefab = prefab;

        var visibility = originObject.AddComponent<TrackedImageVisibility>();
        visibility.Configure(imageManager);

        var lightObject = new GameObject("Directional Light");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.96f, 0.9f);
        light.intensity = 1.15f;
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    static void ConfigureARBackgroundRendererFeatures()
    {
        var rendererGuids = AssetDatabase.FindAssets("t:ScriptableRendererData");
        foreach (var guid in rendererGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
            if (rendererData == null || rendererData.rendererFeatures.Any(feature => feature is ARBackgroundRendererFeature))
                continue;

            var feature = ScriptableObject.CreateInstance<ARBackgroundRendererFeature>();
            feature.name = nameof(ARBackgroundRendererFeature);
            feature.hideFlags |= HideFlags.HideInHierarchy;
            feature.Create();
            AssetDatabase.AddObjectToAsset(feature, rendererData);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

            var serializedRendererData = new SerializedObject(rendererData);
            var features = serializedRendererData.FindProperty("m_RendererFeatures");
            features.arraySize++;
            features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;

            var featureMap = serializedRendererData.FindProperty("m_RendererFeatureMap");
            featureMap.arraySize++;
            featureMap.GetArrayElementAtIndex(featureMap.arraySize - 1).longValue = localId;

            serializedRendererData.ApplyModifiedPropertiesWithoutUndo();
            rendererData.SetDirty();
            EditorUtility.SetDirty(rendererData);
            AssetDatabase.SaveAssetIfDirty(rendererData);
        }
    }

    static void ConfigureARCoreLoader()
    {
        var settingsPerTarget = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>)
            .FirstOrDefault(asset => asset != null);

        if (settingsPerTarget == null)
        {
            settingsPerTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(settingsPerTarget, "Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, settingsPerTarget, true);
        }

        if (!settingsPerTarget.HasSettingsForBuildTarget(BuildTargetGroup.Android))
            settingsPerTarget.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);

        if (!settingsPerTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
            settingsPerTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);

        var managerSettings = settingsPerTarget.ManagerSettingsForBuildTarget(BuildTargetGroup.Android);
        if (!XRPackageMetadataStore.AssignLoader(managerSettings, typeof(ARCoreLoader).FullName, BuildTargetGroup.Android))
            throw new InvalidOperationException("No se pudo asignar ARCoreLoader a Android.");

        // XR Management creates these flags disabled when the settings asset is
        // generated through code. AR Foundation needs both enabled so ARCore is
        // initialized and started automatically when the player launches.
        managerSettings.automaticLoading = true;
        managerSettings.automaticRunning = true;
        EditorUtility.SetDirty(managerSettings);

        var arCoreSettings = ARCoreSettings.GetOrCreateSettings();
        arCoreSettings.requirement = ARCoreSettings.Requirement.Required;
        // El laboratorio usa seguimiento de imágenes, no profundidad. Mantener
        // Depth opcional evita excluir teléfonos ARCore compatibles sin Depth API.
        arCoreSettings.depth = ARCoreSettings.Requirement.Optional;
        ARCoreSettings.currentSettings = arCoreSettings;
        EditorUtility.SetDirty(arCoreSettings);
    }

    static void ConfigureAndroidPlayer()
    {
        PlayerSettings.companyName = "UP";
        PlayerSettings.productName = "Labo1 AR";
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.up.vr.labo1");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        EditorUserBuildSettings.buildAppBundle = false;
    }

    static void ConfigureARKitLoader()
    {
        var settingsPerTarget = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>)
            .FirstOrDefault(asset => asset != null);

        if (settingsPerTarget == null)
            throw new InvalidOperationException("No se encontraron los ajustes generales de XR.");

        if (!settingsPerTarget.HasSettingsForBuildTarget(BuildTargetGroup.iOS))
            settingsPerTarget.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.iOS);

        if (!settingsPerTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.iOS))
            settingsPerTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.iOS);

        var managerSettings = settingsPerTarget.ManagerSettingsForBuildTarget(BuildTargetGroup.iOS);
        if (!XRPackageMetadataStore.AssignLoader(managerSettings, typeof(ARKitLoader).FullName, BuildTargetGroup.iOS))
            throw new InvalidOperationException("No se pudo asignar ARKitLoader a iOS.");

        managerSettings.automaticLoading = true;
        managerSettings.automaticRunning = true;
        EditorUtility.SetDirty(managerSettings);

        var arKitSettings = ARKitSettings.currentSettings;
        if (arKitSettings == null)
        {
            arKitSettings = ScriptableObject.CreateInstance<ARKitSettings>();
            AssetDatabase.CreateAsset(arKitSettings, ARKitSettingsPath);
        }

        arKitSettings.requirement = ARKitSettings.Requirement.Required;
        arKitSettings.faceTracking = false;
        ARKitSettings.currentSettings = arKitSettings;
        EditorUtility.SetDirty(arKitSettings);
    }

    static void ConfigureIOSPlayer()
    {
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.up.vr.labo1");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
        PlayerSettings.iOS.buildNumber = "1";
        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
        PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPadOnly;
        PlayerSettings.iOS.targetOSVersionString = "15.0";
        PlayerSettings.iOS.cameraUsageDescription =
            "La cámara se utiliza para detectar imágenes y mostrar contenido de realidad aumentada.";
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new[] { GraphicsDeviceType.Metal });
    }

    static void ConfigureBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
    }

    static void BuildAndroid()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android
            && !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
        {
            throw new InvalidOperationException("No se pudo activar Android para compilar.");
        }

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = AndroidBuildPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None
        });

        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException($"La compilación Android terminó con estado {report.summary.result} y {report.summary.totalErrors} errores.");

        var apkSizeMegabytes = new FileInfo(AndroidBuildPath).Length / 1024f / 1024f;
        Debug.Log($"Labo1: APK listo en {Path.GetFullPath(AndroidBuildPath)} ({apkSizeMegabytes:F1} MB).");
    }

    static void BuildIOS()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS
            && !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS))
        {
            throw new InvalidOperationException("No se pudo activar iOS para compilar.");
        }

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = IOSBuildPath,
            target = BuildTarget.iOS,
            targetGroup = BuildTargetGroup.iOS,
            options = BuildOptions.None
        });

        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException($"La compilación iOS terminó con estado {report.summary.result} y {report.summary.totalErrors} errores.");

        Debug.Log($"Labo1: proyecto Xcode para iPad listo en {Path.GetFullPath(IOSBuildPath)}.");
    }
}

/// <summary>
/// ARCore only adds its depth feature when it is required, but on incremental
/// Android builds an old required=true entry can remain in the generated
/// manifest. Image tracking does not use Depth API, so normalize that entry on
/// every build to preserve compatibility with all ARCore image-tracking devices.
/// </summary>
public sealed class Labo1AndroidManifestPostprocessor : IPostGenerateGradleAndroidProject
{
    const string AndroidNamespace = "http://schemas.android.com/apk/res/android";
    const string DepthFeature = "com.google.ar.core.depth";

    public int callbackOrder => 1000;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        var manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");
        var document = new XmlDocument();
        document.Load(manifestPath);

        var featureNodes = document.SelectNodes("/manifest/uses-feature");
        if (featureNodes == null)
            return;

        foreach (XmlNode node in featureNodes)
        {
            if (node is not XmlElement feature
                || feature.GetAttribute("name", AndroidNamespace) != DepthFeature)
                continue;

            feature.SetAttribute("required", AndroidNamespace, "false");
        }

        document.Save(manifestPath);
    }
}

#if UNITY_IOS
/// <summary>
/// Keeps the generated Xcode project explicitly restricted to devices that
/// support this lab's required ARKit image-tracking runtime. This is also a
/// safety net for the first iOS build after assigning the ARKit loader, before
/// Unity has refreshed the package's platform define.
/// </summary>
public sealed class Labo1IOSPlistPostprocessor : IPostprocessBuildWithReport
{
    public int callbackOrder => 1000;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.iOS)
            return;

        var plistPath = Path.Combine(report.summary.outputPath, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        const string capabilitiesKey = "UIRequiredDeviceCapabilities";
        var root = plist.root;
        var capabilities = root.values.TryGetValue(capabilitiesKey, out var existing)
            ? existing.AsArray()
            : root.CreateArray(capabilitiesKey);

        EnsureCapability(capabilities, "arm64");
        EnsureCapability(capabilities, "metal");
        EnsureCapability(capabilities, "arkit");

        plist.WriteToFile(plistPath);
    }

    static void EnsureCapability(PlistElementArray capabilities, string value)
    {
        if (!capabilities.values.Any(element => value.Equals(element.AsString(), StringComparison.Ordinal)))
            capabilities.AddString(value);
    }
}
#endif

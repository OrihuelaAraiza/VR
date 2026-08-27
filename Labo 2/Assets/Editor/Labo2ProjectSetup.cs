using System;
using System.IO;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
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
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARKit;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;
using Object = UnityEngine.Object;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

public static class Labo2ProjectSetup
{
    const string ScenePath = "Assets/Scenes/Labo2AR.unity";
    const string LibraryPath = "Assets/Blancos/BibliotecaBlancos.asset";
    const string CubePrefabPath = "Assets/Prefabs/ContenidoCubo.prefab";
    const string SpherePrefabPath = "Assets/Prefabs/ContenidoEsfera.prefab";
    const string CubeMaterialPath = "Assets/Materials/CuboARMaterial.mat";
    const string SphereMaterialPath = "Assets/Materials/EsferaARMaterial.mat";
    const string IOSBuildPath = "Builds/iOS/Labo2AR-iPad";
    const string ARKitSettingsPath = "Assets/XR/Settings/ARKitSettings.asset";
    const string ARKitLoaderDefine = "UNITY_XR_ARKIT_LOADER_ENABLED";
    const string BundleIdentifier = "com.up.vr.labo2";
    const string DeveloperTeam = "8MBP94XP38";
    const float CubePrintedWidthMeters = 0.18f;
    const float SpherePrintedWidthMeters = 0.18f;
    const float FigureSizeMeters = 0.05f;

    [MenuItem("Labo 2/Configurar proyecto AR para iPad")]
    public static void ConfigureFromMenu()
    {
        ConfigureProject();
        ValidateProject();
        Debug.Log("Labo 2: proyecto AR para iPad configurado y validado.");
    }

    [MenuItem("Labo 2/Compilar proyecto Xcode para iPad")]
    public static void BuildIOSFromMenu()
    {
        ConfigureProject();
        ValidateProject();
        BuildIOS();
    }

    public static void ConfigureForBatch()
    {
        ConfigureProject();
        ValidateProject();
        Debug.Log("LABO2_BATCH_CONFIGURE_SUCCESS");
    }

    public static void BuildIOSForBatch()
    {
        ConfigureProject();
        ValidateProject();
        BuildIOS();
        Debug.Log("LABO2_BATCH_BUILD_SUCCESS");
    }

    static void ConfigureProject()
    {
        EnsureFolders();
        ConfigureIOSPlayer();
        ConfigureARKitLoader();
        ConfigureARBackgroundRendererFeatures();
        ConfigureTexture("Assets/Blancos/ar1.jpg");
        ConfigureTexture("Assets/Blancos/ar2.jpg");

        var library = CreateOrUpdateReferenceLibrary();
        var cubePrefab = CreateOrUpdateFigurePrefab(
            "ContenidoCubo",
            PrimitiveType.Cube,
            CubePrefabPath,
            CreateOrUpdateMaterial(CubeMaterialPath, new Color(0.86f, 0.10f, 0.16f)));
        var spherePrefab = CreateOrUpdateFigurePrefab(
            "ContenidoEsfera",
            PrimitiveType.Sphere,
            SpherePrefabPath,
            CreateOrUpdateMaterial(SphereMaterialPath, new Color(0.05f, 0.58f, 0.90f)));

        CreateARScene(library, cubePrefab, spherePrefab);
        ConfigureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void EnsureFolders()
    {
        EnsureFolder("Assets/Blancos");
        EnsureFolder("Assets/Materials");
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
        if (string.IsNullOrEmpty(parent))
            return;

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    static void ConfigureTexture(string path)
    {
        if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            throw new FileNotFoundException($"No se encontró el blanco requerido: {path}");

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

        AddReferenceImage(library, "Assets/Blancos/ar1.jpg", "cubo", CubePrintedWidthMeters);
        AddReferenceImage(library, "Assets/Blancos/ar2.jpg", "esfera", SpherePrintedWidthMeters);
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssetIfDirty(library);
        return library;
    }

    static void AddReferenceImage(
        XRReferenceImageLibrary library,
        string texturePath,
        string imageName,
        float printedWidthMeters)
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
            throw new FileNotFoundException($"No se pudo cargar {texturePath} como Texture2D.");

        var printedHeightMeters = printedWidthMeters * texture.height / texture.width;
        library.Add();
        var index = library.count - 1;
        library.SetName(index, imageName);
        library.SetTexture(index, texture, keepTexture: false);
        library.SetSpecifySize(index, true);
        library.SetSize(index, new Vector2(printedWidthMeters, printedHeightMeters));
    }

    static Material CreateOrUpdateMaterial(string path, Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            throw new InvalidOperationException("No se encontró el shader Universal Render Pipeline/Lit.");

        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", 0.05f);
        material.SetFloat("_Smoothness", 0.38f);
        EditorUtility.SetDirty(material);
        return material;
    }

    static GameObject CreateOrUpdateFigurePrefab(
        string rootName,
        PrimitiveType primitiveType,
        string prefabPath,
        Material material)
    {
        var root = new GameObject(rootName);
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one;

        var figure = GameObject.CreatePrimitive(primitiveType);
        figure.name = primitiveType == PrimitiveType.Cube ? "Cubo" : "Esfera";
        figure.transform.SetParent(root.transform, false);
        figure.transform.localPosition = new Vector3(0f, FigureSizeMeters * 0.5f, 0f);
        figure.transform.localRotation = Quaternion.identity;
        figure.transform.localScale = Vector3.one * FigureSizeMeters;
        figure.GetComponent<Renderer>().sharedMaterial = material;

        var collider = figure.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static void CreateARScene(
        XRReferenceImageLibrary library,
        GameObject cubePrefab,
        GameObject spherePrefab)
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
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 20f;
        cameraObject.AddComponent<AudioListener>();

        var cameraManager = cameraObject.AddComponent<ARCameraManager>();
        cameraManager.autoFocusRequested = true;
        cameraManager.requestedFacingDirection = CameraFacingDirection.World;
        cameraManager.requestedLightEstimation =
            LightEstimation.AmbientIntensity | LightEstimation.AmbientColor;
        cameraObject.AddComponent<ARCameraBackground>();

        var poseDriver = cameraObject.AddComponent<TrackedPoseDriver>();
        var positionAction = new InputAction(
            "Position",
            binding: "<XRHMD>/centerEyePosition",
            expectedControlType: "Vector3");
        positionAction.AddBinding("<HandheldARInputDevice>/devicePosition");
        var rotationAction = new InputAction(
            "Rotation",
            binding: "<XRHMD>/centerEyeRotation",
            expectedControlType: "Quaternion");
        rotationAction.AddBinding("<HandheldARInputDevice>/deviceRotation");
        poseDriver.positionInput = new InputActionProperty(positionAction);
        poseDriver.rotationInput = new InputActionProperty(rotationAction);

        origin.CameraFloorOffsetObject = cameraOffset;
        origin.Camera = camera;

        var imageManager = originObject.AddComponent<ARTrackedImageManager>();
        imageManager.referenceLibrary = library;
        imageManager.requestedMaxNumberOfMovingImages = 2;
        imageManager.trackedImagePrefab = null;

        var distributor = originObject.AddComponent<PrefabPerImage>();
        distributor.Configure(imageManager, cubePrefab, spherePrefab);

        var lightObject = new GameObject("Directional Light");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.96f, 0.90f);
        light.intensity = 1.15f;
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    static void ConfigureARBackgroundRendererFeatures()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:ScriptableRendererData"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
            if (rendererData == null
                || rendererData.rendererFeatures.Any(feature => feature is ARBackgroundRendererFeature))
            {
                continue;
            }

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

    static void ConfigureARKitLoader()
    {
        EnsureIOSDefine(ARKitLoaderDefine);

        var settingsPerTarget = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>)
            .FirstOrDefault(asset => asset != null);

        if (settingsPerTarget == null)
        {
            settingsPerTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(
                settingsPerTarget,
                "Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset");
            EditorBuildSettings.AddConfigObject(
                XRGeneralSettings.k_SettingsKey,
                settingsPerTarget,
                true);
        }

        if (!settingsPerTarget.HasSettingsForBuildTarget(BuildTargetGroup.iOS))
            settingsPerTarget.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.iOS);

        if (!settingsPerTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.iOS))
            settingsPerTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.iOS);

        var managerSettings = settingsPerTarget.ManagerSettingsForBuildTarget(BuildTargetGroup.iOS);
        if (!XRPackageMetadataStore.AssignLoader(
                managerSettings,
                typeof(ARKitLoader).FullName,
                BuildTargetGroup.iOS))
        {
            throw new InvalidOperationException("No se pudo asignar ARKitLoader a iOS.");
        }

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

    static void EnsureIOSDefine(string define)
    {
        var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.iOS)
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.Ordinal);

        if (!defines.Add(define))
            return;

        PlayerSettings.SetScriptingDefineSymbols(
            NamedBuildTarget.iOS,
            string.Join(";", defines.OrderBy(value => value, StringComparer.Ordinal)));
    }

    static void ConfigureIOSPlayer()
    {
        PlayerSettings.companyName = "UP";
        PlayerSettings.productName = "Labo 2 AR";
        PlayerSettings.bundleVersion = "2.0.0";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleIdentifier);
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
        PlayerSettings.iOS.buildNumber = "1";
        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
        PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPadOnly;
        PlayerSettings.iOS.targetOSVersionString = "15.0";
        PlayerSettings.iOS.cameraUsageDescription =
            "La cámara se utiliza para detectar dos imágenes y mostrar contenido de realidad aumentada.";
        PlayerSettings.iOS.appleDeveloperTeamID = DeveloperTeam;
        PlayerSettings.iOS.appleEnableAutomaticSigning = true;
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

    static void ValidateProject()
    {
        var library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(LibraryPath);
        Require(library != null, "Falta BibliotecaBlancos.");
        Require(library.count == 2, "BibliotecaBlancos debe contener exactamente dos imágenes.");
        Require(library[0].name == "cubo", "La primera imagen debe llamarse 'cubo'.");
        Require(library[1].name == "esfera", "La segunda imagen debe llamarse 'esfera'.");
        Require(library[0].specifySize && library[1].specifySize,
            "Ambas imágenes deben tener Specify Size activo.");

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Require(scene.IsValid(), "No se pudo abrir Labo2AR.unity.");

        var imageManager = Object.FindFirstObjectByType<ARTrackedImageManager>();
        Require(imageManager != null, "Falta ARTrackedImageManager en XR Origin.");
        Require(imageManager.referenceLibrary == library,
            "ARTrackedImageManager no usa BibliotecaBlancos.");
        Require(imageManager.requestedMaxNumberOfMovingImages == 2,
            "Max Number Of Moving Images debe ser 2.");
        Require(imageManager.trackedImagePrefab == null,
            "Tracked Image Prefab debe permanecer vacío.");

        var distributor = Object.FindFirstObjectByType<PrefabPerImage>();
        Require(distributor != null, "Falta PrefabPerImage en XR Origin.");
        Require(distributor.Images == imageManager,
            "PrefabPerImage no tiene asignado el manager.");
        Require(AssetDatabase.GetAssetPath(distributor.CubePrefab) == CubePrefabPath,
            "PrefabPerImage no tiene asignado ContenidoCubo.");
        Require(AssetDatabase.GetAssetPath(distributor.SpherePrefab) == SpherePrefabPath,
            "PrefabPerImage no tiene asignado ContenidoEsfera.");
        Require(Object.FindObjectsByType<PrefabPerImage>(FindObjectsSortMode.None).Length == 1,
            "Debe existir un solo PrefabPerImage.");

        ValidateFigurePrefab(CubePrefabPath, "Cubo");
        ValidateFigurePrefab(SpherePrefabPath, "Esfera");
        Require(PlayerSettings.iOS.targetDevice == iOSTargetDevice.iPadOnly,
            "El destino iOS debe estar limitado a iPad.");
        Require(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS) == BundleIdentifier,
            "El bundle identifier de iOS no es el esperado.");
        Require(PlayerSettings.GetScriptingBackend(NamedBuildTarget.iOS) == ScriptingImplementation.IL2CPP,
            "iOS debe usar IL2CPP.");
        Require(PlayerSettings.GetGraphicsAPIs(BuildTarget.iOS).SequenceEqual(
                new[] { GraphicsDeviceType.Metal }),
            "iOS debe usar Metal como única API gráfica.");
        Require(PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.iOS)
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Contains(ARKitLoaderDefine, StringComparer.Ordinal),
            $"iOS debe incluir el símbolo {ARKitLoaderDefine} para enlazar el plugin nativo.");

        Debug.Log("Labo 2: validación completa superada (2 imágenes, 2 prefabs, iPad-only).");
    }

    static void ValidateFigurePrefab(string path, string childName)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Require(prefab != null, $"Falta el prefab {path}.");
        Require(prefab.transform.localPosition == Vector3.zero,
            $"La raíz de {prefab.name} debe estar en posición cero.");
        Require(prefab.transform.localRotation == Quaternion.identity,
            $"La raíz de {prefab.name} debe tener rotación cero.");
        Require(prefab.transform.localScale == Vector3.one,
            $"La raíz de {prefab.name} debe tener escala uno.");

        var child = prefab.transform.Find(childName);
        Require(child != null, $"{prefab.name} no contiene {childName}.");
        Require(Vector3.Distance(child.localPosition, new Vector3(0f, 0.025f, 0f)) < 0.0001f,
            $"{childName} no descansa sobre el plano del blanco.");
        Require(Vector3.Distance(child.localScale, Vector3.one * FigureSizeMeters) < 0.0001f,
            $"{childName} debe medir 5 cm.");
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException($"Validación de Labo 2: {message}");
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
        {
            throw new InvalidOperationException(
                $"La exportación iOS terminó con estado {report.summary.result} " +
                $"y {report.summary.totalErrors} errores.");
        }

        ValidateNativeARKitExport();
        Debug.Log($"Labo 2: proyecto Xcode para iPad listo en {Path.GetFullPath(IOSBuildPath)}.");
    }

    static void ValidateNativeARKitExport()
    {
        var archivePath = Directory.GetFiles(
                IOSBuildPath,
                "libUnityARKit.a",
                SearchOption.AllDirectories)
            .FirstOrDefault();
        var bootstrapPath = Directory.GetFiles(
                IOSBuildPath,
                "UnityARKit.m",
                SearchOption.AllDirectories)
            .FirstOrDefault();

        Require(!string.IsNullOrEmpty(archivePath),
            "La exportación Xcode omitió libUnityARKit.a; ARKit no funcionaría en el iPad.");
        Require(!string.IsNullOrEmpty(bootstrapPath),
            "La exportación Xcode omitió UnityARKit.m; los subsistemas AR no podrían registrarse.");

        var projectPath = Path.Combine(IOSBuildPath, "Unity-iPhone.xcodeproj", "project.pbxproj");
        Require(File.Exists(projectPath), "La exportación no contiene el proyecto Xcode esperado.");

        var projectText = File.ReadAllText(projectPath);
        Require(projectText.Contains("libUnityARKit.a", StringComparison.Ordinal)
                && projectText.Contains("UnityARKit.m in Sources", StringComparison.Ordinal),
            "El proyecto Xcode no enlazó completamente el plugin nativo de ARKit.");
    }
}

#if UNITY_IOS
public sealed class Labo2IOSPostprocessor : IPostprocessBuildWithReport
{
    const string DeveloperTeam = "8MBP94XP38";
    const string BundleIdentifier = "com.up.vr.labo2";

    public int callbackOrder => 1000;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.iOS)
            return;

        ConfigureInfoPlist(report.summary.outputPath);
        ConfigureXcodeProject(report.summary.outputPath);
    }

    static void ConfigureInfoPlist(string outputPath)
    {
        var plistPath = Path.Combine(outputPath, "Info.plist");
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

    static void ConfigureXcodeProject(string outputPath)
    {
        var projectPath = PBXProject.GetPBXProjectPath(outputPath);
        var project = new PBXProject();
        project.ReadFromFile(projectPath);

        var mainTarget = project.GetUnityMainTargetGuid();
        var frameworkTarget = project.GetUnityFrameworkTargetGuid();
        project.SetBuildProperty(mainTarget, "PRODUCT_BUNDLE_IDENTIFIER", BundleIdentifier);
        project.SetBuildProperty(mainTarget, "TARGETED_DEVICE_FAMILY", "2");
        project.SetBuildProperty(mainTarget, "IPHONEOS_DEPLOYMENT_TARGET", "15.0");
        project.SetBuildProperty(mainTarget, "DEVELOPMENT_TEAM", DeveloperTeam);
        project.SetBuildProperty(mainTarget, "CODE_SIGN_STYLE", "Automatic");
        project.SetBuildProperty(mainTarget, "PROVISIONING_PROFILE", string.Empty);
        project.SetBuildProperty(mainTarget, "PROVISIONING_PROFILE_SPECIFIER", string.Empty);

        project.SetBuildProperty(frameworkTarget, "TARGETED_DEVICE_FAMILY", "2");
        project.SetBuildProperty(frameworkTarget, "IPHONEOS_DEPLOYMENT_TARGET", "15.0");
        project.SetBuildProperty(frameworkTarget, "DEVELOPMENT_TEAM", DeveloperTeam);
        project.SetBuildProperty(frameworkTarget, "CODE_SIGN_STYLE", "Automatic");
        project.SetBuildProperty(frameworkTarget, "PROVISIONING_PROFILE", string.Empty);
        project.SetBuildProperty(frameworkTarget, "PROVISIONING_PROFILE_SPECIFIER", string.Empty);
        project.WriteToFile(projectPath);
    }

    static void EnsureCapability(PlistElementArray capabilities, string value)
    {
        if (!capabilities.values.Any(element =>
                value.Equals(element.AsString(), StringComparison.Ordinal)))
        {
            capabilities.AddString(value);
        }
    }
}
#endif

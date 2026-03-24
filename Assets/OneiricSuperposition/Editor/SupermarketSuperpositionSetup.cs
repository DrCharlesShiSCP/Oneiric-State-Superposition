
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Oneiric.Superposition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Oneiric.Superposition.Editor
{
    public static class SupermarketSuperpositionSetup
    {
        private const string ScenePath = "Assets/Scenes/grocery_02.unity";
        private const string RootFolder = "Assets/OneiricSuperposition";
        private const string MaterialFolder = RootFolder + "/Materials";
        private const string ProfileFolder = RootFolder + "/Profiles";
        private const string GeneratedMaterialFolder = MaterialFolder + "/Generated";

        [MenuItem("Tools/Oneiric/Inspect Grocery Scene")]
        public static void InspectGroceryScene()
        {
            EnsureFolders();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject player = FindSceneObject("Player");
            GameObject camera = FindSceneObject("MainCamera", "Main Camera");
            Debug.Log($"[Oneiric] Scene '{scene.name}' opened.");
            Debug.Log($"[Oneiric] Player: {(player != null ? GetPath(player.transform) : "missing")}");
            Debug.Log($"[Oneiric] Camera: {(camera != null ? GetPath(camera.transform) : "missing")}");
            Debug.Log($"[Oneiric] Shelf candidate: {DescribeCandidate("grocery_rack")}");
            Debug.Log($"[Oneiric] Cart candidate: {DescribeCandidate("trolley", "basket", "car_backet")}");
            Debug.Log($"[Oneiric] Checkout candidate: {DescribeCandidate("cashier", "autocachier")}");
            Debug.Log($"[Oneiric] Light candidate: {DescribeCandidate("grocery_lamp", "lamp")}");
            Debug.Log($"[Oneiric] Freezer candidate: {DescribeCandidate("big_fridge", "fridge")}");
        }

        public static void RunSceneSetup()
        {
            EnsureFolders();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SetupBuildSettings();

            Transform root = GetOrCreateRoot("Oneiric Superposition");
            Transform overlayRoot = GetOrCreateChild(root, "Dream Overlay");
            Transform exampleRoot = GetOrCreateChild(root, "Example Targets");
            Transform particleRoot = GetOrCreateChild(root, "Dream Particles");
            Transform zoneRoot = GetOrCreateChild(root, "Dream Zones");
            Transform systemRoot = GetOrCreateChild(root, "Systems");

            GameObject player = FindSceneObject("Player");
            if (player == null)
            {
                throw new InvalidOperationException("Player object was not found in grocery_02.");
            }

            Camera playerCamera = FindPlayerCamera(player);
            ConfigurePlayer(player, playerCamera);
            Volume globalVolume = ConfigureGlobalVolume(systemRoot);
            DreamAudioController audioController = ConfigureAudio(systemRoot);
            ParticleSystem[] particles = ConfigureGlobalParticles(particleRoot);
            ConfigureManager(systemRoot, player, playerCamera, globalVolume, audioController, particles);

            GameObject shelf = FindSceneObject("grocery_rack");
            if (shelf != null)
            {
                GameObject shelfDream = CreateDreamDuplicate(shelf, overlayRoot, "ShelfDream");
                CreateFloatingChildren(shelfDream.transform, 3, new Vector3(0.3f, 0.4f, 0.3f));
                CreateSuperpositionTarget(exampleRoot, "Shelf Superposition", shelf, shelfDream, new SuperpositionPreset
                {
                    BaseBlend = 0.42f,
                    DreamOpacityMin = 0.24f,
                    DreamOpacityMax = 0.95f,
                    PositionDrift = new Vector3(0.05f, 0.08f, 0.04f),
                    ScalePulse = new Vector3(0.025f, 0.035f, 0.025f),
                    RotationDrift = new Vector3(0.8f, 4f, 0.8f),
                    RestingBlend = 0.4f,
                    ObservedBlend = 0.78f,
                    ObservedDistortion = 0.7f,
                    Response = ObservationCollapseTarget.CollapseResponse.TowardDream
                });
            }

            GameObject cart = FindSceneObject("trolley", "basket", "car_backet");
            if (cart == null)
            {
                cart = InstantiateAssetAt(
                    "Assets/Grocery store - interior and props/Models/Trolley.fbx",
                    player.transform.position + player.transform.forward * 2.4f + player.transform.right * 1.2f,
                    Quaternion.Euler(0f, 25f, 0f),
                    null,
                    "Trolley_Waking");
            }

            if (cart != null)
            {
                GameObject cartDream = CreateDreamDuplicate(cart, overlayRoot, "CartDream");
                cartDream.transform.position += new Vector3(0.18f, 0.2f, -0.12f);
                cartDream.transform.localScale += new Vector3(0.15f, 0.35f, 0.1f);
                CreateFloatingChildren(cartDream.transform, 2, new Vector3(0.25f, 0.25f, 0.25f));
                CreateSuperpositionTarget(exampleRoot, "Cart Superposition", cart, cartDream, new SuperpositionPreset
                {
                    BaseBlend = 0.32f,
                    DreamOpacityMin = 0.2f,
                    DreamOpacityMax = 0.92f,
                    PositionDrift = new Vector3(0.04f, 0.14f, 0.08f),
                    ScalePulse = new Vector3(0.03f, 0.06f, 0.03f),
                    RotationDrift = new Vector3(2f, 8f, 2f),
                    RestingBlend = 0.28f,
                    ObservedBlend = 0.86f,
                    ObservedDistortion = 0.9f,
                    Response = ObservationCollapseTarget.CollapseResponse.TowardDream
                });
            }
            GameObject checkout = FindSceneObject("autocachier", "cashier");
            if (checkout != null)
            {
                GameObject checkoutDream = CreateDreamDuplicate(checkout, overlayRoot, "CheckoutDream");
                checkoutDream.transform.position += new Vector3(-0.06f, 0.12f, 0.09f);
                CreateDreamLight(checkoutDream.transform, "Checkout Glow", new Vector3(0f, 1.4f, 0f), 3.2f, 5.5f);
                CreateSuperpositionTarget(exampleRoot, "Checkout Superposition", checkout, checkoutDream, new SuperpositionPreset
                {
                    BaseBlend = 0.48f,
                    DreamOpacityMin = 0.3f,
                    DreamOpacityMax = 0.98f,
                    PositionDrift = new Vector3(0.02f, 0.06f, 0.03f),
                    ScalePulse = new Vector3(0.015f, 0.02f, 0.015f),
                    RotationDrift = new Vector3(0.5f, 2f, 0.4f),
                    RestingBlend = 0.5f,
                    ObservedBlend = 0.15f,
                    ObservedDistortion = 0.65f,
                    Response = ObservationCollapseTarget.CollapseResponse.TowardWaking
                });
            }

            GameObject lightFixture = FindSceneObject("grocery_lamp", "lamp");
            if (lightFixture != null)
            {
                GameObject lightDream = CreateDreamDuplicate(lightFixture, overlayRoot, "AisleLightDream");
                lightDream.transform.position += new Vector3(0f, -0.1f, 0f);
                CreateDreamLight(lightDream.transform, "Dream Point Light", Vector3.zero, 2.6f, 7f);

                SuperpositionTargetBundle bundle = CreateSuperpositionTarget(exampleRoot, "Aisle Light Superposition", lightFixture, lightDream, new SuperpositionPreset
                {
                    BaseBlend = 0.46f,
                    DreamOpacityMin = 0.18f,
                    DreamOpacityMax = 0.9f,
                    PositionDrift = new Vector3(0f, 0.03f, 0f),
                    ScalePulse = new Vector3(0.02f, 0.02f, 0.02f),
                    RotationDrift = new Vector3(0.2f, 1.5f, 0.1f),
                    RestingBlend = 0.44f,
                    ObservedBlend = 0.72f,
                    ObservedDistortion = 0.75f,
                    Response = ObservationCollapseTarget.CollapseResponse.IncreaseDistortion
                });

                CreateZone(zoneRoot, "Aisle Light Zone", bundle.AnchorPosition, 5.5f);
            }

            GameObject freezer = FindSceneObject("big_fridge", "fridge");
            if (freezer != null)
            {
                GameObject freezerDream = CreateDreamDuplicate(freezer, overlayRoot, "FreezerDream");
                freezerDream.transform.position += new Vector3(0.12f, 0.06f, -0.08f);
                CreateDreamLight(freezerDream.transform, "Freezer Halo", new Vector3(0f, 1.1f, 0f), 2.2f, 6.5f);

                SuperpositionTargetBundle bundle = CreateSuperpositionTarget(exampleRoot, "Freezer Superposition", freezer, freezerDream, new SuperpositionPreset
                {
                    BaseBlend = 0.55f,
                    DreamOpacityMin = 0.28f,
                    DreamOpacityMax = 1f,
                    PositionDrift = new Vector3(0.03f, 0.05f, 0.06f),
                    ScalePulse = new Vector3(0.015f, 0.02f, 0.015f),
                    RotationDrift = new Vector3(0.4f, 1.8f, 0.3f),
                    RestingBlend = 0.52f,
                    ObservedBlend = 0.9f,
                    ObservedDistortion = 0.95f,
                    Response = ObservationCollapseTarget.CollapseResponse.TowardDream
                });

                CreateLocalParticles(bundle.AnchorPosition + new Vector3(0f, 1.3f, 0f), particleRoot, "Freezer Motes", new Vector3(1.6f, 1.8f, 1.2f), 18f);
                CreateZone(zoneRoot, "Freezer Dream Zone", bundle.AnchorPosition, 6.5f);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Oneiric] Grocery superposition scene setup complete.");
        }

        private static void EnsureFolders()
        {
            CreateFolderIfMissing("Assets", "OneiricSuperposition");
            CreateFolderIfMissing(RootFolder, "Runtime");
            CreateFolderIfMissing(RootFolder, "Editor");
            CreateFolderIfMissing(RootFolder, "Materials");
            CreateFolderIfMissing(MaterialFolder, "Generated");
            CreateFolderIfMissing(RootFolder, "Profiles");
        }

        private static void SetupBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(scene => scene.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void ConfigurePlayer(GameObject player, Camera playerCamera)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = Undo.AddComponent<CharacterController>(player);
            }

            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.3f;
            controller.minMoveDistance = 0f;
            controller.skinWidth = 0.03f;
            controller.slopeLimit = 45f;

            CapsuleCollider capsuleCollider = player.GetComponent<CapsuleCollider>();
            if (capsuleCollider != null)
            {
                capsuleCollider.enabled = false;
            }

            MeshRenderer meshRenderer = player.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }

            SimplePlayerWalker walker = player.GetComponent<SimplePlayerWalker>();
            if (walker == null)
            {
                walker = Undo.AddComponent<SimplePlayerWalker>(player);
            }

            SerializedObject walkerSerialized = new SerializedObject(walker);
            walkerSerialized.FindProperty("characterController").objectReferenceValue = controller;
            walkerSerialized.FindProperty("cameraPivot").objectReferenceValue = playerCamera != null ? playerCamera.transform : null;
            walkerSerialized.ApplyModifiedPropertiesWithoutUndo();

            if (playerCamera != null)
            {
                playerCamera.tag = "MainCamera";
                UniversalAdditionalCameraData additionalCameraData = playerCamera.GetComponent<UniversalAdditionalCameraData>();
                if (additionalCameraData != null)
                {
                    additionalCameraData.renderPostProcessing = true;
                }
            }
        }

        private static void ConfigureManager(Transform systemRoot, GameObject player, Camera playerCamera, Volume globalVolume, DreamAudioController audioController, ParticleSystem[] particles)
        {
            GameObject managerObject = GetOrCreateChild(systemRoot, "Dream State Manager").gameObject;
            DreamStateManager manager = managerObject.GetComponent<DreamStateManager>();
            if (manager == null)
            {
                manager = Undo.AddComponent<DreamStateManager>(managerObject);
            }

            SerializedObject serialized = new SerializedObject(manager);
            serialized.FindProperty("playerTransform").objectReferenceValue = player.transform;
            serialized.FindProperty("playerCamera").objectReferenceValue = playerCamera;
            serialized.FindProperty("globalVolume").objectReferenceValue = globalVolume;
            serialized.FindProperty("audioController").objectReferenceValue = audioController;
            SerializedProperty particleArray = serialized.FindProperty("dreamParticles");
            particleArray.arraySize = particles.Length;
            for (int index = 0; index < particles.Length; index++)
            {
                particleArray.GetArrayElementAtIndex(index).objectReferenceValue = particles[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static Volume ConfigureGlobalVolume(Transform systemRoot)
        {
            GameObject volumeObject = GetOrCreateChild(systemRoot, "Global Dream Volume").gameObject;
            Volume volume = volumeObject.GetComponent<Volume>();
            if (volume == null)
            {
                volume = Undo.AddComponent<Volume>(volumeObject);
            }

            volume.isGlobal = true;
            volume.priority = 15f;
            volume.sharedProfile = GetOrCreateProfile(ProfileFolder + "/OneiricDreamGlobalProfile.asset", true);
            volume.weight = 0.35f;
            return volume;
        }

        private static DreamAudioController ConfigureAudio(Transform systemRoot)
        {
            GameObject audioRoot = GetOrCreateChild(systemRoot, "Dream Audio").gameObject;
            AudioSource waking = GetOrCreateAudioSource(audioRoot.transform, "Waking Ambience");
            AudioSource dream = GetOrCreateAudioSource(audioRoot.transform, "Dream Ambience");

            waking.loop = true;
            waking.playOnAwake = true;
            waking.spatialBlend = 0f;
            dream.loop = true;
            dream.playOnAwake = true;
            dream.spatialBlend = 0f;

            DreamAudioController controller = audioRoot.GetComponent<DreamAudioController>();
            if (controller == null)
            {
                controller = Undo.AddComponent<DreamAudioController>(audioRoot);
            }

            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("wakingSource").objectReferenceValue = waking;
            serialized.FindProperty("dreamSource").objectReferenceValue = dream;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return controller;
        }

        private static ParticleSystem[] ConfigureGlobalParticles(Transform particleRoot)
        {
            ParticleSystem dust = CreateLocalParticles(new Vector3(-2f, 2.2f, 0.5f), particleRoot, "Dream Dust", new Vector3(8f, 2.5f, 8f), 12f);
            ParticleSystem motes = CreateLocalParticles(new Vector3(4.5f, 2f, -6f), particleRoot, "Floating Motes", new Vector3(5f, 2.2f, 5f), 10f);
            return new[] { dust, motes };
        }

        private static ParticleSystem CreateLocalParticles(Vector3 position, Transform parent, string name, Vector3 boxSize, float rate)
        {
            Transform existing = parent.Find(name);
            GameObject particleObject;
            if (existing != null)
            {
                particleObject = existing.gameObject;
            }
            else
            {
                particleObject = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(particleObject, "Create dream particles");
            }

            particleObject.transform.SetParent(parent, false);
            particleObject.transform.position = position;

            ParticleSystem particleSystem = particleObject.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                particleSystem = Undo.AddComponent<ParticleSystem>(particleObject);
            }

            ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GetOrCreateParticleMaterial();

            var main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.08f);
            main.maxParticles = 200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.65f, 0.85f, 1f, 0.22f));

            var emission = particleSystem.emission;
            emission.rateOverTime = rate;

            var shape = particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = boxSize;

            var noise = particleSystem.noise;
            noise.enabled = true;
            noise.strength = 0.2f;
            noise.frequency = 0.15f;

            var velocity = particleSystem.velocityOverLifetime;
            velocity.enabled = true;
            velocity.y = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);

            return particleSystem;
        }

        private static DreamZone CreateZone(Transform zoneRoot, string name, Vector3 position, float radius)
        {
            GameObject zoneObject = GetOrCreateChild(zoneRoot, name).gameObject;
            zoneObject.transform.position = position;

            DreamZone zone = zoneObject.GetComponent<DreamZone>();
            if (zone == null)
            {
                zone = Undo.AddComponent<DreamZone>(zoneObject);
            }

            Volume volume = zoneObject.GetComponent<Volume>();
            if (volume == null)
            {
                volume = Undo.AddComponent<Volume>(zoneObject);
            }

            volume.isGlobal = false;
            volume.blendDistance = radius * 0.8f;
            volume.priority = 18f;
            volume.sharedProfile = GetOrCreateProfile(ProfileFolder + "/OneiricDreamZoneProfile.asset", false);
            volume.weight = 0f;

            SphereCollider collider = zoneObject.GetComponent<SphereCollider>();
            if (collider == null)
            {
                collider = Undo.AddComponent<SphereCollider>(zoneObject);
            }

            collider.isTrigger = true;
            collider.radius = radius;

            SerializedObject serialized = new SerializedObject(zone);
            serialized.FindProperty("radius").floatValue = radius;
            serialized.FindProperty("extraDreamIntensity").floatValue = 0.25f;
            serialized.FindProperty("localVolume").objectReferenceValue = volume;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return zone;
        }

        private static SuperpositionTargetBundle CreateSuperpositionTarget(Transform parent, string name, GameObject waking, GameObject dream, SuperpositionPreset preset)
        {
            GameObject targetObject = GetOrCreateChild(parent, name).gameObject;
            targetObject.transform.position = waking.transform.position;
            targetObject.transform.rotation = waking.transform.rotation;

            SuperpositionObject superposition = targetObject.GetComponent<SuperpositionObject>();
            if (superposition == null)
            {
                superposition = Undo.AddComponent<SuperpositionObject>(targetObject);
            }

            SerializedObject superpositionSerialized = new SerializedObject(superposition);
            superpositionSerialized.FindProperty("wakingObject").objectReferenceValue = waking;
            superpositionSerialized.FindProperty("dreamObject").objectReferenceValue = dream;
            superpositionSerialized.FindProperty("baseBlend").floatValue = preset.BaseBlend;
            superpositionSerialized.FindProperty("dreamMinimumOpacity").floatValue = preset.DreamOpacityMin;
            superpositionSerialized.FindProperty("dreamMaximumOpacity").floatValue = preset.DreamOpacityMax;
            superpositionSerialized.FindProperty("positionDriftAmplitude").vector3Value = preset.PositionDrift;
            superpositionSerialized.FindProperty("scalePulseAmplitude").vector3Value = preset.ScalePulse;
            superpositionSerialized.FindProperty("rotationDriftAmplitude").vector3Value = preset.RotationDrift;
            superpositionSerialized.ApplyModifiedPropertiesWithoutUndo();

            ObservationCollapseTarget collapseTarget = targetObject.GetComponent<ObservationCollapseTarget>();
            if (collapseTarget == null)
            {
                collapseTarget = Undo.AddComponent<ObservationCollapseTarget>(targetObject);
            }

            SerializedObject collapseSerialized = new SerializedObject(collapseTarget);
            collapseSerialized.FindProperty("superpositionObject").objectReferenceValue = superposition;
            collapseSerialized.FindProperty("collapseResponse").enumValueIndex = (int)preset.Response;
            collapseSerialized.FindProperty("restingBlend").floatValue = preset.RestingBlend;
            collapseSerialized.FindProperty("observedBlend").floatValue = preset.ObservedBlend;
            collapseSerialized.FindProperty("restingDistortion").floatValue = Mathf.Clamp01(preset.BaseBlend);
            collapseSerialized.FindProperty("observedDistortion").floatValue = preset.ObservedDistortion;
            collapseSerialized.ApplyModifiedPropertiesWithoutUndo();

            superposition.ResetObservedState();
            return new SuperpositionTargetBundle { AnchorPosition = CalculateBoundsCenter(waking, dream) };
        }

        private static GameObject CreateDreamDuplicate(GameObject source, Transform overlayRoot, string label)
        {
            string objectName = source.name + "_" + label;
            Transform existing = overlayRoot.Find(objectName);
            GameObject duplicate;
            if (existing != null)
            {
                duplicate = existing.gameObject;
            }
            else
            {
                duplicate = UnityEngine.Object.Instantiate(source, overlayRoot);
                duplicate.name = objectName;
                Undo.RegisterCreatedObjectUndo(duplicate, "Create dream duplicate");
            }

            duplicate.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            duplicate.transform.localScale = source.transform.lossyScale;

            foreach (Collider collider in duplicate.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            foreach (Rigidbody rigidbody in duplicate.GetComponentsInChildren<Rigidbody>(true))
            {
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
            }

            foreach (Renderer renderer in duplicate.GetComponentsInChildren<Renderer>(true))
            {
                Material[] dreamMaterials = renderer.sharedMaterials.Select(CreateOrGetDreamMaterial).ToArray();
                renderer.sharedMaterials = dreamMaterials;
            }

            GameObjectUtility.SetStaticEditorFlags(duplicate, 0);
            return duplicate;
        }
        private static void CreateFloatingChildren(Transform root, int count, Vector3 spread)
        {
            List<Transform> leafRenderers = root.GetComponentsInChildren<Renderer>(true)
                .Select(renderer => renderer.transform)
                .Where(transform => transform.childCount == 0)
                .Take(count)
                .ToList();

            foreach (Transform child in leafRenderers)
            {
                child.localPosition += new Vector3(
                    UnityEngine.Random.Range(-spread.x, spread.x),
                    UnityEngine.Random.Range(0.12f, spread.y),
                    UnityEngine.Random.Range(-spread.z, spread.z));

                if (child.GetComponent<DreamFloatMotion>() == null)
                {
                    Undo.AddComponent<DreamFloatMotion>(child.gameObject);
                }
            }
        }

        private static Light CreateDreamLight(Transform parent, string name, Vector3 localPosition, float intensity, float range)
        {
            Transform existing = parent.Find(name);
            GameObject lightObject;
            if (existing != null)
            {
                lightObject = existing.gameObject;
            }
            else
            {
                lightObject = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(lightObject, "Create dream light");
            }

            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = localPosition;

            Light light = lightObject.GetComponent<Light>();
            if (light == null)
            {
                light = Undo.AddComponent<Light>(lightObject);
            }

            light.type = LightType.Point;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            light.color = new Color(0.65f, 0.9f, 1f);

            DreamLightFlicker flicker = lightObject.GetComponent<DreamLightFlicker>();
            if (flicker == null)
            {
                flicker = Undo.AddComponent<DreamLightFlicker>(lightObject);
            }

            SerializedObject flickerSerialized = new SerializedObject(flicker);
            flickerSerialized.FindProperty("targetLight").objectReferenceValue = light;
            flickerSerialized.FindProperty("baseIntensity").floatValue = intensity;
            flickerSerialized.ApplyModifiedPropertiesWithoutUndo();
            return light;
        }

        private static VolumeProfile GetOrCreateProfile(string assetPath, bool global)
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(assetPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, assetPath);
            }

            Bloom bloom = profile.TryGet(out Bloom existingBloom) ? existingBloom : profile.Add<Bloom>(true);
            Vignette vignette = profile.TryGet(out Vignette existingVignette) ? existingVignette : profile.Add<Vignette>(true);
            ChromaticAberration chromatic = profile.TryGet(out ChromaticAberration existingChromatic) ? existingChromatic : profile.Add<ChromaticAberration>(true);
            ColorAdjustments color = profile.TryGet(out ColorAdjustments existingColor) ? existingColor : profile.Add<ColorAdjustments>(true);

            bloom.active = true;
            bloom.intensity.overrideState = true;
            bloom.threshold.overrideState = true;
            bloom.scatter.overrideState = true;
            bloom.intensity.value = global ? 0.15f : 0.75f;
            bloom.threshold.value = global ? 1f : 0.8f;
            bloom.scatter.value = 0.72f;

            vignette.active = true;
            vignette.intensity.overrideState = true;
            vignette.smoothness.overrideState = true;
            vignette.color.overrideState = true;
            vignette.intensity.value = global ? 0.12f : 0.28f;
            vignette.smoothness.value = 0.75f;
            vignette.color.value = new Color(0.05f, 0.1f, 0.14f);

            chromatic.active = true;
            chromatic.intensity.overrideState = true;
            chromatic.intensity.value = global ? 0.03f : 0.12f;

            color.active = true;
            color.contrast.overrideState = true;
            color.saturation.overrideState = true;
            color.colorFilter.overrideState = true;
            color.contrast.value = global ? 6f : 12f;
            color.saturation.value = global ? -8f : -18f;
            color.colorFilter.value = global ? new Color(0.88f, 0.94f, 1f) : new Color(0.74f, 0.9f, 1f);

            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static Material GetOrCreateParticleMaterial()
        {
            string assetPath = MaterialFolder + "/DreamParticle.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                material = new Material(shader);
                material.SetColor("_BaseColor", new Color(0.7f, 0.9f, 1f, 0.4f));
                AssetDatabase.CreateAsset(material, assetPath);
            }

            return material;
        }

        private static Material CreateOrGetDreamMaterial(Material sourceMaterial)
        {
            if (sourceMaterial == null)
            {
                return null;
            }

            string assetPath = $"{GeneratedMaterialFolder}/Dream_{SanitizeFileName(sourceMaterial.name)}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            Material material = new Material(sourceMaterial);
            if (material.shader == null || material.shader.name.Contains("Standard"))
            {
                Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                if (urpShader != null)
                {
                    material.shader = urpShader;
                }
            }

            Color baseColor = material.HasProperty("_BaseColor")
                ? material.GetColor("_BaseColor")
                : material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
            Color tintedColor = Color.Lerp(baseColor, new Color(0.5f, 0.92f, 1f, baseColor.a), 0.45f);
            tintedColor.a = 0.5f;

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", tintedColor);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", tintedColor);
            }

            SetMaterialTransparent(material);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(0.18f, 0.6f, 0.85f) * 1.75f);
            }

            AssetDatabase.CreateAsset(material, assetPath);
            return material;
        }

        private static void SetMaterialTransparent(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            if (material.HasProperty("_AlphaClip"))
            {
                material.SetFloat("_AlphaClip", 0f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.renderQueue = (int)RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.SetOverrideTag("RenderType", "Transparent");
        }

        private static GameObject InstantiateAssetAt(string assetPath, Vector3 position, Quaternion rotation, Transform parent, string objectName)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Oneiric] Could not load asset at {assetPath}");
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null)
            {
                instance = UnityEngine.Object.Instantiate(prefab, parent);
            }

            instance.name = objectName;
            instance.transform.SetPositionAndRotation(position, rotation);
            Undo.RegisterCreatedObjectUndo(instance, "Instantiate waking object");
            return instance;
        }
        private static AudioSource GetOrCreateAudioSource(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            GameObject audioObject;
            if (existing != null)
            {
                audioObject = existing.gameObject;
            }
            else
            {
                audioObject = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(audioObject, "Create audio source");
            }

            audioObject.transform.SetParent(parent, false);
            AudioSource source = audioObject.GetComponent<AudioSource>();
            if (source == null)
            {
                source = Undo.AddComponent<AudioSource>(audioObject);
            }

            return source;
        }

        private static Transform GetOrCreateRoot(string name)
        {
            GameObject root = GameObject.Find(name);
            if (root == null)
            {
                root = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(root, "Create oneiric root");
            }

            return root.transform;
        }

        private static Transform GetOrCreateChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                return child;
            }

            GameObject childObject = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(childObject, "Create oneiric child");
            childObject.transform.SetParent(parent, false);
            return childObject.transform;
        }

        private static string DescribeCandidate(params string[] fragments)
        {
            GameObject candidate = FindSceneObject(fragments);
            return candidate != null ? GetPath(candidate.transform) : "missing";
        }

        private static GameObject FindSceneObject(params string[] fragments)
        {
            Scene scene = SceneManager.GetActiveScene();
            string[] normalizedFragments = fragments.Select(fragment => fragment.ToLowerInvariant()).ToArray();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform match = FindTransformRecursive(root.transform, normalizedFragments);
                if (match != null)
                {
                    return match.gameObject;
                }
            }

            return null;
        }

        private static Transform FindTransformRecursive(Transform current, IReadOnlyList<string> fragments)
        {
            string lowerName = current.name.ToLowerInvariant();
            if (fragments.Any(lowerName.Contains))
            {
                return current;
            }

            for (int index = 0; index < current.childCount; index++)
            {
                Transform child = FindTransformRecursive(current.GetChild(index), fragments);
                if (child != null)
                {
                    return child;
                }
            }

            return null;
        }

        private static Camera FindPlayerCamera(GameObject player)
        {
            return player.GetComponentInChildren<Camera>(true) ?? Camera.main;
        }

        private static string GetPath(Transform transform)
        {
            List<string> segments = new();
            Transform current = transform;
            while (current != null)
            {
                segments.Add(current.name);
                current = current.parent;
            }

            segments.Reverse();
            return string.Join("/", segments);
        }

        private static Vector3 CalculateBoundsCenter(GameObject waking, GameObject dream)
        {
            List<Renderer> renderers = new();
            renderers.AddRange(waking.GetComponentsInChildren<Renderer>(true));
            renderers.AddRange(dream.GetComponentsInChildren<Renderer>(true));
            if (renderers.Count == 0)
            {
                return waking.transform.position;
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Count; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds.center;
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalidCharacter, '_');
            }

            return name.Replace(" ", "_");
        }

        private static void CreateFolderIfMissing(string parent, string name)
        {
            string folderPath = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private struct SuperpositionTargetBundle
        {
            public Vector3 AnchorPosition;
        }

        private struct SuperpositionPreset
        {
            public float BaseBlend;
            public float DreamOpacityMin;
            public float DreamOpacityMax;
            public Vector3 PositionDrift;
            public Vector3 ScalePulse;
            public Vector3 RotationDrift;
            public float RestingBlend;
            public float ObservedBlend;
            public float ObservedDistortion;
            public ObservationCollapseTarget.CollapseResponse Response;
        }
    }
}


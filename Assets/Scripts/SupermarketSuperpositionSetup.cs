#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
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
        private const string DreamProfilePath = "Assets/Settings/DreamPostProcessProfile.asset";
        private const string DreamVolumeName = "GlobalDreamVolume";
        private const float ProductCoverageRatio = 0.125f;

        private sealed class ExampleDefinition
        {
            public string Label;
            public string[] PrefabNameHints;
            public string[] ObjectNameHints;
            public Vector3 DreamOffset;
            public bool AddFloatMotion;
            public CollapseTargetBehavior CollapseBehavior;
            public float ObservedBlend = 0.8f;
            public float ObservationRadius = 5f;
            public float GazeAngle = 20f;
        }

        [MenuItem("Oneiric/Superposition/Setup Grocery Scene")]
        public static void SetupGroceryScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            List<string> report = new List<string>();
            bool changed = false;

            GameObject player = FindSceneObject("Player");
            Camera playerCamera = null;

            if (player != null)
            {
                playerCamera = player.GetComponentsInChildren<Camera>(true).FirstOrDefault();
                changed |= EnsurePlayerWalker(player, playerCamera, report);
            }
            else
            {
                report.Add("Player object not found. Runtime scripts were created, but player wiring needs to be manual.");
            }

            DreamStateManager manager = EnsureDreamStateManager(report, ref changed);

            HashSet<GameObject> reservedRoots = new HashSet<GameObject>();
            if (player != null)
            {
                reservedRoots.Add(player);
            }

            if (manager != null)
            {
                reservedRoots.Add(manager.gameObject);
            }

            List<ExampleDefinition> definitions = new List<ExampleDefinition>
            {
                new ExampleDefinition
                {
                    Label = "Shelf_A_Superposed",
                    PrefabNameHints = new[] { "bread_rack", "grocery_rack" },
                    ObjectNameHints = new[] { "grocery_rack", "bread_rack" },
                    DreamOffset = new Vector3(0.22f, 0.08f, 0.16f),
                    AddFloatMotion = false,
                    CollapseBehavior = CollapseTargetBehavior.UseObservedBlend,
                    ObservedBlend = 0.7f,
                    ObservationRadius = 5.5f,
                    GazeAngle = 22f
                },
                new ExampleDefinition
                {
                    Label = "Cart_01_Superposed",
                    PrefabNameHints = new[] { "Basket", "car_backet" },
                    ObjectNameHints = new[] { "basket", "car_backet" },
                    DreamOffset = new Vector3(0.18f, 0.14f, -0.12f),
                    AddFloatMotion = true,
                    CollapseBehavior = CollapseTargetBehavior.TowardDream,
                    ObservationRadius = 4.5f,
                    GazeAngle = 18f
                },
                new ExampleDefinition
                {
                    Label = "Checkout_01_Superposed",
                    PrefabNameHints = new[] { "Cashier", "Autocachier", "Price_checker" },
                    ObjectNameHints = new[] { "cashier", "autocachier", "price_checker" },
                    DreamOffset = new Vector3(-0.15f, 0.1f, 0.2f),
                    AddFloatMotion = true,
                    CollapseBehavior = CollapseTargetBehavior.UseObservedBlend,
                    ObservedBlend = 0.85f,
                    ObservationRadius = 5f,
                    GazeAngle = 20f
                },
                new ExampleDefinition
                {
                    Label = "Freezer_Section_Superposed",
                    PrefabNameHints = new[] { "big_fridge" },
                    ObjectNameHints = new[] { "big_fridge" },
                    DreamOffset = new Vector3(0.18f, 0.05f, 0.28f),
                    AddFloatMotion = false,
                    CollapseBehavior = CollapseTargetBehavior.TowardWaking,
                    ObservationRadius = 6f,
                    GazeAngle = 24f
                },
                new ExampleDefinition
                {
                    Label = "Light_Fixture_Superposed",
                    PrefabNameHints = new[] { "grocery_lamp" },
                    ObjectNameHints = new[] { "lamp" },
                    DreamOffset = new Vector3(0f, 0.22f, 0f),
                    AddFloatMotion = false,
                    CollapseBehavior = CollapseTargetBehavior.TowardDream,
                    ObservationRadius = 6f,
                    GazeAngle = 16f
                }
            };

            foreach (ExampleDefinition definition in definitions)
            {
                changed |= EnsureExample(definition, player != null ? player.transform : null, playerCamera != null ? playerCamera.transform : null, reservedRoots, report);
            }

            changed |= EnsureProductCoverage(player != null ? player.transform : null, playerCamera != null ? playerCamera.transform : null, reservedRoots, report, ProductCoverageRatio);

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log("Superposition setup report:\n- " + string.Join("\n- ", report));
        }

        [MenuItem("Oneiric/Superposition/Setup Dream Post Processing")]
        public static void SetupDreamPostProcessing()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            List<string> report = new List<string>();
            bool changed = false;

            VolumeProfile profile = EnsureDreamPostProcessProfile(report);
            GameObject player = FindSceneObject("Player");
            Camera playerCamera = FindSceneObject("PlayerCamera")?.GetComponent<Camera>();

            changed |= EnsureDreamVolume(profile, report);
            changed |= EnsureCameraPostProcessing(player, playerCamera, report);

            AssetDatabase.SaveAssets();

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log("Dream post processing setup report:\n- " + string.Join("\n- ", report));
        }

        // Keep the original executeMethod target available for existing automation calls.
        public static void InspectGroceryScene()
        {
            SetupGroceryScene();
        }

        [MenuItem("Oneiric/Superposition/Setup Product Coverage")]
        public static void SetupProductCoverage()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            List<string> report = new List<string>();
            bool changed = false;

            GameObject player = FindSceneObject("Player");
            Camera playerCamera = FindSceneObject("PlayerCamera")?.GetComponent<Camera>();

            HashSet<GameObject> reservedRoots = new HashSet<GameObject>();
            if (player != null)
            {
                reservedRoots.Add(player);
            }

            DreamStateManager manager = Object.FindFirstObjectByType<DreamStateManager>();
            if (manager != null)
            {
                reservedRoots.Add(manager.gameObject);
            }

            changed |= EnsureProductCoverage(player != null ? player.transform : null, playerCamera != null ? playerCamera.transform : null, reservedRoots, report, ProductCoverageRatio);

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log("Product coverage setup report:\n- " + string.Join("\n- ", report));
        }

        public static void InspectDreamPostProcessing()
        {
            SetupDreamPostProcessing();
        }

        private static bool EnsurePlayerWalker(GameObject player, Camera playerCamera, List<string> report)
        {
            bool changed = false;

            CharacterController controller = player.GetComponent<CharacterController>();
            CapsuleCollider capsuleCollider = player.GetComponent<CapsuleCollider>();

            if (controller == null)
            {
                controller = player.AddComponent<CharacterController>();
                changed = true;
                report.Add("Added CharacterController to Player.");
            }

            if (capsuleCollider != null)
            {
                controller.height = capsuleCollider.height;
                controller.radius = capsuleCollider.radius;
                controller.center = capsuleCollider.center;

                if (capsuleCollider.enabled)
                {
                    capsuleCollider.enabled = false;
                    changed = true;
                    report.Add("Disabled Player CapsuleCollider so CharacterController handles collision cleanly.");
                }
            }

            bool hasCustomController = GetCustomBehaviour(player).Any();
            SimplePlayerWalker walker = player.GetComponent<SimplePlayerWalker>();

            if (walker == null && !hasCustomController)
            {
                walker = player.AddComponent<SimplePlayerWalker>();
                changed = true;
                report.Add("Attached SimplePlayerWalker to Player.");
            }
            else if (walker == null && hasCustomController)
            {
                report.Add("Skipped attaching SimplePlayerWalker because a custom player MonoBehaviour already exists.");
            }

            if (walker != null && playerCamera != null)
            {
                walker.AssignCameraTransform(playerCamera.transform);
                EditorUtility.SetDirty(walker);
                changed = true;
                report.Add("Assigned PlayerCamera to SimplePlayerWalker.");
            }

            return changed;
        }

        private static DreamStateManager EnsureDreamStateManager(List<string> report, ref bool changed)
        {
            DreamStateManager manager = Object.FindFirstObjectByType<DreamStateManager>();
            if (manager == null)
            {
                GameObject managerObject = new GameObject("DreamStateManager");
                manager = managerObject.AddComponent<DreamStateManager>();
                changed = true;
                report.Add("Created DreamStateManager scene object.");
            }
            else
            {
                report.Add("Reused existing DreamStateManager scene object.");
            }

            return manager;
        }

        private static VolumeProfile EnsureDreamPostProcessProfile(List<string> report)
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(DreamProfilePath);
            bool needsRebuild = profile == null || profile.components == null || profile.components.Any(component => component == null);

            if (needsRebuild)
            {
                if (profile != null)
                {
                    AssetDatabase.DeleteAsset(DreamProfilePath);
                }

                bool copied = AssetDatabase.CopyAsset("Assets/Settings/DefaultVolumeProfile.asset", DreamProfilePath);
                profile = copied ? AssetDatabase.LoadAssetAtPath<VolumeProfile>(DreamProfilePath) : null;

                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<VolumeProfile>();
                    AssetDatabase.CreateAsset(profile, DreamProfilePath);
                    report.Add("Created DreamPostProcessProfile asset from a fresh profile.");
                }
                else
                {
                    report.Add("Created DreamPostProcessProfile asset by cloning DefaultVolumeProfile.");
                }
            }
            else
            {
                report.Add("Reused existing DreamPostProcessProfile asset.");
            }

            ConfigureDreamProfile(profile);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static bool EnsureDreamVolume(VolumeProfile profile, List<string> report)
        {
            Volume volume = Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(existing => existing.gameObject.name == DreamVolumeName);

            bool changed = false;
            if (volume == null)
            {
                GameObject volumeObject = new GameObject(DreamVolumeName);
                volume = volumeObject.AddComponent<Volume>();
                changed = true;
                report.Add("Created GlobalDreamVolume scene object.");
            }
            else
            {
                report.Add("Reused existing GlobalDreamVolume scene object.");
            }

            volume.isGlobal = true;
            volume.priority = 5f;
            volume.blendDistance = 0f;
            volume.weight = 1f;
            volume.sharedProfile = profile;
            EditorUtility.SetDirty(volume);

            DreamPostProcessingDriver driver = volume.GetComponent<DreamPostProcessingDriver>();
            if (driver == null)
            {
                driver = volume.gameObject.AddComponent<DreamPostProcessingDriver>();
                changed = true;
                report.Add("Attached DreamPostProcessingDriver to GlobalDreamVolume.");
            }

            EditorUtility.SetDirty(driver);
            return changed;
        }

        private static bool EnsureCameraPostProcessing(GameObject player, Camera playerCamera, List<string> report)
        {
            if (playerCamera == null)
            {
                report.Add("PlayerCamera not found. Volume was created, but camera post-processing still needs manual hookup.");
                return false;
            }

            UniversalAdditionalCameraData cameraData = playerCamera.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData == null)
            {
                report.Add("PlayerCamera is missing UniversalAdditionalCameraData.");
                return false;
            }

            cameraData.renderPostProcessing = true;
            cameraData.volumeLayerMask = ~0;
            cameraData.volumeTrigger = player != null ? player.transform : playerCamera.transform;
            EditorUtility.SetDirty(cameraData);
            report.Add("Enabled post-processing on PlayerCamera and widened volume mask.");
            return true;
        }

        private static void ConfigureDreamProfile(VolumeProfile profile)
        {
            Bloom bloom = GetOrCreate<Bloom>(profile, out _);
            bloom.active = true;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.95f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.22f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.72f;
            bloom.tint.overrideState = true;
            bloom.tint.value = new Color(0.78f, 0.92f, 1f, 1f);

            ColorAdjustments colorAdjustments = GetOrCreate<ColorAdjustments>(profile, out _);
            colorAdjustments.active = true;
            colorAdjustments.postExposure.overrideState = true;
            colorAdjustments.postExposure.value = -0.03f;
            colorAdjustments.contrast.overrideState = true;
            colorAdjustments.contrast.value = 8f;
            colorAdjustments.colorFilter.overrideState = true;
            colorAdjustments.colorFilter.value = new Color(0.92f, 0.98f, 1f, 1f);
            colorAdjustments.hueShift.overrideState = true;
            colorAdjustments.hueShift.value = -6f;
            colorAdjustments.saturation.overrideState = true;
            colorAdjustments.saturation.value = -14f;

            Vignette vignette = GetOrCreate<Vignette>(profile, out _);
            vignette.active = true;
            vignette.color.overrideState = true;
            vignette.color.value = new Color(0.06f, 0.08f, 0.12f, 1f);
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.22f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.45f;

            ChromaticAberration chromaticAberration = GetOrCreate<ChromaticAberration>(profile, out _);
            chromaticAberration.active = true;
            chromaticAberration.intensity.overrideState = true;
            chromaticAberration.intensity.value = 0.08f;

            LensDistortion lensDistortion = GetOrCreate<LensDistortion>(profile, out _);
            lensDistortion.active = true;
            lensDistortion.intensity.overrideState = true;
            lensDistortion.intensity.value = -0.08f;
            lensDistortion.scale.overrideState = true;
            lensDistortion.scale.value = 1f;

            FilmGrain filmGrain = GetOrCreate<FilmGrain>(profile, out _);
            filmGrain.active = true;
            filmGrain.intensity.overrideState = true;
            filmGrain.intensity.value = 0.12f;
            filmGrain.response.overrideState = true;
            filmGrain.response.value = 0.8f;

            Tonemapping tonemapping = GetOrCreate<Tonemapping>(profile, out _);
            tonemapping.active = true;
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.ACES;
        }

        private static T GetOrCreate<T>(VolumeProfile profile, out bool created) where T : VolumeComponent
        {
            if (profile.TryGet(out T component))
            {
                created = false;
                return component;
            }

            created = true;
            return profile.Add<T>(true);
        }

        private static bool EnsureExample(
            ExampleDefinition definition,
            Transform playerTransform,
            Transform cameraTransform,
            HashSet<GameObject> reservedRoots,
            List<string> report)
        {
            if (FindSceneObject(definition.Label) != null)
            {
                report.Add(definition.Label + " already exists, leaving it in place.");
                return false;
            }

            GameObject source = FindBestSceneTarget(definition, reservedRoots);
            if (source == null)
            {
                report.Add("Could not find a safe source object for " + definition.Label + ".");
                return false;
            }

            Transform originalParent = source.transform.parent;
            int siblingIndex = source.transform.GetSiblingIndex();
            Vector3 worldPosition = source.transform.position;
            Quaternion worldRotation = source.transform.rotation;
            Vector3 worldScale = source.transform.lossyScale;

            string sourceName = source.name;

            if (!TryCreateSuperposedContainer(
                    source,
                    definition.Label,
                    playerTransform,
                    cameraTransform,
                    definition.DreamOffset,
                    definition.AddFloatMotion,
                    definition.CollapseBehavior,
                    definition.ObservationRadius,
                    definition.GazeAngle,
                    definition.ObservedBlend,
                    reservedRoots,
                    out _))
            {
                report.Add("Failed to create " + definition.Label + " from source object " + sourceName + ".");
                return false;
            }

            report.Add("Created " + definition.Label + " from source object " + sourceName + ".");
            return true;
        }

        private static bool EnsureProductCoverage(
            Transform playerTransform,
            Transform cameraTransform,
            HashSet<GameObject> reservedRoots,
            List<string> report,
            float targetCoverage)
        {
            List<GameObject> remainingCandidates = FindProductCandidates(reservedRoots);
            List<SuperpositionObject> existingProducts = GetExistingProductSuperpositions();
            int existingCoverage = existingProducts.Count;
            int totalProductPopulation = existingCoverage + remainingCandidates.Count;
            int targetCount = Mathf.CeilToInt(totalProductPopulation * Mathf.Clamp01(targetCoverage));

            if (totalProductPopulation == 0)
            {
                report.Add("No eligible product prefabs were found for additional superposition.");
                return false;
            }

            int productsToRemove = Mathf.Max(0, existingCoverage - targetCount);
            int productsToCreate = Mathf.Max(0, targetCount - existingCoverage);
            int removedCount = 0;
            int createdCount = 0;

            if (productsToRemove > 0)
            {
                List<SuperpositionObject> keepSet = SelectDistributedSuperpositions(existingProducts, targetCount);
                HashSet<SuperpositionObject> keepLookup = new HashSet<SuperpositionObject>(keepSet);

                foreach (SuperpositionObject existing in existingProducts)
                {
                    if (existing == null || keepLookup.Contains(existing))
                    {
                        continue;
                    }

                    if (UnwrapAndRemoveProductSuperposition(existing))
                    {
                        removedCount++;
                    }
                }

                existingProducts = keepSet.Where(item => item != null).ToList();
                existingCoverage = existingProducts.Count;
            }

            if (productsToCreate > 0)
            {
                List<GameObject> selectedProducts = SelectDistributedCandidates(remainingCandidates, productsToCreate);

                for (int index = 0; index < selectedProducts.Count; index++)
                {
                    GameObject source = selectedProducts[index];
                    string containerName = BuildUniqueProductContainerName(source.name, existingCoverage + createdCount + 1, reservedRoots);
                    Vector3 dreamOffset = GenerateProductDreamOffset(existingCoverage + createdCount);
                    bool addFloatMotion = false;
                    CollapseTargetBehavior collapseBehavior = CollapseTargetBehavior.UseObservedBlend;
                    float observationRadius = 2.2f;
                    float gazeAngle = 14f;
                    float observedBlend = 0.68f;

                    if (TryCreateSuperposedContainer(
                            source,
                            containerName,
                            playerTransform,
                            cameraTransform,
                            dreamOffset,
                            addFloatMotion,
                            collapseBehavior,
                            observationRadius,
                            gazeAngle,
                            observedBlend,
                            reservedRoots,
                            out GameObject container))
                    {
                        SuperpositionObject superposition = container.GetComponent<SuperpositionObject>();
                        if (superposition != null)
                        {
                            OptimizeBulkProductSuperposition(superposition, playerTransform, cameraTransform, existingCoverage + createdCount);
                        }

                        createdCount++;
                    }
                }
            }

            List<SuperpositionObject> optimizedProducts = GetExistingProductSuperpositions();
            for (int index = 0; index < optimizedProducts.Count; index++)
            {
                OptimizeBulkProductSuperposition(optimizedProducts[index], playerTransform, cameraTransform, index);
            }

            if (createdCount == 0 && removedCount == 0)
            {
                report.Add($"Product coverage already normalized at {existingCoverage}/{totalProductPopulation} superposed.");
                return false;
            }

            int finalCoverage = CountExistingProductSuperpositions();
            report.Add($"Adjusted product coverage by removing {removedCount} and creating {createdCount}. Final product coverage is approximately {finalCoverage}/{totalProductPopulation}.");
            return true;
        }

        private static bool TryCreateSuperposedContainer(
            GameObject source,
            string containerName,
            Transform playerTransform,
            Transform cameraTransform,
            Vector3 dreamOffset,
            bool addFloatMotion,
            CollapseTargetBehavior collapseBehavior,
            float observationRadius,
            float gazeAngle,
            float observedBlend,
            HashSet<GameObject> reservedRoots,
            out GameObject container)
        {
            container = null;
            if (source == null)
            {
                return false;
            }

            Transform originalParent = source.transform.parent;
            int siblingIndex = source.transform.GetSiblingIndex();
            Vector3 worldPosition = source.transform.position;
            Quaternion worldRotation = source.transform.rotation;
            Vector3 worldScale = source.transform.lossyScale;

            container = new GameObject(containerName);
            container.transform.SetParent(originalParent, false);
            container.transform.position = worldPosition;
            container.transform.rotation = worldRotation;
            CopyApproximateWorldScale(container.transform, originalParent, worldScale);
            container.transform.SetSiblingIndex(siblingIndex);

            string baseName = containerName.EndsWith("_Superposed")
                ? containerName.Substring(0, containerName.Length - "_Superposed".Length)
                : containerName;

            source.transform.SetParent(container.transform, true);
            source.name = baseName + "_Waking";

            GameObject dream = Object.Instantiate(source, container.transform);
            dream.name = baseName + "_Dream";
            SanitizeDreamDuplicate(dream);

            SuperpositionObject superposition = container.AddComponent<SuperpositionObject>();
            superposition.SetRoots(source.transform, dream.transform);
            superposition.RestingBlend = 0.45f;
            superposition.ObservedBlend = observedBlend;
            superposition.TransitionSpeed = 3f;
            superposition.DreamOffset = dreamOffset;
            superposition.GlobalDreamInfluence = 0.35f;

            ObservationCollapseTarget observation = container.AddComponent<ObservationCollapseTarget>();
            observation.Superposition = superposition;
            observation.AssignObserver(playerTransform, cameraTransform);
            observation.Mode = ObservationMode.Both;
            observation.CollapseBehavior = collapseBehavior;
            observation.ObservationRadius = observationRadius;
            observation.GazeAngle = gazeAngle;
            observation.RestingBlend = 0.45f;
            observation.ObservedBlend = observedBlend;
            observation.TransitionSpeed = 3f;

            if (addFloatMotion)
            {
                DreamFloatMotion floatMotion = dream.GetComponent<DreamFloatMotion>();
                if (floatMotion == null)
                {
                    floatMotion = dream.AddComponent<DreamFloatMotion>();
                }

                floatMotion.LocalPositionOffset = dreamOffset * 0.5f;
            }

            reservedRoots.Add(container);
            return true;
        }

        private static IEnumerable<MonoBehaviour> GetCustomBehaviour(GameObject root)
        {
            return root.GetComponentsInChildren<MonoBehaviour>(true)
                .Where(component => component != null)
                .Where(component =>
                    component.GetType() != typeof(SimplePlayerWalker) &&
                    (component.GetType().Namespace == null ||
                     (!component.GetType().Namespace.StartsWith("UnityEngine") &&
                      !component.GetType().Namespace.StartsWith("UnityEditor") &&
                      !component.GetType().Namespace.StartsWith("Unity.VisualScripting"))));
        }

        private static List<GameObject> FindProductCandidates(HashSet<GameObject> reservedRoots)
        {
            HashSet<GameObject> seenRoots = new HashSet<GameObject>();
            List<GameObject> candidates = new List<GameObject>();

            foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                GameObject instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(transform.gameObject);
                GameObject candidate = instanceRoot != null ? instanceRoot : transform.gameObject;

                if (!seenRoots.Add(candidate) || reservedRoots.Contains(candidate))
                {
                    continue;
                }

                if (candidate.GetComponentInParent<SuperpositionObject>() != null || candidate.name.EndsWith("_Dream") || candidate.name.Contains("_Superposed"))
                {
                    continue;
                }

                string prefabPath = NormalizeAssetPath(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(candidate));
                if (!prefabPath.Contains("/models/products/"))
                {
                    continue;
                }

                if (candidate.GetComponentInChildren<Renderer>(true) == null)
                {
                    continue;
                }

                candidates.Add(candidate);
            }

            return candidates
                .OrderBy(candidate => candidate.transform.position.z)
                .ThenBy(candidate => candidate.transform.position.x)
                .ThenBy(candidate => candidate.name)
                .ToList();
        }

        private static int CountExistingProductSuperpositions()
        {
            return GetExistingProductSuperpositions().Count;
        }

        private static List<SuperpositionObject> GetExistingProductSuperpositions()
        {
            List<SuperpositionObject> products = new List<SuperpositionObject>();
            foreach (SuperpositionObject superposition in Object.FindObjectsByType<SuperpositionObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (superposition == null || superposition.WakingRoot == null)
                {
                    continue;
                }

                string prefabPath = NormalizeAssetPath(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(superposition.WakingRoot.gameObject));
                if (prefabPath.Contains("/models/products/"))
                {
                    products.Add(superposition);
                }
            }

            return products
                .OrderBy(item => item.transform.position.z)
                .ThenBy(item => item.transform.position.x)
                .ThenBy(item => item.name)
                .ToList();
        }

        private static List<GameObject> SelectDistributedCandidates(List<GameObject> candidates, int count)
        {
            if (count >= candidates.Count)
            {
                return new List<GameObject>(candidates);
            }

            List<GameObject> selected = new List<GameObject>(count);
            HashSet<GameObject> used = new HashSet<GameObject>();
            float stride = candidates.Count / (float)count;
            float cursor = 0f;

            for (int i = 0; i < count; i++)
            {
                int startIndex = Mathf.Clamp(Mathf.FloorToInt(cursor), 0, candidates.Count - 1);
                int chosenIndex = startIndex;

                while (chosenIndex < candidates.Count && used.Contains(candidates[chosenIndex]))
                {
                    chosenIndex++;
                }

                if (chosenIndex >= candidates.Count)
                {
                    chosenIndex = startIndex;
                    while (chosenIndex >= 0 && used.Contains(candidates[chosenIndex]))
                    {
                        chosenIndex--;
                    }
                }

                if (chosenIndex >= 0 && chosenIndex < candidates.Count && used.Add(candidates[chosenIndex]))
                {
                    selected.Add(candidates[chosenIndex]);
                }

                cursor += stride;
            }

            return selected;
        }

        private static List<SuperpositionObject> SelectDistributedSuperpositions(List<SuperpositionObject> superpositions, int count)
        {
            if (count >= superpositions.Count)
            {
                return new List<SuperpositionObject>(superpositions);
            }

            List<SuperpositionObject> selected = new List<SuperpositionObject>(count);
            HashSet<SuperpositionObject> used = new HashSet<SuperpositionObject>();
            float stride = superpositions.Count / (float)count;
            float cursor = 0f;

            for (int i = 0; i < count; i++)
            {
                int startIndex = Mathf.Clamp(Mathf.FloorToInt(cursor), 0, superpositions.Count - 1);
                int chosenIndex = startIndex;

                while (chosenIndex < superpositions.Count && used.Contains(superpositions[chosenIndex]))
                {
                    chosenIndex++;
                }

                if (chosenIndex >= superpositions.Count)
                {
                    chosenIndex = startIndex;
                    while (chosenIndex >= 0 && used.Contains(superpositions[chosenIndex]))
                    {
                        chosenIndex--;
                    }
                }

                if (chosenIndex >= 0 && chosenIndex < superpositions.Count && used.Add(superpositions[chosenIndex]))
                {
                    selected.Add(superpositions[chosenIndex]);
                }

                cursor += stride;
            }

            return selected;
        }

        private static void OptimizeBulkProductSuperposition(
            SuperpositionObject superposition,
            Transform playerTransform,
            Transform cameraTransform,
            int seed)
        {
            if (superposition == null || superposition.WakingRoot == null || superposition.DreamRoot == null)
            {
                return;
            }

            superposition.RestingBlend = 0.42f;
            superposition.ObservedBlend = 0.68f;
            superposition.TransitionSpeed = 2.2f;
            superposition.GlobalDreamInfluence = 0f;
            superposition.DreamOffset = GenerateProductDreamOffset(seed);

            SerializedObject superpositionSerialized = new SerializedObject(superposition);
            superpositionSerialized.FindProperty("enableDreamScalePulse").boolValue = false;
            superpositionSerialized.FindProperty("enableDreamRotationDrift").boolValue = false;
            superpositionSerialized.FindProperty("enableEmissionBoost").boolValue = false;
            superpositionSerialized.FindProperty("enableRendererFading").boolValue = false;
            superpositionSerialized.ApplyModifiedPropertiesWithoutUndo();

            ObservationCollapseTarget observation = superposition.GetComponent<ObservationCollapseTarget>();
            if (observation != null)
            {
                observation.AssignObserver(playerTransform, cameraTransform);
                observation.Mode = ObservationMode.Proximity;
                observation.ObservationRadius = 2.2f;
                observation.GazeAngle = 14f;
                observation.RestingBlend = 0.42f;
                observation.ObservedBlend = 0.68f;
                observation.TransitionSpeed = 2.2f;

                SerializedObject observationSerialized = new SerializedObject(observation);
                observationSerialized.FindProperty("evaluationInterval").floatValue = 0.2f;
                observationSerialized.ApplyModifiedPropertiesWithoutUndo();
            }

            DreamFloatMotion floatMotion = superposition.DreamRoot.GetComponent<DreamFloatMotion>();
            if (floatMotion != null)
            {
                Object.DestroyImmediate(floatMotion, true);
            }

            foreach (Renderer renderer in superposition.DreamRoot.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private static bool UnwrapAndRemoveProductSuperposition(SuperpositionObject superposition)
        {
            if (superposition == null || superposition.WakingRoot == null || superposition.DreamRoot == null)
            {
                return false;
            }

            GameObject container = superposition.gameObject;
            Transform parent = container.transform.parent;
            int siblingIndex = container.transform.GetSiblingIndex();
            Transform wakingRoot = superposition.WakingRoot;

            string restoredName = RestoreOriginalName(wakingRoot.name, container.name);

            wakingRoot.SetParent(parent, true);
            wakingRoot.SetSiblingIndex(siblingIndex);
            wakingRoot.name = restoredName;

            if (superposition.DreamRoot != null)
            {
                Object.DestroyImmediate(superposition.DreamRoot.gameObject, true);
            }

            Object.DestroyImmediate(container, true);
            return true;
        }

        private static string RestoreOriginalName(string wakingName, string containerName)
        {
            string baseName = wakingName.EndsWith("_Waking")
                ? wakingName.Substring(0, wakingName.Length - "_Waking".Length)
                : containerName;

            int superposedMarker = baseName.IndexOf("_Superposed_", System.StringComparison.Ordinal);
            if (superposedMarker >= 0)
            {
                return baseName.Substring(0, superposedMarker);
            }

            if (baseName.EndsWith("_Superposed"))
            {
                return baseName.Substring(0, baseName.Length - "_Superposed".Length);
            }

            return baseName;
        }

        private static GameObject FindBestSceneTarget(ExampleDefinition definition, HashSet<GameObject> reservedRoots)
        {
            HashSet<GameObject> seenRoots = new HashSet<GameObject>();
            List<GameObject> candidates = new List<GameObject>();

            foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                GameObject instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(transform.gameObject);
                GameObject candidate = instanceRoot != null ? instanceRoot : transform.gameObject;

                if (!seenRoots.Add(candidate) || reservedRoots.Contains(candidate))
                {
                    continue;
                }

                if (candidate.GetComponentInParent<SuperpositionObject>() != null || candidate.name.EndsWith("_Dream"))
                {
                    continue;
                }

                string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(candidate);
                string lowerName = candidate.name.ToLowerInvariant();
                string lowerPath = prefabPath != null ? prefabPath.ToLowerInvariant() : string.Empty;

                bool matchesPrefab = definition.PrefabNameHints.Any(hint => lowerPath.Contains(hint.ToLowerInvariant()));
                bool matchesName = definition.ObjectNameHints.Any(hint => lowerName.Contains(hint.ToLowerInvariant()));

                if (matchesPrefab || matchesName)
                {
                    candidates.Add(candidate);
                }
            }

            return candidates
                .OrderBy(candidate => candidate.transform.position.y)
                .ThenBy(candidate => candidate.name)
                .FirstOrDefault();
        }

        private static void SanitizeDreamDuplicate(GameObject dreamRoot)
        {
            foreach (Collider collider in dreamRoot.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            foreach (Rigidbody body in dreamRoot.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.useGravity = false;
            }

            foreach (AudioSource audioSource in dreamRoot.GetComponentsInChildren<AudioSource>(true))
            {
                audioSource.enabled = false;
            }
        }

        private static void CopyApproximateWorldScale(Transform target, Transform parent, Vector3 worldScale)
        {
            if (parent == null)
            {
                target.localScale = worldScale;
                return;
            }

            Vector3 parentScale = parent.lossyScale;
            target.localScale = new Vector3(
                parentScale.x != 0f ? worldScale.x / parentScale.x : worldScale.x,
                parentScale.y != 0f ? worldScale.y / parentScale.y : worldScale.y,
                parentScale.z != 0f ? worldScale.z / parentScale.z : worldScale.z);
        }

        private static string BuildUniqueProductContainerName(string sourceName, int ordinal, HashSet<GameObject> reservedRoots)
        {
            string baseName = sourceName.Replace("(Clone)", string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = "Product";
            }

            string containerName = $"{baseName}_Superposed_{ordinal:000}";
            while (FindSceneObject(containerName) != null || reservedRoots.Any(root => root != null && root.name == containerName))
            {
                ordinal++;
                containerName = $"{baseName}_Superposed_{ordinal:000}";
            }

            return containerName;
        }

        private static Vector3 GenerateProductDreamOffset(int seed)
        {
            float horizontal = 0.04f + (seed % 4) * 0.015f;
            float vertical = 0.02f + (seed % 3) * 0.015f;
            float depth = 0.03f + (seed % 5) * 0.0125f;

            return new Vector3(
                (seed & 1) == 0 ? horizontal : -horizontal,
                vertical,
                (seed & 2) == 0 ? depth : -depth);
        }

        private static string NormalizeAssetPath(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? string.Empty : path.Replace('\\', '/').ToLowerInvariant();
        }

        private static GameObject FindSceneObject(string name)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Transform match = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
                if (match != null)
                {
                    return match.gameObject;
                }
            }

            return null;
        }
    }
}
#endif

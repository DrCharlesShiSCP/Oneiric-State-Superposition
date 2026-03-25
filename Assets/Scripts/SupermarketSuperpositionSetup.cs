#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Oneiric.Superposition.Editor
{
    public static class SupermarketSuperpositionSetup
    {
        private const string ScenePath = "Assets/Scenes/grocery_02.unity";

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
        public static void InspectGroceryScene()
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

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log("Superposition setup report:\n- " + string.Join("\n- ", report));
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

            GameObject container = new GameObject(definition.Label);
            container.transform.SetParent(originalParent, false);
            container.transform.position = worldPosition;
            container.transform.rotation = worldRotation;
            CopyApproximateWorldScale(container.transform, originalParent, worldScale);
            container.transform.SetSiblingIndex(siblingIndex);

            source.transform.SetParent(container.transform, true);
            source.name = definition.Label.Replace("_Superposed", "_Waking");

            GameObject dream = Object.Instantiate(source, container.transform);
            dream.name = definition.Label.Replace("_Superposed", "_Dream");
            SanitizeDreamDuplicate(dream);

            SuperpositionObject superposition = container.AddComponent<SuperpositionObject>();
            superposition.SetRoots(source.transform, dream.transform);
            superposition.RestingBlend = 0.45f;
            superposition.ObservedBlend = definition.ObservedBlend;
            superposition.TransitionSpeed = 3f;
            superposition.DreamOffset = definition.DreamOffset;
            superposition.GlobalDreamInfluence = 0.35f;

            ObservationCollapseTarget observation = container.AddComponent<ObservationCollapseTarget>();
            observation.Superposition = superposition;
            observation.AssignObserver(playerTransform, cameraTransform);
            observation.Mode = ObservationMode.Both;
            observation.CollapseBehavior = definition.CollapseBehavior;
            observation.ObservationRadius = definition.ObservationRadius;
            observation.GazeAngle = definition.GazeAngle;
            observation.RestingBlend = 0.45f;
            observation.ObservedBlend = definition.ObservedBlend;
            observation.TransitionSpeed = 3f;

            if (definition.AddFloatMotion)
            {
                DreamFloatMotion floatMotion = dream.GetComponent<DreamFloatMotion>();
                if (floatMotion == null)
                {
                    floatMotion = dream.AddComponent<DreamFloatMotion>();
                }

                floatMotion.LocalPositionOffset = definition.DreamOffset * 0.5f;
            }

            reservedRoots.Add(container);
            report.Add("Created " + definition.Label + " from source object " + source.name + ".");
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

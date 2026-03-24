using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Oneiric.Superposition
{
    [DisallowMultipleComponent]
    public class RuntimeSuperpositionBootstrap : MonoBehaviour
    {
        [Header("Bootstrap")]
        [SerializeField] private bool bootstrapOnStart = true;
        [SerializeField] private bool rebuildIfAlreadyPresent = true;

        [Header("Global Dream")]
        [SerializeField, Range(0f, 1f)] private float baseDreamIntensity = 0.35f;
        [SerializeField] private bool pulseIntensity = true;
        [SerializeField, Range(0f, 0.5f)] private float pulseAmplitude = 0.08f;
        [SerializeField] private float pulseSpeed = 0.45f;

        [Header("Audio")]
        [SerializeField] private AudioClip wakingAmbienceClip;
        [SerializeField] private AudioClip dreamAmbienceClip;
        [SerializeField] private float wakingVolume = 0.8f;
        [SerializeField] private float dreamVolume = 0.7f;

        private const string RootName = "Oneiric Superposition";
        private const string OverlayRootName = "Dream Overlay";
        private const string ExampleRootName = "Example Targets";
        private const string ParticleRootName = "Dream Particles";
        private const string ZoneRootName = "Dream Zones";
        private const string SystemRootName = "Systems";

        private readonly Dictionary<Material, Material> dreamMaterialCache = new();
        private bool built;

        private void Start()
        {
            if (bootstrapOnStart)
            {
                BuildRuntimeSetup();
            }
        }

        [ContextMenu("Build Runtime Setup")]
        public void BuildRuntimeSetup()
        {
            if (built)
            {
                return;
            }

            GameObject existingRoot = GameObject.Find(RootName);
            if (existingRoot != null)
            {
                if (!rebuildIfAlreadyPresent)
                {
                    built = true;
                    return;
                }

                Destroy(existingRoot);
            }

            GameObject player = FindSceneObject("Player");
            if (player == null)
            {
                Debug.LogWarning("[Oneiric] Runtime bootstrap could not find Player.");
                return;
            }

            Camera playerCamera = player.GetComponentInChildren<Camera>(true) ?? Camera.main;
            ConfigurePlayer(player, playerCamera);

            Transform root = CreateChild(null, RootName).transform;
            Transform overlayRoot = CreateChild(root, OverlayRootName).transform;
            Transform exampleRoot = CreateChild(root, ExampleRootName).transform;
            Transform particleRoot = CreateChild(root, ParticleRootName).transform;
            Transform zoneRoot = CreateChild(root, ZoneRootName).transform;
            Transform systemRoot = CreateChild(root, SystemRootName).transform;

            Volume globalVolume = CreateGlobalVolume(systemRoot);
            DreamAudioController audioController = CreateAudio(systemRoot);
            ParticleSystem[] particles = CreateGlobalParticles(particleRoot);
            CreateDreamManager(systemRoot, player.transform, playerCamera, globalVolume, audioController, particles);

            ConfigureShelfExample(exampleRoot, overlayRoot);
            ConfigureCartExample(exampleRoot, overlayRoot);
            ConfigureCheckoutExample(exampleRoot, overlayRoot);
            ConfigureLightExample(exampleRoot, overlayRoot, zoneRoot);
            ConfigureFreezerExample(exampleRoot, overlayRoot, particleRoot, zoneRoot);

            built = true;
        }

        private void ConfigurePlayer(GameObject player, Camera playerCamera)
        {
            bool hasExistingMovement = player.GetComponents<MonoBehaviour>()
                .Any(component =>
                    component != null &&
                    component.GetType() != typeof(SimplePlayerWalker) &&
                    (component.GetType().Name.Contains("Controller", StringComparison.OrdinalIgnoreCase) ||
                     component.GetType().Name.Contains("Movement", StringComparison.OrdinalIgnoreCase) ||
                     component.GetType().Name.Contains("FirstPerson", StringComparison.OrdinalIgnoreCase) ||
                     component.GetType().Name.Contains("ThirdPerson", StringComparison.OrdinalIgnoreCase)));

            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = player.AddComponent<CharacterController>();
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

            if (!hasExistingMovement)
            {
                SimplePlayerWalker walker = player.GetComponent<SimplePlayerWalker>();
                if (walker == null)
                {
                    walker = player.AddComponent<SimplePlayerWalker>();
                }

                walker.Configure(controller, playerCamera != null ? playerCamera.transform : null);
            }

            if (playerCamera != null)
            {
                playerCamera.tag = "MainCamera";
                UniversalAdditionalCameraData additionalData = playerCamera.GetUniversalAdditionalCameraData();
                additionalData.renderPostProcessing = true;
            }
        }

        private void ConfigureShelfExample(Transform exampleRoot, Transform overlayRoot)
        {
            GameObject shelf = FindSceneObject("grocery_rack");
            if (shelf == null)
            {
                return;
            }

            GameObject shelfDream = CreateDreamDuplicate(shelf, overlayRoot, "ShelfDream");
            CreateFloatingChildren(shelfDream.transform, 3, new Vector3(0.3f, 0.4f, 0.3f));
            CreateSuperpositionTarget(exampleRoot, "Shelf Superposition", shelf, shelfDream, new SuperpositionPreset
            {
                baseBlend = 0.42f,
                dreamOpacityMin = 0.24f,
                dreamOpacityMax = 0.95f,
                positionDrift = new Vector3(0.05f, 0.08f, 0.04f),
                scalePulse = new Vector3(0.025f, 0.035f, 0.025f),
                rotationDrift = new Vector3(0.8f, 4f, 0.8f),
                restingBlend = 0.4f,
                observedBlend = 0.78f,
                observedDistortion = 0.7f,
                response = ObservationCollapseTarget.CollapseResponse.TowardDream
            });
        }

        private void ConfigureCartExample(Transform exampleRoot, Transform overlayRoot)
        {
            GameObject cart = FindSceneObject("trolley", "basket", "small_basket", "car_backet");
            if (cart == null)
            {
                return;
            }

            GameObject cartDream = CreateDreamDuplicate(cart, overlayRoot, "CartDream");
            cartDream.transform.position += new Vector3(0.18f, 0.2f, -0.12f);
            cartDream.transform.localScale += new Vector3(0.15f, 0.35f, 0.1f);
            CreateFloatingChildren(cartDream.transform, 2, new Vector3(0.25f, 0.25f, 0.25f));
            CreateSuperpositionTarget(exampleRoot, "Cart Superposition", cart, cartDream, new SuperpositionPreset
            {
                baseBlend = 0.32f,
                dreamOpacityMin = 0.2f,
                dreamOpacityMax = 0.92f,
                positionDrift = new Vector3(0.04f, 0.14f, 0.08f),
                scalePulse = new Vector3(0.03f, 0.06f, 0.03f),
                rotationDrift = new Vector3(2f, 8f, 2f),
                restingBlend = 0.28f,
                observedBlend = 0.86f,
                observedDistortion = 0.9f,
                response = ObservationCollapseTarget.CollapseResponse.TowardDream
            });
        }

        private void ConfigureCheckoutExample(Transform exampleRoot, Transform overlayRoot)
        {
            GameObject checkout = FindSceneObject("autocachier", "price_checker", "cashier");
            if (checkout == null)
            {
                return;
            }

            GameObject checkoutDream = CreateDreamDuplicate(checkout, overlayRoot, "CheckoutDream");
            checkoutDream.transform.position += new Vector3(-0.06f, 0.12f, 0.09f);
            CreateDreamLight(checkoutDream.transform, "Checkout Glow", new Vector3(0f, 1.4f, 0f), 3.2f, 5.5f);
            CreateSuperpositionTarget(exampleRoot, "Checkout Superposition", checkout, checkoutDream, new SuperpositionPreset
            {
                baseBlend = 0.48f,
                dreamOpacityMin = 0.3f,
                dreamOpacityMax = 0.98f,
                positionDrift = new Vector3(0.02f, 0.06f, 0.03f),
                scalePulse = new Vector3(0.015f, 0.02f, 0.015f),
                rotationDrift = new Vector3(0.5f, 2f, 0.4f),
                restingBlend = 0.5f,
                observedBlend = 0.15f,
                observedDistortion = 0.65f,
                response = ObservationCollapseTarget.CollapseResponse.TowardWaking
            });
        }

        private void ConfigureLightExample(Transform exampleRoot, Transform overlayRoot, Transform zoneRoot)
        {
            GameObject lightFixture = FindSceneObject("grocery_lamp");
            if (lightFixture == null)
            {
                return;
            }

            GameObject lightDream = CreateDreamDuplicate(lightFixture, overlayRoot, "AisleLightDream");
            lightDream.transform.position += new Vector3(0f, -0.1f, 0f);
            CreateDreamLight(lightDream.transform, "Dream Point Light", Vector3.zero, 2.6f, 7f);

            SuperpositionObject superposition = CreateSuperpositionTarget(exampleRoot, "Aisle Light Superposition", lightFixture, lightDream, new SuperpositionPreset
            {
                baseBlend = 0.46f,
                dreamOpacityMin = 0.18f,
                dreamOpacityMax = 0.9f,
                positionDrift = new Vector3(0f, 0.03f, 0f),
                scalePulse = new Vector3(0.02f, 0.02f, 0.02f),
                rotationDrift = new Vector3(0.2f, 1.5f, 0.1f),
                restingBlend = 0.44f,
                observedBlend = 0.72f,
                observedDistortion = 0.75f,
                response = ObservationCollapseTarget.CollapseResponse.IncreaseDistortion
            });

            CreateZone(zoneRoot, "Aisle Light Zone", superposition.AnchorPosition, 5.5f);
        }

        private void ConfigureFreezerExample(Transform exampleRoot, Transform overlayRoot, Transform particleRoot, Transform zoneRoot)
        {
            GameObject freezer = FindSceneObject("big_fridge");
            if (freezer == null)
            {
                return;
            }

            GameObject freezerDream = CreateDreamDuplicate(freezer, overlayRoot, "FreezerDream");
            freezerDream.transform.position += new Vector3(0.12f, 0.06f, -0.08f);
            CreateDreamLight(freezerDream.transform, "Freezer Halo", new Vector3(0f, 1.1f, 0f), 2.2f, 6.5f);

            SuperpositionObject superposition = CreateSuperpositionTarget(exampleRoot, "Freezer Superposition", freezer, freezerDream, new SuperpositionPreset
            {
                baseBlend = 0.55f,
                dreamOpacityMin = 0.28f,
                dreamOpacityMax = 1f,
                positionDrift = new Vector3(0.03f, 0.05f, 0.06f),
                scalePulse = new Vector3(0.015f, 0.02f, 0.015f),
                rotationDrift = new Vector3(0.4f, 1.8f, 0.3f),
                restingBlend = 0.52f,
                observedBlend = 0.9f,
                observedDistortion = 0.95f,
                response = ObservationCollapseTarget.CollapseResponse.TowardDream
            });

            CreateParticleSystem("Freezer Motes", particleRoot, superposition.AnchorPosition + new Vector3(0f, 1.3f, 0f), new Vector3(1.6f, 1.8f, 1.2f), 18f);
            CreateZone(zoneRoot, "Freezer Dream Zone", superposition.AnchorPosition, 6.5f);
        }

        private DreamStateManager CreateDreamManager(
            Transform systemRoot,
            Transform player,
            Camera playerCamera,
            Volume globalVolume,
            DreamAudioController audioController,
            ParticleSystem[] particles)
        {
            GameObject managerObject = CreateChild(systemRoot, "Dream State Manager");
            DreamStateManager manager = managerObject.AddComponent<DreamStateManager>();
            manager.Configure(player, playerCamera, globalVolume, audioController, particles, baseDreamIntensity, pulseIntensity, pulseAmplitude, pulseSpeed);
            return manager;
        }

        private Volume CreateGlobalVolume(Transform systemRoot)
        {
            GameObject volumeObject = CreateChild(systemRoot, "Global Dream Volume");
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 15f;
            volume.sharedProfile = CreateVolumeProfile(true);
            volume.weight = baseDreamIntensity;
            return volume;
        }

        private DreamAudioController CreateAudio(Transform systemRoot)
        {
            GameObject audioRoot = CreateChild(systemRoot, "Dream Audio");
            AudioSource waking = CreateAudioSource(audioRoot.transform, "Waking Ambience", wakingAmbienceClip);
            AudioSource dream = CreateAudioSource(audioRoot.transform, "Dream Ambience", dreamAmbienceClip);

            DreamAudioController controller = audioRoot.AddComponent<DreamAudioController>();
            controller.Configure(waking, dream, wakingVolume, dreamVolume);
            return controller;
        }

        private ParticleSystem[] CreateGlobalParticles(Transform particleRoot)
        {
            ParticleSystem dust = CreateParticleSystem("Dream Dust", particleRoot, new Vector3(-2f, 2.2f, 0.5f), new Vector3(8f, 2.5f, 8f), 12f);
            ParticleSystem motes = CreateParticleSystem("Floating Motes", particleRoot, new Vector3(4.5f, 2f, -6f), new Vector3(5f, 2.2f, 5f), 10f);
            return new[] { dust, motes };
        }

        private ParticleSystem CreateParticleSystem(string name, Transform parent, Vector3 position, Vector3 boxSize, float rate)
        {
            GameObject particleObject = CreateChild(parent, name);
            particleObject.transform.position = position;

            ParticleSystem particleSystem = particleObject.AddComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = CreateParticleMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

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

        private DreamZone CreateZone(Transform zoneRoot, string name, Vector3 position, float radius)
        {
            GameObject zoneObject = CreateChild(zoneRoot, name);
            zoneObject.transform.position = position;

            Volume volume = zoneObject.AddComponent<Volume>();
            volume.isGlobal = false;
            volume.blendDistance = radius * 0.8f;
            volume.priority = 18f;
            volume.sharedProfile = CreateVolumeProfile(false);

            DreamZone zone = zoneObject.AddComponent<DreamZone>();
            zone.Configure(radius, 0.25f, volume, 1f);
            return zone;
        }

        private SuperpositionObject CreateSuperpositionTarget(
            Transform parent,
            string name,
            GameObject waking,
            GameObject dream,
            SuperpositionPreset preset)
        {
            GameObject targetObject = CreateChild(parent, name);
            targetObject.transform.position = waking.transform.position;
            targetObject.transform.rotation = waking.transform.rotation;

            SuperpositionObject superposition = targetObject.AddComponent<SuperpositionObject>();
            superposition.Configure(
                waking,
                dream,
                preset.baseBlend,
                preset.dreamOpacityMin,
                preset.dreamOpacityMax,
                preset.positionDrift,
                preset.scalePulse,
                preset.rotationDrift);

            ObservationCollapseTarget collapseTarget = targetObject.AddComponent<ObservationCollapseTarget>();
            Camera playerCamera = Camera.main;
            Transform player = playerCamera != null ? playerCamera.transform.root : null;
            collapseTarget.Configure(
                superposition,
                player,
                playerCamera,
                ObservationCollapseTarget.ObservationMode.Both,
                preset.response,
                5f,
                0.35f,
                12f,
                2.5f,
                preset.restingBlend,
                preset.observedBlend,
                Mathf.Clamp01(preset.baseBlend),
                preset.observedDistortion);

            return superposition;
        }

        private GameObject CreateDreamDuplicate(GameObject source, Transform overlayRoot, string label)
        {
            GameObject duplicate = Instantiate(source, overlayRoot);
            duplicate.name = source.name + "_" + label;
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
                renderer.materials = renderer.sharedMaterials.Select(CreateDreamMaterial).ToArray();
            }

            return duplicate;
        }

        private void CreateFloatingChildren(Transform root, int count, Vector3 spread)
        {
            List<Transform> leafRenderers = root.GetComponentsInChildren<Renderer>(true)
                .Select(renderer => renderer.transform)
                .Where(item => item.childCount == 0)
                .Take(count)
                .ToList();

            System.Random random = new System.Random(root.name.GetHashCode());
            foreach (Transform child in leafRenderers)
            {
                child.localPosition += new Vector3(
                    LerpRandom(random, -spread.x, spread.x),
                    LerpRandom(random, 0.12f, spread.y),
                    LerpRandom(random, -spread.z, spread.z));

                if (child.GetComponent<DreamFloatMotion>() == null)
                {
                    child.gameObject.AddComponent<DreamFloatMotion>();
                }
            }
        }

        private Light CreateDreamLight(Transform parent, string name, Vector3 localPosition, float intensity, float range)
        {
            GameObject lightObject = CreateChild(parent, name);
            lightObject.transform.localPosition = localPosition;

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            light.color = new Color(0.65f, 0.9f, 1f);

            DreamLightFlicker flicker = lightObject.AddComponent<DreamLightFlicker>();
            flicker.Configure(light, intensity);
            return light;
        }

        private VolumeProfile CreateVolumeProfile(bool global)
        {
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();

            Bloom bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(global ? 0.15f : 0.75f);
            bloom.threshold.Override(global ? 1f : 0.8f);
            bloom.scatter.Override(0.72f);

            Vignette vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(global ? 0.12f : 0.28f);
            vignette.smoothness.Override(0.75f);
            vignette.color.Override(new Color(0.05f, 0.1f, 0.14f));

            ChromaticAberration chromatic = profile.Add<ChromaticAberration>(true);
            chromatic.intensity.Override(global ? 0.03f : 0.12f);

            ColorAdjustments color = profile.Add<ColorAdjustments>(true);
            color.contrast.Override(global ? 6f : 12f);
            color.saturation.Override(global ? -8f : -18f);
            color.colorFilter.Override(global ? new Color(0.88f, 0.94f, 1f) : new Color(0.74f, 0.9f, 1f));

            return profile;
        }

        private Material CreateParticleMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            Material material = new Material(shader);
            material.SetColor("_BaseColor", new Color(0.7f, 0.9f, 1f, 0.4f));
            return material;
        }

        private Material CreateDreamMaterial(Material sourceMaterial)
        {
            if (sourceMaterial == null)
            {
                return null;
            }

            if (dreamMaterialCache.TryGetValue(sourceMaterial, out Material cached))
            {
                return cached;
            }

            Material material = new Material(sourceMaterial);
            if (material.shader == null || material.shader.name.Contains("Standard", StringComparison.OrdinalIgnoreCase))
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

            dreamMaterialCache[sourceMaterial] = material;
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

        private AudioSource CreateAudioSource(Transform parent, string name, AudioClip clip)
        {
            GameObject audioObject = CreateChild(parent, name);
            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = true;
            source.spatialBlend = 0f;
            return source;
        }

        private GameObject FindSceneObject(params string[] fragments)
        {
            string[] normalized = fragments.Select(fragment => fragment.ToLowerInvariant()).ToArray();
            foreach (Transform root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Select(go => go.transform))
            {
                if (root == transform || root.name == RootName)
                {
                    continue;
                }

                Transform match = FindTransformRecursive(root, normalized);
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

        private static GameObject CreateChild(Transform parent, string name)
        {
            GameObject gameObject = new GameObject(name);
            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
            }

            return gameObject;
        }

        private static float LerpRandom(System.Random random, float min, float max)
        {
            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }

        [Serializable]
        private struct SuperpositionPreset
        {
            public float baseBlend;
            public float dreamOpacityMin;
            public float dreamOpacityMax;
            public Vector3 positionDrift;
            public Vector3 scalePulse;
            public Vector3 rotationDrift;
            public float restingBlend;
            public float observedBlend;
            public float observedDistortion;
            public ObservationCollapseTarget.CollapseResponse response;
        }
    }
}

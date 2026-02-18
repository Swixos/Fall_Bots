using UnityEngine;
using FallBots.Player;
using FallBots.CourseGeneration;
using FallBots.UI;

namespace FallBots.Managers
{
    /// <summary>
    /// Bootstraps the entire game scene at runtime.
    /// Creates player, camera, lighting, course generator, UI, and game manager.
    /// Attach this to an empty GameObject in an empty scene to start the game.
    /// </summary>
    public class SceneBootstrap : MonoBehaviour
    {
        [Header("Course Settings")]
        [SerializeField] private int segmentCount = 8;
        [SerializeField] private bool useRandomSeed = true;
        [SerializeField] private int fixedSeed = 42;

        private void Awake()
        {
            SetupLighting();
            SetupSkybox();
            Material[] courseMaterials = CreateCourseMaterials();
            Material wallMat = CreateMaterial("Wall", new Color(0.75f, 0.75f, 0.85f, 0.7f));
            Material finishMat = CreateMaterial("Finish", new Color(1f, 0.84f, 0f));
            Material slimeMat = CreateMaterial("Slime", new Color(0.2f, 0.85f, 0.15f, 0.8f));

            // Course Generator
            GameObject courseGenObj = new GameObject("CourseGenerator");
            var courseGen = courseGenObj.AddComponent<ProceduralCourseGenerator>();
            SetCourseGeneratorMaterials(courseGen, courseMaterials, wallMat, finishMat, slimeMat);

            // Player
            GameObject player = CreatePlayer();

            // Camera
            GameObject cam = CreateCamera(player.transform);

            // UI
            GameObject uiObj = new GameObject("GameUI");
            var gameUI = uiObj.AddComponent<GameUI>();

            // Game Manager
            GameObject gmObj = new GameObject("GameManager");
            var gm = gmObj.AddComponent<GameManager>();
            SetGameManagerReferences(gm, courseGen, player.GetComponent<PlayerController>(), gameUI);

            // Kill zone below map
            CreateKillZone();

            // Skybox ground plane (visual only, very far below)
            CreateBackgroundPlane();
        }

        private void SetupLighting()
        {
            // Directional light (sun)
            GameObject sunObj = new GameObject("DirectionalLight");
            var light = sunObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.6f;
            sunObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Ambient fill light
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.6f, 0.7f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.5f, 0.5f, 0.6f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.3f, 0.35f);
        }

        private void SetupSkybox()
        {
            // Procedural skybox look with fog
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.65f, 0.78f, 0.95f);
            RenderSettings.fogStartDistance = 80f;
            RenderSettings.fogEndDistance = 300f;

            // Set camera background color instead of skybox material
            Camera.main?.gameObject.GetComponent<Camera>()?.clearFlags.Equals(CameraClearFlags.SolidColor);
        }

        private Material[] CreateCourseMaterials()
        {
            // Fall Guys-style candy colors
            Color[] colors = new Color[]
            {
                new Color(0.95f, 0.45f, 0.55f),  // Pink
                new Color(0.45f, 0.75f, 0.95f),  // Sky Blue
                new Color(0.55f, 0.95f, 0.65f),  // Mint
                new Color(0.95f, 0.75f, 0.35f),  // Gold
                new Color(0.75f, 0.55f, 0.95f),  // Purple
                new Color(0.95f, 0.55f, 0.35f),  // Orange
                new Color(0.45f, 0.95f, 0.85f),  // Teal
                new Color(0.95f, 0.95f, 0.45f),  // Yellow
            };

            Material[] mats = new Material[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                mats[i] = CreateMaterial($"Segment_{i}", colors[i]);
            }
            return mats;
        }

        private Material CreateMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            Material mat = new Material(shader);
            mat.name = name;
            mat.color = color;
            mat.SetFloat("_Smoothness", 0.4f);

            return mat;
        }

        private void SetCourseGeneratorMaterials(ProceduralCourseGenerator gen,
            Material[] segments, Material wall, Material finish, Material slime)
        {
            // Use reflection to set serialized fields since we're creating at runtime
            var type = gen.GetType();

            var segCountField = type.GetField("segmentCount",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            segCountField?.SetValue(gen, segmentCount);

            var matField = type.GetField("segmentMaterials",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            matField?.SetValue(gen, segments);

            var wallField = type.GetField("wallMaterial",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            wallField?.SetValue(gen, wall);

            var finishField = type.GetField("finishMaterial",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            finishField?.SetValue(gen, finish);

            var slimeField = type.GetField("slimeMaterial",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            slimeField?.SetValue(gen, slime);
        }

        private void SetGameManagerReferences(GameManager gm, ProceduralCourseGenerator gen,
            PlayerController player, GameUI ui)
        {
            var type = gm.GetType();

            var genField = type.GetField("courseGenerator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            genField?.SetValue(gm, gen);

            var playerField = type.GetField("player",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            playerField?.SetValue(gm, player);

            var uiField = type.GetField("gameUI",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            uiField?.SetValue(gm, ui);

            var seedField = type.GetField("useRandomSeed",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            seedField?.SetValue(gm, useRandomSeed);

            if (!useRandomSeed)
            {
                var fixedSeedField = type.GetField("fixedSeed",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                fixedSeedField?.SetValue(gm, fixedSeed);
            }
        }

        private GameObject CreatePlayer()
        {
            // Bean-shaped character (capsule body + sphere head)
            GameObject player = new GameObject("Player");
            player.layer = LayerMask.NameToLayer("Player");

            // Physics
            var rb = player.AddComponent<Rigidbody>();
            var capsule = player.AddComponent<CapsuleCollider>();
            capsule.height = 1.8f;
            capsule.radius = 0.4f;
            capsule.center = new Vector3(0f, 0.9f, 0f);

            // Body visual
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(player.transform);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            Destroy(body.GetComponent<Collider>()); // visual only

            var bodyMat = CreateMaterial("PlayerBody", new Color(0.3f, 0.6f, 1f));
            bodyMat.SetFloat("_Smoothness", 0.7f);
            body.GetComponent<Renderer>().material = bodyMat;

            // Eyes
            CreateEye(body.transform, new Vector3(-0.15f, 0.5f, 0.4f));
            CreateEye(body.transform, new Vector3(0.15f, 0.5f, 0.4f));

            // Controller
            var pc = player.AddComponent<PlayerController>();

            // Animation
            var animController = player.AddComponent<PlayerAnimationController>();
            var animType = animController.GetType();
            var bodyField = animType.GetField("bodyTransform",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bodyField?.SetValue(animController, body.transform);

            // Effects
            player.AddComponent<PlayerEffects>();

            return player;
        }

        private void CreateEye(Transform parent, Vector3 localPos)
        {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "Eye";
            eye.transform.SetParent(parent);
            eye.transform.localPosition = localPos;
            eye.transform.localScale = new Vector3(0.15f, 0.2f, 0.1f);
            Destroy(eye.GetComponent<Collider>());

            var eyeMat = CreateMaterial("Eye", Color.white);
            eyeMat.SetFloat("_Smoothness", 0.9f);
            eye.GetComponent<Renderer>().material = eyeMat;

            // Pupil
            GameObject pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pupil.name = "Pupil";
            pupil.transform.SetParent(eye.transform);
            pupil.transform.localPosition = new Vector3(0f, 0f, 0.3f);
            pupil.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            Destroy(pupil.GetComponent<Collider>());

            var pupilMat = CreateMaterial("Pupil", new Color(0.15f, 0.15f, 0.15f));
            pupil.GetComponent<Renderer>().material = pupilMat;
        }

        private GameObject CreateCamera(Transform target)
        {
            // Remove existing main camera if any
            if (Camera.main != null)
            {
                Destroy(Camera.main.gameObject);
            }

            GameObject camObj = new GameObject("MainCamera");
            camObj.tag = "MainCamera";

            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.72f, 0.92f); // Soft sky blue
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 500f;

            camObj.AddComponent<AudioListener>();

            var camController = camObj.AddComponent<CameraController>();
            camController.SetTarget(target);

            camObj.transform.position = target.position + new Vector3(0f, 5f, -8f);

            return camObj;
        }

        private void CreateKillZone()
        {
            GameObject killZone = new GameObject("KillZone");
            killZone.tag = "KillZone";

            var box = killZone.AddComponent<BoxCollider>();
            box.size = new Vector3(200f, 1f, 2000f);
            box.isTrigger = true;
            box.center = new Vector3(0f, -20f, 100f);

            killZone.AddComponent<Utils.KillZone>();
        }

        private void CreateBackgroundPlane()
        {
            // Large ground plane way below for visual depth
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "BackgroundGround";
            ground.transform.position = new Vector3(0f, -50f, 100f);
            ground.transform.localScale = new Vector3(50f, 1f, 50f);
            Destroy(ground.GetComponent<Collider>());

            var mat = CreateMaterial("BackgroundGround", new Color(0.45f, 0.65f, 0.4f));
            ground.GetComponent<Renderer>().material = mat;
        }
    }
}

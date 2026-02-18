using System.Collections.Generic;
using UnityEngine;
using FallBots.Obstacles;

namespace FallBots.CourseGeneration
{
    /// <summary>
    /// Generates a procedural obstacle course at runtime.
    /// Builds segments, places obstacles, and handles difficulty scaling.
    /// </summary>
    public class ProceduralCourseGenerator : MonoBehaviour
    {
        [Header("Course Settings")]
        [SerializeField] private int segmentCount = 8;
        [SerializeField] private float baseSegmentLength = 25f;
        [SerializeField] private float segmentWidth = 12f;
        [SerializeField] private float wallHeight = 4f;
        [SerializeField] private float platformThickness = 1f;

        [Header("Difficulty")]
        [SerializeField] private int startDifficulty = 1;
        [SerializeField] private int maxDifficulty = 5;
        [SerializeField] private float difficultyRampPerSegment = 0.5f;

        [Header("Visual")]
        [SerializeField] private Material[] segmentMaterials;
        [SerializeField] private Material wallMaterial;
        [SerializeField] private Material finishMaterial;
        [SerializeField] private Material slimeMaterial;

        [Header("Checkpoint")]
        [SerializeField] private int checkpointEveryNSegments = 3;

        private List<CourseSegment> generatedSegments = new List<CourseSegment>();
        private List<Vector3> checkpoints = new List<Vector3>();
        private Vector3 currentBuildPosition;
        private float currentDifficulty;

        public List<Vector3> Checkpoints => checkpoints;
        public Vector3 StartPosition => Vector3.up * 2f;
        public Vector3 FinishPosition { get; private set; }

        public void GenerateCourse(int? seed = null)
        {
            if (seed.HasValue)
                Random.InitState(seed.Value);
            else
                Random.InitState(System.Environment.TickCount);

            ClearCourse();

            currentBuildPosition = Vector3.zero;
            currentDifficulty = startDifficulty;

            // Start platform
            CreateStartPlatform();
            checkpoints.Add(currentBuildPosition + Vector3.up * 2f);

            // Generate segments
            for (int i = 0; i < segmentCount; i++)
            {
                SegmentType type = PickSegmentType(i);
                CourseSegment segment = BuildSegment(type, i);
                generatedSegments.Add(segment);

                // Checkpoints
                if ((i + 1) % checkpointEveryNSegments == 0)
                {
                    checkpoints.Add(currentBuildPosition + Vector3.up * 2f);
                    CreateCheckpointVisual(currentBuildPosition);
                }

                currentDifficulty = Mathf.Min(maxDifficulty,
                    startDifficulty + i * difficultyRampPerSegment);
            }

            // Finish line
            CreateFinishPlatform();
        }

        public void ClearCourse()
        {
            foreach (var segment in generatedSegments)
            {
                if (segment.root != null)
                    Destroy(segment.root);
            }
            generatedSegments.Clear();
            checkpoints.Clear();

            // Remove any leftover generated objects
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }

        private SegmentType PickSegmentType(int index)
        {
            // Weight selection based on difficulty
            float roll = Random.value;

            if (currentDifficulty < 2)
            {
                if (roll < 0.3f) return SegmentType.Straight;
                if (roll < 0.5f) return SegmentType.Ramp;
                if (roll < 0.7f) return SegmentType.Platform;
                return SegmentType.SlidingFloor;
            }
            else if (currentDifficulty < 4)
            {
                if (roll < 0.2f) return SegmentType.Straight;
                if (roll < 0.35f) return SegmentType.NarrowBridge;
                if (roll < 0.5f) return SegmentType.Gauntlet;
                if (roll < 0.65f) return SegmentType.Ramp;
                if (roll < 0.8f) return SegmentType.TumblingBlocks;
                return SegmentType.Platform;
            }
            else
            {
                if (roll < 0.2f) return SegmentType.NarrowBridge;
                if (roll < 0.4f) return SegmentType.Gauntlet;
                if (roll < 0.6f) return SegmentType.TumblingBlocks;
                if (roll < 0.8f) return SegmentType.SlidingFloor;
                return SegmentType.Ramp;
            }
        }

        private CourseSegment BuildSegment(SegmentType type, int index)
        {
            GameObject root = new GameObject($"Segment_{index}_{type}");
            root.transform.SetParent(transform);
            root.transform.position = currentBuildPosition;

            CourseSegment segment = new CourseSegment
            {
                root = root,
                type = type,
                startPosition = currentBuildPosition
            };

            float length = baseSegmentLength + Random.Range(-3f, 5f);
            float width = segmentWidth;

            Material mat = segmentMaterials != null && segmentMaterials.Length > 0
                ? segmentMaterials[index % segmentMaterials.Length]
                : null;

            switch (type)
            {
                case SegmentType.Straight:
                    BuildStraightSegment(root, length, width, mat);
                    PopulateObstacles(root, length, width, type);
                    break;

                case SegmentType.Ramp:
                    float rampHeight = Random.Range(2f, 5f) * (Random.value > 0.5f ? 1f : -1f);
                    BuildRampSegment(root, length, width, rampHeight, mat);
                    currentBuildPosition.y += rampHeight;
                    break;

                case SegmentType.NarrowBridge:
                    float bridgeWidth = Mathf.Lerp(6f, 3f, currentDifficulty / maxDifficulty);
                    BuildStraightSegment(root, length, bridgeWidth, mat);
                    PopulateObstacles(root, length, bridgeWidth, type);
                    break;

                case SegmentType.Platform:
                    BuildPlatformSegment(root, length, width, mat);
                    break;

                case SegmentType.Gauntlet:
                    BuildStraightSegment(root, length * 1.3f, width, mat);
                    PopulateGauntlet(root, length * 1.3f, width);
                    length *= 1.3f;
                    break;

                case SegmentType.SlidingFloor:
                    BuildSlidingFloorSegment(root, length, width, mat);
                    break;

                case SegmentType.TumblingBlocks:
                    BuildTumblingBlocksSegment(root, length, width, mat);
                    break;
            }

            currentBuildPosition += Vector3.forward * length;
            segment.endPosition = currentBuildPosition;

            return segment;
        }

        #region Segment Builders

        private void BuildStraightSegment(GameObject root, float length, float width, Material mat)
        {
            // Floor
            GameObject floor = CreatePlatform(root.transform, Vector3.zero,
                new Vector3(width, platformThickness, length), mat);

            // Walls
            if (width >= segmentWidth * 0.7f)
            {
                CreateWall(root.transform, new Vector3(-width / 2f - 0.25f, wallHeight / 2f, length / 2f),
                    new Vector3(0.5f, wallHeight, length));
                CreateWall(root.transform, new Vector3(width / 2f + 0.25f, wallHeight / 2f, length / 2f),
                    new Vector3(0.5f, wallHeight, length));
            }
        }

        private void BuildRampSegment(GameObject root, float length, float width, float height, Material mat)
        {
            // Create angled platform
            GameObject ramp = CreatePlatform(root.transform,
                new Vector3(0f, height / 2f, length / 2f),
                new Vector3(width, platformThickness, length), mat);

            float angle = Mathf.Atan2(height, length) * Mathf.Rad2Deg;
            ramp.transform.localRotation = Quaternion.Euler(angle, 0f, 0f);
            // Adjust position after rotation
            ramp.transform.localPosition = new Vector3(0f, height / 2f, length / 2f);

            // Walls along ramp
            CreateWall(root.transform, new Vector3(-width / 2f - 0.25f, height / 2f + wallHeight / 2f, length / 2f),
                new Vector3(0.5f, wallHeight + Mathf.Abs(height), length));
            CreateWall(root.transform, new Vector3(width / 2f + 0.25f, height / 2f + wallHeight / 2f, length / 2f),
                new Vector3(0.5f, wallHeight + Mathf.Abs(height), length));
        }

        private void BuildPlatformSegment(GameObject root, float length, float width, Material mat)
        {
            int platformCount = Random.Range(3, 6);
            float spacing = length / platformCount;
            float gapSize = Random.Range(1.5f, 3f);

            for (int i = 0; i < platformCount; i++)
            {
                float platformWidth = Random.Range(3f, width * 0.6f);
                float xOffset = Random.Range(-width * 0.3f, width * 0.3f);
                float zPos = i * spacing + spacing * 0.5f;
                float yOffset = Random.Range(-0.5f, 1.5f);

                GameObject platform = CreatePlatform(root.transform,
                    new Vector3(xOffset, yOffset, zPos),
                    new Vector3(platformWidth, platformThickness, spacing - gapSize), mat);

                // Some platforms move
                if (Random.value > 0.5f && currentDifficulty > 1.5f)
                {
                    var mover = platform.AddComponent<MovingPlatform>();
                    mover.SetMovement(
                        new Vector3(Random.Range(-2f, 2f), Random.Range(-0.5f, 0.5f), 0f),
                        Random.Range(1.5f, 3f)
                    );
                }
            }
        }

        private void BuildSlidingFloorSegment(GameObject root, float length, float width, Material mat)
        {
            // Main floor
            BuildStraightSegment(root, length, width, mat);

            // Add slime zones
            int slimeCount = Random.Range(2, 4);
            for (int i = 0; i < slimeCount; i++)
            {
                float zPos = Random.Range(2f, length - 2f);
                float slimeLength = Random.Range(3f, 6f);
                float slimeWidth = Random.Range(width * 0.4f, width * 0.8f);
                float xPos = Random.Range(-width * 0.2f, width * 0.2f);

                GameObject slime = CreatePlatform(root.transform,
                    new Vector3(xPos, platformThickness * 0.5f + 0.02f, zPos),
                    new Vector3(slimeWidth, 0.05f, slimeLength),
                    slimeMaterial);
                slime.name = "SlimeZone";
                slime.tag = "SlimeZone";
                slime.layer = LayerMask.NameToLayer("Trigger");

                var collider = slime.GetComponent<BoxCollider>();
                if (collider != null) collider.isTrigger = true;

                slime.AddComponent<SlimeZone>();
            }

            PopulateObstacles(root, length, width, SegmentType.SlidingFloor);
        }

        private void BuildTumblingBlocksSegment(GameObject root, float length, float width, Material mat)
        {
            BuildStraightSegment(root, length, width, mat);

            int blockCount = Mathf.CeilToInt(currentDifficulty) + 1;
            for (int i = 0; i < blockCount; i++)
            {
                float zPos = Random.Range(3f, length - 3f);
                float xPos = Random.Range(-width * 0.3f, width * 0.3f);
                float size = Random.Range(1.5f, 3f);

                GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.transform.SetParent(root.transform);
                block.transform.localPosition = new Vector3(xPos, size * 3f, zPos);
                block.transform.localScale = new Vector3(size, size, size);
                block.name = "FallingBlock";

                if (mat != null)
                {
                    var renderer = block.GetComponent<Renderer>();
                    renderer.material = mat;
                    renderer.material.color = new Color(0.9f, 0.3f, 0.3f, 0.9f);
                }

                var fallingTile = block.AddComponent<FallingTile>();
                fallingTile.SetParameters(1.5f, 3f, true);
            }
        }

        #endregion

        #region Obstacle Population

        private void PopulateObstacles(GameObject root, float length, float width, SegmentType type)
        {
            int obstacleCount = Mathf.CeilToInt(currentDifficulty) + Random.Range(0, 2);

            for (int i = 0; i < obstacleCount; i++)
            {
                float zPos = Random.Range(3f, length - 3f);
                float xPos = Random.Range(-width * 0.35f, width * 0.35f);
                Vector3 pos = new Vector3(xPos, 0f, zPos);

                ObstacleType obsType = PickObstacleType(type);
                CreateObstacle(root.transform, pos, obsType, width);
            }
        }

        private void PopulateGauntlet(GameObject root, float length, float width)
        {
            // Dense obstacle placement
            int obstacleCount = Mathf.CeilToInt(currentDifficulty * 2f) + 2;
            float spacing = length / (obstacleCount + 1);

            for (int i = 0; i < obstacleCount; i++)
            {
                float zPos = spacing * (i + 1);
                float xPos = Random.Range(-width * 0.3f, width * 0.3f);

                ObstacleType obsType;
                float roll = Random.value;
                if (roll < 0.3f) obsType = ObstacleType.SpinningBar;
                else if (roll < 0.5f) obsType = ObstacleType.Pendulum;
                else if (roll < 0.7f) obsType = ObstacleType.Bumper;
                else if (roll < 0.85f) obsType = ObstacleType.PunchWall;
                else obsType = ObstacleType.Roller;

                CreateObstacle(root.transform, new Vector3(xPos, 0f, zPos), obsType, width);
            }
        }

        private ObstacleType PickObstacleType(SegmentType segType)
        {
            float roll = Random.value;
            switch (segType)
            {
                case SegmentType.Straight:
                    if (roll < 0.3f) return ObstacleType.SpinningBar;
                    if (roll < 0.5f) return ObstacleType.Bumper;
                    if (roll < 0.7f) return ObstacleType.Pendulum;
                    return ObstacleType.Windmill;

                case SegmentType.NarrowBridge:
                    if (roll < 0.4f) return ObstacleType.Pendulum;
                    if (roll < 0.7f) return ObstacleType.Windmill;
                    return ObstacleType.Bumper;

                case SegmentType.SlidingFloor:
                    if (roll < 0.3f) return ObstacleType.SpinningBar;
                    if (roll < 0.6f) return ObstacleType.Bumper;
                    return ObstacleType.Roller;

                default:
                    if (roll < 0.25f) return ObstacleType.SpinningBar;
                    if (roll < 0.5f) return ObstacleType.Pendulum;
                    if (roll < 0.75f) return ObstacleType.Bumper;
                    return ObstacleType.Launcher;
            }
        }

        private void CreateObstacle(Transform parent, Vector3 localPos, ObstacleType type, float segWidth)
        {
            GameObject obs;

            switch (type)
            {
                case ObstacleType.SpinningBar:
                    obs = CreateSpinningBar(parent, localPos, segWidth);
                    break;
                case ObstacleType.Pendulum:
                    obs = CreatePendulum(parent, localPos);
                    break;
                case ObstacleType.Bumper:
                    obs = CreateBumper(parent, localPos);
                    break;
                case ObstacleType.Windmill:
                    obs = CreateWindmill(parent, localPos);
                    break;
                case ObstacleType.PunchWall:
                    obs = CreatePunchWall(parent, localPos, segWidth);
                    break;
                case ObstacleType.Roller:
                    obs = CreateRoller(parent, localPos, segWidth);
                    break;
                case ObstacleType.Launcher:
                    obs = CreateLauncher(parent, localPos);
                    break;
                default:
                    obs = CreateBumper(parent, localPos);
                    break;
            }

            obs.tag = "Obstacle";
        }

        #endregion

        #region Obstacle Creators

        private GameObject CreateSpinningBar(Transform parent, Vector3 pos, float width)
        {
            GameObject pivot = new GameObject("SpinningBar");
            pivot.transform.SetParent(parent);
            pivot.transform.localPosition = pos + Vector3.up * 1.2f;

            // Bar
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.transform.SetParent(pivot.transform);
            bar.transform.localPosition = Vector3.zero;
            float barLength = Mathf.Min(width * 0.8f, 8f);
            bar.transform.localScale = new Vector3(barLength, 0.6f, 0.6f);
            bar.name = "Bar";

            // Pillar
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.transform.SetParent(pivot.transform);
            pillar.transform.localPosition = Vector3.down * 0.6f;
            pillar.transform.localScale = new Vector3(0.5f, 0.6f, 0.5f);

            var spinner = pivot.AddComponent<SpinningObstacle>();
            float speed = 50f + currentDifficulty * 20f;
            spinner.SetParameters(Vector3.up, speed);
            spinner.knockbackForce = 8f + currentDifficulty;

            ColorObstacle(bar, new Color(1f, 0.3f, 0.2f));
            ColorObstacle(pillar, new Color(0.4f, 0.4f, 0.4f));

            return pivot;
        }

        private GameObject CreatePendulum(Transform parent, Vector3 pos)
        {
            GameObject pivot = new GameObject("Pendulum");
            pivot.transform.SetParent(parent);
            pivot.transform.localPosition = pos + Vector3.up * 6f;

            // Arm
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.transform.SetParent(pivot.transform);
            arm.transform.localPosition = Vector3.down * 2f;
            arm.transform.localScale = new Vector3(0.2f, 4f, 0.2f);

            // Ball
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.transform.SetParent(pivot.transform);
            ball.transform.localPosition = Vector3.down * 4.5f;
            ball.transform.localScale = Vector3.one * 2f;

            var pendulumComp = pivot.AddComponent<PendulumObstacle>();
            float speed = 1.5f + currentDifficulty * 0.3f;
            float angle = 40f + currentDifficulty * 5f;
            pendulumComp.SetParameters(speed, angle);
            pendulumComp.knockbackForce = 10f + currentDifficulty * 2f;

            ColorObstacle(arm, new Color(0.5f, 0.5f, 0.5f));
            ColorObstacle(ball, new Color(1f, 0.6f, 0.1f));

            return pivot;
        }

        private GameObject CreateBumper(Transform parent, Vector3 pos)
        {
            GameObject bumper = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bumper.transform.SetParent(parent);
            bumper.transform.localPosition = pos + Vector3.up * 0.75f;
            bumper.transform.localScale = new Vector3(1.5f, 0.75f, 1.5f);
            bumper.name = "Bumper";

            var bumperComp = bumper.AddComponent<BumperObstacle>();
            bumperComp.bounceForce = 12f + currentDifficulty * 2f;

            ColorObstacle(bumper, new Color(1f, 0.2f, 0.6f));

            return bumper;
        }

        private GameObject CreateWindmill(Transform parent, Vector3 pos)
        {
            GameObject pivot = new GameObject("Windmill");
            pivot.transform.SetParent(parent);
            pivot.transform.localPosition = pos + Vector3.up * 2f;

            // Four blades
            for (int i = 0; i < 4; i++)
            {
                GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.transform.SetParent(pivot.transform);
                blade.transform.localRotation = Quaternion.Euler(0f, 0f, i * 90f);
                blade.transform.localPosition = blade.transform.up * 2f;
                blade.transform.localScale = new Vector3(0.8f, 3.5f, 0.5f);
                blade.name = $"Blade_{i}";

                ColorObstacle(blade, new Color(0.2f, 0.8f, 1f));
            }

            // Center
            GameObject center = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            center.transform.SetParent(pivot.transform);
            center.transform.localPosition = Vector3.zero;
            center.transform.localScale = Vector3.one * 0.8f;
            ColorObstacle(center, new Color(0.3f, 0.3f, 0.3f));

            var spinner = pivot.AddComponent<SpinningObstacle>();
            spinner.SetParameters(Vector3.forward, 60f + currentDifficulty * 15f);
            spinner.knockbackForce = 10f;

            return pivot;
        }

        private GameObject CreatePunchWall(Transform parent, Vector3 pos, float segWidth)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(parent);
            wall.transform.localPosition = pos;
            wall.transform.localScale = new Vector3(segWidth * 0.4f, 3f, 0.8f);
            wall.name = "PunchWall";

            var mover = wall.AddComponent<MovingPlatform>();
            mover.SetMovement(
                new Vector3(segWidth * 0.3f, 0f, 0f),
                1f + currentDifficulty * 0.2f
            );
            mover.knockbackOnHit = true;
            mover.knockbackForce = 8f + currentDifficulty * 2f;

            ColorObstacle(wall, new Color(0.8f, 0.2f, 0.2f));

            return wall;
        }

        private GameObject CreateRoller(Transform parent, Vector3 pos, float segWidth)
        {
            GameObject roller = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            roller.transform.SetParent(parent);
            roller.transform.localPosition = pos + Vector3.up * 1f;
            roller.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            roller.transform.localScale = new Vector3(2f, segWidth * 0.4f, 2f);
            roller.name = "Roller";

            var spinner = roller.AddComponent<SpinningObstacle>();
            spinner.SetParameters(Vector3.up, 80f + currentDifficulty * 10f);
            spinner.knockbackForce = 6f;
            spinner.isRoller = true;

            ColorObstacle(roller, new Color(0.3f, 0.9f, 0.3f));

            return roller;
        }

        private GameObject CreateLauncher(Transform parent, Vector3 pos)
        {
            GameObject launcher = GameObject.CreatePrimitive(PrimitiveType.Cube);
            launcher.transform.SetParent(parent);
            launcher.transform.localPosition = pos + Vector3.up * 0.25f;
            launcher.transform.localScale = new Vector3(2f, 0.5f, 2f);
            launcher.name = "Launcher";

            var launcherComp = launcher.AddComponent<LauncherObstacle>();
            launcherComp.launchForce = 15f + currentDifficulty * 3f;

            ColorObstacle(launcher, new Color(1f, 1f, 0.2f));

            return launcher;
        }

        #endregion

        #region Utility

        private GameObject CreatePlatform(Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.transform.SetParent(parent);
            platform.transform.localPosition = localPos;
            platform.transform.localScale = scale;
            platform.name = "Platform";
            platform.layer = LayerMask.NameToLayer("Ground");

            if (mat != null)
            {
                platform.GetComponent<Renderer>().material = mat;
            }

            return platform;
        }

        private void CreateWall(Transform parent, Vector3 localPos, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(parent);
            wall.transform.localPosition = localPos;
            wall.transform.localScale = scale;
            wall.name = "Wall";

            if (wallMaterial != null)
                wall.GetComponent<Renderer>().material = wallMaterial;
            else
                ColorObstacle(wall, new Color(0.7f, 0.7f, 0.8f, 0.5f));
        }

        private void CreateStartPlatform()
        {
            GameObject start = new GameObject("StartPlatform");
            start.transform.SetParent(transform);
            start.transform.position = currentBuildPosition;

            CreatePlatform(start.transform, new Vector3(0, 0, 5f),
                new Vector3(segmentWidth * 1.5f, platformThickness, 10f), null);

            // Start walls
            CreateWall(start.transform, new Vector3(-segmentWidth * 0.75f - 0.25f, 2f, 5f),
                new Vector3(0.5f, 4f, 10f));
            CreateWall(start.transform, new Vector3(segmentWidth * 0.75f + 0.25f, 2f, 5f),
                new Vector3(0.5f, 4f, 10f));

            // Starting arch decoration
            GameObject arch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arch.transform.SetParent(start.transform);
            arch.transform.localPosition = new Vector3(0f, 5f, 1f);
            arch.transform.localScale = new Vector3(segmentWidth * 1.5f, 1f, 0.5f);
            ColorObstacle(arch, new Color(0.2f, 0.8f, 0.3f));

            currentBuildPosition += Vector3.forward * 10f;
        }

        private void CreateFinishPlatform()
        {
            GameObject finish = new GameObject("FinishPlatform");
            finish.transform.SetParent(transform);
            finish.transform.position = currentBuildPosition;

            GameObject floor = CreatePlatform(finish.transform, new Vector3(0, 0, 5f),
                new Vector3(segmentWidth * 1.5f, platformThickness, 10f),
                finishMaterial);

            if (finishMaterial == null)
                ColorObstacle(floor, new Color(1f, 0.85f, 0f));

            // Finish line trigger
            GameObject trigger = new GameObject("FinishTrigger");
            trigger.transform.SetParent(finish.transform);
            trigger.transform.localPosition = new Vector3(0f, 2f, 5f);
            trigger.tag = "FinishLine";

            var box = trigger.AddComponent<BoxCollider>();
            box.size = new Vector3(segmentWidth * 1.5f, 4f, 1f);
            box.isTrigger = true;

            // Finish arch
            GameObject finishArch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            finishArch.transform.SetParent(finish.transform);
            finishArch.transform.localPosition = new Vector3(0f, 5f, 5f);
            finishArch.transform.localScale = new Vector3(segmentWidth * 1.5f, 1f, 0.5f);
            ColorObstacle(finishArch, new Color(1f, 0.84f, 0f));

            // Pillars
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.transform.SetParent(finish.transform);
                pillar.transform.localPosition = new Vector3(side * segmentWidth * 0.75f, 2.5f, 5f);
                pillar.transform.localScale = new Vector3(0.6f, 2.5f, 0.6f);
                ColorObstacle(pillar, new Color(1f, 0.84f, 0f));
            }

            FinishPosition = currentBuildPosition + new Vector3(0f, 2f, 5f);
        }

        private void CreateCheckpointVisual(Vector3 position)
        {
            GameObject checkpoint = new GameObject("Checkpoint");
            checkpoint.transform.SetParent(transform);
            checkpoint.transform.position = position;
            checkpoint.tag = "Checkpoint";

            // Visual indicator - a ring
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.transform.SetParent(checkpoint.transform);
            ring.transform.localPosition = Vector3.up * 3f;
            ring.transform.localScale = new Vector3(3f, 0.1f, 3f);
            ColorObstacle(ring, new Color(0.2f, 0.6f, 1f));

            // Checkpoint trigger
            var box = checkpoint.AddComponent<BoxCollider>();
            box.size = new Vector3(segmentWidth, 4f, 2f);
            box.isTrigger = true;
        }

        private void ColorObstacle(GameObject obj, Color color)
        {
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                renderer.material.color = color;
            }
        }

        #endregion
    }

    public class CourseSegment
    {
        public GameObject root;
        public SegmentType type;
        public Vector3 startPosition;
        public Vector3 endPosition;
    }
}

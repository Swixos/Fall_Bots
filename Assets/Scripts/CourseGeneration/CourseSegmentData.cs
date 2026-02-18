using UnityEngine;

namespace FallBots.CourseGeneration
{
    /// <summary>
    /// Defines a course segment type with its properties.
    /// Used by the procedural generator to assemble courses.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSegment", menuName = "FallBots/Course Segment Data")]
    public class CourseSegmentData : ScriptableObject
    {
        public string segmentName;
        public SegmentType type;
        public int difficultyMin = 1;
        public int difficultyMax = 5;

        [Tooltip("Length of this segment along the Z axis")]
        public float length = 20f;

        [Tooltip("Width of the platform")]
        public float width = 10f;

        [Tooltip("Height offset relative to previous segment end")]
        public float heightOffset = 0f;

        [Tooltip("Obstacle prefabs to spawn in this segment")]
        public ObstacleSpawnInfo[] obstacles;

        [Tooltip("Colors for this segment's platform")]
        public Color primaryColor = Color.white;
        public Color secondaryColor = Color.gray;
    }

    public enum SegmentType
    {
        Straight,
        Ramp,
        NarrowBridge,
        Platform,
        Gauntlet,
        SlidingFloor,
        TumblingBlocks,
        FinishLine
    }

    [System.Serializable]
    public class ObstacleSpawnInfo
    {
        public ObstacleType obstacleType;
        public int minCount = 1;
        public int maxCount = 3;
        public float yOffset = 0f;
        public bool randomizePosition = true;
    }

    public enum ObstacleType
    {
        SpinningBar,
        MovingPlatform,
        Pendulum,
        SlimeZone,
        FallingTile,
        Bumper,
        Windmill,
        PunchWall,
        Roller,
        Launcher
    }
}

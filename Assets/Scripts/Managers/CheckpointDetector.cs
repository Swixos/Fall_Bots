using UnityEngine;
using FallBots.Player;

namespace FallBots.Managers
{
    /// <summary>
    /// Detects when the player reaches a checkpoint.
    /// </summary>
    public class CheckpointDetector : MonoBehaviour
    {
        [SerializeField] private int checkpointIndex;

        public void SetIndex(int index)
        {
            checkpointIndex = index;
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnCheckpointReached(checkpointIndex);
                }
            }
        }
    }
}

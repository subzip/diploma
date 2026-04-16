using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class CheckpointTrigger : MonoBehaviour
{
    [Header("Checkpoint")]
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private bool oneTime = true;
    [SerializeField] private string checkpointId = "checkpoint";

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private bool consumed;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryActivate(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryActivate(other);
    }

    private void TryActivate(Collider other)
    {
        if (consumed) return;
        if (other == null) return;
        if (!other.CompareTag("Player") && !other.transform.root.CompareTag("Player")) return;

        Transform point = respawnPoint != null ? respawnPoint : transform;
        string scene = SceneManager.GetActiveScene().name;
        RespawnCheckpointState.SetCheckpoint(scene, point.position, point.rotation, checkpointId);

        if (debugLog)
        {
            Debug.Log($"[Checkpoint] Activated '{checkpointId}' in scene '{scene}' at {point.position}");
        }

        if (oneTime)
        {
            consumed = true;
        }
    }
}

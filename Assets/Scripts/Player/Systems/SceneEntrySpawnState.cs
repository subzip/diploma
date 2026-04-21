using UnityEngine;

public static class SceneEntrySpawnState
{
    private static bool hasPending;
    private static string targetScene;
    private static Vector3 position;
    private static Quaternion rotation = Quaternion.identity;
    private static bool registerCheckpoint;
    private static string checkpointId;

    public static void SetPending(string sceneName, Vector3 worldPosition, Quaternion worldRotation, bool shouldRegisterCheckpoint, string entryCheckpointId)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return;

        targetScene = sceneName;
        position = worldPosition;
        rotation = worldRotation;
        registerCheckpoint = shouldRegisterCheckpoint;
        checkpointId = string.IsNullOrWhiteSpace(entryCheckpointId) ? "scene_entry" : entryCheckpointId;
        hasPending = true;
    }

    public static bool TryConsume(string sceneName, out Vector3 worldPosition, out Quaternion worldRotation, out bool shouldRegisterCheckpoint, out string entryCheckpointId)
    {
        if (!hasPending || !string.Equals(targetScene, sceneName, System.StringComparison.Ordinal))
        {
            worldPosition = default;
            worldRotation = Quaternion.identity;
            shouldRegisterCheckpoint = false;
            entryCheckpointId = string.Empty;
            return false;
        }

        worldPosition = position;
        worldRotation = rotation;
        shouldRegisterCheckpoint = registerCheckpoint;
        entryCheckpointId = checkpointId;
        Clear();
        return true;
    }

    public static void Clear()
    {
        hasPending = false;
        targetScene = string.Empty;
        position = Vector3.zero;
        rotation = Quaternion.identity;
        registerCheckpoint = false;
        checkpointId = string.Empty;
    }
}

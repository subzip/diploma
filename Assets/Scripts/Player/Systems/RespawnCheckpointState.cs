using UnityEngine;

public static class RespawnCheckpointState
{
    private static bool hasCheckpoint;
    private static string sceneName;
    private static string checkpointId;
    private static Vector3 position;
    private static Quaternion rotation = Quaternion.identity;

    public static bool HasCheckpoint => hasCheckpoint;

    public static void SetCheckpoint(string scene, Vector3 worldPosition, Quaternion worldRotation, string id = "")
    {
        if (string.IsNullOrEmpty(scene)) return;

        sceneName = scene;
        checkpointId = string.IsNullOrWhiteSpace(id) ? scene : id;
        position = worldPosition;
        rotation = worldRotation;
        hasCheckpoint = true;
    }

    public static bool TryGetCheckpoint(string scene, out Vector3 worldPosition, out Quaternion worldRotation)
    {
        if (hasCheckpoint && sceneName == scene)
        {
            worldPosition = position;
            worldRotation = rotation;
            return true;
        }

        worldPosition = default;
        worldRotation = Quaternion.identity;
        return false;
    }

    public static void RegisterSceneStartIfMissing(string scene, Vector3 worldPosition, Quaternion worldRotation)
    {
        if (!hasCheckpoint || sceneName != scene)
        {
            SetCheckpoint(scene, worldPosition, worldRotation, "scene_start");
        }
    }

    public static string GetCheckpointId(string scene)
    {
        if (!hasCheckpoint || sceneName != scene) return string.Empty;
        return checkpointId;
    }

    public static void Clear()
    {
        hasCheckpoint = false;
        sceneName = string.Empty;
        checkpointId = string.Empty;
        position = Vector3.zero;
        rotation = Quaternion.identity;
    }
}

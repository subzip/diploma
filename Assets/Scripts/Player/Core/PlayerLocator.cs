using UnityEngine;

public static class PlayerLocator
{
    private static Transform cachedPlayer;

    public static Transform GetPlayerTransform(bool forceRefresh = false)
    {
        if (!forceRefresh && cachedPlayer != null) return cachedPlayer;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        cachedPlayer = playerObj != null ? playerObj.transform : null;
        return cachedPlayer;
    }
}

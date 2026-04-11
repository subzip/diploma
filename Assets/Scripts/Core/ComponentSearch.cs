using UnityEngine;

public static class ComponentSearch
{
    public static T FindInHierarchy<T>(Component source) where T : Component
    {
        if (source == null) return null;

        return source.GetComponent<T>()
               ?? source.GetComponentInParent<T>()
               ?? source.GetComponentInChildren<T>(true);
    }

    public static bool IsPlayer(Component source)
    {
        if (source == null) return false;
        return source.CompareTag("Player") || source.transform.root.CompareTag("Player");
    }
}

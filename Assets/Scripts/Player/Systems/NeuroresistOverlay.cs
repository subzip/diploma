
using UnityEngine;

public class NeuroresistOverlay : MonoBehaviour
{
    [SerializeField] private GameObject silhouettePrefab;

    private GameObject[] activeSilhouettes = new GameObject[50];
    private int silhouetteCount = 0;

    public void ShowEnemiesInRadius(Vector3 origin, float radius, LayerMask enemyLayer)
    {
        Collider[] hits = Physics.OverlapSphere(origin, radius, enemyLayer);
        silhouetteCount = 0;

        foreach (Collider col in hits)
        {
            if (silhouetteCount >= activeSilhouettes.Length) break;

            
            GameObject sil = Instantiate(silhouettePrefab, col.transform.position, col.transform.rotation);
            sil.transform.localScale = col.transform.lossyScale;
            activeSilhouettes[silhouetteCount] = sil;
            silhouetteCount++;
        }
    }

    public void HideAllEnemies()
    {
        for (int i = 0; i < silhouetteCount; i++)
        {
            if (activeSilhouettes[i] != null)
                Destroy(activeSilhouettes[i]);
        }
        silhouetteCount = 0;
    }
}
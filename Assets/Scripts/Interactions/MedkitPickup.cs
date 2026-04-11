using UnityEngine;

public class MedkitPickup : PulsingPickupHighlight
{
    [Header("Heal")]
    [SerializeField] private float healAmount = 50f;

    protected override void Awake()
    {
        emissionColor = new Color(0.8f, 0.1f, 0.1f);
        base.Awake();
    }

    private void OnTriggerEnter(Collider other) => TryConsume(other);
    private void OnTriggerStay(Collider other) => TryConsume(other);

    private void TryConsume(Component other)
    {
        if (consumed) return;
        if (other == null) return;
        if (!ComponentSearch.IsPlayer(other)) return;

        PlayerHealth health = ComponentSearch.FindInHierarchy<PlayerHealth>(other);
        if (health == null) return;
        if (!health.TryHeal(healAmount)) return;

        ConsumeAndDisableHighlight();
        Destroy(gameObject);
    }
}

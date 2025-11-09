using UnityEngine;

public class MockWeapon : MonoBehaviour
{
    public int currentAmmo = 1;
    public float damage = 25f;
    //public MockDamageable testTarget;

    public void Shoot()
    {
        if (currentAmmo <= 0) return;
        currentAmmo--;
        //testTarget?.TakeDamage(damage, Vector3.zero);
    }
}

// public class MockDamageable : MonoBehaviour, IDamageable
// {
//     public float damageReceived = 0f;
//     public void TakeDamage(float damage, Vector3 hitPoint) => damageReceived += damage;
// }

// public class MockHealth : MonoBehaviour, IDamageable
// {
//     public float currentHealth = 100f;
//     public void TakeDamage(float damage, Vector3 hitPoint) => currentHealth -= damage;
// }

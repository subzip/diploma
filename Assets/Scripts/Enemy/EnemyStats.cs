// Assets/Scripts/Enemies/EnemyStats.cs
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStats", menuName = "Enemies/Enemy Stats", order = 1)]
public class EnemyStats : ScriptableObject
{
    [Header("General")]
    public string enemyName = "Stigmar";
    public int maxHealth = 100;
    public float deathDelay = 5f; // Задержка перед удалением

    [Header("Visuals")]
    public GameObject deathEffectPrefab; // Взрыв, кровь и т.д.
    public AudioClip deathSound;
}
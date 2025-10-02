using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Enemy))]
public class EnemyDetection : MonoBehaviour
{
    public Transform player; // Сюда передаем игрока
    public float viewDistance = 10f; // Дистанция обзора
    
    private NavMeshAgent agent;
    
    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }
    
    private void Update()
    {
        if (player == null) return;
        
        // Проверяем расстояние до игрока
        float distance = Vector3.Distance(transform.position, player.position);
        
        if (distance <= viewDistance)
        {
            // Проверяем, видим ли мы игрока
            if (CanSeePlayer())
            {
                // Если видим - идем к игроку
                agent.SetDestination(player.position);
            }
            else
            {
                // Если не видим - стоим на месте
                agent.isStopped = true;
            }
        }
    }
    
    private bool CanSeePlayer()
    {
        // Пускаем луч от головы врага к игроку
        Vector3 rayOrigin = transform.position + Vector3.up * 1.5f;
        Vector3 rayDirection = player.position - rayOrigin;
        
        // Проверяем, нет ли препятствий между нами и игроком
        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, viewDistance))
        {
            // Если луч попал в игрока - мы его видим
            return hit.transform == player;
        }
        
        return false;
    }
}
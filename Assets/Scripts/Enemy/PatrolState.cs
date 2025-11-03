using UnityEngine;
using System.Linq;

public class PatrolState : EnemyStateBase
{
    private Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private float waitTime = 0f;
    private float waitDuration = 2f;      // Время остановки в точке
    private bool isWaiting = false;

    public PatrolState(EnemyBase enemy) : base(enemy)
    {
        // Получаем точки патрулирования (например, из дочерних объектов)
        waypoints = enemy.GetComponentsInChildren<Transform>()
                         .Where(t => t.CompareTag("PatrolPoint"))
                         .ToArray();

        if (waypoints.Length == 0)
        {
            Debug.LogWarning($"[{enemy.name}] PatrolState: не найдены точки патрулирования с тегом 'PatrolPoint'");
        }
    }

    public override void OnEnter()
    {
        if (enemy.animator != null)
            enemy.animator.SetBool("IsMoving", true);

        if (agent != null)
        {
            agent.speed = enemy.patrolSpeed;
            agent.isStopped = false;
        }

        isWaiting = false;
        waitTime = 0f;

        if (waypoints.Length > 0)
        {
            SetDestinationToNextWaypoint();
        }
    }

    public override void OnUpdate()
    {
        // Если стоим и ждем
        if (isWaiting)
        {
            waitTime += Time.deltaTime;
            if (waitTime >= waitDuration)
            {
                isWaiting = false;
                SetDestinationToNextWaypoint();
            }
            return;
        }

        // Если идем к точке
        if (agent != null && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            // Достигли точки — останавливаемся
            agent.isStopped = true;
            if (enemy.animator != null)
                enemy.animator.SetBool("IsMoving", false);

            isWaiting = true;
            waitTime = 0f;

            // "Оглядываемся" — поворачиваем в стороны
            LookAround();
        }
    }

    public override void OnExit()
    {
        if (agent != null)
        {
            agent.isStopped = false;
        }
        if (enemy.animator != null)
        {
            enemy.animator.SetBool("IsMoving", false);
        }
    }

    private void SetDestinationToNextWaypoint()
    {
        if (waypoints.Length == 0) return;

        Transform target = waypoints[currentWaypointIndex];
        agent.SetDestination(target.position);
        agent.isStopped = false;
        if (enemy.animator != null)
            enemy.animator.SetBool("IsMoving", true);

        // ПРАВИЛЬНО: зацикливаем индекс
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }

    private void LookAround()
    {
        // Пример: поворачиваем влево и вправо
        // Это можно реализовать через анимацию или корутину
        // Для простоты — просто поворачиваем тело на 45 градусов в обе стороны
        // Это можно реализовать через анимацию или скрипт поворота
        // enemy.StartCoroutine(LookAroundCoroutine());
    }
}
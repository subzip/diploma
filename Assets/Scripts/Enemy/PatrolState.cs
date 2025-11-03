using UnityEngine;

public class PatrolState : EnemyStateBase
{
    private Transform[] waypoints;         // Точки патрулирования
    private int currentWaypointIndex = 0;
    private float waitTime = 0f;
    private float waitDuration = 1f;      // Время остановки в точке
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
        enemy.animator.SetBool("IsMoving", true);
        agent.speed = enemy.patrolSpeed;
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
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            // Достигли точки — останавливаемся
            agent.isStopped = true;
            enemy.animator.SetBool("IsMoving", false);
            isWaiting = true;
            waitTime = 0f;

            // "Оглядываемся" — поворачиваем в стороны
            LookAround();
        }

        // Проверяем зрение и слух (реакция на игрока)
        enemy.UpdateVision();
        // Слух проверяется извне через HearSound()
    }

    public override void OnExit()
    {
        agent.isStopped = false;
        enemy.animator.SetBool("IsMoving", false);
    }

    private void SetDestinationToNextWaypoint()
    {
        if (waypoints.Length == 0) return;

        Transform target = waypoints[currentWaypointIndex];
        agent.SetDestination(target.position);
        agent.isStopped = false;
        enemy.animator.SetBool("IsMoving", true);

        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }

    private void LookAround()
    {
        // Пример: поворачиваем влево и вправо
        // Это можно сделать через анимацию или корутину
        // Для простоты — просто поворачиваем тело на 45 градусов в обе стороны

        // Это можно реализовать через анимацию или скрипт поворота
        // enemy.StartCoroutine(LookAroundCoroutine());
    }

    /*
    private System.Collections.IEnumerator LookAroundCoroutine()
    {
        float originalY = enemy.transform.eulerAngles.y;

        // Поворот влево
        for (float t = 0; t < 1f; t += Time.deltaTime)
        {
            enemy.transform.rotation = Quaternion.Euler(0, Mathf.Lerp(originalY, originalY - 45, t), 0);
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        // Поворот вправо
        for (float t = 0; t < 1f; t += Time.deltaTime)
        {
            enemy.transform.rotation = Quaternion.Euler(0, Mathf.Lerp(originalY - 45, originalY + 45, t), 0);
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        // Возврат в исходное
        for (float t = 0; t < 1f; t += Time.deltaTime)
        {
            enemy.transform.rotation = Quaternion.Euler(0, Mathf.Lerp(originalY + 45, originalY, t), 0);
            yield return null;
        }
    }
    */
}
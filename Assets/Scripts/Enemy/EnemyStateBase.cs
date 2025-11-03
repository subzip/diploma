

public abstract class EnemyStateBase
{
    protected EnemyBase enemy;
    protected UnityEngine.AI.NavMeshAgent agent;

    public EnemyStateBase(EnemyBase enemy)
    {
        this.enemy = enemy;
        this.agent = enemy.agent;
    }

    public abstract void OnEnter();
    public abstract void OnUpdate();
    public abstract void OnExit();
}
public interface IAIState
{
    string Name { get; }
    void Enter();
    void Tick(float deltaTime);
    void Exit();
}

public sealed class EnemyStateMachine
{
    public IAIState CurrentState { get; private set; }

    public void ChangeState(IAIState nextState)
    {
        if (CurrentState == nextState) return;
        CurrentState?.Exit();
        CurrentState = nextState;
        CurrentState?.Enter();
    }

    public void Tick(float deltaTime)
    {
        CurrentState?.Tick(deltaTime);
    }
}

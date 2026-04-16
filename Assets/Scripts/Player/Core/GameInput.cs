using UnityEngine;

/// <summary>
/// Единый провайдер PlayerInputActions для всех компонентов игрока.
/// Создаёт один экземпляр, включает/выключает карту и корректно освобождает ресурсы.
/// </summary>
[DefaultExecutionOrder(-200)]
public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    public PlayerInputActions Actions { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        Actions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        if (Actions == null)
            Actions = new PlayerInputActions();

        Actions?.Player.Enable();
        Actions?.UI.Enable();
    }

    private void OnDisable()
    {
        Actions?.Player.Disable();
        Actions?.UI.Disable();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Actions?.Dispose();
            Instance = null;
        }
    }
}

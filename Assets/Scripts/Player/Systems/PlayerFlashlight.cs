using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerFlashlight : MonoBehaviour
{
    [Header("Flashlight")]
    [SerializeField] private Light flashlight;
    [SerializeField] private bool startEnabled;
    [SerializeField] private Key toggleKey = Key.F;

    [Header("Optional Audio")]
    [SerializeField] private AudioCue toggleOnCue;
    [SerializeField] private AudioCue toggleOffCue;
    [SerializeField, Range(0f, 2f)] private float toggleVolume = 0.85f;

    private bool isOn;

    private void Awake()
    {
        if (flashlight == null)
            flashlight = GetComponentInChildren<Light>(true);
    }

    private void Start()
    {
        SetFlashlight(startEnabled, playSound: false);
    }

    private void Update()
    {
        if (flashlight == null) return;
        if (DeathScreen.GlobalDeathActive) return;
        if (CycleTransitionScreen.IsTransitionActive) return;
        if (PauseManager.IsPaused || PauseManager.IsInputGuardActive) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (!keyboard[toggleKey].wasPressedThisFrame) return;

        SetFlashlight(!isOn, playSound: true);
    }

    public void SetFlashlight(bool enabled, bool playSound)
    {
        isOn = enabled;
        if (flashlight != null)
            flashlight.enabled = isOn;

        if (!playSound) return;
        AudioCue cue = isOn ? toggleOnCue : toggleOffCue;
        if (cue == null) return;
        AudioService.PlayAt(cue, transform.position, toggleVolume, ambience: false);
    }
}


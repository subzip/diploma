using System;
using UnityEngine;

[Serializable]
public class GameSettingsData
{
    [Range(0.03f, 10f)] public float mouseSensitivity = 2f;
    [Range(0, 2)] public int graphicsPreset = 1;
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float weaponsVolume = 1f;
    [Range(0f, 1f)] public float otherVolume = 1f;
}

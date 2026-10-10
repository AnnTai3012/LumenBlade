using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class UiButtonAudio : MonoBehaviour
{
    private Button button;

    private void Awake() => button = GetComponent<Button>();

    private void OnEnable()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(PlayClick);
    }

    private void OnDisable()
    {
        if (button != null) button.onClick.RemoveListener(PlayClick);
    }

    private static void PlayClick() => GameAudioManager.PlayCue(GameAudioCue.UiButtonClick);
}

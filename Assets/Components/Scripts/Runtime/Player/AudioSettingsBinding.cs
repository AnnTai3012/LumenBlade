using System;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;

// Shared slider behavior for the menu and in-game settings panels.
public sealed class AudioSettingsBinding : IDisposable
{
    private readonly Slider music;
    private readonly TMP_Text musicValue;
    private readonly Slider[] groups;
    private readonly TMP_Text[] groupValues;
    private readonly UnityAction<float> musicAction;
    private readonly UnityAction<float>[] groupActions;

    public AudioSettingsBinding(Slider music, TMP_Text musicValue, Slider[] groups, TMP_Text[] groupValues)
    {
        this.music = music;
        this.musicValue = musicValue;
        this.groups = groups ?? Array.Empty<Slider>();
        this.groupValues = groupValues ?? Array.Empty<TMP_Text>();
        musicAction = value => { GameAudioManager.SetMusicVolume(value); Label(musicValue, GameAudioManager.MusicVolume); };
        if (music != null) music.onValueChanged.AddListener(musicAction);
        groupActions = new UnityAction<float>[this.groups.Length];
        for (int i = 0; i < this.groups.Length && i < 3; i++)
        {
            int index = i;
            groupActions[i] = value =>
            {
                GameAudioManager.SetCategoryVolume((SfxCategory)index, value);
                if (index < this.groupValues.Length) Label(this.groupValues[index], GameAudioManager.GetCategoryVolume((SfxCategory)index));
            };
            if (this.groups[i] != null) this.groups[i].onValueChanged.AddListener(groupActions[i]);
        }
        Refresh();
    }

    public void Refresh()
    {
        if (music != null) music.SetValueWithoutNotify(GameAudioManager.MusicVolume);
        Label(musicValue, GameAudioManager.MusicVolume);
        for (int i = 0; i < groups.Length && i < 3; i++)
        {
            float value = GameAudioManager.GetCategoryVolume((SfxCategory)i);
            if (groups[i] != null) groups[i].SetValueWithoutNotify(value);
            if (i < groupValues.Length) Label(groupValues[i], value);
        }
    }

    public void Dispose()
    {
        if (music != null) music.onValueChanged.RemoveListener(musicAction);
        for (int i = 0; i < groupActions.Length; i++)
            if (groups[i] != null && groupActions[i] != null) groups[i].onValueChanged.RemoveListener(groupActions[i]);
    }

    private static void Label(TMP_Text text, float value)
    {
        if (text != null) text.text = UnityEngine.Mathf.RoundToInt(value * 100f) + "%";
    }
}

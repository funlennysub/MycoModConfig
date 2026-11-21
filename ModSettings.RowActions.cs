using System;
using System.Linq;
using BepInEx.Configuration;
using MycoMenuLib;
using UnityEngine;

namespace MycoModConfig;

public static partial class ModSettings
{
    private static void TrackDefault<T>(Option option, ConfigEntry<T> entry, Func<T, float> toWidget)
    {
        if (option == null) return;
        option.applyValue.AddListener(value => option.defaultValue = value);

        EventHandler handler = (_, _) =>
        {
            if (option != null)
                option.defaultValue = toWidget(entry.Value);
        };

        AttachBinder(option.gameObject, entry, handler);
    }

    private static void TrackDefault(Option option, ConfigEntryBase entry, Func<object, float> toWidget)
    {
        if (option == null) return;
        option.applyValue.AddListener(value => option.defaultValue = value);

        var handler = (EventHandler)typeof(ModSettings)
            .GetMethod(nameof(BuildHandler), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.MakeGenericMethod(entry.SettingType)
            .Invoke(null, [option, entry, toWidget]);

        AttachBinder(option.gameObject, entry, handler);
    }

    private static EventHandler BuildHandler<T>(Option option, ConfigEntryBase entry, Func<object, float> toWidget)
    {
        var typed = (ConfigEntry<T>)entry;
        return (_, _) =>
        {
            if (option != null)
                option.defaultValue = toWidget(typed.Value);
        };
    }

    private static void AttachBinder(GameObject row, ConfigEntryBase entry, EventHandler handler)
    {
        row.AddComponent<SettingChangedBinder>().Bind(entry, handler);
    }

    private sealed class SettingChangedBinder : MonoBehaviour
    {
        private ConfigEntryBase _entry;
        private EventHandler _handler;

        private EventHandler<SettingChangedEventArgs> _adapter;
        private bool _subscribed;

        public void Bind(ConfigEntryBase entry, EventHandler handler)
        {
            _entry = entry;
            _handler = handler;

            if (isActiveAndEnabled)
                Subscribe();
        }

        private void OnEnable() => Subscribe();

        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed || _entry == null || _handler == null) return;

            _adapter = (_, args) =>
            {
                if (args.ChangedSetting == _entry)
                    _handler(null, EventArgs.Empty);
            };

            _entry.ConfigFile.SettingChanged += _adapter;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;

            _entry.ConfigFile.SettingChanged -= _adapter;
            _adapter = null;
            _subscribed = false;
        }
    }

    private const string ReadOnlyNote = "<color=#FFB347>Read-only: edit this value in the mod's config file.</color>";

    private static void AddReadOnlyNote(ModInputField input)
    {
        if (input == null) return;

        var hover = input.GetComponentInChildren<ConfigHoverInfo>(true);
        if (hover == null)
        {
            ModUI.SetDescription(input, ReadOnlyNote);
            return;
        }

        hover.description = string.IsNullOrEmpty(hover.description)
            ? ReadOnlyNote
            : hover.description + "\n" + ReadOnlyNote;
    }

    private static void AddDescription(Component control, ConfigEntryBase entry)
    {
        var description = entry.Description?.Description;
        if (control == null || string.IsNullOrWhiteSpace(description)) return;

        var hover = ModUI.SetDescription(control, description);
        if (hover == null) return;

        hover.customizeAnchor = false;
    }

    private static void FinishRow(Component control, ConfigEntryBase entry, ModsView view)
    {
        AddDescription(control, entry);
        AddRowButtons(entry, control, view);
    }

    private static void AddRowButtons(ConfigEntryBase entry, Component control, ModsView view)
    {
        if (control == null || view == null) return;

        var revert = ModUI.AddRowAction(control, GetRevertIcon(), 0, () => view.DiscardEntry(entry));
        if (revert != null)
            view.RegisterRevertButton(entry, revert);

        var reset = ModUI.AddRowAction(control, GetResetIcon(), 1, () => ResetEntry(entry, view));
        if (reset != null)
            view.RegisterResetButton(entry, reset);
    }

    private static T Buffered<T>(ConfigEntry<T> entry, ModsView view)
    {
        return view != null && view.GetBuffered(entry) is T value ? value : entry.Value;
    }

    private static void ResetEntry(ConfigEntryBase entry, ModsView view)
    {
        try
        {
            var configFile = entry.ConfigFile;

            entry.BoxedValue = entry.DefaultValue;

            if (configFile != null && !configFile.SaveOnConfigSet)
                configFile.Save();

            view.DiscardEntry(entry);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"Could not reset {entry.Definition.Key}: {e}");
        }
    }

    private static Sprite _revertIcon;
    private static Sprite _resetIcon;

    private static Sprite GetRevertIcon() =>
        _revertIcon ??= FindSprite("RefreshIcon");

    private static Sprite GetResetIcon() =>
        _resetIcon ??= FindSprite("wrench") ?? FindSprite("settings gear");

    private static Sprite FindSprite(string name) =>
        Resources.FindObjectsOfTypeAll<Sprite>()
            .FirstOrDefault(sprite => sprite != null && sprite.name == name);
}

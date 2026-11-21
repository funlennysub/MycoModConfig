using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace MycoModConfig;

public sealed partial class ModsView
{
    private void UpdateActionButtons()
    {
        var hasPending = HasPending;
        SetActionButtonEnabled(_saveAction, hasPending);
        SetActionButtonEnabled(_revertAction, hasPending);
    }

    internal void RegisterRevertButton(ConfigEntryBase entry, GameObject button)
    {
        _revertButtons[entry] = button;
    }

    internal void RegisterResetButton(ConfigEntryBase entry, GameObject button)
    {
        _resetButtons[entry] = button;
    }

    internal object GetBuffered(ConfigEntryBase entry) => _pending.GetValueOrDefault(entry);

    private void UpdateRevertVisible(ConfigEntryBase entry)
    {
        if (!_revertButtons.TryGetValue(entry, out var button) || button == null) return;

        button.SetActive(_pending.ContainsKey(entry));
    }

    private void UpdateResetVisible(ConfigEntryBase entry)
    {
        if (!_resetButtons.TryGetValue(entry, out var button) || button == null) return;

        var effective = GetBuffered(entry) ?? entry.BoxedValue;

        button.SetActive(!SameValue(effective, entry.DefaultValue));
    }

    private void MarkDirty(ConfigEntryBase entry, object value)
    {
        if (_building) return;

        if (SameValue(entry.BoxedValue, value))
            _pending.Remove(entry);
        else
            _pending[entry] = value;

        UpdateRevertVisible(entry);
        UpdateResetVisible(entry);
        UpdateActionButtons();
    }

    private static bool SameValue(object left, object right)
    {
        if (left == null || right == null)
            return left == right;

        if (left is float leftFloat && right is float rightFloat)
            return Mathf.Abs(leftFloat - rightFloat) < 0.00001f;

        return left.Equals(right);
    }

    public void SavePending()
    {
        if (_pending.Count == 0) return;

        try
        {
            foreach (var pair in _pending)
                pair.Key.BoxedValue = pair.Value;

            foreach (var configFile in _pending.Keys
                         .Select(e => e.ConfigFile)
                         .Where(f => f != null && !f.SaveOnConfigSet)
                         .Distinct())
                configFile.Save();

            Plugin.Logger.LogInfo($"Saved {_pending.Count} config value(s)");
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"Could not save config: {e}");
            return;
        }

        _pending.Clear();
        HideRevertButtons();
        UpdateActionButtons();
    }

    private void HideRevertButtons()
    {
        foreach (var button in _revertButtons.Values)
            if (button != null)
                button.SetActive(false);
    }

    public void DiscardPending(bool rebuild = true)
    {
        _pending.Clear();

        if (rebuild)
            RebuildConfig();

        UpdateActionButtons();
    }

    internal void DiscardEntry(ConfigEntryBase entry)
    {
        _pending.Remove(entry);
        RebuildConfig();
        UpdateActionButtons();
    }

    private void ConfirmResetDefaults()
    {
        if (string.IsNullOrEmpty(_selected)) return;

        if (_popup == null && _section.parent is RectTransform root)
            _popup = ModsConfirmPopup.Create(root);

        if (_popup == null)
        {
            Plugin.Logger.LogError("Could not create the reset confirmation popup");
            return;
        }

        _popup.ShowConfirmation(
            "Reset all settings to their default values?",
            "RESET",
            ResetToDefaults);
    }

    private void ResetToDefaults()
    {
        var mod = _mods.FirstOrDefault(m => m.Name == _selected);
        if (mod == null) return;

        try
        {
            foreach (var entry in mod.Entries)
                entry.BoxedValue = entry.DefaultValue;

            _pending.Clear();

            foreach (var configFile in mod.Entries
                         .Select(e => e.ConfigFile)
                         .Where(f => f != null && !f.SaveOnConfigSet)
                         .Distinct())
                configFile.Save();

            Plugin.Logger.LogInfo(
                $"Reset {mod.Entries.Length} config value(s) to defaults");
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"Could not reset config defaults: {e}");
            return;
        }

        RebuildConfig();
        UpdateActionButtons();
    }
}

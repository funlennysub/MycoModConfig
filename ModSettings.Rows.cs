using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx.Configuration;
using MycoMenuLib;
using UnityEngine;
using UnityEngine.UI;

namespace MycoModConfig;

public static partial class ModSettings
{
    internal static void BuildModConfig(RectTransform items, string modName, IEnumerable<ConfigEntryBase> entries, float width, Action<ConfigEntryBase, object> onChanged, ModsView view)
    {
        var categoryGroups = entries
            .GroupBy(GetCategory)
            .ToList();

        for (var g = 0; g < categoryGroups.Count; g++)
        {
            var group = categoryGroups[g];

            if (categoryGroups.Count > 1 ||
                !string.Equals(group.Key, "General", StringComparison.OrdinalIgnoreCase))
                ModUI.AddCategory(items, group.Key);

            var categoryContent = CreateContentGroup(items, width);
            CreateModEntries(categoryContent, modName, group, onChanged, view);
            FitContentHeight(categoryContent);

            if (g < categoryGroups.Count - 1)
                ModUI.AddDivider(items);
        }
    }

    private static void CreateModEntries(RectTransform items, string modName, IEnumerable<ConfigEntryBase> entries, Action<ConfigEntryBase, object> onChanged, ModsView view)
    {
        foreach (var entry in entries)
        {
            var definitionKey = entry.Definition.Key;
            var displayName = GetDisplayName(entry);
            var id = $"{ModConfigIds.Prefix}{modName}.{entry.Definition.Section}.{definitionKey}";

            switch (entry)
            {
                case ConfigEntry<bool> boolEntry:
                {
                    var toggle = ModUI.AddToggle(items, id, displayName, Buffered(boolEntry, view),
                        value => onChanged(boolEntry, value));
                    TrackDefault(toggle, boolEntry, v => v ? 1f : 0f);
                    FinishRow(toggle, entry, view);
                    break;
                }
                case ConfigEntry<float> floatEntry
                    when floatEntry.Description.AcceptableValues is AcceptableValueList<float> floatList:
                {
                    var values = floatList.AcceptableValues;
                    var index = Array.IndexOf(values, Buffered(floatEntry, view));

                    var floatDropdown = ModUI.AddDropdown(items, id, displayName,
                        values.Select(v => v.ToString(CultureInfo.InvariantCulture)).ToArray(),
                        index < 0 ? 0 : index,
                        i => onChanged(floatEntry, values[i]));
                    TrackDefault(floatDropdown, floatEntry,
                        v => Mathf.Max(0, Array.IndexOf(values, v)));
                    FinishRow(floatDropdown, entry, view);
                    break;
                }
                case ConfigEntry<float> floatEntry:
                {
                    var (min, max) = GetFloatRange(floatEntry);
                    var digits = GetDigits(min, max, floatEntry.Value);

                    var slider = ModUI.AddFloatSlider(items, id, displayName, Buffered(floatEntry, view), min, max, digits,
                        value => onChanged(floatEntry, value));
                    TrackDefault(slider, floatEntry, v => v);
                    FinishRow(slider, entry, view);
                    break;
                }
                case ConfigEntry<int> intEntry
                    when intEntry.Description.AcceptableValues is AcceptableValueList<int> intList:
                {
                    var values = intList.AcceptableValues;
                    var index = Array.IndexOf(values, Buffered(intEntry, view));

                    var intDropdown = ModUI.AddDropdown(items, id, displayName,
                        values.Select(v => v.ToString(CultureInfo.InvariantCulture)).ToArray(),
                        index < 0 ? 0 : index,
                        i => onChanged(intEntry, values[i]));
                    TrackDefault(intDropdown, intEntry,
                        v => Mathf.Max(0, Array.IndexOf(values, v)));
                    FinishRow(intDropdown, entry, view);
                    break;
                }
                case ConfigEntry<int> intEntry:
                {
                    var (min, max) = GetIntRange(intEntry);

                    var intSlider = ModUI.AddIntSlider(items, id, displayName, Buffered(intEntry, view), min, max,
                        value => onChanged(intEntry, value));
                    TrackDefault(intSlider, intEntry, v => v);
                    FinishRow(intSlider, entry, view);
                    break;
                }
                case ConfigEntry<string> stringEntry
                    when stringEntry.Description.AcceptableValues is AcceptableValueList<string> stringList:
                {
                    var values = stringList.AcceptableValues;
                    var index = Array.IndexOf(values, Buffered(stringEntry, view));

                    var stringDropdown = ModUI.AddDropdown(items, id, displayName, values,
                        index < 0 ? 0 : index,
                        i => onChanged(stringEntry, values[i]));
                    TrackDefault(stringDropdown, stringEntry,
                        v => Mathf.Max(0, Array.IndexOf(values, v)));
                    FinishRow(stringDropdown, entry, view);
                    break;
                }
                case ConfigEntry<string> stringEntry:
                {
                    if (HasTag(stringEntry, "Password"))
                    {
                        var secret = ModUI.AddInput(items, id, displayName, Buffered(stringEntry, view),
                            new InputFieldOptions { Type = ModInputType.Password },
                            value => onChanged(stringEntry, value));
                        FinishRow(secret, entry, view);
                        break;
                    }

                    var input = ModUI.AddTextInput(items, id, displayName, Buffered(stringEntry, view),
                        value => onChanged(stringEntry, value));
                    FinishRow(input, entry, view);
                    break;
                }
                case ConfigEntry<KeyCode> keyEntry:
                {
                    var keybind = ModUI.AddKeybind(items, id, displayName, Buffered(keyEntry, view),
                        (key, _) => key,
                        key => key.ToString(),
                        key => onChanged(keyEntry, key));
                    FinishRow(keybind, entry, view);
                    break;
                }
                case ConfigEntry<KeyboardShortcut> shortcutEntry:
                {
                    var shortcut = ModUI.AddKeybind(items, id, displayName, Buffered(shortcutEntry, view),
                        (key, modifiers) => new KeyboardShortcut(key, modifiers),
                        FormatShortcut,
                        shortcut => onChanged(shortcutEntry, shortcut));
                    FinishRow(shortcut, entry, view);
                    break;
                }
                case not null when entry.SettingType.IsEnum:
                {
                    var enumType = entry.SettingType;
                    var names = Enum.GetNames(enumType);
                    var values = Enum.GetValues(enumType);
                    var index = Array.IndexOf(values, view.GetBuffered(entry) ?? entry.BoxedValue);

                    var enumDropdown = ModUI.AddDropdown(items, id, displayName, names,
                        index < 0 ? 0 : index,
                        i => onChanged(entry, Enum.ToObject(enumType, values.GetValue(i))));
                    TrackDefault(enumDropdown, entry,
                        v => Mathf.Max(0, Array.IndexOf(values, v)));
                    FinishRow(enumDropdown, entry, view);
                    break;
                }
                case not null when IsNumeric(entry.SettingType):
                {
                    var type = entry.SettingType;
                    var current = Convert.ToDouble(view.GetBuffered(entry) ?? entry.BoxedValue);
                    var (min, max) = GetNumericRange(entry, current);

                    if (type == typeof(double) || type == typeof(decimal))
                    {
                        var slider = ModUI.AddFloatSlider(items, id, displayName,
                            (float)current, (float)min, (float)max,
                            GetDigits((float)min, (float)max, (float)current),
                            value => onChanged(entry, Convert.ChangeType(value, type)));
                        TrackDefault(slider, entry, v => (float)Convert.ToDouble(v));
                        FinishRow(slider, entry, view);
                    }
                    else
                    {
                        var low = (int)Math.Clamp(min, int.MinValue, int.MaxValue);
                        var high = (int)Math.Clamp(max, int.MinValue, int.MaxValue);
                        var clamped = Mathf.Clamp((int)Math.Round(current), low, high);

                        var slider = ModUI.AddIntSlider(items, id, displayName,
                            clamped, low, high,
                            value => onChanged(entry, Convert.ChangeType(value, type)));
                        TrackDefault(slider, entry, v => (float)Convert.ToDouble(v));
                        FinishRow(slider, entry, view);
                    }

                    break;
                }
                default:
                {
                    var readOnly = ModUI.AddInput(items, id, displayName, FormatValue(entry.BoxedValue),
                        new InputFieldOptions { ReadOnly = true });
                    FinishRow(readOnly, entry, view);
                    AddReadOnlyNote(readOnly);
                    break;
                }
            }
        }
    }

    private static RectTransform CreateContentGroup(RectTransform parent, float width)
    {
        var content = new GameObject("Content",
            typeof(RectTransform), typeof(VerticalLayoutGroup));
        var rt = (RectTransform)content.transform;

        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.sizeDelta = new Vector2(width, 0f);
        rt.anchoredPosition = Vector2.zero;

        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 20f;
        layout.childAlignment = TextAnchor.UpperRight;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        return rt;
    }

    private static void FitContentHeight(RectTransform content)
    {
        var layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout == null) return;

        var height = (float)(layout.padding.top + layout.padding.bottom);
        var count = 0;

        foreach (Transform child in content)
        {
            if (!child.gameObject.activeSelf) continue;

            if (count > 0)
                height += layout.spacing;

            height += ((RectTransform)child).sizeDelta.y;
            count++;
        }

        content.sizeDelta = new Vector2(content.sizeDelta.x, height);
    }
}

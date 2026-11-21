using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using Pigeon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace MycoModConfig;

public static partial class ModSettings
{
    public static void CreateOptions(PlayerOptions options)
    {
        if (Array.Exists(options.sectionButtons, b => b != null && b.name == "Mods")) return;

        var graphicsIndex = Array.FindIndex(options.sectionButtons, b => b != null && b.name == "Graphics");

        if (graphicsIndex < 0)
        {
            Plugin.Logger.LogWarning("No Graphics settings section found; cannot create Mods tab");
            return;
        }

        if (graphicsIndex >= options.sections.Length || graphicsIndex >= options.sectionButtons.Length)
        {
            Plugin.Logger.LogWarning("Graphics section index is out of range; cannot create Mods tab");
            return;
        }

        var graphicsSection = options.sections[graphicsIndex];
        var graphicsButton = options.sectionButtons[graphicsIndex];

        if (graphicsSection == null || graphicsButton == null)
        {
            Plugin.Logger.LogWarning("Graphics settings row is missing; cannot create Mods tab");
            return;
        }

        var graphicsRt = (RectTransform)graphicsButton.transform;

        var section = Object
            .Instantiate(graphicsSection.gameObject, graphicsSection.parent, false)
            .GetComponent<RectTransform>();

        if (section == null)
        {
            Plugin.Logger.LogWarning("Could not clone the Graphics settings section");
            return;
        }

        section.name = "Mods";
        section.anchoredPosition = graphicsSection.anchoredPosition;
        section.gameObject.SetActive(false);

        var optionsContainer = section.GetComponent<OptionsContainer>();
        if (optionsContainer == null || optionsContainer.container == null)
        {
            Plugin.Logger.LogWarning("Cloned Graphics section has no OptionsContainer");
            Object.Destroy(section.gameObject);
            return;
        }

        var sectionOptions = optionsContainer.container;
        for (var i = sectionOptions.childCount - 1; i >= 0; i--)
            Object.Destroy(sectionOptions.GetChild(i).gameObject);

        var view = section.gameObject.AddComponent<ModsView>();
        view.Initialize(section, sectionOptions, GetMods());

        var buttonGo = Object.Instantiate(
            graphicsButton.gameObject, graphicsButton.transform.parent, false);
        buttonGo.name = "Mods";

        var button = buttonGo.GetComponent<DefaultButton>();

        var newIndex = options.sections.Length;

        if (!RetargetPersistentInt(button.OnClickUp, "SetSection", newIndex))
            button.OnClickUp.AddListener(() => options.SetSection(newIndex));

        var rowStep = 70f;
        if (graphicsIndex + 1 < options.sectionButtons.Length)
        {
            var nextRt = (RectTransform)options.sectionButtons[graphicsIndex + 1].transform;
            rowStep = Mathf.Abs(graphicsRt.anchoredPosition.y - nextRt.anchoredPosition.y);
        }

        for (var i = graphicsIndex + 1; i < options.sectionButtons.Length; i++)
        {
            var other = options.sectionButtons[i];
            if (other != null)
                ((RectTransform)other.transform).anchoredPosition += new Vector2(0f, -rowStep);
        }

        var rt = (RectTransform)button.transform;
        rt.anchoredPosition = graphicsRt.anchoredPosition + new Vector2(0f, -rowStep);
        rt.sizeDelta = graphicsRt.sizeDelta;
        buttonGo.transform.SetSiblingIndex(graphicsButton.transform.GetSiblingIndex() + 1);

        if (button.SecondaryGraphic is TextMeshProUGUI label)
        {
            label.text = "MODS";
            label.ForceMeshUpdate();
        }

        Array.Resize(ref options.sections, newIndex + 1);
        options.sections[newIndex] = section;

        Array.Resize(ref options.sectionButtons, newIndex + 1);
        options.sectionButtons[newIndex] = button;

        Array.Resize(ref options.sectionPositions, newIndex + 1);
    }

    private static bool RetargetPersistentInt(UnityEventBase evt, string methodName, int value)
    {
        var group = typeof(UnityEventBase)
            .GetField("m_PersistentCalls", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(evt);

        var calls = group?.GetType()
            .GetField("m_Calls", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(group) as IList;
        if (calls == null)
            return false;

        var patched = false;
        foreach (var call in calls)
        {
            var type = call.GetType();
            if (type.GetProperty("methodName")?.GetValue(call) as string != methodName)
                continue;

            var args = type.GetProperty("arguments")?.GetValue(call);
            args?.GetType().GetProperty("intArgument")?.SetValue(args, value);
            patched = true;
        }

        return patched;
    }

    internal static List<ModEntry> GetMods()
    {
        var mods = new List<ModEntry>();

        foreach (var plugin in Chainloader.PluginInfos.Values.OrderBy(p => p.Metadata.Name))
        {
            var entries = new List<ConfigEntryBase>();

            var instance = plugin.Instance;
            if (instance?.Config == null) continue;

            foreach (var configEntryBase in instance.Config.Select(configEntry => configEntry.Value))
            {
                if (IsHidden(configEntryBase)) continue;

                entries.Add(configEntryBase);
            }

            if (entries.Count == 0) continue;

            mods.Add(new ModEntry
            {
                Name = plugin.Metadata.Name,
                Entries = entries.ToArray()
            });
        }

        return mods;
    }
}

public sealed class ModEntry
{
    public ConfigEntryBase[] Entries;
    public string Name;
}

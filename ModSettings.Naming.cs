using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx.Configuration;

namespace MycoModConfig;

public static partial class ModSettings
{
    private static string GetCategory(ConfigEntryBase entry)
    {
        var section = entry.Definition.Section;
        return string.IsNullOrWhiteSpace(section) ? "General" : FixNaming(section);
    }

    private static string GetDisplayName(ConfigEntryBase entry)
    {
        var displayName = GetTagValue(entry, "DispName") as string;
        return !string.IsNullOrWhiteSpace(displayName) ? displayName : FixNaming(entry.Definition.Key);
    }

    private static bool IsHidden(ConfigEntryBase entry)
    {
        if (HasTag(entry, "Hidden"))
            return true;

        return GetTagValue(entry, "Browsable") is bool browsable && !browsable;
    }

    private static bool HasTag(ConfigEntryBase entry, string tag)
    {
        var tags = entry.Description?.Tags;
        return tags != null && tags.Contains(tag);
    }

    private static object GetTagValue(ConfigEntryBase entry, string memberName)
    {
        var tags = entry.Description?.Tags;
        if (tags == null) return null;

        foreach (var tag in tags)
        {
            if (tag == null) continue;

            var type = tag.GetType();
            var property = type.GetProperty(memberName,
                BindingFlags.Public | BindingFlags.Instance);
            if (property != null)
                return property.GetValue(tag);

            var field = type.GetField(memberName,
                BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
                return field.GetValue(tag);
        }

        return null;
    }

    private static string FixNaming(string input)
    {
        input = Regex.Replace(input, @"\s+", " ");
        input = Regex.Replace(input, @"([A-Z]\.)\s([A-Z]\.)", "$1$2");

        return input.Trim();
    }
}

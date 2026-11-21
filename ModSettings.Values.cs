using System;
using System.Globalization;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace MycoModConfig;

public static partial class ModSettings
{
    private static bool IsNumeric(Type type)
    {
        return type == typeof(byte) || type == typeof(sbyte) ||
               type == typeof(short) || type == typeof(ushort) ||
               type == typeof(uint) || type == typeof(long) ||
               type == typeof(ulong) || type == typeof(double) ||
               type == typeof(decimal);
    }

    private static (double min, double max) GetNumericRange(ConfigEntryBase entry, double current)
    {
        var acceptable = entry.Description?.AcceptableValues;
        if (acceptable != null)
        {
            var type = acceptable.GetType();
            if (type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(AcceptableValueRange<>))
            {
                var min = type.GetProperty("MinValue")?.GetValue(acceptable);
                var max = type.GetProperty("MaxValue")?.GetValue(acceptable);
                if (min != null && max != null)
                    return (Convert.ToDouble(min), Convert.ToDouble(max));
            }
        }

        var magnitude = Math.Max(
            Math.Abs(current), Math.Abs(Convert.ToDouble(entry.DefaultValue)));

        var bound = magnitude switch
        {
            0d => 100d,
            <= 0.001 => 10d,
            <= 0.01 => 50d,
            <= 100 => magnitude * 3,
            _ => magnitude * 2
        };

        return (-bound, bound);
    }

    private static (float min, float max) GetFloatRange(ConfigEntry<float> entry)
    {
        if (entry.Description.AcceptableValues is AcceptableValueRange<float> range)
        {
            var (rangeMin, rangeMax) = (range.MinValue, range.MaxValue);
            return rangeMax > rangeMin ? (rangeMin, rangeMax) : (rangeMin, rangeMin + 1);
        }

        var magnitude = Mathf.Max(Mathf.Abs(entry.Value), Mathf.Abs((float)entry.DefaultValue));
        var bound = magnitude switch
        {
            0f => 100f,
            <= 0.001f => 10f,
            <= 0.01f => 50f,
            <= 100f => magnitude * 3f,
            _ => magnitude * 2f
        };

        return (Mathf.Min(-bound, entry.Value), Mathf.Max(bound, entry.Value));
    }

    private static (int min, int max) GetIntRange(ConfigEntry<int> entry)
    {
        if (entry.Description.AcceptableValues is AcceptableValueRange<int> range)
        {
            var (rangeMin, rangeMax) = (range.MinValue, range.MaxValue);
            return rangeMax > rangeMin ? (rangeMin, rangeMax) : (rangeMin, rangeMin + 1);
        }

        var magnitude = Math.Max(Math.Abs(entry.Value), Math.Abs((int)entry.DefaultValue));
        var bound = magnitude switch
        {
            0 => 100,
            <= 100 => magnitude * 3,
            _ => magnitude * 2
        };

        return (Math.Min(-bound, entry.Value), Math.Max(bound, entry.Value));
    }

    private static int GetDigits(float min, float max, float current)
    {
        var digits = Math.Max(GetDecimalPlaces(min),
            Math.Max(GetDecimalPlaces(max), GetDecimalPlaces(current)));
        return Mathf.Clamp(Math.Max(digits, 2), 2, 4);
    }

    private static int GetDecimalPlaces(float value)
    {
        var text = Mathf.Abs(value).ToString("0.####", CultureInfo.InvariantCulture);
        var decimalPoint = text.IndexOf('.');
        return decimalPoint == -1 ? 0 : text.Length - decimalPoint - 1;
    }

    private static string FormatValue(object value)
    {
        return value switch
        {
            null => string.Empty,
            Color color => "#" + ColorUtility.ToHtmlStringRGBA(color),
            Vector2 v => $"{v.x:0.##}, {v.y:0.##}",
            Vector3 v => $"{v.x:0.##}, {v.y:0.##}, {v.z:0.##}",
            Vector4 v => $"{v.x:0.##}, {v.y:0.##}, {v.z:0.##}, {v.w:0.##}",
            Quaternion q => $"{q.x:0.##}, {q.y:0.##}, {q.z:0.##}, {q.w:0.##}",
            _ => value.ToString()
        };
    }

    private static string FormatShortcut(KeyboardShortcut shortcut)
    {
        if (shortcut.MainKey == KeyCode.None) return "Unbound";

        var parts = shortcut.Modifiers.Select(ShortKey).ToList();
        parts.Add(ShortKey(shortcut.MainKey));
        return string.Join("+", parts);
    }

    private static string ShortKey(KeyCode key) => key switch
    {
        KeyCode.LeftControl => "LCtrl",
        KeyCode.RightControl => "RCtrl",
        KeyCode.LeftShift => "LShift",
        KeyCode.RightShift => "RShift",
        KeyCode.LeftAlt => "LAlt",
        KeyCode.RightAlt => "RAlt",
        KeyCode.LeftWindows => "LWin",
        KeyCode.RightWindows => "RWin",
        KeyCode.LeftCommand => "LCmd",
        KeyCode.RightCommand => "RCmd",
        KeyCode.Escape => "Esc",
        _ => key.ToString()
    };
}

using Pigeon.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MycoModConfig;

internal static class ModAccents
{
    internal static readonly Color Save = new(0.46f, 0.9f, 0.56f, 1f);
    internal static readonly Color Danger = new(1f, 0.37f, 0.44f, 1f);
    internal static readonly Color Warning = new(1f, 179f / 255f, 71f / 255f, 1f);
    internal const string WarningHex = "FFB347";

    internal static Color Hover(Color accent) => new(
        0.10f + accent.r * 0.22f,
        0.10f + accent.g * 0.22f,
        0.10f + accent.b * 0.22f,
        1f);

    internal static void Apply(GameObject target, Color color, bool enabled = true)
    {
        if (target == null) return;

        var tint = enabled
            ? color
            : new Color(color.r, color.g, color.b, 0.25f);

        var outline = target.GetComponent<BevelRectOutline>();
        if (outline == null)
            outline = target.GetComponentInChildren<BevelRectOutline>(true);

        if (outline != null)
        {
            outline.color = tint;
            return;
        }

        var border = target.GetComponent<Pigeon.UI.Rect>();
        if (border == null) return;

        Graphic main = null;
        if (target.GetComponent<DefaultButton>() is { } button)
            main = button.MainGraphic;
        else if (target.GetComponent<DoubleDefaultButton>() is { } doubleButton)
            main = doubleButton.MainGraphic;

        if (main != null && border == main) return;

        border.color = tint;
    }
}

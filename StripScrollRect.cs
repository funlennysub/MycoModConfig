using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MycoModConfig;

internal sealed class StripScrollRect : ScrollRect
{
    public override void OnScroll(PointerEventData data)
    {
        if (Plugin.ReverseStripScroll == null || !Plugin.ReverseStripScroll.Value)
        {
            base.OnScroll(data);
            return;
        }

        var original = data.scrollDelta;
        data.scrollDelta = -original;
        base.OnScroll(data);
        data.scrollDelta = original;
    }
}

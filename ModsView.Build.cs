using MycoMenuLib;
using Pigeon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MycoModConfig;

public sealed partial class ModsView
{
    private void BuildStrip()
    {
        var strip = new GameObject("ModStrip",
            typeof(RectTransform), typeof(Image), typeof(ScrollRect));

        var rt = (RectTransform)strip.transform;
        rt.SetParent(_section, false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, StripHeight);

        var background = strip.GetComponent<Image>();
        background.color = Color.clear;
        background.raycastTarget = false;

        _sectionScroll = _section.GetComponent<ScrollArea>();
        _stripRect = rt;

        var canvas = rt.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            _canvasCamera = canvas.worldCamera;

        var viewport = new GameObject("Viewport",
            typeof(RectTransform), typeof(RectMask2D), typeof(Image)).GetComponent<RectTransform>();
        viewport.SetParent(rt, false);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(4f, 18f);
        viewport.offsetMax = new Vector2(-4f, -4f);

        var viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = Color.clear;
        viewportImage.raycastTarget = true;

        var content = new GameObject("Content",
            typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        var contentRt = (RectTransform)content.transform;
        contentRt.SetParent(viewport, false);
        contentRt.anchorMin = new Vector2(0f, 0f);
        contentRt.anchorMax = new Vector2(0f, 1f);
        contentRt.pivot = new Vector2(0f, 0.5f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = Vector2.zero;
        _cardContent = contentRt;

        var cards = content.GetComponent<HorizontalLayoutGroup>();
        cards.spacing = 10f;
        cards.childAlignment = TextAnchor.MiddleLeft;
        cards.childControlWidth = true;
        cards.childControlHeight = true;
        cards.childForceExpandWidth = false;
        cards.childForceExpandHeight = false;

        var fitter = content.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        var scroll = strip.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = contentRt;
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true;
        scroll.scrollSensitivity = 35f;
        scroll.horizontalScrollbar = BuildScrollbar(rt, scroll);

        if (_section.Find("ScrollOutline") is RectTransform outline)
            outline.offsetMax = new Vector2(outline.offsetMax.x, -(StripHeight + ActionBarHeight));
    }

    private static Scrollbar BuildScrollbar(RectTransform parent, ScrollRect scroll)
    {
        var barGo = new GameObject("Scrollbar",
            typeof(RectTransform), typeof(Image), typeof(Scrollbar));

        var rt = (RectTransform)barGo.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 6f);
        rt.sizeDelta = new Vector2(-16f, 4f);

        var track = barGo.GetComponent<Image>();
        track.color = new Color(0.18f, 0.18f, 0.18f, 0.9f);
        track.raycastTarget = true;

        var slide = new GameObject("Sliding Area",
            typeof(RectTransform)).GetComponent<RectTransform>();
        slide.SetParent(rt, false);
        slide.anchorMin = Vector2.zero;
        slide.anchorMax = Vector2.one;
        slide.offsetMin = Vector2.zero;
        slide.offsetMax = Vector2.zero;

        var handle = new GameObject("Handle",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var handleRt = (RectTransform)handle.transform;
        handleRt.SetParent(slide, false);
        handleRt.anchorMin = Vector2.zero;
        handleRt.anchorMax = Vector2.one;
        handleRt.offsetMin = Vector2.zero;
        handleRt.offsetMax = Vector2.zero;

        var handleImage = handle.GetComponent<Image>();
        handleImage.color = new Color(0.45f, 0.45f, 0.45f, 1f);
        handleImage.raycastTarget = true;

        var bar = barGo.GetComponent<Scrollbar>();
        bar.handleRect = handleRt;
        bar.targetGraphic = handleImage;
        bar.direction = Scrollbar.Direction.LeftToRight;
        bar.transition = Selectable.Transition.None;

        scroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        return bar;
    }

    private void BuildActionBar()
    {
        var bar = new GameObject("ModActions",
            typeof(RectTransform), typeof(Image));

        var rt = (RectTransform)bar.transform;
        rt.SetParent(_section, false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -StripHeight);
        rt.sizeDelta = new Vector2(0f, ActionBarHeight);

        var background = bar.GetComponent<Image>();
        background.color = Color.clear;
        background.raycastTarget = false;

        _saveAction = MakeActionButton(rt, "SAVE", 90f, -8f);
        _saveAction.OnClickUp.AddListener(SavePending);

        _revertAction = MakeActionButton(rt, "REVERT", 110f, -104f);
        _revertAction.OnClickUp.AddListener(() => DiscardPending());

        var reset = MakeActionButton(rt, "RESET DEFAULTS", 180f, -220f);
        reset.OnClickUp.AddListener(ConfirmResetDefaults);

        UpdateActionButtons();
    }

    private static void SetActionButtonEnabled(DefaultButton button, bool enabled)
    {
        if (button == null) return;

        button.ignoreEvents = !enabled;

        var fill = enabled
            ? new Color(0.09f, 0.09f, 0.09f, 1f)
            : new Color(0.035f, 0.035f, 0.035f, 1f);

        button.SetDefaultColor(fill);
        button.SetHoverColor(enabled
            ? new Color(0.18f, 0.18f, 0.18f, 1f)
            : new Color(0.04f, 0.04f, 0.04f, 1f));
        button.SetSecondaryColor(enabled
            ? Color.white
            : new Color(1f, 1f, 1f, 0.35f));

        if (button.MainGraphic != null)
            button.MainGraphic.color = fill;

        if (button.GetComponent<BevelRectOutline>() is { } outline)
            outline.color = enabled
                ? Color.white
                : new Color(1f, 1f, 1f, 0.25f);
    }

    private static DefaultButton MakeActionButton(RectTransform parent, string label, float width, float x)
    {
        var button = Instantiate(
            ModResources.Instance.NormalButton, parent, false);

        button.gameObject.name = label;

        var rt = (RectTransform)button.transform;
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(width, -8f);
        rt.anchoredPosition = new Vector2(x, 0f);

        if (button.SecondaryGraphic is TextMeshProUGUI text)
        {
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 14f;
            text.raycastTarget = false;
        }

        SetCardCorners(button);
        button.SetDefaultColor(new Color(0.09f, 0.09f, 0.09f, 1f));
        button.SetHoverColor(new Color(0.18f, 0.18f, 0.18f, 1f));
        return button;
    }

    private void BuildPlaceholder()
    {
        _placeholder = ModUI.AddCategory(_section, "Select a mod to configure");
        if (_placeholder == null) return;

        _placeholder.gameObject.name = "SelectModHint";

        var labelRow = _placeholder.transform.parent as RectTransform;
        if (labelRow != null && labelRow.parent == _section)
        {
            _placeholder.transform.SetParent(_section, false);
            Destroy(labelRow.gameObject);
        }

        var rt = _placeholder.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = new Vector2(0f, -(StripHeight + ActionBarHeight));

        _placeholder.alignment = TextAlignmentOptions.Center;
        _placeholder.fontStyle = FontStyles.Normal;
        _placeholder.fontSize = 22f;
        _placeholder.color = new Color(1f, 1f, 1f, 0.4f);
    }

    private void BuildCards()
    {
        foreach (var mod in _mods)
        {
            var button = Instantiate(
                ModResources.Instance.NormalButton, _cardContent, false);

            button.gameObject.name = $"Mod {mod.Name}";

            var layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = CardWidth;
            layout.preferredHeight = CardHeight;

            SetCardCorners(button);

            if (button.SecondaryGraphic is TextMeshProUGUI text)
            {
                text.text = mod.Name;
                text.alignment = TextAlignmentOptions.MidlineLeft;
                text.fontSize = 17f;
                text.enableAutoSizing = false;
                text.overflowMode = TextOverflowModes.Ellipsis;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.raycastTarget = false;

                var textRt = text.rectTransform;
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = new Vector2(14f, 0f);
                textRt.offsetMax = new Vector2(-10f, 0f);
            }

            var card = new Card { Name = mod.Name, Button = button };
            button.OnClickUp.AddListener(() => RequestSelect(mod.Name));
            _cards.Add(card);
        }
    }

    private static void SetCardCorners(DefaultButton button)
    {
        var bkg = button.transform.Find("BKG");
        if (bkg != null && bkg.GetComponent<BevelRect>() is { } bevel)
            bevel.SetCorners(0f);

        if (button.GetComponent<BevelRectOutline>() is { } outline)
            outline.SetCorners(0f);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Pigeon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MycoModConfig;

public sealed partial class ModsView : MonoBehaviour
{
    private const float StripHeight = 92f;
    private const float ActionBarHeight = 40f;
    private const float CardWidth = 250f;
    private const float CardHeight = 60f;

    private static readonly List<ModsView> _views = [];
    private readonly List<Card> _cards = [];
    private readonly Dictionary<ConfigEntryBase, object> _pending = new();
    private readonly Dictionary<ConfigEntryBase, GameObject> _revertButtons = new();
    private readonly Dictionary<ConfigEntryBase, GameObject> _resetButtons = new();
    private bool _building;
    private RectTransform _cardContent;
    private RectTransform _items;

    private List<ModEntry> _mods;
    private TextMeshProUGUI _placeholder;
    private ModsConfirmPopup _popup;
    private DefaultButton _revertAction;
    private DefaultButton _saveAction;

    private RectTransform _section;

    private ScrollArea _sectionScroll;
    private RectTransform _stripRect;
    private Camera _canvasCamera;

    private string _selected;
    private float _width;

    internal static ModsView BlockingView =>
        _views.FirstOrDefault(v => v.HasPending && v.IsVisible);

    private bool HasPending => _pending.Count > 0;

    private bool IsVisible => _section != null && _section.gameObject.activeInHierarchy;

    private void OnEnable()
    {
        RefreshMods();

        if (_sectionScroll != null)
            _sectionScroll.enabled = false;
    }

    private void RefreshMods()
    {
        _mods = ModSettings.GetMods();

        for (var i = _cardContent.childCount - 1; i >= 0; i--)
        {
            var child = _cardContent.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }

        _cards.Clear();
        BuildCards();

        if (_selected != null && _mods.All(m => m.Name != _selected))
        {
            _selected = null;
            _pending.Clear();
            RebuildConfig();
            if (_placeholder != null)
                _placeholder.gameObject.SetActive(true);
            UpdateActionButtons();
        }

        UpdateCardStates();
    }

    private void OnDestroy()
    {
        PlayerInput.Controls.Menu.ScrollWheel.performed -= OnWheel;
        _views.Remove(this);
    }

    public void Initialize(RectTransform section, RectTransform items, List<ModEntry> mods)
    {
        _views.Add(this);
        _section = section;
        _items = items;
        _mods = mods;
        _width = items.rect.width;

        CreateOptionsViewport(section, items);

        BuildStrip();
        BuildActionBar();
        BuildPlaceholder();
        BuildCards();

        UpdateCardStates();
        if (_placeholder != null)
            _placeholder.gameObject.SetActive(true);

        _sectionScroll.enabled = false;
        PlayerInput.Controls.Menu.ScrollWheel.performed += OnWheel;
    }

    // The game's wheel subscription only starts on pointer enter, which
    // Pigeon dispatches unreliably; drive the section ourselves instead.
    private void OnWheel(InputAction.CallbackContext context)
    {
        if (_sectionScroll == null || !IsVisible) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        var pointer = mouse.position.ReadValue();

        if (!RectTransformUtility.RectangleContainsScreenPoint(
                _section, pointer, _canvasCamera))
            return;

        if (_stripRect != null && RectTransformUtility.RectangleContainsScreenPoint(
                _stripRect, pointer, _canvasCamera))
            return;

        _sectionScroll.enabled = false;
        _sectionScroll.Scroll(context.ReadValue<Vector2>() * 120f);
    }

    private static void CreateOptionsViewport(RectTransform section, RectTransform items)
    {
        var viewport = new GameObject("ModViewport",
            typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();

        var siblingIndex = items.GetSiblingIndex();
        viewport.SetParent(section, false);
        viewport.SetSiblingIndex(siblingIndex);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.pivot = new Vector2(0.5f, 0.5f);
        viewport.offsetMin = Vector2.zero;
        viewport.offsetMax = new Vector2(0f, -(StripHeight + ActionBarHeight));

        items.SetParent(viewport, false);
        items.anchorMin = Vector2.zero;
        items.anchorMax = Vector2.one;
        items.pivot = new Vector2(0.5f, 0.5f);
        items.offsetMin = Vector2.zero;
        items.offsetMax = Vector2.zero;

        if (items.GetComponent<VerticalLayoutGroup>() is { } layout)
            layout.padding = new RectOffset(
                layout.padding.left,
                layout.padding.right,
                12,
                layout.padding.bottom);
    }

    private void RequestSelect(string mod)
    {
        if (mod == _selected) return;

        if (_pending.Count == 0)
        {
            Select(mod);
            return;
        }

        PromptLeave(() => Select(mod));
    }

    public void PromptLeave(Action onLeave)
    {
        if (!HasPending)
        {
            onLeave();
            return;
        }

        if (_popup == null && _section.parent is RectTransform root)
            _popup = ModsConfirmPopup.Create(root);

        if (_popup == null)
        {
            Plugin.Logger.LogError("Could not create the unsaved-changes popup");
            return;
        }

        _popup.Show(
            "You have unsaved changes.",
            () =>
            {
                SavePending();
                onLeave();
            },
            () =>
            {
                DiscardPending();
                onLeave();
            },
            null);
    }

    private void Select(string mod)
    {
        _selected = mod;

        if (_placeholder != null)
            _placeholder.gameObject.SetActive(false);

        UpdateCardStates();
        RebuildConfig();
        ResetScroll();
        UpdateActionButtons();
    }

    private void ResetScroll()
    {
        if (_items != null)
            _items.anchoredPosition = new Vector2(_items.anchoredPosition.x, 0f);

        if (_sectionScroll != null && _sectionScroll.ScrollBar != null)
            _sectionScroll.ScrollBar.SetScroll(new Vector2(_sectionScroll.ScrollBar.Value.x, 1f));
    }

    private void RebuildConfig()
    {
        _revertButtons.Clear();
        _resetButtons.Clear();

        for (var i = _items.childCount - 1; i >= 0; i--)
        {
            var child = _items.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }

        var mod = _mods.FirstOrDefault(m => m.Name == _selected);
        if (mod != null)
        {
            _width = _items.rect.width;

            _building = true;
            ModSettings.BuildModConfig(_items, mod.Name, mod.Entries, _width, MarkDirty, this);
            _building = false;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_items);

        if (mod != null)
            foreach (var entry in mod.Entries)
            {
                UpdateRevertVisible(entry);
                UpdateResetVisible(entry);
            }
    }

    private void UpdateCardStates()
    {
        foreach (var card in _cards)
        {
            var selected = card.Name == _selected;
            var fill = selected
                ? new Color(0.18f, 0.18f, 0.18f, 1f)
                : new Color(0.09f, 0.09f, 0.09f, 1f);

            card.Button.SetDefaultColor(fill);
            card.Button.SetHoverColor(selected
                ? new Color(0.22f, 0.22f, 0.22f, 1f)
                : new Color(0.14f, 0.14f, 0.14f, 1f));

            if (card.Button.MainGraphic != null)
                card.Button.MainGraphic.color = fill;

            var outline = card.Button.GetComponent<BevelRectOutline>();
            if (outline != null)
                outline.color = selected ? Color.white : new Color(1f, 1f, 1f, 0.3f);
        }
    }

    private sealed class Card
    {
        public DefaultButton Button;
        public string Name;
    }
}

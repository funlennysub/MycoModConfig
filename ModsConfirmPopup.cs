using System;
using System.Collections;
using System.Reflection;
using Pigeon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MycoModConfig;

public sealed class ModsConfirmPopup : MonoBehaviour
{
    private DefaultButton _cancelButton;
    private DefaultButton _discardButton;
    private float _ignoreInputUntil;
    private Action _onCancel;
    private Action _onDiscard;
    private Action _onSave;
    private DefaultButton _saveButton;
    private TextMeshProUGUI _text;

    public static bool IsOpen { get; private set; }

    private void Update()
    {
        if (!IsOpen || Time.unscaledTime < _ignoreInputUntil) return;

        if (PlayerInput.Controls.Menu.Cancel.WasPressedThisFrame() ||
            PlayerInput.Controls.Menu.Close.WasPressedThisFrame())
            Choose(_onCancel);
    }

    public static ModsConfirmPopup Create(RectTransform settingsRoot)
    {
        var template = settingsRoot.Find("ConfirmScreen");
        if (template == null) return null;

        var clone = Instantiate(template.gameObject, settingsRoot, false);
        clone.name = "ModsConfirmScreen";

        var popup = clone.AddComponent<ModsConfirmPopup>();
        popup.Build();
        clone.SetActive(false);
        return popup;
    }

    private void Build()
    {
        var root = (RectTransform)transform;
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = Vector2.zero;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.SetAsLastSibling();

        if (GetComponent<Image>() is { } overlay)
        {
            overlay.color = new Color(0.03f, 0.03f, 0.03f, 0.92f);
            overlay.raycastTarget = true;
        }

        _text = transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
        if (_text != null)
        {
            var textRt = _text.rectTransform;
            textRt.anchorMin = new Vector2(0.5f, 0.5f);
            textRt.anchorMax = new Vector2(0.5f, 0.5f);
            textRt.pivot = new Vector2(0.5f, 0.5f);
            textRt.sizeDelta = new Vector2(900f, 200f);
            textRt.anchoredPosition = new Vector2(0f, 100f);
            _text.alignment = TextAlignmentOptions.Center;
        }

        if (transform.Find("Timer") is { } timer)
            timer.gameObject.SetActive(false);

        _saveButton = ConfigureButton(transform.Find("Y"), "SAVE", -260f);
        _discardButton = ConfigureButton(transform.Find("N"), "DISCARD", 0f);

        if (_discardButton != null)
        {
            var cancel = Instantiate(_discardButton, transform, false);
            cancel.gameObject.name = "CANCEL";
            _cancelButton = ConfigureButton(cancel.transform, "CANCEL", 260f);
        }

        if (_saveButton != null) _saveButton.OnClickUp.AddListener(() => Choose(_onSave));
        if (_discardButton != null) _discardButton.OnClickUp.AddListener(() => Choose(_onDiscard));
        if (_cancelButton != null) _cancelButton.OnClickUp.AddListener(() => Choose(_onCancel));
    }

    private static DefaultButton ConfigureButton(Transform buttonTransform, string label, float x)
    {
        if (buttonTransform == null) return null;

        var button = buttonTransform.GetComponent<DefaultButton>();
        if (button == null) return null;

        button.OnClickUp.RemoveAllListeners();
        ClearPersistentCalls(button.OnClickUp);

        var rt = (RectTransform)button.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(240f, 64f);
        rt.anchoredPosition = new Vector2(x, -100f);

        if (button.SecondaryGraphic is TextMeshProUGUI text)
        {
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 18f;
            text.raycastTarget = false;
        }

        return button;
    }

    private static void ClearPersistentCalls(UnityEventBase evt)
    {
        var group = typeof(UnityEventBase)
            .GetField("m_PersistentCalls", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(evt);

        var calls = group?.GetType()
            .GetField("m_Calls", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(group) as IList;

        calls?.Clear();
    }

    public void Show(string message, Action onSave, Action onDiscard, Action onCancel)
    {
        _onSave = onSave;
        _onDiscard = onDiscard;
        _onCancel = onCancel;

        SetButtonLabel(_saveButton, "SAVE");
        SetButtonLabel(_discardButton, "DISCARD");
        SetButtonLabel(_cancelButton, "CANCEL");

        if (_saveButton != null) _saveButton.gameObject.SetActive(true);
        if (_discardButton != null) _discardButton.gameObject.SetActive(true);
        if (_cancelButton != null) _cancelButton.gameObject.SetActive(true);

        Open(message);
    }

    public void ShowConfirmation(string message, string confirmLabel, Action onConfirm)
    {
        _onSave = onConfirm;
        _onDiscard = null;
        _onCancel = null;

        SetButtonLabel(_saveButton, confirmLabel);
        SetButtonLabel(_cancelButton, "CANCEL");

        if (_saveButton != null) _saveButton.gameObject.SetActive(true);
        if (_discardButton != null) _discardButton.gameObject.SetActive(false);
        if (_cancelButton != null) _cancelButton.gameObject.SetActive(true);

        Open(message);
    }

    private void Open(string message)
    {
        if (_text != null) _text.text = message;

        gameObject.SetActive(true);
        IsOpen = true;
        _ignoreInputUntil = Time.unscaledTime + 0.15f;
        transform.SetAsLastSibling();
    }

    private static void SetButtonLabel(DefaultButton button, string label)
    {
        if (button?.SecondaryGraphic is TextMeshProUGUI text)
            text.text = label;
    }

    private void Choose(Action action)
    {
        Hide();
        action?.Invoke();
    }

    private void Hide()
    {
        IsOpen = false;
        _onSave = null;
        _onDiscard = null;
        _onCancel = null;
        gameObject.SetActive(false);
    }
}
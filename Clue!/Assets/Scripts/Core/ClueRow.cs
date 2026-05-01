using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CluedoRow : MonoBehaviour
{
    [Header("Row references")]
    public TMP_Text labelText;
    public Button noButton;
    public Button maybeButton;
    public Button yesButton;

    static readonly Color ColNo = new Color(0.89f, 0.29f, 0.29f, 1f);
    static readonly Color ColMaybe = new Color(0.22f, 0.54f, 0.87f, 1f);
    static readonly Color ColYes = new Color(0.39f, 0.60f, 0.13f, 1f);
    static readonly Color ColInactive = new Color(0.82f, 0.82f, 0.82f, 1f);

    ClueEntry entry;
    Action onChange;

    public void Initialize(ClueEntry e, Action onChanged)
    {
        entry = e;
        onChange = onChanged;
        labelText.text = e.name;
        noButton.onClick.AddListener(() => Toggle(ClueState.No));
        maybeButton.onClick.AddListener(() => Toggle(ClueState.Maybe));
        yesButton.onClick.AddListener(() => Toggle(ClueState.Yes));
        RefreshUI();
    }

    public void Init(ClueEntry e, Action onChanged) => Initialize(e, onChanged);

    void Toggle(ClueState clicked)
    {
        entry.state = (entry.state == clicked) ? ClueState.None : clicked;
        RefreshUI();
        onChange?.Invoke();
    }

    public void RefreshUI()
    {
        SetColor(noButton, entry.state == ClueState.No ? ColNo : ColInactive);
        SetColor(maybeButton, entry.state == ClueState.Maybe ? ColMaybe : ColInactive);
        SetColor(yesButton, entry.state == ClueState.Yes ? ColYes : ColInactive);

        noButton.interactable = true;
        maybeButton.interactable = true;
        yesButton.interactable = true;
    }

    public void Refresh() => RefreshUI();

    void SetColor(Button btn, Color c)
    {
        var cb = btn.colors;
        cb.normalColor = c;
        cb.highlightedColor = c * 1.1f;
        cb.selectedColor = c;
        btn.colors = cb;
    }
}
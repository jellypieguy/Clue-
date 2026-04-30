using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CluedoRow : MonoBehaviour
{
    [Header("Row references")]
    public TMP_Text labelText;
    public Button   noButton;
    public Button   maybeButton;
    public Button   yesButton;

    static readonly Color ColNo       = new Color(0.89f, 0.29f, 0.29f, 1f);
    static readonly Color ColMaybe    = new Color(0.22f, 0.54f, 0.87f, 1f);
    static readonly Color ColYes      = new Color(0.39f, 0.60f, 0.13f, 1f);
    static readonly Color ColInactive = new Color(0.82f, 0.82f, 0.82f, 1f);
    static readonly Color ColLocked   = new Color(0.25f, 0.72f, 0.43f, 1f);

    ClueEntry entry;
    Action    onChange;

    public void Init(ClueEntry e, Action onChanged)
    {
        entry    = e;
        onChange = onChanged;
        labelText.text = e.name;
        noButton   .onClick.AddListener(() => Toggle(ClueState.No));
        maybeButton.onClick.AddListener(() => Toggle(ClueState.Maybe));
        yesButton  .onClick.AddListener(() => Toggle(ClueState.Yes));
        Refresh();
    }

    void Toggle(ClueState clicked)
    {
        if (entry.isInHand) return;
        entry.state = (entry.state == clicked) ? ClueState.None : clicked;
        Refresh();
        onChange?.Invoke();
    }

    public void Refresh()
    {
        if (entry.isInHand)
        {
            SetColor(noButton,    ColLocked);
            SetColor(maybeButton, ColLocked);
            SetColor(yesButton,   ColLocked);
            noButton   .interactable = false;
            maybeButton.interactable = false;
            yesButton  .interactable = false;
            if (!labelText.text.EndsWith(" ✓"))
                labelText.text += " ✓";
            return;
        }
        SetColor(noButton,    entry.state == ClueState.No    ? ColNo    : ColInactive);
        SetColor(maybeButton, entry.state == ClueState.Maybe ? ColMaybe : ColInactive);
        SetColor(yesButton,   entry.state == ClueState.Yes   ? ColYes   : ColInactive);
    }

    void SetColor(Button btn, Color c)
    {
        var cb = btn.colors;
        cb.normalColor      = c;
        cb.highlightedColor = c * 1.1f;
        cb.selectedColor    = c;
        btn.colors = cb;
    }
}
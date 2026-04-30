using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class InstructionsUI : MonoBehaviour
{
    [SerializeField] private GameObject instructionsPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        
        instructionsPanel.SetActive(false);
        closeButton.onClick.AddListener(Hide);
    }

    public void Show()
    {
        instructionsPanel.SetActive(true);
        TriggerFade(0f, 1f, 0.2f);
    }

    public void Hide()
    {
        TriggerFade(1f, 0f, 0.2f, () => instructionsPanel.SetActive(false));
    }

    private void TriggerFade(float startAlpha, float targetAlpha, float duration, Action onComplete = null)
    {
        if (canvasGroup == null)
        {
            onComplete?.Invoke();
            return;
        }
        
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(startAlpha, targetAlpha, duration, onComplete));
    }

    private IEnumerator FadeRoutine(float startAlpha, float targetAlpha, float duration, Action onComplete)
    {
        float elapsed = 0f;
        canvasGroup.alpha = startAlpha;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }
        
        canvasGroup.alpha = targetAlpha;
        onComplete?.Invoke();
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class InstructionsUI : MonoBehaviour
{
    [Header("UI Ref")]
    [SerializeField] private GameObject instructionsPanel; // show/hide panel
    [SerializeField] private Button closeButton;          // back button
    [SerializeField] private CanvasGroup canvasGroup;  //animeation     

    private void Awake()
    {

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        instructionsPanel.SetActive(false);
        closeButton.onClick.AddListener(Hide);
    }

    public void Show()
    {
        instructionsPanel.SetActive(true);
        StartFade(0f, 1f, 0.2f);
    }

    public void Hide()
    {
        StartFade(1f, 0f, 0.2f, () => instructionsPanel.SetActive(false));
    }

    private void StartFade(float from, float to, float duration, System.Action onComplete = null)
    {
        if (canvasGroup == null)
        {
            onComplete?.Invoke();
            return;
        }
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(from, to, duration, onComplete));
    }

    private IEnumerator FadeRoutine(float from, float to, float duration, System.Action onComplete)
    {
        float elapsed = 0f;
        canvasGroup.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
        onComplete?.Invoke();
    }
}

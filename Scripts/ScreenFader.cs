using System.Collections;

using UnityEngine;

/// <summary>
/// 풀스크린 검은 오버레이의 알파를 코루틴으로 제어하는 페이드 유틸.
/// FadeCanvas 오브젝트의 CanvasGroup 에 붙여서 사용한다.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float defaultDuration = 0.35f;

    void Awake()
    {
        SetOpaque();
    }

    /// <summary>즉시 완전 불투명 상태(검은 화면)로 설정한다.</summary>
    public void SetOpaque()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    /// <summary>즉시 완전 투명 상태(화면 보임)로 설정한다.</summary>
    public void SetClear()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    /// <summary>투명 → 불투명으로 페이드한다 (화면이 검어짐).</summary>
    public IEnumerator FadeIn(float duration = -1f)
    {
        return Fade(canvasGroup.alpha, 1f, duration < 0f ? defaultDuration : duration);
    }

    /// <summary>불투명 → 투명으로 페이드한다 (화면이 보이기 시작함).</summary>
    public IEnumerator FadeOut(float duration = -1f)
    {
        return Fade(canvasGroup.alpha, 0f, duration < 0f ? defaultDuration : duration);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        canvasGroup.alpha = from;
        canvasGroup.blocksRaycasts = true;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
        canvasGroup.blocksRaycasts = to > 0f;
    }
}

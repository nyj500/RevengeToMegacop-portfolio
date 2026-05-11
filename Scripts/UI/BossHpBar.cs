using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스 등장 시 상단 중앙에 표시되는 HP 바.
/// StageSelectController가 현재 Stage의 EnemySpawner를 주입하면,
/// OnBossSpawned 이벤트를 통해 보스에 바인딩되어 fill/text를 갱신한다.
/// </summary>
[RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
public class BossHpBar : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform rootRect;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("Intro/Outro")]
    [SerializeField] private float fadeInDuration = 0.35f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private Vector3 introScale = new(0.6f, 0.6f, 1f);
    [SerializeField] private Vector3 shownScale = Vector3.one;

    private EnemySpawner boundSpawner;
    private BossEnemy boundBoss;
    private Coroutine activeTween;

    void Awake()
    {
        if (canvasGroup == null) TryGetComponent(out canvasGroup);
        if (rootRect == null) rootRect = transform as RectTransform;
        ResetVisuals();
    }

    void OnDestroy()
    {
        UnbindSpawner();
        UnbindBoss();
    }

    /// <summary>
    /// StageSelectController가 현재 Stage 시작 시 호출해 EnemySpawner를 주입한다.
    /// 이전 바인딩은 해제하고 UI를 숨김 상태로 리셋한다.
    /// </summary>
    public void BindSpawner(EnemySpawner newSpawner)
    {
        UnbindSpawner();
        UnbindBoss();
        ResetVisuals();

        boundSpawner = newSpawner;
        if (boundSpawner != null)
            boundSpawner.OnBossSpawned += HandleBossSpawned;
    }

    private void UnbindSpawner()
    {
        if (boundSpawner == null) return;
        boundSpawner.OnBossSpawned -= HandleBossSpawned;
        boundSpawner = null;
    }

    private void UnbindBoss()
    {
        if (boundBoss == null) return;
        boundBoss.OnHpChanged -= HandleHpChanged;
        boundBoss.OnDeath -= HandleDeath;
        boundBoss = null;
    }

    private void ResetVisuals()
    {
        if (activeTween != null)
        {
            StopCoroutine(activeTween);
            activeTween = null;
        }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        if (rootRect != null) rootRect.localScale = introScale;
    }

    private void HandleBossSpawned(BossEnemy boss)
    {
        if (boss == null) return;
        UnbindBoss();
        boundBoss = boss;
        boss.OnHpChanged += HandleHpChanged;
        boss.OnDeath += HandleDeath;
        HandleHpChanged(boss.HpRatio);
        PlayIntro();
    }

    private void HandleHpChanged(float ratio)
    {
        if (hpSlider != null) hpSlider.SetValueWithoutNotify(ratio);
        if (hpText != null && boundBoss != null)
        {
            int current = Mathf.CeilToInt(boundBoss.Hp);
            int max = Mathf.CeilToInt(boundBoss.MaxHp);
            hpText.SetText("{0} / {1}", current, max);
        }
    }

    private void HandleDeath(Enemy _)
    {
        PlayOutro();
        UnbindBoss();
    }

    private void PlayIntro()
    {
        if (activeTween != null) StopCoroutine(activeTween);
        activeTween = StartCoroutine(IntroRoutine());
    }

    private void PlayOutro()
    {
        if (activeTween != null) StopCoroutine(activeTween);
        activeTween = StartCoroutine(OutroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        Vector3 startScale = rootRect != null ? rootRect.localScale : shownScale;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            float eased = 1f - (1f - t) * (1f - t);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, eased);
            if (rootRect != null) rootRect.localScale = Vector3.Lerp(startScale, shownScale, eased);
            yield return null;
        }
        canvasGroup.alpha = 1f;
        if (rootRect != null) rootRect.localScale = shownScale;
        activeTween = null;
    }

    private IEnumerator OutroRoutine()
    {
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        activeTween = null;
    }
}

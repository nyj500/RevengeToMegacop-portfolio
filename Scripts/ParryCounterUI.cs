using System.Collections;
using DamageNumbersPro;
using MoreMountains.Feedbacks;
using UnityEngine;

/// <summary>
/// 패리 성공 횟수를 화면 우측에 콤보 카운터 스타일로 표시하는 UI.
/// 콤보가 쌓일수록 스케일·색상·회전이 점점 강해지는 하이프 스타일.
/// 패리 없거나 피격 시 페이드아웃 후 리셋.
/// 하위 오브젝트(컨테이너·텍스트·피드백 플레이어)는 프리팹에 미리 배치되어야 한다.
/// </summary>
public class ParryCounterUI : MonoBehaviour
{
    [SerializeField] private PlayerHitController playerHitController;

    [Header("UI 참조")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private DamageNumberGUI counterNumber;
    [SerializeField] private DamageNumberGUI rankNumber;

    [Header("피드백 플레이어")]
    [SerializeField] private MMF_Player fadeInPlayer;
    [SerializeField] private MMF_Player fadeOutPlayer;
    [SerializeField] private MMF_Player rotationPlayer;
    [SerializeField] private MMF_Player rankDownPlayer;
    [SerializeField] private MMF_Player damagedPlayer;

    [Header("타이밍")]
    [SerializeField] private float resetDelay = 3f;

    [Header("회전 Wobble")]
    [SerializeField] private int rotationMinCombo = 3;

    [Header("랭크")]
    [SerializeField] private int rankStep = 10;
    [SerializeField] private string[] rankLabels = { "E", "D", "C", "B", "A", "S", "SS", "SSS" };

    [Header("랭크 색상")]
    [SerializeField] private Color[] rankColors = {
        Color.white,                            // E
        new Color(1.00f, 0.90f, 0.43f),         // D
        new Color(1.00f, 0.55f, 0.26f),         // C
        new Color(1.00f, 0.24f, 0.24f),         // B
        new Color(1.00f, 0.20f, 1.00f),         // A
        new Color(1.00f, 0.85f, 0.00f),         // S
        new Color(0.00f, 0.90f, 1.00f),         // SS
        new Color(1.00f, 0.00f, 0.40f)          // SSS
    };

    private int parryCount;
    private int lastRankIndex = -1;
    private Coroutine resetCoroutine;
    private WaitForSeconds waitForResetDelay;

    void Start()
    {
        if (playerHitController == null)
        {
            playerHitController = FindFirstObjectByType<PlayerHitController>();
        }

        if (playerHitController == null)
        {
            Debug.LogError("ParryCounterUI: PlayerHitController를 찾을 수 없습니다.");
            return;
        }

        if (canvasGroup == null || counterNumber == null || rankNumber == null ||
            fadeInPlayer == null || fadeOutPlayer == null || rotationPlayer == null ||
            rankDownPlayer == null || damagedPlayer == null)
        {
            Debug.LogError("ParryCounterUI: 프리팹 참조가 누락되었습니다.");
            return;
        }

        waitForResetDelay = new WaitForSeconds(resetDelay);

        // 페이드아웃 완료 시 카운터 리셋 (UnityEvent 런타임 구독)
        fadeOutPlayer.Events.OnComplete.AddListener(ResetCounter);

        canvasGroup.alpha = 0f;

        playerHitController.OnParry += OnParry;
        playerHitController.OnDamaged += OnDamaged;
    }

    void OnDestroy()
    {
        if (playerHitController != null)
        {
            playerHitController.OnParry -= OnParry;
            playerHitController.OnDamaged -= OnDamaged;
        }

        if (fadeOutPlayer != null)
        {
            fadeOutPlayer.Events.OnComplete.RemoveListener(ResetCounter);
        }
    }

    private void OnParry()
    {
        if (fadeOutPlayer.IsPlaying)
        {
            fadeOutPlayer.StopFeedbacks();
            canvasGroup.alpha = 1f;
        }

        parryCount++;
        int rankIndex = Mathf.Min(parryCount / rankStep, rankLabels.Length - 1);

        // SetColor는 UpdateText 이전에 호출해야 한다.
        // UpdateText가 tmp.color를 mesh 버텍스 컬러 캐시(colors[])에 저장하고
        // 매 프레임 UpdateAlpha가 그 캐시를 사용하므로, UpdateText 이후 SetColor는
        // 다음 UpdateText 호출 전까지 시각적으로 반영되지 않는다.
        if (rankIndex != lastRankIndex)
        {
            Color rankColor = GetRankColor(rankIndex);
            counterNumber.SetColor(rankColor);
            rankNumber.SetColor(rankColor);
            lastRankIndex = rankIndex;
        }

        counterNumber.number = parryCount;
        counterNumber.UpdateText();
        rankNumber.leftText = rankLabels[rankIndex];
        rankNumber.UpdateText();

        counterNumber.FadeIn();

        if (parryCount == 1)
        {
            fadeInPlayer.PlayFeedbacks();
        }

        if (parryCount >= rotationMinCombo)
        {
            rotationPlayer.PlayFeedbacks();
        }

        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
        }
        resetCoroutine = StartCoroutine(ResetAfterDelay());
    }

    private void OnDamaged()
    {
        if (parryCount <= 0) return;

        damagedPlayer.PlayFeedbacks();

        int previousRankIndex = lastRankIndex;
        parryCount = Mathf.Max(0, parryCount - 5);

        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
            resetCoroutine = null;
        }

        int rankIndex = Mathf.Min(parryCount / rankStep, rankLabels.Length - 1);

        if (rankIndex < previousRankIndex)
        {
            rankDownPlayer.PlayFeedbacks();
        }

        if (parryCount <= 0)
        {
            StartFadeOutAndReset();
            return;
        }

        // SetColor는 UpdateText 이전에 호출 (OnParry의 주석 참고).
        if (rankIndex != lastRankIndex)
        {
            Color rankColor = GetRankColor(rankIndex);
            counterNumber.SetColor(rankColor);
            rankNumber.SetColor(rankColor);
            lastRankIndex = rankIndex;
        }

        counterNumber.number = parryCount;
        counterNumber.UpdateText();
        rankNumber.leftText = rankLabels[rankIndex];
        rankNumber.UpdateText();

        resetCoroutine = StartCoroutine(ResetAfterDelay());
    }

    private IEnumerator ResetAfterDelay()
    {
        yield return waitForResetDelay;
        resetCoroutine = null;
        StartFadeOutAndReset();
    }

    private void StartFadeOutAndReset()
    {
        if (fadeOutPlayer.IsPlaying) return;
        fadeOutPlayer.PlayFeedbacks();
    }

    private void ResetCounter()
    {
        parryCount = 0;
        lastRankIndex = -1;
        counterNumber.number = 0;
        rankNumber.leftText = "";
        counterNumber.SetColor(Color.white);
        rankNumber.SetColor(Color.white);
        counterNumber.UpdateText();
        rankNumber.UpdateText();
    }

    private Color GetRankColor(int rankIndex)
    {
        if (rankColors == null || rankColors.Length == 0) return Color.white;
        return rankColors[Mathf.Clamp(rankIndex, 0, rankColors.Length - 1)];
    }
}

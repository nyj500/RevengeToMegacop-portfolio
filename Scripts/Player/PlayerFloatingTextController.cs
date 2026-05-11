using System.Collections;
using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;

/// <summary>
/// 플레이어 머리 위에 상황별 텍스트를 단일 TMP로 재사용해 표시한다.
/// - 패리 성공 → "Parry" (시안)
/// - 가드 성공 → "Guard" (앰버)
/// - 피격     → "Noob"  (빨강)
///
/// 이벤트 발생 시: 텍스트/색 갱신 → (숨김 상태면) 페이드인 → 펀치 스케일 → 바운스.
/// resetDelay 후 자동으로 페이드아웃 (새 이벤트 발생 시 리셋 타이머 재시작).
/// </summary>
public class PlayerFloatingTextController : MonoBehaviour
{
    [SerializeField] private PlayerHitController playerHitController;

    [Header("UI 참조")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI textA;
    [SerializeField] private TextMeshProUGUI textB;

    [Header("피드백 플레이어")]
    [SerializeField] private MMF_Player fadeInPlayer;
    [SerializeField] private MMF_Player fadeOutPlayer;
    [SerializeField] private MMF_Player punchPlayer;
    [SerializeField] private MMF_Player bouncePlayer;

    [Header("바운스 오프셋 (월드 미터, HeadText local)")]
    [SerializeField] private float bounceOffsetX = 0.6f;
    [SerializeField] private float bounceOffsetY = 0.4f;

    [Header("타이밍")]
    [SerializeField] private float resetDelay = 1.2f;

    [Header("색상")]
    [SerializeField] private Color parryColor = new Color(0f, 0.898f, 1f);
    [SerializeField] private Color guardColor = new Color(1f, 0.757f, 0.027f);
    [SerializeField] private Color damagedColor = new Color(1f, 0.231f, 0.188f);

    private Coroutine resetCoroutine;
    private WaitForSeconds waitForResetDelay;
    private MMF_Position bounceFeedback;
    private Transform canvasTransform;

    void Awake()
    {
        if (playerHitController == null)
        {
            TryGetComponent(out playerHitController);
        }
    }

    void Start()
    {
        if (playerHitController == null)
        {
            Debug.LogError("PlayerFloatingTextController: PlayerHitController를 찾을 수 없습니다.");
            return;
        }

        if (canvasGroup == null || textA == null || textB == null ||
            fadeInPlayer == null || fadeOutPlayer == null ||
            punchPlayer == null || bouncePlayer == null)
        {
            Debug.LogError("PlayerFloatingTextController: 참조가 누락되었습니다.");
            return;
        }

        waitForResetDelay = new WaitForSeconds(resetDelay);

        canvasTransform = textA.transform.parent;

        foreach (var feedback in bouncePlayer.FeedbacksList)
        {
            if (feedback is MMF_Position position)
            {
                bounceFeedback = position;
                break;
            }
        }

        fadeOutPlayer.Events.OnComplete.AddListener(HideText);

        canvasGroup.alpha = 0f;

        playerHitController.OnParry += OnParry;
        playerHitController.OnGuard += OnGuard;
        playerHitController.OnDamaged += OnDamaged;
    }

    void OnDestroy()
    {
        if (playerHitController != null)
        {
            playerHitController.OnParry -= OnParry;
            playerHitController.OnGuard -= OnGuard;
            playerHitController.OnDamaged -= OnDamaged;
        }

        if (fadeOutPlayer != null)
        {
            fadeOutPlayer.Events.OnComplete.RemoveListener(HideText);
        }
    }

    private void OnParry()
    {
        ShowText("Parry", parryColor);
    }

    private void OnGuard()
    {
        ShowText("Guard", guardColor);
    }

    private void OnDamaged()
    {
        ShowText("Noob", damagedColor);
    }

    private void ShowText(string value, Color color)
    {
        if (fadeOutPlayer.IsPlaying)
        {
            fadeOutPlayer.StopFeedbacks();
        }

        textA.text = value;
        textB.text = value;
        textA.color = color;
        textB.color = color;

        if (canvasGroup.alpha < 1f)
        {
            fadeInPlayer.PlayFeedbacks();
        }

        punchPlayer.PlayFeedbacks();

        if (canvasTransform != null)
        {
            canvasTransform.localPosition = Vector3.zero;
        }

        if (bounceFeedback != null)
        {
            float sign = Random.value < 0.5f ? -1f : 1f;
            bounceFeedback.DestinationPosition = new Vector3(sign * bounceOffsetX, bounceOffsetY, 0f);
        }
        bouncePlayer.PlayFeedbacks();

        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
        }
        resetCoroutine = StartCoroutine(ResetAfterDelay());
    }

    private IEnumerator ResetAfterDelay()
    {
        yield return waitForResetDelay;
        resetCoroutine = null;
        StartFadeOut();
    }

    private void StartFadeOut()
    {
        if (fadeOutPlayer.IsPlaying) return;
        fadeOutPlayer.PlayFeedbacks();
    }

    private void HideText()
    {
        canvasGroup.alpha = 0f;
        if (canvasTransform != null)
        {
            canvasTransform.localPosition = Vector3.zero;
        }
    }
}

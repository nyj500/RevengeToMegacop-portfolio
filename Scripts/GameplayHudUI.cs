using UnityEngine;
using UnityEngine.UI;

public class GameplayHudUI : MonoBehaviour
{
    [SerializeField] private Slider hpBar;
    [SerializeField] private Slider staminaBar;
    [SerializeField] private Slider executionGaugeBar;
    [SerializeField] private PlayerStateController playerStateController;

    void Start()
    {
        if (hpBar == null || staminaBar == null || executionGaugeBar == null || playerStateController == null)
        {
            Debug.LogError("GameplayHudUI: 필수 참조가 할당되지 않았습니다.");
            return;
        }

        hpBar.interactable = false;
        staminaBar.interactable = false;
        executionGaugeBar.interactable = false;

        playerStateController.OnHpChanged += UpdateHp;
        playerStateController.OnStaminaChanged += UpdateStamina;
        playerStateController.OnExecutionGaugeChanged += UpdateExecutionGauge;

        UpdateHp(playerStateController.HpRatio);
        UpdateStamina(playerStateController.StaminaRatio);
        UpdateExecutionGauge(playerStateController.ExecutionGaugeRatio);
    }

    void OnDestroy()
    {
        if (playerStateController != null)
        {
            playerStateController.OnHpChanged -= UpdateHp;
            playerStateController.OnStaminaChanged -= UpdateStamina;
            playerStateController.OnExecutionGaugeChanged -= UpdateExecutionGauge;
        }
    }

    private void UpdateHp(float ratio) => hpBar.value = ratio;
    private void UpdateStamina(float ratio) => staminaBar.value = ratio;
    private void UpdateExecutionGauge(float ratio) => executionGaugeBar.value = ratio;
}

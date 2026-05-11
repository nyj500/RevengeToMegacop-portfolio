using UnityEngine;

public class UIController : MonoBehaviour
{
    [SerializeField] private Transform hp;
    [SerializeField] private Transform executionGauge;
    [SerializeField] private Transform stamina;
    [SerializeField] private PlayerStateController playerStateController;

    void Start()
    {
        if (hp == null || executionGauge == null || stamina == null || playerStateController == null)
        {
            Debug.LogError("One or more required components are not assigned in UIController.");
            return;
        }
        playerStateController.OnHpChanged += UpdateHp;
        playerStateController.OnExecutionGaugeChanged += UpdateExecutionGauge;
        playerStateController.OnStaminaChanged += UpdateStamina;
        UpdateHp(playerStateController.HpRatio);
        UpdateExecutionGauge(playerStateController.ExecutionGaugeRatio);
        UpdateStamina(playerStateController.StaminaRatio);
    }

    void OnDestroy()
    {
        if (playerStateController != null)
        {
            playerStateController.OnHpChanged -= UpdateHp;
            playerStateController.OnExecutionGaugeChanged -= UpdateExecutionGauge;
            playerStateController.OnStaminaChanged -= UpdateStamina;
        }
    }

    private void UpdateHp(float ratio)
    {
        hp.localScale = new Vector3(ratio, 1, 1);
    }

    private void UpdateExecutionGauge(float ratio)
    {
        executionGauge.localScale = new Vector3(ratio, 1, 1);
    }

    private void UpdateStamina(float ratio)
    {
        stamina.localScale = new Vector3(ratio, 1, 1);
    }
}

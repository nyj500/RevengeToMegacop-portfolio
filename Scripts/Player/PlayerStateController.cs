using System;

using UnityEngine;

public class PlayerStateController : MonoBehaviour
{
    [SerializeField] private float hp;
    public float Hp
    {
        get => hp;
        private set
        {
            hp = Mathf.Clamp(value, 0, maxHp);
            OnHpChanged?.Invoke(HpRatio);
        }
    }
    [SerializeField] private float maxHp = 100f;
    public float MaxHp { get => maxHp; private set => maxHp = value; }
    [SerializeField] private float executionGauge;
    public float ExecutionGauge
    {
        get => executionGauge;
        private set
        {
            executionGauge = Mathf.Clamp(value, 0, maxExecutionGauge);
            OnExecutionGaugeChanged?.Invoke(ExecutionGaugeRatio);
        }
    }
    [SerializeField] private float maxExecutionGauge = 100f;
    public float MaxExecutionGauge { get => maxExecutionGauge; private set => maxExecutionGauge = value; }
    [SerializeField] private float executionGaugeIncreaseStep = 10f;
    public float ExecutionGaugeIncreaseStep { get => executionGaugeIncreaseStep; private set => executionGaugeIncreaseStep = value; }

    [SerializeField] private float stamina;
    public float Stamina
    {
        get => stamina;
        private set
        {
            stamina = Mathf.Clamp(value, 0, maxStamina);
            OnStaminaChanged?.Invoke(StaminaRatio);
        }
    }
    [SerializeField] private float maxStamina = 100f;
    public float MaxStamina { get => maxStamina; private set => maxStamina = value; }

    [SerializeField] private float staminaRecoveryStep = 10f;

    [SerializeField] private float staminaDecreaseStep = 10f;

    [SerializeField] private float staminaRecoveryTimeStep = 0.5f;
    private float currentStaminaRecoveryTime;

    public float HpRatio => MaxHp > 0f ? Hp / MaxHp : 0f;
    public float ExecutionGaugeRatio => MaxExecutionGauge > 0f ? ExecutionGauge / MaxExecutionGauge : 0f;
    public float StaminaRatio => MaxStamina > 0f ? Stamina / MaxStamina : 0f;

    public bool IsDead => Hp <= 0;

    public event Action<float> OnHpChanged;
    public event Action<float> OnExecutionGaugeChanged;
    public event Action<float> OnStaminaChanged;
    public event Action OnDeath;

    void Awake()
    {
        if (hp <= 0) hp = maxHp;
        if (stamina <= 0) stamina = maxStamina;
        currentStaminaRecoveryTime = staminaRecoveryTimeStep;
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0) return;
        Hp -= damage;
        if (Hp <= 0) OnDeath?.Invoke();
    }

    public void Heal(float amount)
    {
        if (amount <= 0) return;
        Hp += amount;
    }

    public void IncreaseExecutionGauge()
    {
        if (ExecutionGaugeIncreaseStep <= 0) return;
        ExecutionGauge += ExecutionGaugeIncreaseStep;
    }

    public bool CanExecute()
    {
        return MaxExecutionGauge <= ExecutionGauge;
    }

    public void Executed()
    {
        ExecutionGauge = 0;
    }

    public void UpdateStamina()
    {
        currentStaminaRecoveryTime -= Time.deltaTime;
        if (currentStaminaRecoveryTime <= 0)
        {
            if (staminaRecoveryStep > 0) Stamina += staminaRecoveryStep;
            currentStaminaRecoveryTime = staminaRecoveryTimeStep;
        }
    }

    public void DecreaseStamina()
    {
        Stamina -= staminaDecreaseStep;
    }

    public void OnSuccessfulParry()
    {
        IncreaseExecutionGauge();
    }

    public void OnSuccessfulGuard()
    {
        DecreaseStamina();
    }

    [SerializeField] private float parryStaminaCostRatio = 0.2f;

    public bool CanParry()
    {
        float parryStaminaCost = MaxStamina * parryStaminaCostRatio;
        return parryStaminaCost <= Stamina;
    }

    public bool CanGuard()
    {
        return 0 < Stamina;
    }

    private void NotifyUI()
    {
        OnHpChanged?.Invoke(HpRatio);
        OnExecutionGaugeChanged?.Invoke(ExecutionGaugeRatio);
        OnStaminaChanged?.Invoke(StaminaRatio);
    }

    void OnValidate()
    {
        if (Application.isPlaying) NotifyUI();
    }
}

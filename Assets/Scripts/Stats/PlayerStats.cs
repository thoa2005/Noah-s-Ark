using UnityEngine;
using System;

public class PlayerStats : MonoBehaviour
{
    [Header("Health/Stability")]
    public float currentStability = 100f;
    public float maxStability = 100f;
    public float recoveryTime = 3f;
    public bool isKnockedOut = false;
    public int lives = 3;


    [Header("Stamina")]
    public float currentStamina = 100f;
    public float maxStamina = 100f;
    public float staminaRegenRate = 15f;
    public float staminaRecoveryDelay = 1.0f;
    private float staminaDelayTimer;

    [Header("Movement Stats")]
    public float moveSpeed = 8f;
    public float jumpForce = 10f;

    [Header("Combat Stats")]
    public float punchForce = 15f;
    public float pushForce = 50f;
    public float punchCooldown = 0.5f;
    public float punchStaminaCost = 10f;

    [Header("Grab Stats")]
    public float grabStaminaCost = 15f;
    public float grabStaminaDrainRate = 20f;
    public float grabSpring = 15000f;
    public float grabDamper = 1000f;
    public float grabBreakForce = 800f;

    [Header("Throw Stats")]
    public float minThrowForce = 5f;
    public float maxThrowForce = 20f;
    public float maxChargeTime = 1.0f;

    // Sự kiện báo cho Ragdoll Controller biết khi nào cần gục
    public event Action OnKnockout;
    public event Action OnWakeUp;

    void Start()
    {
        currentStability = maxStability;
        currentStamina = maxStamina;
    }

    void Update()
    {
        RegenStamina();
    }

    private void RegenStamina()
    {
        if (isKnockedOut) return;

        if (staminaDelayTimer > 0)
        {
            staminaDelayTimer -= Time.deltaTime;
        }
        else if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isKnockedOut) return;

        currentStability -= amount;

        if (currentStability <= 0)
        {
            currentStability = 0;
            isKnockedOut = true;
            OnKnockout?.Invoke();
        }
    }

    public bool UseStamina(float amount)
    {
        if (currentStamina >= amount)
        {
            currentStamina -= amount;
            staminaDelayTimer = staminaRecoveryDelay;
            return true;
        }
        return false;
    }

    public void ResetAfterWakeUp()
    {
        currentStability = maxStability;
        isKnockedOut = false;
        OnWakeUp?.Invoke();
    }
}

using UnityEngine;
using System;
using Fusion;

public class PlayerStats : NetworkBehaviour
{
    [Header("Health/Stability")]
    [Networked] public float currentStability { get; set; }
    public float maxStability = 100f;
    public float recoveryTime = 3f;
    [Networked] public NetworkBool isKnockedOut { get; set; }
    [Networked] public int lives { get; set; }


    [Header("Stamina")]
    [Networked] public float currentStamina { get; set; }
    public float maxStamina = 200f;
    public float staminaRegenRate = 15f;
    public float staminaRecoveryDelay = 1.0f;
    [Networked] private float staminaDelayTimer { get; set; }

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

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            currentStability = maxStability;
            currentStamina = maxStamina;
            lives = 3;
            isKnockedOut = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        RegenStamina();
    }

    private void RegenStamina()
    {
        if (isKnockedOut) return;

        if (staminaDelayTimer > 0)
        {
            staminaDelayTimer -= Runner.DeltaTime;
        }
        else if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Runner.DeltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_TakeDamage(float amount)
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
        if (!HasStateAuthority) return;
        currentStability = maxStability;
        isKnockedOut = false;
        OnWakeUp?.Invoke();
    }
}

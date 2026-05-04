using UnityEngine;
using Mirror;
using System;

[RequireComponent(typeof(PlayerStats))]
public class PlayerCombat : NetworkBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float attackCooldown = 1.0f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Debug")]
    [SerializeField] private bool showCombatDebugLogs = true;

    private float lastAttackTime;
    private bool isAttacking;
    private bool combatEnabled = true;
    private bool attackHitPending;
    private bool autoAttackEnabled;

    private PlayerStats stats;
    private PlayerMovement movement;
    private Animator anim;
    private Transform currentTarget;

    public event Action OnAttackStarted;
    public event Action OnAttackFinished;
    public event Action<Transform> OnTargetChanged;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        movement = GetComponent<PlayerMovement>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        if (Input.GetKeyDown(KeyCode.Space))
            TryAttackTarget();

        TickAutoAttack();
    }

    public void SetTarget(Transform target)
    {
        currentTarget = target;
        OnTargetChanged?.Invoke(target);

        if (showCombatDebugLogs)
            Debug.Log("[PlayerCombat] Alvo definido: " + (target != null ? target.name : "NULL"));
    }

    public void ClearTarget()
    {
        autoAttackEnabled = false;
        currentTarget = null;
        attackHitPending = false;
        OnTargetChanged?.Invoke(null);
    }

    public void StartAutoAttack(Transform target)
    {
        if (target == null)
        {
            ClearTarget();
            return;
        }

        SetTarget(target);
        autoAttackEnabled = true;
        TryAttackTarget();
    }

    public void StopAutoAttack()
    {
        autoAttackEnabled = false;
    }

    [Client]
    public void TryAttackTarget()
    {
        if (!CanAttackNow())
            return;

        float dist = Vector3.Distance(transform.position, currentTarget.position);
        if (dist > attackRange)
        {
            if (autoAttackEnabled)
                ChaseCurrentTarget();

            if (showCombatDebugLogs)
                Debug.Log("[PlayerCombat] TryAttackTarget CANCELADO: alvo fora de range (" + dist.ToString("F2") + " > " + attackRange + ")");
            return;
        }

        movement?.StopMovement();
        transform.LookAt(new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z));

        if (showCombatDebugLogs)
        {
            Debug.Log("[PlayerCombat] ===============================================");
            Debug.Log("[PlayerCombat] TryAttackTarget() -> INICIANDO ATAQUE");
            Debug.Log("[PlayerCombat] Alvo: " + currentTarget.name);
            Debug.Log("[PlayerCombat] Distancia: " + dist.ToString("F2") + "m");
            Debug.Log("[PlayerCombat] HP: " + stats.Health + "/" + stats.MaxHealth);
            Debug.Log("[PlayerCombat] ===============================================");
        }

        lastAttackTime = Time.time;
        isAttacking = true;
        attackHitPending = false;
        OnAttackStarted?.Invoke();

        if (anim != null)
        {
            anim.ResetTrigger("Attack");
            anim.SetTrigger("Attack");
            anim.SetBool("IsAttacking", true);
        }

        NetworkIdentity targetNetId = currentTarget.GetComponent<NetworkIdentity>();
        if (targetNetId != null)
        {
            CmdStartAttack(targetNetId.netId);
        }
        else
        {
            Debug.LogError("[PlayerCombat] Alvo " + currentTarget.name + " nao tem NetworkIdentity!");
        }
    }

    private bool CanAttackNow()
    {
        if (stats == null)
        {
            Debug.LogWarning("[PlayerCombat] TryAttackTarget CANCELADO: PlayerStats eh null");
            return false;
        }

        if (stats.MaxHealth <= 0)
        {
            Debug.LogWarning("[PlayerCombat] TryAttackTarget CANCELADO: PlayerStats ainda nao inicializado (MaxHealth=" + stats.MaxHealth + ")");
            return false;
        }

        if (!combatEnabled)
        {
            if (showCombatDebugLogs) Debug.Log("[PlayerCombat] TryAttackTarget CANCELADO: combat desabilitado");
            return false;
        }

        if (stats.IsDead)
        {
            if (showCombatDebugLogs) Debug.Log("[PlayerCombat] TryAttackTarget CANCELADO: player esta morto");
            return false;
        }

        if (isAttacking)
        {
            if (showCombatDebugLogs) Debug.Log("[PlayerCombat] TryAttackTarget CANCELADO: ataque em andamento");
            return false;
        }

        if (Time.time < lastAttackTime + GetAttackInterval())
        {
            if (showCombatDebugLogs) Debug.Log("[PlayerCombat] TryAttackTarget CANCELADO: em cooldown");
            return false;
        }

        if (currentTarget == null)
        {
            if (showCombatDebugLogs) Debug.Log("[PlayerCombat] TryAttackTarget CANCELADO: sem alvo");
            return false;
        }

        if (!IsTargetAlive(currentTarget))
        {
            if (showCombatDebugLogs) Debug.Log("[PlayerCombat] TryAttackTarget CANCELADO: alvo invalido ou morto");
            ClearTarget();
            return false;
        }

        return true;
    }

    private void TickAutoAttack()
    {
        if (!autoAttackEnabled)
            return;

        if (currentTarget == null || !IsTargetAlive(currentTarget))
        {
            if (showCombatDebugLogs)
                Debug.Log("[PlayerCombat] AutoAttack cancelado: alvo perdido ou morto");

            movement?.StopMovement();
            ClearTarget();
            return;
        }

        if (!combatEnabled || stats == null || stats.IsDead)
        {
            movement?.StopMovement();
            return;
        }

        if (isAttacking)
            return;

        float distance = Vector3.Distance(transform.position, currentTarget.position);
        if (distance > attackRange)
        {
            ChaseCurrentTarget();
            return;
        }

        if (movement != null && movement.IsMoving)
            movement.StopMovement();

        if (Time.time >= lastAttackTime + GetAttackInterval())
            TryAttackTarget();
    }

    private void ChaseCurrentTarget()
    {
        if (movement == null || currentTarget == null)
            return;

        movement.FollowTarget(currentTarget, attackRange);
    }

    private float GetAttackInterval()
    {
        return attackCooldown / Mathf.Max(stats != null ? stats.AttackSpeed : 1f, 0.1f);
    }

    private bool IsTargetAlive(Transform target)
    {
        if (target == null)
            return false;

        ICharacterStats targetStats = target.GetComponent<ICharacterStats>()
            ?? target.GetComponentInParent<ICharacterStats>();

        return targetStats != null && !targetStats.IsDead;
    }

    [Command]
    private void CmdStartAttack(uint targetNetId)
    {
        if (showCombatDebugLogs)
            Debug.Log("[PlayerCombat] CmdStartAttack recebido | targetNetId=" + targetNetId + " | isDead=" + stats.IsDead + " | HP=" + stats.Health + "/" + stats.MaxHealth);

        if (stats.IsDead)
            Debug.Log("[PlayerCombat] CmdStartAttack CANCELADO: player morto no servidor");
    }

    [Client]
    public void OnAttackHit()
    {
        if (showCombatDebugLogs)
            Debug.Log("[PlayerCombat] OnAttackHit (Animation Event) | isDead=" + stats.IsDead + " | isAttacking=" + isAttacking);

        if (stats.IsDead || currentTarget == null || !IsTargetAlive(currentTarget))
        {
            if (showCombatDebugLogs) Debug.Log("[PlayerCombat] OnAttackHit IGNORADO: alvo perdido ou player morto");
            return;
        }

        attackHitPending = true;

        NetworkIdentity targetNetId = currentTarget.GetComponent<NetworkIdentity>();
        if (targetNetId != null)
            CmdPerformAttack(targetNetId.netId);
    }

    [Command]
    private void CmdPerformAttack(uint targetNetId)
    {
        if (showCombatDebugLogs)
        {
            Debug.Log("[PlayerCombat] ===============================================");
            Debug.Log("[PlayerCombat] CmdPerformAttack() no SERVIDOR");
            Debug.Log("[PlayerCombat] attacker=" + gameObject.name);
            Debug.Log("[PlayerCombat] targetNetId=" + targetNetId);
            Debug.Log("[PlayerCombat] isDead=" + stats.IsDead + " | HP=" + stats.Health + "/" + stats.MaxHealth);
            Debug.Log("[PlayerCombat] attackHitPending=" + attackHitPending);
            Debug.Log("[PlayerCombat] ===============================================");
        }

        if (!attackHitPending)
        {
            Debug.LogWarning("[PlayerCombat] CmdPerformAttack BLOQUEADO: attackHitPending=false");
            return;
        }

        if (stats.IsDead)
        {
            Debug.LogWarning("[PlayerCombat] CmdPerformAttack BLOQUEADO: player esta morto no servidor");
            attackHitPending = false;
            return;
        }

        if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity))
        {
            Debug.LogWarning("[PlayerCombat] CmdPerformAttack FALHOU: targetNetId " + targetNetId + " nao encontrado");
            attackHitPending = false;
            return;
        }

        GameObject targetObj = targetIdentity.gameObject;
        ICharacterStats targetStats = targetObj.GetComponent<ICharacterStats>();

        if (targetStats == null)
        {
            Debug.LogError("[PlayerCombat] CmdPerformAttack FALHOU: alvo " + targetObj.name + " nao implementa ICharacterStats");
            attackHitPending = false;
            return;
        }

        if (targetStats.IsDead)
        {
            Debug.Log("[PlayerCombat] CmdPerformAttack CANCELADO: alvo " + targetObj.name + " ja esta morto");
            attackHitPending = false;
            return;
        }

        float dist = Vector3.Distance(transform.position, targetObj.transform.position);
        if (dist > attackRange * 1.5f)
        {
            Debug.LogWarning("[PlayerCombat] CmdPerformAttack BLOQUEADO: alvo fora de range (" + dist.ToString("F2") + "m)");
            attackHitPending = false;
            return;
        }

        int damage = CalculateDamage();
        bool isCritical = UnityEngine.Random.Range(0, 100) < stats.CriticalRate;
        if (isCritical)
            damage = Mathf.RoundToInt(damage * stats.CriticalDamage);

        Debug.Log("[PlayerCombat] " + gameObject.name + " causando " + damage + " dano em " + targetObj.name + " (crit=" + isCritical + ")");

        targetStats.TakeDamage(damage, netId, DamageType.Physical);
        stats.RestoreStamina(-5);

        attackHitPending = false;
        RpcOnAttackPerformed(targetNetId, damage, isCritical);
    }

    [ClientRpc]
    private void RpcOnAttackPerformed(uint targetNetId, int damage, bool isCritical)
    {
        if (showCombatDebugLogs)
            Debug.Log("[PlayerCombat] RpcOnAttackPerformed: " + damage + " dmg no netId=" + targetNetId + " | crit=" + isCritical);
    }

    [Client]
    public void OnAttackEnd()
    {
        if (showCombatDebugLogs) Debug.Log("[PlayerCombat] OnAttackEnd (Animation Event)");

        isAttacking = false;
        attackHitPending = false;

        if (anim != null)
            anim.SetBool("IsAttacking", false);

        OnAttackFinished?.Invoke();
    }

    [Server]
    private int CalculateDamage()
    {
        int baseDamage = stats.Attack;
        float variance = UnityEngine.Random.Range(0.9f, 1.1f);
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * variance));
    }

    public void SetCombatEnabled(bool enabled)
    {
        combatEnabled = enabled;
        if (!enabled)
        {
            autoAttackEnabled = false;
            isAttacking = false;
            attackHitPending = false;
            movement?.StopMovement();
            if (anim != null) anim.SetBool("IsAttacking", false);
        }

        if (showCombatDebugLogs) Debug.Log("[PlayerCombat] SetCombatEnabled=" + enabled);
    }

    public void UpdateAttackSpeed(float newSpeed)
    {
        if (showCombatDebugLogs) Debug.Log("[PlayerCombat] AttackSpeed atualizado para " + newSpeed.ToString("F2"));
    }

    public bool IsAttacking => isAttacking;
    public bool IsAutoAttacking => autoAttackEnabled;
    public Transform CurrentTarget => currentTarget;
    public float AttackRange => attackRange;

    public void Trace(string message)
    {
        if (showCombatDebugLogs)
            Debug.Log("[PlayerCombat] " + message);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (currentTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
}

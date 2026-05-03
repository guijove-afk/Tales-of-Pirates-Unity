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

    // Estado
    private float lastAttackTime;
    private bool isAttacking;
    private bool combatEnabled = true;
    private bool attackHitPending = false;

    // Referencias
    private PlayerStats stats;
    private Animator anim;
    private Transform currentTarget;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        // Input de ataque (SPACE)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryAttackTarget();
        }
    }

    #region Target Selection

    public void SetTarget(Transform target)
    {
        currentTarget = target;
        if (showCombatDebugLogs)
            Debug.Log("[PlayerCombat] Alvo definido: " + (target != null ? target.name : "NULL"));
    }

    public void ClearTarget()
    {
        currentTarget = null;
    }

    #endregion

    #region Attack Flow

    [Client]
    public void TryAttackTarget()
    {
        // CORRECAO: Verifica se stats foi inicializado
        if (stats == null)
        {
            Debug.LogWarning("[PlayerCombat] TryAttackTarget CANCELADO: PlayerStats eh null");
            return;
        }

        // CORRECAO: Se nao tem vida maxima definida, ainda nao foi inicializado
        if (stats.MaxHealth <= 0)
        {
            Debug.LogWarning("[PlayerCombat] TryAttackTarget CANCELADO: PlayerStats ainda nao inicializado (MaxHealth=" + stats.MaxHealth + ")");
            return;
        }

        if (!combatEnabled)
        {
            if (showCombatDebugLogs) Debug.Log("[PlayerCombat] TryAttackTarget CANCELADO: combat desabilitado");
            return;
        }

        if (stats.IsDead)
        {
            if (showCombatDebugLogs) Debug.Log("[PlayerCombat] TryAttackTarget CANCELADO: player esta morto (HP=" + stats.Health + "/" + stats.MaxHealth + ")");
            return;
        }

        if (Time.time < lastAttackTime + (attackCooldown / Mathf.Max(stats.AttackSpeed, 0.1f)))
        {
            if (showCombatDebugLogs) Debug.Log("[PlayerCombat] TryAttackTarget CANCELADO: em cooldown");
            return;
        }

        if (currentTarget == null)
        {
            if (showCombatDebugLogs) Debug.Log("[PlayerCombat] TryAttackTarget CANCELADO: sem alvo");
            return;
        }

        // Verifica distancia
        float dist = Vector3.Distance(transform.position, currentTarget.position);
        if (dist > attackRange)
        {
            if (showCombatDebugLogs) Debug.Log("[PlayerCombat] TryAttackTarget CANCELADO: alvo fora de range (" + dist.ToString("F2") + " > " + attackRange + ")");
            return;
        }

        // Olha para o alvo
        transform.LookAt(new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z));

        if (showCombatDebugLogs)
        {
            Debug.Log("[PlayerCombat] ===============================================");
            Debug.Log("[PlayerCombat] TryAttackTarget() -> INICIANDO ATAQUE");
            Debug.Log("[PlayerCombat]   Alvo: " + currentTarget.name);
            Debug.Log("[PlayerCombat]   Distancia: " + dist.ToString("F2") + "m");
            Debug.Log("[PlayerCombat]   HP: " + stats.Health + "/" + stats.MaxHealth);
            Debug.Log("[PlayerCombat] ===============================================");
        }

        lastAttackTime = Time.time;
        isAttacking = true;
        attackHitPending = false;

        // Dispara animacao local
        if (anim != null)
        {
            anim.SetTrigger("Attack");
            anim.SetBool("IsAttacking", true);
        }

        // Inicia sequencia de ataque no servidor
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

    [Command]
    private void CmdStartAttack(uint targetNetId)
    {
        if (showCombatDebugLogs)
            Debug.Log("[PlayerCombat] CmdStartAttack recebido | targetNetId=" + targetNetId + " | isDead=" + stats.IsDead + " | HP=" + stats.Health + "/" + stats.MaxHealth);

        if (stats.IsDead)
        {
            Debug.Log("[PlayerCombat] CmdStartAttack CANCELADO: player morto no servidor");
            return;
        }
    }

    [Client]
    public void OnAttackHit()
    {
        if (showCombatDebugLogs)
            Debug.Log("[PlayerCombat] OnAttackHit (Animation Event) | isDead=" + stats.IsDead + " | isAttacking=" + isAttacking);

        if (stats.IsDead)
        {
            if (showCombatDebugLogs) Debug.Log("[PlayerCombat] OnAttackHit IGNORADO: player morreu antes do hit");
            return;
        }

        if (currentTarget == null)
        {
            if (showCombatDebugLogs) Debug.Log("[PlayerCombat] OnAttackHit IGNORADO: alvo perdido");
            return;
        }

        attackHitPending = true;

        NetworkIdentity targetNetId = currentTarget.GetComponent<NetworkIdentity>();
        if (targetNetId != null)
        {
            CmdPerformAttack(targetNetId.netId);
        }
    }

    [Command]
    private void CmdPerformAttack(uint targetNetId)
    {
        if (showCombatDebugLogs)
        {
            Debug.Log("[PlayerCombat] ===============================================");
            Debug.Log("[PlayerCombat] CmdPerformAttack() no SERVIDOR");
            Debug.Log("[PlayerCombat]   attacker=" + gameObject.name);
            Debug.Log("[PlayerCombat]   targetNetId=" + targetNetId);
            Debug.Log("[PlayerCombat]   isDead=" + stats.IsDead + " | HP=" + stats.Health + "/" + stats.MaxHealth);
            Debug.Log("[PlayerCombat]   attackHitPending=" + attackHitPending);
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

        // Verifica range no servidor
        float dist = Vector3.Distance(transform.position, targetObj.transform.position);
        if (dist > attackRange * 1.5f)
        {
            Debug.LogWarning("[PlayerCombat] CmdPerformAttack BLOQUEADO: alvo fora de range (" + dist.ToString("F2") + "m)");
            attackHitPending = false;
            return;
        }

        // Calcula dano
        int damage = CalculateDamage();
        bool isCritical = UnityEngine.Random.Range(0, 100) < stats.CriticalRate;
        if (isCritical)
        {
            damage = Mathf.RoundToInt(damage * stats.CriticalDamage);
        }

        Debug.Log("[PlayerCombat] " + gameObject.name + " causando " + damage + " dano em " + targetObj.name + " (crit=" + isCritical + ")");

        // APLICA DANO NO ALVO
        targetStats.TakeDamage(damage, netId, DamageType.Physical);

        // Consome stamina
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
        if (anim != null) anim.SetBool("IsAttacking", false);
    }

    [Server]
    private int CalculateDamage()
    {
        int baseDamage = stats.Attack;
        float variance = UnityEngine.Random.Range(0.9f, 1.1f);
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * variance));
    }

    #endregion

    #region Public API

    public void SetCombatEnabled(bool enabled)
    {
        combatEnabled = enabled;
        if (!enabled)
        {
            isAttacking = false;
            attackHitPending = false;
            if (anim != null) anim.SetBool("IsAttacking", false);
        }
        if (showCombatDebugLogs) Debug.Log("[PlayerCombat] SetCombatEnabled=" + enabled);
    }

    public void UpdateAttackSpeed(float newSpeed)
    {
        if (showCombatDebugLogs) Debug.Log("[PlayerCombat] AttackSpeed atualizado para " + newSpeed.ToString("F2"));
    }

    public bool IsAttacking => isAttacking;
    public Transform CurrentTarget => currentTarget;

    // Metodo para debug - substitui Trace se nao existir
    public void Trace(string message)
    {
        if (showCombatDebugLogs)
            Debug.Log("[PlayerCombat] " + message);
    }

    #endregion

    #region Gizmos

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

    #endregion
}
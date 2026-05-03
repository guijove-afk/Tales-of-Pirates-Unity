using UnityEngine;
using Mirror;
using System;

[RequireComponent(typeof(EnemyStats))]
[RequireComponent(typeof(Animator))]
public class EnemyAI : NetworkBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private float aggroRange = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float stopChaseRange = 15f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float moveSpeed = 3.5f;

    [Header("Debug")]
    [SerializeField] private bool showAIDebugLogs = true;

    // Estado
    public enum AIState { Idle, Chase, Attack, Dead, Return }
    private AIState currentState = AIState.Idle;
    private float lastAttackTime;
    private bool aiEnabled = true;

    // Referencias
    private EnemyStats stats;
    private Animator anim;
    private Transform target;
    private Vector3 spawnPosition;
    private CharacterController charController;

    void Awake()
    {
        stats = GetComponent<EnemyStats>();
        anim = GetComponent<Animator>();
        charController = GetComponent<CharacterController>();
        spawnPosition = transform.position;
    }

    void Start()
    {
        if (showAIDebugLogs)
            Debug.Log("[EnemyAI] " + gameObject.name + " (netId=" + netId + ") iniciado | spawn=" + spawnPosition + " | aggroRange=" + aggroRange + " | attackRange=" + attackRange);
    }

    void Update()
    {
        if (!isServer) return;
        if (!aiEnabled) return;
        if (stats.IsDead)
        {
            if (currentState != AIState.Dead)
            {
                if (showAIDebugLogs) Debug.Log("[EnemyAI] " + gameObject.name + " mudando para estado DEAD");
                ChangeState(AIState.Dead);
            }
            return;
        }

        switch (currentState)
        {
            case AIState.Idle:
                UpdateIdle();
                break;
            case AIState.Chase:
                UpdateChase();
                break;
            case AIState.Attack:
                UpdateAttack();
                break;
            case AIState.Return:
                UpdateReturn();
                break;
        }
    }

    #region State Machine

    private void ChangeState(AIState newState)
    {
        if (currentState == newState) return;

        if (showAIDebugLogs)
            Debug.Log("[EnemyAI] " + gameObject.name + ": " + currentState + " -> " + newState);

        currentState = newState;

        switch (newState)
        {
            case AIState.Idle:
                anim.SetBool("IsMoving", false);
                anim.SetBool("IsAttacking", false);
                target = null;
                break;
            case AIState.Chase:
                anim.SetBool("IsMoving", true);
                anim.SetBool("IsAttacking", false);
                break;
            case AIState.Attack:
                anim.SetBool("IsMoving", false);
                anim.SetBool("IsAttacking", true);
                break;
            case AIState.Return:
                anim.SetBool("IsMoving", true);
                anim.SetBool("IsAttacking", false);
                target = null;
                break;
            case AIState.Dead:
                anim.SetBool("IsMoving", false);
                anim.SetBool("IsAttacking", false);
                anim.SetTrigger("Die");
                break;
        }
    }

    #endregion

    #region Idle State

    private void UpdateIdle()
    {
        // Procura por jogadores dentro do aggroRange
        Collider[] hits = Physics.OverlapSphere(transform.position, aggroRange, LayerMask.GetMask("Player"));

        if (showAIDebugLogs && hits.Length > 0)
            Debug.Log("[EnemyAI] UpdateIdle: " + hits.Length + " player(s) detectado(s) no aggroRange");

        Transform closestPlayer = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            PlayerStats playerStats = hit.GetComponent<PlayerStats>();
            if (playerStats == null) continue;
            if (playerStats.IsDead) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestPlayer = hit.transform;
            }
        }

        if (closestPlayer != null)
        {
            if (showAIDebugLogs)
            {
                Debug.Log("[EnemyAI] ===============================================");
                Debug.Log("[EnemyAI] AGGRO! Player detectado: " + closestPlayer.name);
                Debug.Log("[EnemyAI] Distancia: " + closestDist.ToString("F2") + "m");
                Debug.Log("[EnemyAI] ===============================================");
            }
            target = closestPlayer;
            ChangeState(AIState.Chase);
        }
    }

    #endregion

    #region Chase State

    private void UpdateChase()
    {
        if (target == null)
        {
            if (showAIDebugLogs) Debug.Log("[EnemyAI] UpdateChase: alvo perdido, voltando para IDLE");
            ChangeState(AIState.Idle);
            return;
        }

        PlayerStats playerStats = target.GetComponent<PlayerStats>();
        if (playerStats == null || playerStats.IsDead)
        {
            if (showAIDebugLogs) Debug.Log("[EnemyAI] UpdateChase: alvo " + target.name + " morto ou invalido, voltando para IDLE");
            ChangeState(AIState.Idle);
            return;
        }

        float distToTarget = Vector3.Distance(transform.position, target.position);
        float distToSpawn = Vector3.Distance(transform.position, spawnPosition);

        // Se saiu muito longe do spawn, volta
        if (distToSpawn > stopChaseRange)
        {
            if (showAIDebugLogs)
                Debug.Log("[EnemyAI] UpdateChase: distancia do spawn (" + distToSpawn.ToString("F2") + "m) > stopChaseRange (" + stopChaseRange + "), RETORNANDO");
            ChangeState(AIState.Return);
            return;
        }

        // Se chegou na range de ataque
        if (distToTarget <= attackRange)
        {
            if (showAIDebugLogs)
                Debug.Log("[EnemyAI] UpdateChase: alvo na range de ataque (" + distToTarget.ToString("F2") + "m <= " + attackRange + "m), mudando para ATTACK");
            ChangeState(AIState.Attack);
            return;
        }

        // Move em direcao ao alvo
        MoveTowards(target.position);
    }

    #endregion

    #region Attack State

    private void UpdateAttack()
    {
        if (target == null)
        {
            if (showAIDebugLogs) Debug.Log("[EnemyAI] UpdateAttack: alvo perdido");
            ChangeState(AIState.Idle);
            return;
        }

        PlayerStats playerStats = target.GetComponent<PlayerStats>();
        if (playerStats == null || playerStats.IsDead)
        {
            if (showAIDebugLogs) Debug.Log("[EnemyAI] UpdateAttack: alvo " + target.name + " morto, indo para IDLE");
            ChangeState(AIState.Idle);
            return;
        }

        float distToTarget = Vector3.Distance(transform.position, target.position);

        // Se o alvo saiu da range de ataque, volta a perseguir
        if (distToTarget > attackRange * 1.2f)
        {
            if (showAIDebugLogs)
                Debug.Log("[EnemyAI] UpdateAttack: alvo saiu da range (" + distToTarget.ToString("F2") + "m), voltando para CHASE");
            ChangeState(AIState.Chase);
            return;
        }

        // Olha para o alvo
        LookAt(target.position);

        // Verifica cooldown de ataque
        float cooldownTime = attackCooldown / stats.AttackSpeed;
        if (Time.time >= lastAttackTime + cooldownTime)
        {
            lastAttackTime = Time.time;
            PerformAttack();
        }
    }

    /// <summary>
    /// Executa o ataque no alvo. Chama EnemyStats.PerformAttack que aplica dano real.
    /// </summary>
    [Server]
    private void PerformAttack()
    {
        if (showAIDebugLogs)
        {
            Debug.Log("[EnemyAI] ===============================================");
            Debug.Log("[EnemyAI] PerformAttack() chamado");
            Debug.Log("[EnemyAI]   Mob: " + gameObject.name);
            Debug.Log("[EnemyAI]   Alvo: " + (target != null ? target.name : "NULL"));
            Debug.Log("[EnemyAI]   Distancia: " + Vector3.Distance(transform.position, target.position).ToString("F2") + "m");
            Debug.Log("[EnemyAI] ===============================================");
        }

        if (target == null)
        {
            Debug.LogWarning("[EnemyAI] PerformAttack CANCELADO: target eh null");
            return;
        }

        // Toca animacao de ataque
        anim.SetTrigger("Attack");

        // Delega o dano para EnemyStats
        stats.PerformAttack(target.gameObject);
    }

    #endregion

    #region Return State

    private void UpdateReturn()
    {
        float distToSpawn = Vector3.Distance(transform.position, spawnPosition);

        if (distToSpawn < 1f)
        {
            if (showAIDebugLogs) Debug.Log("[EnemyAI] UpdateReturn: chegou no spawn, indo para IDLE");
            ChangeState(AIState.Idle);
            return;
        }

        MoveTowards(spawnPosition);
    }

    #endregion

    #region Movement Helpers

    private void MoveTowards(Vector3 destination)
    {
        Vector3 dir = (destination - transform.position).normalized;
        dir.y = 0;

        if (dir.sqrMagnitude > 0.001f)
        {
            LookAt(destination);

            if (charController != null && charController.enabled)
            {
                charController.Move(dir * moveSpeed * Time.deltaTime);
            }
            else
            {
                transform.position += dir * moveSpeed * Time.deltaTime;
            }
        }
    }

    private void LookAt(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
        }
    }

    #endregion

    #region Public API

    public void SetEnabled(bool enabled)
    {
        aiEnabled = enabled;
        if (!enabled)
        {
            anim.SetBool("IsMoving", false);
            anim.SetBool("IsAttacking", false);
        }
        if (showAIDebugLogs) Debug.Log("[EnemyAI] SetEnabled=" + enabled);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null && currentState != AIState.Chase && currentState != AIState.Attack)
        {
            ChangeState(AIState.Chase);
        }
    }

    public Transform CurrentTarget => target;
    public AIState State => currentState;

    #endregion

    #region Gizmos

    void OnDrawGizmosSelected()
    {
        // Aggro range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Stop chase range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(spawnPosition, stopChaseRange);

        // Spawn point
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(spawnPosition, 0.3f);

        // Linha para alvo
        if (target != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }

    #endregion
}

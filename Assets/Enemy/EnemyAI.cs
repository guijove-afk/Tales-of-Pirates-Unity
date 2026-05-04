using UnityEngine;
using UnityEngine.AI;
using Mirror;
using System;

[RequireComponent(typeof(EnemyStats))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : NetworkBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private float aggroRange = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float stopChaseRange = 20f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Debug")]
    [SerializeField] private bool showAIDebugLogs = true;

    public enum AIState { Idle, Chase, Attack, Dead, Return }
    private AIState currentState = AIState.Idle;
    private float lastAttackTime;
    private bool aiEnabled = true;

    private EnemyStats stats;
    private Animator anim;
    private NavMeshAgent agent;
    private Transform target;
    private Vector3 spawnPosition;

    void Awake()
    {
        stats = GetComponent<EnemyStats>();
        agent = GetComponent<NavMeshAgent>();
        
        // Procura Animator no próprio objeto OU nos filhos
        anim = GetComponent<Animator>();
        if (anim == null)
            anim = GetComponentInChildren<Animator>();
        
        spawnPosition = transform.position;
    }

    void Start()
    {
        if (agent != null)
        {
            agent.speed = stats != null ? stats.MoveSpeed : 3.5f;
            agent.stoppingDistance = attackRange;
            agent.acceleration = 8f;
            agent.angularSpeed = rotationSpeed * 36f;
            agent.autoBraking = true;
        }

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
                ChangeState(AIState.Dead);
            return;
        }

        switch (currentState)
        {
            case AIState.Idle: UpdateIdle(); break;
            case AIState.Chase: UpdateChase(); break;
            case AIState.Attack: UpdateAttack(); break;
            case AIState.Return: UpdateReturn(); break;
        }

        UpdateAnimation();
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
                SafeResetPath();
                target = null;
                break;
            case AIState.Chase:
                break;
            case AIState.Attack:
                SafeResetPath();
                break;
            case AIState.Return:
                target = null;
                if (agent != null && agent.isActiveAndEnabled)
                    agent.SetDestination(spawnPosition);
                break;
            case AIState.Dead:
                SafeResetPath();
                if (agent != null) agent.enabled = false;
                break;
        }
    }

    #endregion

    #region Idle State

    private void UpdateIdle()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, aggroRange, LayerMask.GetMask("Player"));

        if (showAIDebugLogs && hits.Length > 0)
            Debug.Log("[EnemyAI] UpdateIdle: " + hits.Length + " player(s) detectado(s)");

        Transform closestPlayer = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            var netId = hit.GetComponent<NetworkIdentity>();
            if (netId == null) continue;

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
            ChangeState(AIState.Idle);
            return;
        }

        PlayerStats playerStats = target.GetComponent<PlayerStats>();
        if (playerStats == null || playerStats.IsDead)
        {
            ChangeState(AIState.Idle);
            return;
        }

        float distToTarget = Vector3.Distance(transform.position, target.position);
        float distToSpawn = Vector3.Distance(transform.position, spawnPosition);

        if (distToSpawn > stopChaseRange)
        {
            if (showAIDebugLogs)
                Debug.Log("[EnemyAI] UpdateChase: distancia do spawn (" + distToSpawn.ToString("F2") + "m) > stopChaseRange, RETORNANDO");
            ChangeState(AIState.Return);
            return;
        }

        if (distToTarget <= attackRange)
        {
            ChangeState(AIState.Attack);
            return;
        }

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.SetDestination(target.position);
        }
    }

    #endregion

    #region Attack State

    private void UpdateAttack()
    {
        if (target == null)
        {
            ChangeState(AIState.Idle);
            return;
        }

        PlayerStats playerStats = target.GetComponent<PlayerStats>();
        if (playerStats == null || playerStats.IsDead)
        {
            ChangeState(AIState.Idle);
            return;
        }

        float distToTarget = Vector3.Distance(transform.position, target.position);

        if (distToTarget > attackRange * 1.2f)
        {
            ChangeState(AIState.Chase);
            return;
        }

        LookAt(target.position);

        float cooldownTime = attackCooldown / Mathf.Max(stats.AttackSpeed, 0.1f);
        if (Time.time >= lastAttackTime + cooldownTime)
        {
            lastAttackTime = Time.time;
            PerformAttack();
        }
    }

    [Server]
    private void PerformAttack()
    {
        if (target == null) return;

        if (showAIDebugLogs)
            Debug.Log("[EnemyAI] PerformAttack() | Mob: " + gameObject.name + " | Alvo: " + target.name);

        // [CORREÇÃO] Agora apenas dispara a animação.
        // O dano será aplicado pelo Animation Event quando a animação chegar no frame de impacto.
        if (anim != null)
            anim.SetTrigger("Attack");

        // [CORREÇÃO] REMOVIDO: stats.PerformAttack(target.gameObject);
        // Antes causava ataque duplo porque o dano era aplicado aqui E no Animation Event.
    }

    #endregion

    #region Return State

    private void UpdateReturn()
    {
        float distToSpawn = Vector3.Distance(transform.position, spawnPosition);

        if (distToSpawn < 1f)
        {
            if (showAIDebugLogs) Debug.Log("[EnemyAI] UpdateReturn: chegou no spawn");
            ChangeState(AIState.Idle);
            return;
        }

        if (agent != null && agent.isActiveAndEnabled && !agent.hasPath)
        {
            agent.SetDestination(spawnPosition);
        }
    }

    #endregion

    #region Animation & Movement

    private void UpdateAnimation()
    {
        if (anim == null) return;
        if (anim.runtimeAnimatorController == null) return;  // sem controller atribuído
        
        bool isMoving = agent != null && agent.isActiveAndEnabled && 
                        agent.hasPath && agent.remainingDistance > agent.stoppingDistance + 0.1f;
        anim.SetBool("IsMoving", isMoving);
        anim.SetBool("IsAttacking", currentState == AIState.Attack);
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

    private void SafeResetPath()
    {
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.ResetPath();
        }
    }

    #endregion

    #region Public API

    public void SetEnabled(bool enabled)
    {
        aiEnabled = enabled;
        if (!enabled)
        {
            SafeResetPath();
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null && currentState != AIState.Chase && currentState != AIState.Attack)
            ChangeState(AIState.Chase);
    }

    // [CORREÇÃO] Propriedade pública para o EnemyAnimationEvents acessar o alvo atual
    public Transform CurrentTarget => target;
    public AIState State => currentState;

    #endregion

    #region Gizmos

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(spawnPosition, stopChaseRange);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(spawnPosition, 0.3f);

        if (target != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }

    #endregion
}
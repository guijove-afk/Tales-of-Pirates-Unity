using UnityEngine;
using Mirror;
using TOP.Core;
using TOP.Systems;
using TOP.Gameplay;
using System;

namespace TOP.Player
{
    public class PlayerCombat : NetworkBehaviour
    {
        public event Action OnAttackStarted;
        public event Action OnAttackFinished;
        public event Action<Transform> OnTargetChanged;

        [Header("Combat")]
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private Transform attackPoint;

        [Header("Target Selection Visual")]
        [SerializeField] private bool showSelectionCircle = true;

        [SyncVar] private NetworkIdentity _currentTarget;
        [SyncVar] private bool _isAttacking;

        private float _lastAttackTime;
        private bool _attackHitPending = false;
        private bool _attackAnimationPlaying = false;  // ✅ NOVO: evita tremedeira
        private PlayerStats _stats;
        private PlayerAnimation _animation;
        private PlayerEquipment _equipment;
        private PlayerController _controller;
        private PlayerMovement _movement;
        private EnemySelection _currentEnemySelection;

        void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _animation = GetComponent<PlayerAnimation>();
            _equipment = GetComponent<PlayerEquipment>();
            _controller = GetComponent<PlayerController>();
            _movement = GetComponent<PlayerMovement>();
        }

        void Update()
        {
            if (!isServer) return;

            if (_currentTarget != null && _isAttacking)
            {
                float distance = Vector3.Distance(transform.position, _currentTarget.transform.position);

                // Se o target morreu, para de atacar
                if (_currentTarget.GetComponent<EnemyStats>()?.IsDead == true)
                {
                    StopAttack();
                    return;
                }

                // ✅ CORRIGIDO: Só ataca se estiver em range E cooldown passou
                if (distance <= attackRange)
                {
                    // ✅ NOVO: Só inicia nova animação se a anterior terminou
                    if (Time.time >= _lastAttackTime + attackCooldown && !_attackAnimationPlaying)
                    {
                        StartAttackAnimation();
                    }

                    // Aplica dano quando o Animation Event dispara
                    if (_attackHitPending)
                    {
                        ApplyDamage();
                        _attackHitPending = false;
                    }
                }
                // Se estiver fora de range, continua perseguindo (movimento normal)
            }
        }

        [Server]
        void StartAttackAnimation()
        {
            _lastAttackTime = Time.time;
            _attackHitPending = true;
            _attackAnimationPlaying = true;  // ✅ Marca que animação está rodando

            // Dispara animação nos clientes
            _animation?.RpcTriggerAttack(0);
            OnAttackStarted?.Invoke();  // ✅ Só dispara uma vez por ataque

            Debug.Log($"[PlayerCombat] 🎬 Animação de ataque iniciada em {_currentTarget.name}");

            // ✅ NOVO: Agenda o fim da animação (evita tremedeira)
            Invoke(nameof(EndAttackAnimation), attackCooldown * 0.8f);
        }

        [Server]
        void EndAttackAnimation()
        {
            _attackAnimationPlaying = false;
            OnAttackFinished?.Invoke();
            Debug.Log("[PlayerCombat] 🏁 Animação de ataque finalizada");
        }

        [Server]
        void ApplyDamage()
        {
            if (_currentTarget == null) return;

            int damage = CalculateDamage();

            EnemyStats targetStats = _currentTarget.GetComponent<EnemyStats>();
            if (targetStats != null)
            {
                targetStats.TakeDamage(damage, netId, DamageType.Physical);
                Debug.Log($"[PlayerCombat] ⚔️ {damage} de dano em {_currentTarget.name} | HP={targetStats.Health}/{targetStats.MaxHealth}");
            }

            _stats?.ConsumeSp(5);
        }

        [Server]
        public void OnAnimationAttackHit()
        {
            if (!isServer) return;
            _attackHitPending = true;
        }

        [Server]
        public void SetTarget(NetworkIdentity target)
        {
            if (_currentEnemySelection != null)
            {
                _currentEnemySelection.SetSelected(false);
                _currentEnemySelection = null;
            }

            _currentTarget = target;
            OnTargetChanged?.Invoke(target?.transform);

            if (target != null && showSelectionCircle)
            {
                _currentEnemySelection = target.GetComponent<EnemySelection>();
                if (_currentEnemySelection == null)
                    _currentEnemySelection = target.GetComponentInChildren<EnemySelection>();

                if (_currentEnemySelection != null)
                {
                    _currentEnemySelection.SetSelected(true);
                    Debug.Log($"[PlayerCombat] ✅ Círculo verde ativado em {target.name}");
                }
                else
                {
                    Debug.LogWarning($"[PlayerCombat] {target.name} não tem EnemySelection!");
                }
            }

            Debug.Log($"[PlayerCombat] Target setado: {(target != null ? target.name : "NULL")}");
        }

        [Server]
        public void AttackTarget(NetworkIdentity target)
        {
            if (target == null) return;

            SetTarget(target);
            _isAttacking = true;

            Debug.Log($"[PlayerCombat] Auto-attack iniciado em {target.name}");
        }

        [Server]
        int CalculateDamage()
        {
            int baseDamage = _stats.PhysicalAttack;

            if (_equipment != null)
                baseDamage += _equipment.GetTotalAttackBonus();

            float variation = UnityEngine.Random.Range(0.9f, 1.1f);
            int finalDamage = Mathf.RoundToInt(baseDamage * variation);

            if (UnityEngine.Random.value < _stats.CriticalRate)
                finalDamage = Mathf.RoundToInt(finalDamage * _stats.CriticalDamage);

            return Mathf.Max(1, finalDamage);
        }

        [Server]
        public void StopAttack()
        {
            if (_currentEnemySelection != null)
            {
                _currentEnemySelection.SetSelected(false);
                _currentEnemySelection = null;
            }

            _isAttacking = false;
            _currentTarget = null;
            _attackHitPending = false;
            _attackAnimationPlaying = false;
            CancelInvoke(nameof(EndAttackAnimation));  // ✅ Cancela Invoke pendente
            OnTargetChanged?.Invoke(null);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint ? attackPoint.position : transform.position, attackRange);
        }
    }
}
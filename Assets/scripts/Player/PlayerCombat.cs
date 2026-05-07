using UnityEngine;
using Mirror;
using TOP.Core;
using TOP.Systems;
using TOP.Gameplay;  // ✅ para PlayerController
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

        [SyncVar] private NetworkIdentity _currentTarget;
        [SyncVar] private bool _isAttacking;

        private float _lastAttackTime;
        private PlayerStats _stats;
        private PlayerAnimation _animation;
        private PlayerEquipment _equipment;
        private PlayerController _controller;  // ✅ resolvido

        void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _animation = GetComponent<PlayerAnimation>();
            _equipment = GetComponent<PlayerEquipment>();
            _controller = GetComponent<PlayerController>();  // ✅ resolvido
        }

        void Update()
        {
            if (!isServer) return;

            if (_currentTarget != null && _isAttacking)
            {
                if (Time.time >= _lastAttackTime + attackCooldown)
                {
                    PerformAttack();
                }
            }
        }

        [Server]
        public void SetTarget(NetworkIdentity target)
        {
            _currentTarget = target;
            OnTargetChanged?.Invoke(target?.transform);
        }

        [Server]
        public void AttackTarget(NetworkIdentity target)
        {
            if (target == null) return;

            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance > attackRange) return;

            _currentTarget = target;
            _isAttacking = true;
            OnAttackStarted?.Invoke();
        }

        [Server]
        void PerformAttack()
        {
            if (_currentTarget == null)
            {
                _isAttacking = false;
                return;
            }

            _lastAttackTime = Time.time;

            int damage = CalculateDamage();

            PlayerStats targetStats = _currentTarget.GetComponent<PlayerStats>();
            if (targetStats != null)
            {
                targetStats.TakeDamage(damage);

                // ✅ PlayerController resolvido
                if (_controller != null)
                {
                    _controller.RpcTakeDamage(damage, _currentTarget.transform.position);
                }
            }

            // ✅ RpcTriggerAttack com tipo de ataque
            _animation?.RpcTriggerAttack(0);  // 0 = ataque básico

            _stats?.ConsumeSp(5);
            OnAttackFinished?.Invoke();
        }

        [Server]
        int CalculateDamage()
        {
            int baseDamage = _stats.PhysicalAttack;

            if (_equipment != null)
            {
                // ✅ preparado para PlayerEquipment.GetTotalAttackBonus()
                baseDamage += _equipment.GetTotalAttackBonus();
            }

            // ✅ Random ambíguo resolvido
            float variation = UnityEngine.Random.Range(0.9f, 1.1f);
            int finalDamage = Mathf.RoundToInt(baseDamage * variation);

            if (UnityEngine.Random.value < _stats.CriticalRate)  // ✅ Random ambíguo resolvido
            {
                finalDamage = Mathf.RoundToInt(finalDamage * _stats.CriticalDamage);
            }

            return Mathf.Max(1, finalDamage);
        }

        [Server]
        public void StopAttack()
        {
            _isAttacking = false;
            _currentTarget = null;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint ? attackPoint.position : transform.position, attackRange);
        }
    }
}
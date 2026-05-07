using UnityEngine;
using Mirror;
using TOP.Core;
using TOP.Systems;

namespace TOP.Player
{
    public class PlayerCombat : NetworkBehaviour
    {
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

        void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _animation = GetComponent<PlayerAnimation>();
            _equipment = GetComponent<PlayerEquipment>();
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
        }

        [Server]
        public void AttackTarget(NetworkIdentity target)
        {
            if (target == null) return;

            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance > attackRange) return;

            _currentTarget = target;
            _isAttacking = true;
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

                PlayerController controller = GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.RpcTakeDamage(damage, _currentTarget.transform.position);
                }
            }

            _animation.RpcTriggerAttack();

            _stats.ConsumeSp(5);
        }

        [Server]
        int CalculateDamage()
        {
            int baseDamage = _stats.PhysicalAttack;

            if (_equipment != null)
            {
                baseDamage += _equipment.GetTotalAttackBonus();
            }

            float variation = Random.Range(0.9f, 1.1f);
            int finalDamage = Mathf.RoundToInt(baseDamage * variation);

            if (Random.value < _stats.CriticalRate)
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

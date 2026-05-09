using UnityEngine;
using System;
using Mirror;
using UnityEngine.AI;
using TOP.Inventory;
using UnityEngine.EventSystems;
using TOP.Gameplay;

namespace TOP.Player
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class PlayerMovement : NetworkBehaviour
    {
        public event Action OnMovementStarted;
        public event Action OnMovementStopped;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float runSpeed = 10f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float stoppingDistance = 0.5f;

        [Header("Target Selection")]
        [SerializeField] private LayerMask enemyLayers;
        [SerializeField] private float raycastDistance = 100f;
        [SerializeField] private bool showDebugRay = true;

        private NavMeshAgent _agent;
        private PlayerAnimation _animation;
        private Vector3 _targetPosition;
        private bool _isMoving;
        private bool _wasMoving;

        // Lazy load do PlayerController
        private PlayerController _playerController;
        private PlayerController PlayerControllerRef
        {
            get
            {
                if (_playerController == null)
                {
                    _playerController = GetComponent<PlayerController>();
                    if (_playerController == null)
                        _playerController = GetComponentInParent<PlayerController>();
                    if (_playerController == null)
                        _playerController = GetComponentInChildren<PlayerController>();
                }
                return _playerController;
            }
        }

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _animation = GetComponent<PlayerAnimation>();
        }

        void Start()
        {
            if (_agent != null)
            {
                _agent.speed = walkSpeed;
                _agent.stoppingDistance = stoppingDistance;
                _agent.acceleration = 20f;
            }
        }

        void Update()
        {
            if (isLocalPlayer)
            {
                HandleInput();
            }

            if (isServer)
            {
                UpdateMovementState();
            }
        }

        void HandleInput()
{
    if (Input.GetMouseButtonDown(0))
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (showDebugRay)
            Debug.DrawRay(ray.origin, ray.direction * raycastDistance, Color.red, 1f);

        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
        {
            // ✅ PRIORIDADE 1: Verifica se clicou em um enemy
            EnemyStats enemyStats = hit.collider.GetComponent<EnemyStats>();
            if (enemyStats == null)
                enemyStats = hit.collider.GetComponentInParent<EnemyStats>();

            NetworkIdentity enemyIdentity = hit.collider.GetComponent<NetworkIdentity>();
            if (enemyIdentity == null)
                enemyIdentity = hit.collider.GetComponentInParent<NetworkIdentity>();

            if (enemyStats != null && enemyIdentity != null && !enemyStats.IsDead)
            {
                Debug.Log($"[PlayerMovement] 🎯 Mob clicado: {hit.collider.name} (netId={enemyIdentity.netId})");

                var controller = PlayerControllerRef;
                if (controller != null)
                {
                    // ✅ 1. Seta o target (ativa círculo verde)
                    controller.CmdSetTarget(enemyIdentity);

                    // ✅ 2. Inicia auto-attack (o PlayerCombat cuida do range)
                    controller.CmdAttackTarget(enemyIdentity);

                    // ✅ 3. Move o player até o mob
                    controller.CmdMoveTo(hit.point);

                    Debug.Log($"[PlayerMovement] ✅ Comandos enviados: SetTarget + AttackTarget + MoveTo");
                }
                else
                {
                    Debug.LogError("[PlayerMovement] ❌ PlayerController é NULL!");
                }
                return;
            }

            // ✅ PRIORIDADE 2: Clicou no chão → move e para o ataque atual
            Debug.Log($"[PlayerMovement] 🚶 Movendo para: {hit.point}");

            // ✅ NOVO: Para o ataque atual quando clica no chão
            var ctrl = PlayerControllerRef;
            if (ctrl != null && ctrl.Combat != null)
            {
                ctrl.Combat.StopAttack();
                Debug.Log("[PlayerMovement] ⚔️ Ataque interrompido — movimento prioritário");
            }

            CmdMoveTo(hit.point);
        }
        else
        {
            Debug.Log("[PlayerMovement] Raycast não acertou nada.");
        }
    }
}
        

        [Command]
        public void CmdMoveTo(Vector3 destination)
        {
            SetDestination(destination);
        }

        [Server]
        public void SetDestination(Vector3 destination)
        {
            if (_agent == null || !_agent.isActiveAndEnabled) return;
            _agent.SetDestination(destination);
            _targetPosition = destination;
        }

        [Server]
        void UpdateMovementState()
        {
            if (_agent == null) return;

            _wasMoving = _isMoving;
            _isMoving = _agent.hasPath && _agent.remainingDistance > stoppingDistance;

            if (_isMoving)
            {
                Vector3 direction = _agent.desiredVelocity.normalized;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }

            if (_wasMoving != _isMoving)
            {
                if (_isMoving)
                {
                    OnMovementStarted?.Invoke();
                    _animation?.RpcSetMoving(true);
                }
                else
                {
                    OnMovementStopped?.Invoke();
                    _animation?.RpcSetMoving(false);
                }
            }
        }

        public bool IsMoving => _isMoving;
        public Vector3 TargetPosition => _targetPosition;

        [Server]
        public void Stop()
        {
            if (_agent != null)
                _agent.ResetPath();
            _isMoving = false;
        }
    }
}
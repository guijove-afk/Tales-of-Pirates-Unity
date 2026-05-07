using UnityEngine;
using System;
using Mirror;
using UnityEngine.AI;
using TOP.Inventory;  // ✅ para PlayerAnimation
using UnityEngine.EventSystems;

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

        private NavMeshAgent _agent;
        private PlayerAnimation _animation;
        private Vector3 _targetPosition;
        private bool _isMoving;
        private bool _wasMoving;

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


    if (Input.GetMouseButton(0)) 
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                CmdMoveTo(hit.point);
            }
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

            // ✅ Eventos disparados
            if (_wasMoving != _isMoving)
            {
                if (_isMoving)
                {
                    OnMovementStarted?.Invoke();
                    _animation?.RpcSetMoving(true);  // ✅ só bool
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

        // ✅ Métodos públicos para PlayerController
        [Server]
        public void Stop()
        {
            if (_agent != null)
                _agent.ResetPath();
            _isMoving = false;
        }
    }
}
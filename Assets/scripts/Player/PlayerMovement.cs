using UnityEngine;
using Mirror;
using UnityEngine.AI;

namespace TOP.Player
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class PlayerMovement : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float runSpeed = 10f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float stoppingDistance = 0.5f;

        private NavMeshAgent _agent;
        private PlayerAnimation _animation;
        private Vector3 _targetPosition;
        private bool _isMoving;

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
            // Click to move
            if (Input.GetMouseButtonDown(1))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Ground")))
                {
                    CmdMoveTo(hit.point);
                }
            }

            // WASD movement
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            if (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f)
            {
                Vector3 moveDir = new Vector3(h, 0, v).normalized;
                Vector3 targetPos = transform.position + moveDir * 2f;
                CmdMoveTo(targetPos);
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

            bool wasMoving = _isMoving;
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

            if (wasMoving != _isMoving)
            {
                _animation.RpcSetMoving(_isMoving, _agent.velocity.magnitude);
            }
        }

        public bool IsMoving => _isMoving;
        public Vector3 TargetPosition => _targetPosition;
    }
}

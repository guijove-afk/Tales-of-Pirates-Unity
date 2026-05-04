using UnityEngine;
using UnityEngine.AI;
using Mirror;
using System;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float baseMoveSpeed = 5f;
    [SerializeField] private float runSpeedMultiplier = 1.5f;
    [SerializeField] private float rotationSpeed = 360f;
    [SerializeField] private float stoppingDistance = 0.1f;
    [SerializeField] private float arrivalTolerance = 0.2f;
    [SerializeField] private float destinationSettleTime = 0.1f;

    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer = 1;
    [SerializeField] private LayerMask clickableLayers = ~0;
    [SerializeField] private LayerMask interactableLayers = ~0;

    [Header("Animation")]
    [SerializeField] private string moveSpeedParam = "MoveSpeed";
    [SerializeField] private string isMovingParam = "IsMoving";
    [SerializeField] private string isRunningParam = "IsRunning";

    private NavMeshAgent agent;
    private Animator anim;
    private Camera mainCamera;
    private PlayerStats stats;
    private PlayerCombat combat;

    private bool isMovementEnabled = true;
    private bool isRunning;
    private Transform followTarget;
    private float followStopDistance;
    private Vector3 targetPoint;
    private bool hasPointDestination;
    private Action onReachTarget;
    private bool arrivalTriggered;
    private float nextArrivalCheckTime;

    public event Action OnMovementStarted;
    public event Action OnMovementStopped;
    public event Action OnDestinationSet;
#pragma warning disable CS0067
    public event Action OnInteractableFound;
#pragma warning restore CS0067

    public bool IsMoving => agent != null && !agent.isStopped && (agent.pathPending || agent.remainingDistance > agent.stoppingDistance + arrivalTolerance);
    public Vector3 CurrentDestination => agent != null && agent.hasPath ? agent.destination : transform.position;
    public float CurrentSpeed => agent != null ? agent.velocity.magnitude : 0f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        stats = GetComponent<PlayerStats>();
        combat = GetComponent<PlayerCombat>();
        mainCamera = Camera.main;

        if (agent == null)
        {
            Debug.LogError("[PlayerMovement] NavMeshAgent nao encontrado!", this);
            enabled = false;
            return;
        }

        agent.speed = baseMoveSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.acceleration = baseMoveSpeed * 3f;
        agent.angularSpeed = rotationSpeed;
        agent.autoBraking = true;
    }

    void Start()
    {
        if (!isLocalPlayer)
        {
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (!isLocalPlayer || !isMovementEnabled) return;

        HandleInput();
        UpdateMoveGoal();
        UpdateAnimation();
    }

    private void HandleInput()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        agent.speed = isRunning ? baseMoveSpeed * runSpeedMultiplier : baseMoveSpeed;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, clickableLayers))
            {
                if (TryGetEnemyFromCollider(hit.collider, out EnemyStats enemyStats))
                {
                    combat?.Trace($"Raycast LMB inimigo col={hit.collider.name} root={enemyStats.gameObject.name}");
                    DeselectAllEnemies();

                    Transform enemyRoot = enemyStats.transform;
                    var selection = enemyRoot.GetComponent<EnemySelection>()
                        ?? enemyRoot.GetComponentInChildren<EnemySelection>();
                    selection?.SetSelected(true);

                    combat?.StartAutoAttack(enemyRoot);
                    return;
                }

                var pvpStats = hit.collider.GetComponent<PlayerStats>()
                    ?? hit.collider.GetComponentInParent<PlayerStats>();
                if (pvpStats != null && !ReferenceEquals(pvpStats, stats) && !pvpStats.IsDead)
                {
                    combat?.StartAutoAttack(((Component)pvpStats).transform);
                    return;
                }

                combat?.StopAutoAttack();

                if (hit.collider.TryGetComponent(out IInteractable interactable))
                {
                    float distance = Vector3.Distance(transform.position, hit.point);
                    if (distance <= interactable.GetInteractionRange())
                    {
                        interactable.OnInteract(this);
                    }
                    else
                    {
                        MoveToPoint(hit.point, interactable.GetInteractionRange(),
                            () => interactable.OnInteract(this));
                    }
                    return;
                }

                if (hit.collider.TryGetComponent(out WorldItem worldItem))
                {
                    float distance = Vector3.Distance(transform.position, hit.point);
                    if (distance <= 1.5f)
                    {
                        worldItem.Pickup(GetComponent<PlayerInventory>());
                    }
                    else
                    {
                        MoveToPoint(hit.point, 1.5f, () => worldItem.Pickup(GetComponent<PlayerInventory>()));
                    }
                    return;
                }

                MoveToPoint(hit.point);
            }
            else
            {
                combat?.Trace("Raycast LMB sem hit (layer clickable) ou distancia 0.");
                DeselectAllEnemies();
                combat?.ClearTarget();
            }
        }
    }

    private static bool TryGetEnemyFromCollider(Collider collider, out EnemyStats stats)
    {
        stats = collider.GetComponent<EnemyStats>();
        if (stats == null)
            stats = collider.GetComponentInParent<EnemyStats>();
        return stats != null && !stats.IsDead;
    }

    private void DeselectAllEnemies()
    {
        var allEnemies = UnityEngine.Object.FindObjectsByType<EnemySelection>(FindObjectsInactive.Include);
        foreach (var enemy in allEnemies)
            enemy.SetSelected(false);
    }

    public void MoveToPoint(Vector3 destination, float stopDistance = 0.1f, Action onArrive = null)
    {
        if (!TryGetNavMeshPoint(destination, out Vector3 navDestination))
        {
            Debug.LogWarning("[PlayerMovement] Destino fora do NavMesh!");
            return;
        }

        followTarget = null;
        hasPointDestination = true;
        targetPoint = navDestination;
        followStopDistance = stopDistance;
        onReachTarget = onArrive;
        arrivalTriggered = false;
        nextArrivalCheckTime = Time.time + destinationSettleTime;

        agent.isStopped = false;
        agent.stoppingDistance = stopDistance;
        agent.SetDestination(navDestination);

        OnDestinationSet?.Invoke();
        CmdSetDestination(navDestination, stopDistance);
    }

    public void FollowTarget(Transform target, float stopDistance, Action onArrive = null)
    {
        if (target == null)
        {
            StopMovement();
            return;
        }

        followTarget = target;
        hasPointDestination = false;
        followStopDistance = stopDistance;
        onReachTarget = onArrive;
        arrivalTriggered = false;
        nextArrivalCheckTime = Time.time + destinationSettleTime;
        agent.isStopped = false;
        agent.stoppingDistance = stopDistance;

        UpdateFollowDestination();
    }

    public void StopMovement()
    {
        followTarget = null;
        hasPointDestination = false;
        onReachTarget = null;
        arrivalTriggered = false;
        nextArrivalCheckTime = 0f;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }

        if (anim != null)
        {
            anim.SetFloat(moveSpeedParam, 0f);
            anim.SetBool(isMovingParam, false);
            anim.SetBool(isRunningParam, false);
        }
    }

    public void SetMovementEnabled(bool enabled)
    {
        isMovementEnabled = enabled;
        if (!enabled) StopMovement();
    }

    [Command]
    private void CmdSetDestination(Vector3 destination, float stopDistance)
    {
        if (TryGetNavMeshPoint(destination, out Vector3 navDestination))
        {
            agent.isStopped = false;
            agent.stoppingDistance = stopDistance;
            agent.SetDestination(navDestination);
        }
    }

    public void UpdateMoveSpeed(float newSpeed)
    {
        baseMoveSpeed = newSpeed;
        if (agent != null)
            agent.speed = isRunning ? baseMoveSpeed * runSpeedMultiplier : baseMoveSpeed;
    }

    private void UpdateMoveGoal()
    {
        if (Time.time < nextArrivalCheckTime)
            return;

        if (followTarget != null)
        {
            if (!followTarget.gameObject.activeInHierarchy)
            {
                StopMovement();
                return;
            }

            float distance = Vector3.Distance(transform.position, followTarget.position);
            if (distance <= followStopDistance + arrivalTolerance)
            {
                CompleteArrival();
                return;
            }

            UpdateFollowDestination();
            return;
        }

        if (!hasPointDestination)
            return;

        float pointDistance = Vector3.Distance(transform.position, targetPoint);
        if (pointDistance <= followStopDistance + arrivalTolerance)
            CompleteArrival();
    }

    private void UpdateFollowDestination()
    {
        if (followTarget == null)
            return;

        Vector3 desiredPosition = GetApproachPosition(followTarget);
        if (!TryGetNavMeshPoint(desiredPosition, out Vector3 navDestination))
        {
            Debug.LogWarning($"[PlayerMovement] FollowTarget: posicao {desiredPosition} sem NavMesh valido.", this);
            return;
        }

        agent.isStopped = false;
        agent.stoppingDistance = followStopDistance;
        agent.SetDestination(navDestination);
        nextArrivalCheckTime = Time.time + destinationSettleTime;
    }

    private Vector3 GetApproachPosition(Transform target)
    {
        Collider targetCollider = target.GetComponent<Collider>() ?? target.GetComponentInChildren<Collider>();
        if (targetCollider != null)
            return targetCollider.ClosestPoint(transform.position);

        return target.position;
    }

    private bool TryGetNavMeshPoint(Vector3 source, out Vector3 navPoint)
    {
        if (NavMesh.SamplePosition(source, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
        {
            navPoint = navHit.position;
            return true;
        }

        navPoint = source;
        return false;
    }

    private void CompleteArrival()
    {
        if (arrivalTriggered)
            return;

        arrivalTriggered = true;

        Action callback = onReachTarget;
        followTarget = null;
        hasPointDestination = false;
        onReachTarget = null;
        nextArrivalCheckTime = 0f;

        agent.isStopped = true;
        agent.ResetPath();

        callback?.Invoke();
    }

    private void UpdateAnimation()
    {
        bool wasMoving = anim.GetBool(isMovingParam);
        bool isMovingNow = IsMoving;

        float velocity = agent.speed > 0f ? agent.velocity.magnitude / agent.speed : 0f;
        anim.SetFloat(moveSpeedParam, velocity);
        anim.SetBool(isMovingParam, isMovingNow);
        anim.SetBool(isRunningParam, isRunning && isMovingNow);

        if (!wasMoving && isMovingNow)
            OnMovementStarted?.Invoke();
        else if (wasMoving && !isMovingNow)
            OnMovementStopped?.Invoke();
    }

    void OnDrawGizmosSelected()
    {
        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.cyan;
            Vector3[] corners = agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
                Gizmos.DrawLine(corners[i], corners[i + 1]);

            Gizmos.DrawWireSphere(agent.destination, 0.3f);
        }
    }
}

public interface IInteractable
{
    void OnInteract(PlayerMovement player);
    string GetInteractionName();
    float GetInteractionRange();
    bool CanInteract(PlayerMovement player);
}

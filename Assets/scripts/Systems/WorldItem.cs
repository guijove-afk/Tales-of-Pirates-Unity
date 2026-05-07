using UnityEngine;
using Mirror;

public class WorldItem : NetworkBehaviour
{
    [SyncVar] private int itemId;
    [SyncVar] private int quantity;
    [SyncVar] private float despawnTime;

    private ItemData itemData;
    private GameObject visualModel;
    private float spawnTime;
    private bool isPickedUp;
    private bool visualCreated = false;

    public int ItemId => itemId;
    public int Quantity => quantity;

    [Server]
    public void Initialize(ItemData item, int qty, float despawnDuration = 120f)
    {
        itemId = item.itemId;
        quantity = qty;
        despawnTime = Time.time + despawnDuration;
        spawnTime = Time.time;

        RpcCreateVisual(item.itemId);
    }

    [ClientRpc]
    private void RpcCreateVisual(int id)
    {
        if (ItemDatabase.Instance == null)
        {
            Debug.LogWarning("[WorldItem] ItemDatabase ainda não inicializado. Aguardando...");
            StartCoroutine(WaitForDatabase(id));
            return;
        }

        CreateVisual(id);
    }

    private System.Collections.IEnumerator WaitForDatabase(int id)
    {
        float timeout = 5f;
        float elapsed = 0f;

        while (ItemDatabase.Instance == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (ItemDatabase.Instance != null)
        {
            CreateVisual(id);
        }
        else
        {
            Debug.LogError("[WorldItem] Timeout aguardando ItemDatabase!");
        }
    }

    private void CreateVisual(int id)
    {
        if (visualCreated) return;

        itemData = ItemDatabase.Instance?.GetItem(id);
        if (itemData == null)
        {
            Debug.LogError($"[WorldItem] Item ID {id} não encontrado no ItemDatabase!");
            return;
        }

        if (itemData.worldModelPrefab != null)
        {
            visualModel = Instantiate(itemData.worldModelPrefab, transform);
            visualModel.transform.localRotation = Quaternion.Euler(itemData.dropRotation);
            visualModel.transform.localScale = itemData.dropScale;
        }
        else
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(transform);
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localScale = Vector3.one * 0.3f;
            Destroy(cube.GetComponent<Collider>());
            visualModel = cube;
        }

        if (GetComponent<Collider>() == null)
        {
            SphereCollider col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.5f;
        }

        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        visualCreated = true;
        StartCoroutine(FloatAnimation());
    }

    private System.Collections.IEnumerator FloatAnimation()
    {
        float offset = Random.Range(0f, Mathf.PI * 2f);

        while (visualModel != null)
        {
            float y = Mathf.Sin(Time.time * 2f + offset) * 0.2f;
            visualModel.transform.localPosition = new Vector3(0, y, 0);
            visualModel.transform.Rotate(Vector3.up, 50f * Time.deltaTime);
            yield return null;
        }
    }

    void Update()
    {
        if (!isServer) return;

        if (Time.time >= despawnTime && !isPickedUp)
        {
            NetworkServer.Destroy(gameObject);
        }
    }

    [Server]
    public void Pickup(PlayerInventory inventory)
    {
        if (isPickedUp) return;
        if (inventory == null) return;

        isPickedUp = true;

        // 🔧 CORREÇÃO: AddItem retorna void, verificar slot vazio antes
        int emptySlot = inventory.FindEmptySlot();
        if (emptySlot != -1)
        {
            inventory.AddItem(itemId, quantity);
            RpcPickupSuccess();
            NetworkServer.Destroy(gameObject);
        }
        else
        {
            isPickedUp = false;
            TargetInventoryFull(inventory.connectionToClient);
        }
    }

    [ClientRpc]
    private void RpcPickupSuccess()
    {
        if (itemData != null)
        {
            DamagePopupManager.Instance?.ShowText(transform.position,
                $"+{quantity} {itemData.itemName}", Color.yellow);
        }
    }

    [TargetRpc]
    private void TargetInventoryFull(NetworkConnectionToClient target)
    {
        DamagePopupManager.Instance?.ShowText(transform.position, "Inventário Cheio!", Color.red);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        if (other.TryGetComponent(out PlayerInventory inventory))
        {
            // Auto-pickup opcional
            // Pickup(inventory);
        }
    }
}
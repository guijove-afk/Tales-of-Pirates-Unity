using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "TOP/Item")]
public class ItemData : ScriptableObject
{
    [Header("Informações Básicas")]
    public string itemId;
    public string itemName;
    [TextArea(3, 5)]
    public string description;
    
    [Header("Visual")]
    public Sprite icon;
    public int maxStack = 1;
    
    [Header("Propriedades")]
    public bool isDroppable = true;
    public ItemType itemType;
    public ItemRarity rarity = ItemRarity.Normal;
    
    [Header("Efeitos (opcional)")]
    public ParticleSystem useEffect;   // <-- já existia, usado por ConsumableData
    public AudioClip useSound;         // <-- já existia, usado por ConsumableData
    
    [Header("Drop no Mundo")]
    public GameObject worldModelPrefab;  // <-- NOVO: modelo 3D no chão
    public Vector3 dropRotation;         // <-- NOVO: rotação ao dropar
    public float dropScale = 1f;       // <-- NOVO: escala ao dropar
}
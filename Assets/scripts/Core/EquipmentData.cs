using UnityEngine;

[CreateAssetMenu(fileName = "NewEquipment", menuName = "Tales of Pirates/Equipment Data")]
public class EquipmentData : ItemData
{
    [Header("Equipamento")]
    public EquipmentSlot slot;
    public WeaponType weaponType;
    public ArmorType armorType;
    
    [Header("Requisitos")]
    public int requiredLevel;
    public CharacterClass requiredClass;
    
    [Header("Durabilidade")]
    public int durability = 100;
    public int maxDurability = 100;
    
    [Header("Slots de Gema")]
    public int gemSlots = 0;
    
    [Header("Modelos 3D")]
    public GameObject maleModelPrefab;
    public GameObject femaleModelPrefab;
    public Vector3 modelOffset;
    public Vector3 modelRotation;
    public Vector3 modelScale = Vector3.one;
    
    [Header("Efeitos")]
    public ParticleSystem equipEffect;
    public AudioClip equipSound;
    
    [Header("Bônus de Status")]
    public int bonusSTR;
    public int bonusAGI;
    public int bonusCON;
    public int bonusSPR;
    public int bonusACC;
    public int bonusLUCK;
    public int bonusHP;
    public int bonusMP;
    public int bonusSP;
    public int bonusAttack;
    public int bonusDefense;
    public int bonusMagicAttack;
    public int bonusMagicDefense;
    public float bonusAttackSpeed;
    public float bonusMoveSpeed;
}
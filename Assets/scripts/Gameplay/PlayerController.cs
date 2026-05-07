using UnityEngine;
using Mirror;
using TOP.Player;  // ✅ Todos os componentes aqui
using TOP.Core;    // ✅ ItemData, SkillData
using TOP.Inventory; // ✅ ItemDatabase
using System.Collections;
using System.Linq;
using TOP.UI; // ✅ UIManager para mensagens

namespace TOP.Gameplay
{
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(PlayerInventory))]
    [RequireComponent(typeof(PlayerEquipment))]
    [RequireComponent(typeof(PlayerCombat))]
    [RequireComponent(typeof(PlayerSkills))]
    [RequireComponent(typeof(PlayerAnimation))]
    public class PlayerController : NetworkBehaviour
    {
        [SyncVar] public long CharacterId;
        [SyncVar] public long AccountId;
        [SyncVar] public string CharacterName;
        [SyncVar] public byte Job;
        [SyncVar] public int Level;
        [SyncVar] public int CurrentHp;
        [SyncVar] public int CurrentMp;
        [SyncVar] public int CurrentSp;

        [Header("Components")]
        public PlayerMovement Movement;
        public PlayerStats Stats;
        public PlayerInventory Inventory;
        public PlayerEquipment Equipment;
        public PlayerCombat Combat;
        public PlayerSkills Skills;
        public PlayerAnimation Animation;
        public PlayerConsumables Consumables;

        [Header("Settings")]
        [SerializeField] private float autoSaveInterval = 60f;
        private float _lastSaveTime;

        void Awake()
        {
            Movement = GetComponent<PlayerMovement>();
            Stats = GetComponent<PlayerStats>();
            Inventory = GetComponent<PlayerInventory>();
            Equipment = GetComponent<PlayerEquipment>();
            Combat = GetComponent<PlayerCombat>();
            Skills = GetComponent<PlayerSkills>();
            Animation = GetComponent<PlayerAnimation>();
            // Consumables = GetComponent<PlayerConsumables>(); // ✅ Criaremos depois
        }

public override void OnStartLocalPlayer()
{
    base.OnStartLocalPlayer();

    // ✅ CameraFollow no PLAYER (não na Main Camera)
    CameraFollow camFollow = GetComponent<CameraFollow>();
    if (camFollow != null)
        camFollow.enabled = true;

    UIManager.Instance?.SetupLocalPlayer(this);
}

        public override void OnStartServer()
        {
            base.OnStartServer();

            // ✅ DebugGiveItem inicial
            StartCoroutine(GiveStarterItems());
        }

        // ✅ Dá itens iniciais para teste
        IEnumerator GiveStarterItems()
        {
            yield return new WaitForSeconds(1f);

            Inventory.DebugGiveItem(1001, 99);  // Health Potion
            Inventory.DebugGiveItem(1002, 1);   // Iron Sword
            Skills.LearnSkill(1001, 1);         // Fireball skill

            Debug.Log($"[PlayerController] Itens iniciais dados para {CharacterName}");
        }

        void Update()
        {
            if (isServer && Time.time > _lastSaveTime + autoSaveInterval)
            {
                _lastSaveTime = Time.time;
                SaveToDatabase();
            }
        }

[Server]
public void InitializeFromDatabase(CharacterDbModel data) // Use o nome da classe que representa sua tabela
{
    // Por enquanto, mantemos o MOCK, mas o parâmetro resolve o erro de compilação
    CharacterId = netId; 
    CharacterName = data != null ? data.name : $"Player{netId}";
    
    // ... restante do seu código de inicialização
    Debug.Log($"[PlayerController] Pronto para carregar dados de: {CharacterName}");
}

        [Server]
        public void SaveToDatabase()
        {
            if (Stats == null) return;

            Debug.Log($"[PlayerController] Salvando {CharacterName}: HP={Stats.CurrentHp}/{Stats.MaxHp}");
            // TODO: DatabaseService real
        }

        [Command]
        public void CmdMoveTo(Vector3 destination)
        {
            if (Movement != null)
                Movement.SetDestination(destination);
        }

        [Command]
        public void CmdAttackTarget(NetworkIdentity target)
        {
            if (Combat != null && target != null)
                Combat.AttackTarget(target);
        }

        [Command]
        public void CmdUseSkill(int skillId, Vector3 targetPos, NetworkIdentity target)
        {
            if (Skills != null)
                Skills.UseSkill(skillId, targetPos, target);
        }

        [Command]
        public void CmdSetTarget(NetworkIdentity target)
        {
            if (Combat != null)
                Combat.SetTarget(target);
        }

        [ClientRpc]
        public void RpcTakeDamage(int damage, Vector3 hitPosition)
        {
            // ✅ Popup simples no console (popup real depois)
            Debug.Log($"<color=red>-{damage}</color> em {CharacterName}");

            // Hit flash (criaremos depois)
            // GetComponentInChildren<HitFlashEffect>()?.Flash();
        }

        [ClientRpc]
        public void RpcHeal(int amount)
        {
            Debug.Log($"<color=green>+{amount}</color> em {CharacterName}");
        }

        [ClientRpc]
        public void RpcLevelUp()
        {
            Level++;
            Debug.Log($"<color=yellow>LEVEL UP! {CharacterName} agora nível {Level}</color>");
        }

        [ClientRpc]
        public void RpcShowMessage(string message, PlayerMessageType type)
        {
            string color = type switch
            {
                PlayerMessageType.Info => "white",
                PlayerMessageType.Warning => "yellow",
                PlayerMessageType.Error => "red",
                PlayerMessageType.Success => "green",
                _ => "white"
            };
            Debug.Log($"[{type}] <color={color}>{message}</color>");
        }

        // ✅ Chamado por PlayerStats
        [Server]
        public void Die()
        {
            RpcDie();
            Invoke(nameof(RespawnPlayer), 5f);
        }

        [ClientRpc]
        void RpcDie()
        {
            Animation?.PlayDeath();
            if (Movement != null) Movement.enabled = false;
            if (Combat != null) Combat.enabled = false;
        }

        [Server]
        public void RespawnPlayer()
        {
            CurrentHp = Stats.MaxHp;
            CurrentMp = Stats.MaxMp;
            CurrentSp = Stats.MaxSp;

            transform.position = Vector3.zero + Vector3.up * 1.5f;  // ✅ Corrigido

            RpcRespawn();
            Debug.Log($"[PlayerController] {CharacterName} ressuscitado!");
        }

        [ClientRpc]
        void RpcRespawn()
        {
            Animation?.PlayRespawn();
            if (Movement != null) Movement.enabled = true;
            if (Combat != null) Combat.enabled = true;
        }

        void OnDestroy()
        {
            if (isServer)
                SaveToDatabase();
        }

        // ✅ Enum local
        public enum PlayerMessageType
        {
            Info, Warning, Error, Success
        }
    }
}
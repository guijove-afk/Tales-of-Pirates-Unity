// =================================================================================
// TOPFullSetupCreator.cs - Parte 1/5
// Cria TODO o sistema de Login/Character Select/Game para Tales of Pirates Unity
// Unity 6 + Mirror Networking
// =================================================================================
// Como usar:
// 1. Crie uma pasta "Editor" em Assets/ (se nao existir)
// 2. Salve este arquivo em Assets/Editor/TOPFullSetupCreator.cs
// 3. No Unity, va em Tools > TOP > Full Setup Creator
// 4. Clique em "Create Full Setup" e siga as instrucoes
// =================================================================================

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Linq;
using Mirror;
using TOP.Core;
using TOP.Data;
using TOP.Network;
using TOP.Services;
using TOP.UI;
using TOP.Inventory;
using TOP.Player;
using TOP.Systems;
using TOP.Gameplay;




namespace TOP.Editor
{
    public class TOPFullSetupCreator : EditorWindow
    {
        private string projectNamespace = "TOP";
        private bool createScenes = true;
        private bool createScripts = true;
        private bool createPrefabs = true;
        private bool createUI = true;
        private bool setupMirror = true;
        private Vector2 scrollPos;

        [MenuItem("Tools/TOP/Full Setup Creator")]
        public static void ShowWindow()
        {
            GetWindow<TOPFullSetupCreator>("TOP Full Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Tales of Pirates - Full Setup Creator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.HelpBox(
                "Este wizard cria automaticamente:\n" +
                "- 3 Scenes (LoginScene, CharacterSelectScene, GameScene)\n" +
                "- Scripts de Network (TOPNetworkManager, NetworkMessages, etc.)\n" +
                "- Scripts de Database (DatabaseService, CharacterData, etc.)\n" +
                "- Scripts de UI (LoginUIManager, CharacterSelectUI, etc.)\n" +
                "- Scripts de Player (PlayerController, PlayerInventory, etc.)\n" +
                "- Prefabs (Player, NetworkManager, Canvas, etc.)\n" +
                "- Setup do Mirror Networking", MessageType.Info);

            GUILayout.Space(10);
            projectNamespace = EditorGUILayout.TextField("Namespace", projectNamespace);
            GUILayout.Space(10);

            createScenes = EditorGUILayout.Toggle("Criar Scenes", createScenes);
            createScripts = EditorGUILayout.Toggle("Criar Scripts", createScripts);
            createPrefabs = EditorGUILayout.Toggle("Criar Prefabs", createPrefabs);
            createUI = EditorGUILayout.Toggle("Criar UI", createUI);
            setupMirror = EditorGUILayout.Toggle("Setup Mirror", setupMirror);

            GUILayout.Space(20);
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("CREATE FULL SETUP", GUILayout.Height(50)))
            {
                if (EditorUtility.DisplayDialog("Confirmar",
                    "Isso vai criar/modificar varios arquivos no projeto. Continuar?",
                    "Sim", "Cancelar"))
                {
                    RunFullSetup();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndScrollView();
        }

        private void RunFullSetup()
        {
            try
            {
                AssetDatabase.StartAssetEditing();

                if (createScripts) CreateAllScripts();
                if (createScenes) CreateAllScenes();
                if (createPrefabs) CreateAllPrefabs();
                if (setupMirror) SetupMirrorNetwork();

                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Sucesso!",
                    "Setup completo criado!\n\n" +
                    "Proximos passos:\n" +
                    "1. Adicione as 3 scenes no Build Settings (File > Build Settings)\n" +
                    "2. Arraste o prefab 'TOPNetworkManager' para a LoginScene\n" +
                    "3. Arraste o prefab 'Player' no campo Player Prefab do NetworkManager\n" +
                    "4. Configure a conexao do DatabaseService no Inspector\n" +
                    "5. Adicione o package Mirror via Package Manager", "OK");
            }
            catch (System.Exception ex)
            {
                AssetDatabase.StopAssetEditing();
                Debug.LogError($"[TOP Setup] Erro: {ex}");
                EditorUtility.DisplayDialog("Erro", ex.Message, "OK");
            }
        }

        // =================================================================================
        // HELPERS
        // =================================================================================
        private void EnsureFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                string metaPath = path + ".meta";
                if (!File.Exists(metaPath))
                {
                    File.WriteAllText(metaPath, "fileFormatVersion: 2\nguid: " + System.Guid.NewGuid().ToString("N") + "\n");
                }
            }
        }

        private void WriteFile(string path, string content)
        {
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            // Nao sobrescreve arquivos ja existentes
            if (File.Exists(path))
            {
                Debug.Log($"[TOP Setup] Pulando {path} (ja existe)");
                return;
            }

            File.WriteAllText(path, content);
        }

        // =================================================================================
        // CREATE ALL SCRIPTS
        // =================================================================================
        private void CreateAllScripts()
        {
            string scriptsPath = "Assets/scripts";
            EnsureFolder(scriptsPath);
            EnsureFolder($"{scriptsPath}/Core");
            EnsureFolder($"{scriptsPath}/Data");
            EnsureFolder($"{scriptsPath}/Database");
            EnsureFolder($"{scriptsPath}/Gameplay");
            EnsureFolder($"{scriptsPath}/Inventory");
            EnsureFolder($"{scriptsPath}/Network");
            EnsureFolder($"{scriptsPath}/Player");
            EnsureFolder($"{scriptsPath}/Services");
            EnsureFolder($"{scriptsPath}/UI");
            EnsureFolder($"{scriptsPath}/World");

            // --- CORE ---
            WriteFile($"{scriptsPath}/Core/DamageType.cs", GetDamageTypeScript()); // CORRIGIDO: Adicionado
            WriteFile($"{scriptsPath}/Core/ItemData.cs", GetItemDataScript());
            WriteFile($"{scriptsPath}/Core/CharacterClassData.cs", GetCharacterClassDataScript());
            WriteFile($"{scriptsPath}/Core/EquipmentData.cs", GetEquipmentDataScript());
            WriteFile($"{scriptsPath}/Core/ConsumableData.cs", GetConsumableDataScript());
            WriteFile($"{scriptsPath}/Core/GemData.cs", GetGemDataScript());
            WriteFile($"{scriptsPath}/Core/SetData.cs", GetSetDataScript());
            WriteFile($"{scriptsPath}/Core/SkillData.cs", GetSkillDataScript());
            WriteFile($"{scriptsPath}/Core/SkillBookData.cs", GetSkillBookDataScript());
            WriteFile($"{scriptsPath}/Core/ICharacterStats.cs", GetICharacterStatsScript());

            // --- DATA ---
            WriteFile($"{scriptsPath}/Data/CharacterData.cs", GetCharacterDataScript());
            WriteFile($"{scriptsPath}/Data/SyncData.cs", GetSyncDataScript());

            // --- DATABASE ---
            WriteFile($"{scriptsPath}/Database/QuestDatabase.cs", GetQuestDatabaseScript());
            WriteFile($"{scriptsPath}/Database/SkillDatabase.cs", GetSkillDatabaseScript());

            // --- GAMEPLAY ---
            WriteFile($"{scriptsPath}/Gameplay/PlayerController.cs", GetPlayerControllerScript());

            // --- INVENTORY ---
            WriteFile($"{scriptsPath}/Inventory/InventoryItem.cs", GetInventoryItemScript());
            WriteFile($"{scriptsPath}/Inventory/EquippedItem.cs", GetEquippedItemScript());
            WriteFile($"{scriptsPath}/Inventory/InventorySystem.cs", GetInventorySystemScript());
            WriteFile($"{scriptsPath}/Inventory/ItemDatabase.cs", GetItemDatabaseScript());
            WriteFile($"{scriptsPath}/Inventory/ItemSlotUI.cs", GetItemSlotUIScript());
            WriteFile($"{scriptsPath}/Inventory/EquipmentSlotUI.cs", GetEquipmentSlotUIScript());
            WriteFile($"{scriptsPath}/Inventory/InventoryUI.cs", GetInventoryUIScript());
            WriteFile($"{scriptsPath}/Inventory/CloseInventoryButton.cs", GetCloseInventoryButtonScript());
            WriteFile($"{scriptsPath}/Inventory/InventoryTestHelper.cs", GetInventoryTestHelperScript());

            // --- NETWORK ---
            WriteFile($"{scriptsPath}/Network/NetworkMessages.cs", GetNetworkMessagesScript());
            WriteFile($"{scriptsPath}/Network/TOPNetworkManager.cs", GetTOPNetworkManagerScript());

            // --- PLAYER ---
            WriteFile($"{scriptsPath}/Player/CameraFollow.cs", GetCameraFollowScript());
            WriteFile($"{scriptsPath}/Player/PlayerAnimation.cs", GetPlayerAnimationScript());
            WriteFile($"{scriptsPath}/Player/PlayerClass.cs", GetPlayerClassScript());
            WriteFile($"{scriptsPath}/Player/PlayerCombat.cs", GetPlayerCombatScript());
            WriteFile($"{scriptsPath}/Player/PlayerConsumables.cs", GetPlayerConsumablesScript());
            WriteFile($"{scriptsPath}/Player/PlayerDamageDetector.cs", GetPlayerDamageDetectorScript());
            WriteFile($"{scriptsPath}/Player/PlayerEquipment.cs", GetPlayerEquipmentScript());
            WriteFile($"{scriptsPath}/Player/PlayerHotbar.cs", GetPlayerHotbarScript());
            WriteFile($"{scriptsPath}/Player/PlayerInventory.cs", GetPlayerInventoryScript());
            WriteFile($"{scriptsPath}/Player/PlayerMovement.cs", GetPlayerMovementScript());
            WriteFile($"{scriptsPath}/Player/PlayerRespawn.cs", GetPlayerRespawnScript());
            WriteFile($"{scriptsPath}/Player/PlayerSkills.cs", GetPlayerSkillsScript());
            WriteFile($"{scriptsPath}/Player/PlayerStats.cs", GetPlayerStatsScript());

            // --- SERVICES ---
            WriteFile($"{scriptsPath}/Services/DatabaseService.cs", GetDatabaseServiceScript());
            WriteFile($"{scriptsPath}/Services/SecurityLogService.cs", GetSecurityLogServiceScript());

            // --- UI ---
            WriteFile($"{scriptsPath}/UI/LoginUIManager.cs", GetLoginUIManagerScript());
            WriteFile($"{scriptsPath}/UI/CharacterSelectUIManager.cs", GetCharacterSelectUIManagerScript());
            WriteFile($"{scriptsPath}/UI/UIManager.cs", GetUIManagerScript());
            WriteFile($"{scriptsPath}/UI/DamagePopup.cs", GetDamagePopupScript());
            WriteFile($"{scriptsPath}/UI/CombatFeedbackType.cs", GetCombatFeedbackTypeScript());
            WriteFile($"{scriptsPath}/UI/HitFlashEffect.cs", GetHitFlashEffectScript());
            WriteFile($"{scriptsPath}/UI/ScreenHitFlash.cs", GetScreenHitFlashScript());
            WriteFile($"{scriptsPath}/UI/OneShotAutoDestroy.cs", GetOneShotAutoDestroyScript());

            // --- WORLD ---
            WriteFile($"{scriptsPath}/World/WorldItemManager.cs", GetWorldItemManagerScript());

            // --- ROOT ---
            WriteFile($"{scriptsPath}/AutoStart.cs", GetAutoStartScript());
            WriteFile($"{scriptsPath}/DontDestroyOnLoad.cs", GetDontDestroyOnLoadScript());
            WriteFile($"{scriptsPath}/DebugGiveItem.cs", GetDebugGiveItemScript());

            // --- SYSTEMS (na raiz do projeto) ---
            string systemsPath = "Assets/Systems";
            EnsureFolder(systemsPath);
            WriteFile($"{systemsPath}/BuffManager.cs", GetBuffManagerScript());
            WriteFile($"{systemsPath}/CameraController.cs", GetCameraControllerScript());
            WriteFile($"{systemsPath}/DamagePopupManager.cs", GetDamagePopupManagerScript());
            WriteFile($"{systemsPath}/LevelUpEffectManager.cs", GetLevelUpEffectManagerScript());
            WriteFile($"{systemsPath}/SkillProjectile.cs", GetSkillProjectileScript());
            WriteFile($"{systemsPath}/WorldItem.cs", GetWorldItemScript());

            Debug.Log("[TOP Setup] Scripts criados com sucesso!");
        }

        // =================================================================================
        // SCRIPTS - DATA LAYER
        // =================================================================================
        private string GetCharacterDataScript()
        {
            return @"using System;
using System.Collections.Generic;

namespace TOP.Data
{
    [Serializable]
    public class CharacterData
    {
        public long Id;
        public long AccountId;
        public string Name;
        public byte Job;
        public byte Gender;
        public byte HairStyle;
        public byte HairColor;
        public byte FaceStyle;

        public int Level;
        public ulong Exp;

        public int BaseStr;
        public int BaseAgi;
        public int BaseCon;
        public int BaseSpr;

        public int MaxHp;
        public int MaxMp;
        public int MaxSp;
        public int CurrentHp;
        public int CurrentMp;
        public int CurrentSp;

        public ulong Gold;
        public string MapName;
        public float PosX, PosY, PosZ;
        public float RotationY;

        public DateTime CreatedAt;
        public DateTime LastOnline;
        public bool IsDeleted;
        public bool IsOnline;

        public List<DbInventoryItem> Inventory = new List<DbInventoryItem>();
        public List<CharacterSkillData> Skills = new List<CharacterSkillData>();
    }

    // CORRIGIDO: Renomeado para evitar conflito com TOP.Inventory.InventoryItem
    [Serializable]
    public class DbInventoryItem
    {
        public long Id;
        public long CharacterId;
        public ushort SlotIndex;
        public int ItemId;
        public int Quantity;
        public ushort Durability;
        public bool IsEquipped;
    }

    [Serializable]
    public class CharacterSkillData
    {
        public long Id;
        public long CharacterId;
        public int SkillId;
        public byte Level;
        public ulong Exp;
    }
}";
        }

        private string GetSyncDataScript()
        {
            return @"using System;
using UnityEngine;

namespace TOP.Data
{
    [Serializable]
    public struct SyncCharacterData
    {
        public long CharacterId;
        public string Name;
        public byte Job;
        public int Level;
        public int CurrentHp;
        public int CurrentMp;
        public int CurrentSp;
        public Vector3 Position;
        public float RotationY;
        public string MapName;
    }
}";
        }

        // =================================================================================
        // SCRIPTS - CORE DATA
        // =================================================================================
        private string GetItemDataScript()
        {
            return @"using UnityEngine;

namespace TOP.Core
{
    [CreateAssetMenu(fileName = ""NewItem"", menuName = ""TOP/Item Data"")]
    public class ItemData : ScriptableObject
    {
        public int ItemId;
        public string ItemName;
        public string Description;
        public Sprite Icon;
        public ItemType Type;
        public int MaxStack = 1;
        public int GoldValue;
        public bool IsTradable = true;
        public bool IsDroppable = true;
        public int RequiredLevel;
        public byte RequiredJob;
    }

    public enum ItemType
    {
        Weapon, Armor, Accessory, Consumable, Material, Quest, Gem, SkillBook, Misc
    }
}";
        }

        private string GetCharacterClassDataScript()
        {
            return @"using UnityEngine;

namespace TOP.Core
{
    [CreateAssetMenu(fileName = ""NewClass"", menuName = ""TOP/Character Class"")]
    public class CharacterClassData : ScriptableObject
    {
        public byte ClassId;
        public string ClassName;
        public string Description;
        public Sprite Icon;
        public ClassType Type;
        public int BaseStr;
        public int BaseAgi;
        public int BaseCon;
        public int BaseSpr;
        public int BaseHp;
        public int BaseMp;
        public int BaseSp;
        public float HpGrowth;
        public float MpGrowth;
        public float SpGrowth;
        public float StrGrowth;
        public float AgiGrowth;
        public float ConGrowth;
        public float SprGrowth;
        public int[] SkillIds;
    }

    public enum ClassType
    {
        Swordsman, Hunter, Herbalist, Explorer
    }
}";
        }

        private string GetEquipmentDataScript()
        {
            return @"using UnityEngine;

namespace TOP.Core
{
    [CreateAssetMenu(fileName = ""NewEquipment"", menuName = ""TOP/Equipment Data"")]
    public class EquipmentData : ItemData
    {
        public EquipmentSlot Slot;
        public int PhysicalAttack;
        public int MagicAttack;
        public int PhysicalDefense;
        public int MagicDefense;
        public int MaxDurability = 100;
        public int StrBonus;
        public int AgiBonus;
        public int ConBonus;
        public int SprBonus;
        public int HpBonus;
        public int MpBonus;
        public int SpBonus;
        public float MoveSpeedBonus;
        public float AttackSpeedBonus;
        public int GemSlots;
        public GameObject ModelPrefab;
    }

    public enum EquipmentSlot
    {
        Head, Body, Gloves, Boots, Weapon, Shield, Ring1, Ring2, Necklace, Cape
    }
}";
        }

        private string GetConsumableDataScript()
        {
            return @"using UnityEngine;

namespace TOP.Core
{
    [CreateAssetMenu(fileName = ""NewConsumable"", menuName = ""TOP/Consumable Data"")]
    public class ConsumableData : ItemData
    {
        public ConsumableType ConsumableType;
        public int HpRestore;
        public int MpRestore;
        public int SpRestore;
        public float Duration;
        public int BuffId;
        public int BuffLevel;
    }

    public enum ConsumableType
    {
        HpPotion, MpPotion, SpPotion, Food, Buff, Scroll, Resurrection
    }
}";
        }

        private string GetGemDataScript()
        {
            return @"using UnityEngine;

namespace TOP.Core
{
    [CreateAssetMenu(fileName = ""NewGem"", menuName = ""TOP/Gem Data"")]
    public class GemData : ItemData
    {
        public GemType Type;
        public int Level;
        public int StrBonus;
        public int AgiBonus;
        public int ConBonus;
        public int SprBonus;
        public int HpBonus;
        public int MpBonus;
        public int AttackBonus;
        public int DefenseBonus;
    }

    public enum GemType
    {
        Ruby, Sapphire, Emerald, Diamond, Amethyst, Topaz, Onyx, Pearl
    }
}";
        }

        private string GetSetDataScript()
        {
            return @"using UnityEngine;

namespace TOP.Core
{
    [CreateAssetMenu(fileName = ""NewSet"", menuName = ""TOP/Set Data"")]
    public class SetData : ScriptableObject
    {
        public int SetId;
        public string SetName;
        public int[] RequiredItemIds;
        public SetBonus[] Bonuses;
    }

    [System.Serializable]
    public class SetBonus
    {
        public int PiecesRequired;
        public int HpBonus;
        public int MpBonus;
        public int AttackBonus;
        public int DefenseBonus;
        public float MoveSpeedBonus;
    }
}";
        }

        private string GetSkillDataScript()
        {
            return @"using UnityEngine;

namespace TOP.Core
{
    [CreateAssetMenu(fileName = ""NewSkill"", menuName = ""TOP/Skill Data"")]
    public class SkillData : ScriptableObject
    {
        public int SkillId;
        public string SkillName;
        public string Description;
        public Sprite Icon;
        public SkillType Type;
        public int RequiredJob;
        public int RequiredLevel;
        public int RequiredSkillId;
        public int RequiredSkillLevel;
        public int MpCost;
        public int SpCost;
        public float CastTime;
        public float Cooldown;
        public float Range;
        public int BaseDamage;
        public float DamageMultiplier;
        public DamageType DamageType;
        public TargetType TargetType;
        public int MaxLevel;
        public GameObject EffectPrefab;
        public GameObject ProjectilePrefab;
        public AudioClip CastSound;
    }

    public enum SkillType
    {
        Active, Passive, Buff, Debuff, Summon, Transform
    }

    public enum TargetType
    {
        Self, Single, AoE, Line, Cone, Chain
    }
}";
        }

        private string GetSkillBookDataScript()
        {
            return @"using UnityEngine;

namespace TOP.Core
{
    [CreateAssetMenu(fileName = ""NewSkillBook"", menuName = ""TOP/Skill Book"")]
    public class SkillBookData : ItemData
    {
        public int SkillId;
        public int RequiredLevel;
    }
}";
        }

        private string GetDamageTypeScript()
        {
            return @"namespace TOP.Core
{
    public enum DamageType
    {
        Physical, Magic, True, Heal
    }
}";
        }

        private string GetICharacterStatsScript()
        {
            return @"namespace TOP.Core
{
    public interface ICharacterStats
    {
        int Strength { get; }
        int Agility { get; }
        int Constitution { get; }
        int Spirit { get; }
        int MaxHp { get; }
        int MaxMp { get; }
        int MaxSp { get; }
        int PhysicalAttack { get; }
        int MagicAttack { get; }
        int PhysicalDefense { get; }
        int MagicDefense { get; }
        float MoveSpeed { get; }
        float AttackSpeed { get; }
        float CriticalRate { get; }
        float CriticalDamage { get; }
        float HpRegen { get; }
        float MpRegen { get; }
        float SpRegen { get; }

        // CORRIGIDO: Metodos de combate padronizados
        void TakeDamage(int damage, uint attackerId = 0, DamageType damageType = DamageType.Physical);
        void Heal(int amount);
        bool IsDead { get; }
    }
}";
        }

        // =================================================================================
        // SCRIPTS - NETWORK
        // =================================================================================
        private string GetNetworkMessagesScript()
        {
            return @"using Mirror;
using UnityEngine;
using System;
using TOP.Core;
using TOP.Services;

namespace TOP.Network
{
    public struct LoginRequest : NetworkMessage
    {
        public string Username;
        public string Password;
    }

    public struct LoginResponse : NetworkMessage
    {
        public bool Success;
        public string SessionToken;
        public string ErrorCode;
        public string Message;
    }

    public struct CharacterListRequest : NetworkMessage { }

    public struct CharacterListResponse : NetworkMessage
    {
        public bool Success;
        public string Error;
        public CharacterPreviewData[] Characters;
    }

    public struct CreateCharacterRequest : NetworkMessage
    {
        public byte SlotIndex;
        public string Name;
        public byte Gender;
        public byte Job;
        public byte HairStyle;
        public byte HairColor;
    }

    public struct CreateCharacterResponse : NetworkMessage
    {
        public bool Success;
        public long CharacterId;
        public string Error;
    }

    public struct SelectCharacterRequest : NetworkMessage
    {
        public long CharacterId;
    }

    public struct SelectCharacterResponse : NetworkMessage
    {
        public bool Success;
        public long CharacterId;
        public string MapName;
        public Vector3 Position;
        public float RotationY;
        public string Error;
    }

    public struct DeleteCharacterRequest : NetworkMessage
    {
        public long CharacterId;
        public string Password;
    }

    public struct DeleteCharacterResponse : NetworkMessage
    {
        public bool Success;
        public string Error;
    }

    public struct ClientPing : NetworkMessage
    {
        public float ClientTime;
    }

    public struct ServerPong : NetworkMessage
    {
        public float ClientTime;
        public float ServerTime;
    }

    public struct ServerMessage : NetworkMessage
    {
        public MessageType Type;
        public string Text;
    }

    public enum MessageType
    {
        Info, Warning, Error, Kicked, Banned, Maintenance
    }

    public struct MoveItemRequest : NetworkMessage
    {
        public ushort FromSlot;
        public ushort ToSlot;
    }

    public struct EquipItemRequest : NetworkMessage
    {
        public ushort InventorySlot;
        public EquipmentSlot TargetSlot;
    }

    public struct DropItemRequest : NetworkMessage
    {
        public ushort SlotIndex;
        public int Quantity;
        public Vector3 DropPosition;
    }

    public struct UseItemRequest : NetworkMessage
    {
        public ushort SlotIndex;
    }

    public struct ChatMessage : NetworkMessage
    {
        public ChatChannel Channel;
        public string Text;
        public string TargetName;
    }

    public enum ChatChannel
    {
        World, Party, Guild, Whisper, System, Trade, Shout
    }
}";
        }

        private string GetTOPNetworkManagerScript()
        {
            return @"using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TOP.Services;
using TOP.Data;
using TOP.Gameplay;
using TOP.Core;

namespace TOP.Network
{
    public class TOPNetworkManager : NetworkManager
    {
        public static TOPNetworkManager Instance { get; private set; }

        [Header(""Tales of Pirates - Config"")]
        [SerializeField] private float autoSaveInterval = 60f;
        [SerializeField] private string serverInstanceId = ""server_01"";

        [Header(""Scenes"")]
        [SerializeField] private string loginScene = ""LoginScene"";
        [SerializeField] private string characterSelectScene = ""CharacterSelectScene"";
        [SerializeField] private string gameScene = ""GameScene"";

        private readonly Dictionary<int, PlayerConnection> _connections = new Dictionary<int, PlayerConnection>();
        private readonly Dictionary<long, NetworkConnectionToClient> _accountConnections = new Dictionary<long, NetworkConnectionToClient>();
        private readonly Dictionary<int, RateLimiter> _rateLimiters = new Dictionary<int, RateLimiter>();

        public override void Awake()
        {
            base.Awake();
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            Debug.Log(""[TOPNetworkManager] Servidor iniciado"");

            NetworkServer.RegisterHandler<LoginRequest>(OnLoginRequest);
            NetworkServer.RegisterHandler<CharacterListRequest>(OnCharacterListRequest);
            NetworkServer.RegisterHandler<CreateCharacterRequest>(OnCreateCharacterRequest);
            NetworkServer.RegisterHandler<SelectCharacterRequest>(OnSelectCharacterRequest);
            NetworkServer.RegisterHandler<DeleteCharacterRequest>(OnDeleteCharacterRequest);
            NetworkServer.RegisterHandler<ClientPing>(OnClientPing);
            NetworkServer.RegisterHandler<MoveItemRequest>(OnMoveItemRequest);
            NetworkServer.RegisterHandler<EquipItemRequest>(OnEquipItemRequest);
            NetworkServer.RegisterHandler<DropItemRequest>(OnDropItemRequest);
            NetworkServer.RegisterHandler<UseItemRequest>(OnUseItemRequest);
            NetworkServer.RegisterHandler<ChatMessage>(OnChatMessage);

            InvokeRepeating(nameof(AutoSaveAll), autoSaveInterval, autoSaveInterval);
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            CancelInvoke();

            Debug.Log(""[TOPNetworkManager] Servidor fechando - salvando todos..."");
            foreach (var conn in _connections.Values)
            {
                if (conn.State == ConnectionState.InGame && conn.PlayerController != null)
                {
                    _ = SavePlayerAsync(conn);
                }
            }
        }

        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            base.OnServerConnect(conn);

            var playerConn = new PlayerConnection
            {
                ConnectionId = conn.connectionId,
                State = ConnectionState.Login,
                Connection = conn,
                RateLimiter = new RateLimiter(50, 1f),
                ConnectTime = Time.time
            };

            _connections[conn.connectionId] = playerConn;
            _rateLimiters[conn.connectionId] = playerConn.RateLimiter;

            Debug.Log($""[Server] Cliente conectado: {conn.connectionId}"");
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            if (_connections.TryGetValue(conn.connectionId, out var playerConn))
            {
                if (playerConn.State == ConnectionState.InGame && conn.identity != null)
                {
                    _ = SaveAndDisconnectAsync(playerConn);
                }
                else
                {
                    if (playerConn.AccountId > 0)
                    {
                        _accountConnections.Remove(playerConn.AccountId);
                    }
                }

                _connections.Remove(conn.connectionId);
                _rateLimiters.Remove(conn.connectionId);
            }

            base.OnServerDisconnect(conn);
            Debug.Log($""[Server] Cliente desconectado: {conn.connectionId}"");
        }

        bool CheckRateLimit(int connectionId)
        {
            if (_rateLimiters.TryGetValue(connectionId, out var limiter))
                return limiter.CanProcess();
            return true;
        }

        // ========== LOGIN ==========
        async void OnLoginRequest(NetworkConnectionToClient conn, LoginRequest msg)
        {
            if (!CheckRateLimit(conn.connectionId))
            {
                conn.Send(new LoginResponse { Success = false, ErrorCode = ""RATE_LIMITED"" });
                return;
            }

            if (!_connections.TryGetValue(conn.connectionId, out var playerConn))
                return;

            if (playerConn.State != ConnectionState.Login)
            {
                conn.Send(new LoginResponse { Success = false, ErrorCode = ""INVALID_STATE"" });
                return;
            }

            Debug.Log($""[Auth] Login: {msg.Username}"");

            var (success, accountId, error) = await DatabaseService.Instance.ValidateLoginAsync(
                msg.Username, msg.Password);

            if (!success)
            {
                conn.Send(new LoginResponse { Success = false, ErrorCode = error });
                return;
            }

            playerConn.State = ConnectionState.CharacterSelect;
            playerConn.AccountId = accountId;
            playerConn.Username = msg.Username;
            _accountConnections[accountId] = conn;

            string token = Guid.NewGuid().ToString(""N"");
            playerConn.SessionToken = token;

            conn.Send(new LoginResponse
            {
                Success = true,
                SessionToken = token,
                Message = ""Login bem-sucedido""
            });

            Debug.Log($""[Auth] OK: {msg.Username} (ID: {accountId})"");
        }

        // ========== CHARACTER LIST ==========
        async void OnCharacterListRequest(NetworkConnectionToClient conn, CharacterListRequest msg)
        {
            if (!CheckRateLimit(conn.connectionId)) return;

            if (!_connections.TryGetValue(conn.connectionId, out var playerConn))
                return;

            if (playerConn.State != ConnectionState.CharacterSelect)
            {
                conn.Send(new CharacterListResponse { Success = false, Error = ""INVALID_STATE"" });
                return;
            }

            var characters = await DatabaseService.Instance.GetCharacterListAsync(playerConn.AccountId);

            conn.Send(new CharacterListResponse
            {
                Success = true,
                Characters = characters.ToArray()
            });
        }

        // ========== CREATE CHARACTER ==========
        async void OnCreateCharacterRequest(NetworkConnectionToClient conn, CreateCharacterRequest msg)
        {
            if (!CheckRateLimit(conn.connectionId)) return;

            if (!_connections.TryGetValue(conn.connectionId, out var playerConn))
                return;

            if (playerConn.State != ConnectionState.CharacterSelect)
            {
                conn.Send(new CreateCharacterResponse { Success = false, Error = ""INVALID_STATE"" });
                return;
            }

            if (string.IsNullOrWhiteSpace(msg.Name) || msg.Name.Length < 3)
            {
                conn.Send(new CreateCharacterResponse { Success = false, Error = ""NAME_TOO_SHORT"" });
                return;
            }

            if (msg.SlotIndex > 2)
            {
                conn.Send(new CreateCharacterResponse { Success = false, Error = ""INVALID_SLOT"" });
                return;
            }

            var (success, charId, error) = await DatabaseService.Instance.CreateCharacterAsync(
                playerConn.AccountId, msg.SlotIndex, msg.Name, msg.Gender, msg.Job,
                msg.HairStyle, msg.HairColor);

            conn.Send(new CreateCharacterResponse
            {
                Success = success,
                CharacterId = charId,
                Error = success ? null : error
            });

            if (success)
                Debug.Log($""[CharCreate] {playerConn.Username} criou ""{msg.Name}"""");
        }

        // ========== DELETE CHARACTER ==========
        async void OnDeleteCharacterRequest(NetworkConnectionToClient conn, DeleteCharacterRequest msg)
        {
            if (!CheckRateLimit(conn.connectionId)) return;

            if (!_connections.TryGetValue(conn.connectionId, out var playerConn))
                return;

            var success = await DatabaseService.Instance.DeleteCharacterAsync(
                msg.CharacterId, playerConn.AccountId, msg.Password);

            conn.Send(new DeleteCharacterResponse
            {
                Success = success,
                Error = success ? null : ""DELETE_FAILED""
            });
        }

        // ========== SELECT CHARACTER (ENTER WORLD) ==========
        async void OnSelectCharacterRequest(NetworkConnectionToClient conn, SelectCharacterRequest msg)
        {
            if (!CheckRateLimit(conn.connectionId)) return;

            if (!_connections.TryGetValue(conn.connectionId, out var playerConn))
                return;

            if (playerConn.State != ConnectionState.CharacterSelect)
            {
                conn.Send(new SelectCharacterResponse { Success = false, Error = ""INVALID_STATE"" });
                return;
            }

            var charData = await DatabaseService.Instance.LoadCharacterAsync(
                msg.CharacterId, playerConn.AccountId);

            if (charData == null)
            {
                conn.Send(new SelectCharacterResponse { Success = false, Error = ""CHARACTER_NOT_FOUND"" });
                return;
            }

            playerConn.State = ConnectionState.InGame;
            playerConn.CharacterId = msg.CharacterId;

            GameObject playerObj = Instantiate(playerPrefab);

            var controller = playerObj.GetComponent<PlayerController>();
            if (controller == null)
            {
                Debug.LogError(""[TOPNetworkManager] PlayerPrefab sem PlayerController!"");
                Destroy(playerObj);
                conn.Send(new SelectCharacterResponse { Success = false, Error = ""SERVER_ERROR"" });
                return;
            }

            controller.InitializeFromDatabase(charData);

            playerObj.transform.position = new Vector3(charData.PosX, charData.PosY, charData.PosZ);
            playerObj.transform.rotation = Quaternion.Euler(0, charData.RotationY, 0);

            NetworkServer.AddPlayerForConnection(conn, playerObj);

            playerConn.PlayerController = controller;

            conn.Send(new SelectCharacterResponse
            {
                Success = true,
                CharacterId = msg.CharacterId,
                MapName = charData.MapName,
                Position = new Vector3(charData.PosX, charData.PosY, charData.PosZ),
                RotationY = charData.RotationY
            });

            await DatabaseService.Instance.LogAuditAsync(
                playerConn.AccountId, msg.CharacterId, ""ENTER_WORLD"",
                new { map = charData.MapName, pos = new Vector3(charData.PosX, charData.PosY, charData.PosZ) },
                conn.address);

            Debug.Log($""[EnterWorld] {charData.Name} entrou em {charData.MapName}"");
        }

        // ========== INVENTORY COMMANDS ==========
        void OnMoveItemRequest(NetworkConnectionToClient conn, MoveItemRequest msg)
        {
            if (!CheckRateLimit(conn.connectionId)) return;
            if (conn.identity == null) return;

            var inv = conn.identity.GetComponent<PlayerInventory>();
            if (inv != null)
            {
                inv.CmdMoveItem(msg.FromSlot, msg.ToSlot);
            }
        }

        void OnEquipItemRequest(NetworkConnectionToClient conn, EquipItemRequest msg)
        {
            if (!CheckRateLimit(conn.connectionId)) return;
            if (conn.identity == null) return;

            var equip = conn.identity.GetComponent<PlayerEquipment>();
            if (equip != null)
            {
                equip.CmdEquipItem(msg.InventorySlot, msg.TargetSlot);
            }
        }

        void OnDropItemRequest(NetworkConnectionToClient conn, DropItemRequest msg)
        {
            if (!CheckRateLimit(conn.connectionId)) return;
            if (conn.identity == null) return;

            var inv = conn.identity.GetComponent<PlayerInventory>();
            if (inv != null)
            {
                inv.CmdDropItem(msg.SlotIndex, msg.Quantity, msg.DropPosition);
            }
        }

        void OnUseItemRequest(NetworkConnectionToClient conn, UseItemRequest msg)
        {
            if (!CheckRateLimit(conn.connectionId)) return;
            if (conn.identity == null) return;

            var consumables = conn.identity.GetComponent<PlayerConsumables>();
            if (consumables != null)
            {
                consumables.CmdUseItem(msg.SlotIndex);
            }
        }

        // ========== CHAT ==========
        void OnChatMessage(NetworkConnectionToClient conn, ChatMessage msg)
        {
            if (!CheckRateLimit(conn.connectionId)) return;
            if (string.IsNullOrWhiteSpace(msg.Text) || msg.Text.Length > 200) return;

            // TODO: Implementar sistema de chat com canais
            NetworkServer.SendToAll(new ChatMessage
            {
                Channel = ChatChannel.World,
                Text = msg.Text
            });
        }

        // ========== AUTO SAVE ==========
        async void AutoSaveAll()
        {
            foreach (var conn in _connections.Values)
            {
                if (conn.State == ConnectionState.InGame && conn.PlayerController != null)
                {
                    try { await SavePlayerAsync(conn); }
                    catch (Exception ex) { Debug.LogError($""[AutoSave] Erro: {ex}""); }
                }
            }
        }

        async System.Threading.Tasks.Task SavePlayerAsync(PlayerConnection conn)
        {
            if (conn?.PlayerController == null) return;

            var data = new CharacterData
            {
                Id = conn.PlayerController.CharacterId,
                AccountId = conn.PlayerController.AccountId,
                Name = conn.PlayerController.CharacterName,
                Job = conn.PlayerController.Job,
                Level = conn.PlayerController.Level,
                CurrentHp = conn.PlayerController.CurrentHp,
                CurrentMp = conn.PlayerController.CurrentMp,
                CurrentSp = conn.PlayerController.CurrentSp,
                PosX = conn.PlayerController.transform.position.x,
                PosY = conn.PlayerController.transform.position.y,
                PosZ = conn.PlayerController.transform.position.z,
                RotationY = conn.PlayerController.transform.rotation.eulerAngles.y
            };

            await DatabaseService.Instance.SaveCharacterAsync(data);
        }

        async System.Threading.Tasks.Task SaveAndDisconnectAsync(PlayerConnection conn)
        {
            if (conn?.PlayerController != null)
            {
                var data = new CharacterData
                {
                    Id = conn.PlayerController.CharacterId,
                    AccountId = conn.PlayerController.AccountId,
                    Name = conn.PlayerController.CharacterName,
                    PosX = conn.PlayerController.transform.position.x,
                    PosY = conn.PlayerController.transform.position.y,
                    PosZ = conn.PlayerController.transform.position.z,
                    RotationY = conn.PlayerController.transform.rotation.eulerAngles.y
                };
                await DatabaseService.Instance.SaveCharacterAsync(data);
            }

            _accountConnections.Remove(conn.AccountId);
        }

        void OnClientPing(NetworkConnectionToClient conn, ClientPing msg)
        {
            if (_connections.TryGetValue(conn.connectionId, out var playerConn))
                playerConn.LastPingTime = Time.time;

            conn.Send(new ServerPong
            {
                ClientTime = msg.ClientTime,
                ServerTime = Time.time
            });
        }

        public bool IsAccountOnline(long accountId) => _accountConnections.ContainsKey(accountId);
    }

    public enum ConnectionState
    {
        Login,
        CharacterSelect,
        InGame
    }

    public class PlayerConnection
    {
        public int ConnectionId;
        public ConnectionState State;
        public long AccountId;
        public long CharacterId;
        public string Username;
        public string SessionToken;
        public NetworkConnectionToClient Connection;
        public PlayerController PlayerController;
        public RateLimiter RateLimiter;
        public float LastPingTime;
        public float ConnectTime;
    }

    public class RateLimiter
    {
        private readonly Queue<float> _timestamps = new Queue<float>();
        private readonly int _maxRequests;
        private readonly float _timeWindow;

        public RateLimiter(int maxRequests, float timeWindow)
        {
            _maxRequests = maxRequests;
            _timeWindow = timeWindow;
        }

        public bool CanProcess()
        {
            float now = Time.time;
            while (_timestamps.Count > 0 && _timestamps.Peek() < now - _timeWindow)
                _timestamps.Dequeue();

            if (_timestamps.Count >= _maxRequests)
                return false;

            _timestamps.Enqueue(now);
            return true;
        }
    }
}";
        }

        // =================================================================================
        // SCRIPTS - SERVICES
        // =================================================================================
        private string GetDatabaseServiceScript()
        {
            return @"using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TOP.Data;

namespace TOP.Services
{
    public class DatabaseService : MonoBehaviour
    {
        public static DatabaseService Instance;

        [Header(""Configuracao"")]
        [SerializeField] private string server = ""localhost"";
        [SerializeField] private string database = ""top_unity"";
        [SerializeField] private string uid = ""root"";
        [SerializeField] private string password = ""sua_senha"";
        [SerializeField] private int maxPoolSize = 100;
        [SerializeField] private int connectionTimeout = 30;

        private string _connectionString;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _connectionString = $""Server={server};Database={database};"" +
                $""User ID={uid};Password={password};"" +
                $""Max Pool Size={maxPoolSize};"" +
                $""Connection Timeout={connectionTimeout};"";
        }

        // ========== AUTH ==========
        public async Task<(bool success, long accountId, string error)> ValidateLoginAsync(
            string username, string password)
        {
            // TODO: Implementar conexao real com MySQL/SQLite
            // Por enquanto retorna mock para testes
            await Task.Yield();
            return (true, 1, null);
        }

        public async Task<bool> CreateAccountAsync(string username, string password, string email)
        {
            await Task.Yield();
            return true;
        }

        // ========== CHARACTERS ==========
        public async Task<List<CharacterPreviewData>> GetCharacterListAsync(long accountId)
        {
            await Task.Yield();
            // Mock data para testes
            return new List<CharacterPreviewData>
            {
                new CharacterPreviewData
                {
                    Id = 1,
                    SlotIndex = 0,
                    Name = ""Hero"",
                    Gender = 0,
                    Job = 0,
                    Level = 1,
                    MapName = ""Shaitan"",
                    Position = Vector3.zero,
                    RotationY = 0,
                    HairStyle = 0,
                    HairColor = 0,
                    LastOnline = DateTime.Now
                }
            };
        }

        public async Task<(bool success, long charId, string error)> CreateCharacterAsync(
            long accountId, byte slot, string name, byte gender, byte job,
            byte hairStyle, byte hairColor)
        {
            await Task.Yield();
            return (true, DateTime.Now.Ticks, null);
        }

        public async Task<CharacterData> LoadCharacterAsync(long charId, long accountId)
        {
            await Task.Yield();
            // Mock data
            return new CharacterData
            {
                Id = charId,
                AccountId = accountId,
                Name = ""Hero"",
                Job = 0,
                Gender = 0,
                Level = 1,
                CurrentHp = 100,
                CurrentMp = 50,
                CurrentSp = 30,
                PosX = 0,
                PosY = 1,
                PosZ = 0,
                RotationY = 0,
                MapName = ""Shaitan"",
                BaseStr = 10,
                BaseAgi = 10,
                BaseCon = 10,
                BaseSpr = 10,
                MaxHp = 100,
                MaxMp = 50,
                MaxSp = 30,
                Gold = 0,
                Inventory = new List<DbInventoryItem>(),
                Skills = new List<CharacterSkillData>()
            };
        }

        public async Task SaveCharacterAsync(CharacterData data)
        {
            await Task.Yield();
            Debug.Log($""[DB] Saved character {data.Name}"");
        }

        public async Task<bool> DeleteCharacterAsync(long charId, long accountId, string password)
        {
            await Task.Yield();
            return true;
        }

        // ========== INVENTORY ==========
        public long CreateItem(long characterId, int itemId, int quantity, ushort slotIndex)
        {
            return DateTime.Now.Ticks;
        }

        public async Task SaveInventoryAsync(long characterId, List<DbInventoryItem> items)
        {
            await Task.Yield();
        }

        // ========== AUDIT ==========
        public async Task LogAuditAsync(long? accountId, long? charId, string action,
            object detail, string ip)
        {
            await Task.Yield();
        }
    }

    [Serializable]
    public class CharacterPreviewData
    {
        public long Id;
        public byte SlotIndex;
        public string Name;
        public byte Gender;
        public byte Job;
        public int Level;
        public string MapName;
        public Vector3 Position;
        public float RotationY;
        public byte HairStyle;
        public byte HairColor;
        public DateTime? LastOnline;
    }
}";
        }

        private string GetSecurityLogServiceScript()
        {
            return @"using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TOP.Services
{
    public class SecurityLogService : MonoBehaviour
    {
        public static SecurityLogService Instance { get; private set; }

        [Header(""Config"")]
        [SerializeField] private bool logToConsole = true;
        [SerializeField] private bool logToDatabase = true;
        [SerializeField] private int bufferSize = 100;
        [SerializeField] private float flushInterval = 30f;

        private readonly Queue<SecurityLogEntry> _buffer = new Queue<SecurityLogEntry>();
        private float _nextFlush;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            if (Time.time >= _nextFlush && _buffer.Count > 0)
            {
                FlushBuffer();
                _nextFlush = Time.time + flushInterval;
            }
        }

        public void Log(long? characterId, long? accountId, string message,
            LogSeverity severity = LogSeverity.Warning, string ipAddress = null)
        {
            var entry = new SecurityLogEntry
            {
                Timestamp = DateTime.Now,
                CharacterId = characterId,
                AccountId = accountId,
                Message = message,
                Severity = severity,
                IpAddress = ipAddress
            };

            _buffer.Enqueue(entry);

            if (logToConsole)
            {
                string log = $""[SECURITY] [{severity}] {message} (Char:{characterId}, Acc:{accountId})"";
                switch (severity)
                {
                    case LogSeverity.Info: Debug.Log(log); break;
                    case LogSeverity.Warning: Debug.LogWarning(log); break;
                    case LogSeverity.Critical: Debug.LogError(log); break;
                }
            }

            if (severity == LogSeverity.Critical)
                FlushBuffer();

            if (_buffer.Count >= bufferSize)
                FlushBuffer();
        }

        void FlushBuffer()
        {
            if (!logToDatabase || _buffer.Count == 0) return;

            var entries = new List<SecurityLogEntry>();
            while (_buffer.Count > 0 && entries.Count < bufferSize)
                entries.Add(_buffer.Dequeue());

            _ = FlushToDatabaseAsync(entries);
        }

        async Task FlushToDatabaseAsync(List<SecurityLogEntry> entries)
        {
            try
            {
                foreach (var entry in entries)
                {
                    await DatabaseService.Instance.LogAuditAsync(
                        entry.AccountId,
                        entry.CharacterId,
                        ""SECURITY"",
                        new
                        {
                            severity = entry.Severity.ToString(),
                            message = entry.Message,
                            ip = entry.IpAddress
                        },
                        entry.IpAddress
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($""[SecurityLog] Falha ao flush: {ex.Message}"");
            }
        }
    }

    public enum LogSeverity
    {
        Info,
        Warning,
        Critical
    }

    public struct SecurityLogEntry
    {
        public DateTime Timestamp;
        public long? CharacterId;
        public long? AccountId;
        public string Message;
        public LogSeverity Severity;
        public string IpAddress;
    }
}";
        }

        // =================================================================================
        // SCRIPTS - UI MANAGERS
        // =================================================================================
        private string GetLoginUIManagerScript()
        {
            return @"using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using TOP.Network;
using TOP.Services;

namespace TOP.UI
{
    public class LoginUIManager : MonoBehaviour
    {
        [Header(""Panels"")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject registerPanel;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private GameObject errorPanel;

        [Header(""Login Inputs"")]
        [SerializeField] private TMP_InputField loginUsername;
        [SerializeField] private TMP_InputField loginPassword;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button gotoRegisterButton;

        [Header(""Register Inputs"")]
        [SerializeField] private TMP_InputField registerUsername;
        [SerializeField] private TMP_InputField registerPassword;
        [SerializeField] private TMP_InputField registerConfirmPassword;
        [SerializeField] private TMP_InputField registerEmail;
        [SerializeField] private Button registerButton;
        [SerializeField] private Button backToLoginButton;

        [Header(""Error"")]
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private Button errorOkButton;

        [Header(""Server"")]
        [SerializeField] private TMP_InputField serverAddress;
        [SerializeField] private TMP_InputField serverPort;

        private NetworkManager networkManager;

        void Start()
        {
            networkManager = NetworkManager.singleton;

            loginButton.onClick.AddListener(OnLoginClick);
            gotoRegisterButton.onClick.AddListener(() => ShowPanel(registerPanel));
            registerButton.onClick.AddListener(OnRegisterClick);
            backToLoginButton.onClick.AddListener(() => ShowPanel(loginPanel));
            errorOkButton.onClick.AddListener(() => errorPanel.SetActive(false));

            NetworkClient.RegisterHandler<LoginResponse>(OnLoginResponse);
            NetworkClient.RegisterHandler<ServerMessage>(OnServerMessage);

            ShowPanel(loginPanel);

            // Carregar dados salvos
            if (PlayerPrefs.HasKey(""TOP_LastUser""))
                loginUsername.text = PlayerPrefs.GetString(""TOP_LastUser"");

            if (PlayerPrefs.HasKey(""TOP_Server""))
                serverAddress.text = PlayerPrefs.GetString(""TOP_Server"");
            else
                serverAddress.text = ""localhost"";

            if (PlayerPrefs.HasKey(""TOP_Port""))
                serverPort.text = PlayerPrefs.GetString(""TOP_Port"");
            else
                serverPort.text = ""7777"";
        }

        void OnDestroy()
        {
            loginButton.onClick.RemoveAllListeners();
            gotoRegisterButton.onClick.RemoveAllListeners();
            registerButton.onClick.RemoveAllListeners();
            backToLoginButton.onClick.RemoveAllListeners();
            errorOkButton.onClick.RemoveAllListeners();
        }

        void ShowPanel(GameObject panel)
        {
            loginPanel.SetActive(panel == loginPanel);
            registerPanel.SetActive(panel == registerPanel);
            loadingPanel.SetActive(panel == loadingPanel);
        }

        void ShowError(string message)
        {
            errorText.text = message;
            errorPanel.SetActive(true);
            loadingPanel.SetActive(false);
        }

        void OnLoginClick()
        {
            if (string.IsNullOrWhiteSpace(loginUsername.text))
            {
                ShowError(""Digite um nome de usuario!"");
                return;
            }

            if (string.IsNullOrWhiteSpace(loginPassword.text))
            {
                ShowError(""Digite uma senha!"");
                return;
            }

            ShowPanel(loadingPanel);

            // Conectar ao servidor
            networkManager.networkAddress = serverAddress.text;
            if (ushort.TryParse(serverPort.text, out ushort port))
            {
                // Mirror usa transport para porta
            }

            PlayerPrefs.SetString(""TOP_LastUser"", loginUsername.text);
            PlayerPrefs.SetString(""TOP_Server"", serverAddress.text);
            PlayerPrefs.SetString(""TOP_Port"", serverPort.text);
            PlayerPrefs.Save();

            networkManager.StartClient();

            // Aguardar conexao e enviar login
            StartCoroutine(SendLoginAfterConnect());
        }

        System.Collections.IEnumerator SendLoginAfterConnect()
        {
            float timeout = 10f;
            float elapsed = 0f;

            while (!NetworkClient.isConnected && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!NetworkClient.isConnected)
            {
                ShowError(""Nao foi possivel conectar ao servidor!"");
                yield break;
            }

            NetworkClient.Send(new LoginRequest
            {
                Username = loginUsername.text,
                Password = loginPassword.text
            });
        }

        void OnLoginResponse(LoginResponse msg)
        {
            if (!msg.Success)
            {
                ShowError($""Login falhou: {msg.ErrorCode}"");
                NetworkClient.Disconnect();
                return;
            }

            Debug.Log($""Login OK! Token: {msg.SessionToken}"");

            // Ir para Character Select
            UnityEngine.SceneManagement.SceneManager.LoadScene(""CharacterSelectScene"");
        }

        void OnRegisterClick()
        {
            if (string.IsNullOrWhiteSpace(registerUsername.text) || registerUsername.text.Length < 3)
            {
                ShowError(""Nome de usuario muito curto (min 3 chars)"");
                return;
            }

            if (registerPassword.text != registerConfirmPassword.text)
            {
                ShowError(""As senhas nao coincidem!"");
                return;
            }

            if (string.IsNullOrWhiteSpace(registerEmail.text))
            {
                ShowError(""Digite um email!"");
                return;
            }

            ShowPanel(loadingPanel);

            // TODO: Enviar request de registro
            _ = RegisterAsync(registerUsername.text, registerPassword.text, registerEmail.text);
        }

        async System.Threading.Tasks.Task RegisterAsync(string user, string pass, string email)
        {
            var success = await DatabaseService.Instance.CreateAccountAsync(user, pass, email);

            if (success)
            {
                ShowError(""Conta criada com sucesso! Faca login."");
                ShowPanel(loginPanel);
                loginUsername.text = user;
            }
            else
            {
                ShowError(""Falha ao criar conta. Tente outro nome."");
                ShowPanel(registerPanel);
            }
        }

        void OnServerMessage(ServerMessage msg)
        {
            ShowError(msg.Text);
        }
    }
}";
        }

        private string GetCharacterSelectUIManagerScript()
        {
            return @"using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using TOP.Network;
using TOP.Services;
using System.Collections.Generic;

namespace TOP.UI
{
    public class CharacterSelectUIManager : MonoBehaviour
    {
        [Header(""Panels"")]
        [SerializeField] private GameObject characterListPanel;
        [SerializeField] private GameObject createCharacterPanel;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private GameObject errorPanel;

        [Header(""Character Slots"")]
        [SerializeField] private Transform characterSlotsParent;
        [SerializeField] private GameObject characterCardPrefab;

        [Header(""Create Character"")]
        [SerializeField] private TMP_InputField charNameInput;
        [SerializeField] private TMP_Dropdown jobDropdown;
        [SerializeField] private TMP_Dropdown genderDropdown;
        [SerializeField] private Button createCharButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button createNewButton;

        [Header(""Error"")]
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private Button errorOkButton;

        [Header(""Character Preview"")]
        [SerializeField] private Transform previewSpawnPoint;
        [SerializeField] private GameObject[] classPreviewPrefabs;

        private List<CharacterPreviewData> currentCharacters = new List<CharacterPreviewData>();
        private GameObject currentPreview;
        private int selectedSlot = -1;

        void Start()
        {
            createCharButton.onClick.AddListener(OnCreateCharacterClick);
            backButton.onClick.AddListener(() => ShowPanel(characterListPanel));
            createNewButton.onClick.AddListener(() => ShowPanel(createCharacterPanel));
            errorOkButton.onClick.AddListener(() => errorPanel.SetActive(false));

            NetworkClient.RegisterHandler<CharacterListResponse>(OnCharacterListResponse);
            NetworkClient.RegisterHandler<CreateCharacterResponse>(OnCreateCharacterResponse);
            NetworkClient.RegisterHandler<SelectCharacterResponse>(OnSelectCharacterResponse);
            NetworkClient.RegisterHandler<DeleteCharacterResponse>(OnDeleteCharacterResponse);

            ShowPanel(loadingPanel);

            // Solicitar lista de personagens
            NetworkClient.Send(new CharacterListRequest());

            // Setup dropdowns
            jobDropdown.ClearOptions();
            jobDropdown.AddOptions(new List<string> { ""Swordsman"", ""Hunter"", ""Herbalist"", ""Explorer"" });

            genderDropdown.ClearOptions();
            genderDropdown.AddOptions(new List<string> { ""Male"", ""Female"" });

            jobDropdown.onValueChanged.AddListener(OnJobChanged);
        }

        void OnDestroy()
        {
            createCharButton.onClick.RemoveAllListeners();
            backButton.onClick.RemoveAllListeners();
            createNewButton.onClick.RemoveAllListeners();
            errorOkButton.onClick.RemoveAllListeners();
            jobDropdown.onValueChanged.RemoveAllListeners();
        }

        void ShowPanel(GameObject panel)
        {
            characterListPanel.SetActive(panel == characterListPanel);
            createCharacterPanel.SetActive(panel == createCharacterPanel);
            loadingPanel.SetActive(panel == loadingPanel);
        }

        void ShowError(string message)
        {
            errorText.text = message;
            errorPanel.SetActive(true);
            loadingPanel.SetActive(false);
        }

        void OnCharacterListResponse(CharacterListResponse msg)
        {
            if (!msg.Success)
            {
                ShowError($""Erro: {msg.Error}"");
                return;
            }

            currentCharacters = new List<CharacterPreviewData>(msg.Characters);
            RefreshCharacterList();
            ShowPanel(characterListPanel);
        }

        void RefreshCharacterList()
        {
            // Limpar slots existentes
            foreach (Transform child in characterSlotsParent)
                Destroy(child.gameObject);

            for (int i = 0; i < 3; i++)
            {
                var card = Instantiate(characterCardPrefab, characterSlotsParent);
                var cardUI = card.GetComponent<CharacterCardUI>();

                var character = currentCharacters.Find(c => c.SlotIndex == i);
                bool hasChar = character != null;

                cardUI.Setup(i, hasChar, character,
                    onSelect: () => OnSelectCharacter(i, character?.Id ?? 0),
                    onDelete: () => OnDeleteCharacter(character.Id),
                    onCreate: () => { selectedSlot = i; ShowPanel(createCharacterPanel); });
            }
        }

        void OnSelectCharacter(int slot, long charId)
        {
            if (charId == 0) return;

            ShowPanel(loadingPanel);
            NetworkClient.Send(new SelectCharacterRequest { CharacterId = charId });
        }

        void OnSelectCharacterResponse(SelectCharacterResponse msg)
        {
            if (!msg.Success)
            {
                ShowError($""Erro ao entrar: {msg.Error}"");
                return;
            }

            Debug.Log($""Entrando no mundo: {msg.MapName}"");
            UnityEngine.SceneManagement.SceneManager.LoadScene(""GameScene"");
        }

        void OnCreateCharacterClick()
        {
            if (string.IsNullOrWhiteSpace(charNameInput.text) || charNameInput.text.Length < 3)
            {
                ShowError(""Nome muito curto (min 3 caracteres)"");
                return;
            }

            ShowPanel(loadingPanel);

            NetworkClient.Send(new CreateCharacterRequest
            {
                SlotIndex = (byte)selectedSlot,
                Name = charNameInput.text,
                Gender = (byte)genderDropdown.value,
                Job = (byte)jobDropdown.value,
                HairStyle = 0,
                HairColor = 0
            });
        }

        void OnCreateCharacterResponse(CreateCharacterResponse msg)
        {
            if (!msg.Success)
            {
                ShowError($""Falha: {msg.Error}"");
                return;
            }

            // Atualizar lista
            NetworkClient.Send(new CharacterListRequest());
            ShowPanel(loadingPanel);
        }

        void OnDeleteCharacter(long charId)
        {
            // TODO: Mostrar confirmacao com senha
            NetworkClient.Send(new DeleteCharacterRequest
            {
                CharacterId = charId,
                Password = """" // TODO: Pedir senha
            });
        }

        void OnDeleteCharacterResponse(DeleteCharacterResponse msg)
        {
            if (msg.Success)
            {
                NetworkClient.Send(new CharacterListRequest());
                ShowPanel(loadingPanel);
            }
            else
            {
                ShowError(""Falha ao deletar personagem"");
            }
        }

        void OnJobChanged(int jobIndex)
        {
            if (currentPreview != null)
                Destroy(currentPreview);

            if (classPreviewPrefabs != null && jobIndex < classPreviewPrefabs.Length)
            {
                currentPreview = Instantiate(classPreviewPrefabs[jobIndex], previewSpawnPoint);
            }
        }
    }

    // Helper class para cada card de personagem
    public class CharacterCardUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI jobText;
        [SerializeField] private Image charImage;
        [SerializeField] private Button selectButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button createButton;
        [SerializeField] private GameObject emptyState;
        [SerializeField] private GameObject filledState;

        public void Setup(int slot, bool hasCharacter, CharacterPreviewData data,
            System.Action onSelect, System.Action onDelete, System.Action onCreate)
        {
            if (hasCharacter && data != null)
            {
                emptyState.SetActive(false);
                filledState.SetActive(true);

                nameText.text = data.Name;
                levelText.text = $""Lv. {data.Level}"";
                jobText.text = GetJobName(data.Job);

                selectButton.onClick.AddListener(() => onSelect?.Invoke());
                deleteButton.onClick.AddListener(() => onDelete?.Invoke());
            }
            else
            {
                emptyState.SetActive(true);
                filledState.SetActive(false);
                createButton.onClick.AddListener(() => onCreate?.Invoke());
            }
        }

        string GetJobName(byte job)
        {
            switch (job)
            {
                case 0: return ""Swordsman"";
                case 1: return ""Hunter"";
                case 2: return ""Herbalist"";
                case 3: return ""Explorer"";
                default: return ""Unknown"";
            }
        }
    }
}";
        }

        // =================================================================================
        // SCRIPTS - GAMEPLAY (PlayerController)
        // =================================================================================
        private string GetPlayerControllerScript()
        {
            return @"using UnityEngine;
using Mirror;
using TOP.Data;
using TOP.Player;
using TOP.Core;

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

        [Header(""Components"")]
        public PlayerMovement Movement;
        public PlayerStats Stats;
        public PlayerInventory Inventory;
        public PlayerEquipment Equipment;
        public PlayerCombat Combat;
        public PlayerSkills Skills;
        public PlayerAnimation Animation;
        public PlayerConsumables Consumables;

        [Header(""Settings"")]
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
            Consumables = GetComponent<PlayerConsumables>();
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            // Configurar camera
            var camFollow = Camera.main?.GetComponent<CameraFollow>();
            if (camFollow != null)
                camFollow.SetTarget(transform);

            // Configurar UI
            var uiManager = Object.FindFirstObjectByType<UIManager>();
            if (uiManager != null)
                uiManager.SetupLocalPlayer(this);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            // Auto-save final
            SaveToDatabase();
        }

        void Update()
        {
            if (isServer && Time.time > _lastSaveTime + autoSaveInterval)
            {
                _lastSaveTime = Time.time;
                SaveToDatabase();
            }
        }

        // ========== INITIALIZATION ==========
        public void InitializeFromDatabase(CharacterData data)
        {
            CharacterId = data.Id;
            AccountId = data.AccountId;
            CharacterName = data.Name;
            Job = data.Job;
            Level = data.Level;
            CurrentHp = data.CurrentHp;
            CurrentMp = data.CurrentMp;
            CurrentSp = data.CurrentSp;

            if (Stats != null)
            {
                Stats.Initialize(data.BaseStr, data.BaseAgi, data.BaseCon, data.BaseSpr,
                    data.MaxHp, data.MaxMp, data.MaxSp);
                Stats.SetCurrentHpMpSp(data.CurrentHp, data.CurrentMp, data.CurrentSp);
            }

            if (Inventory != null && data.Inventory != null)
            {
                foreach (var item in data.Inventory)
                {
                    Inventory.AddItem(item.ItemId, item.Quantity, item.SlotIndex);
                }
            }

            if (Skills != null && data.Skills != null)
            {
                foreach (var skill in data.Skills)
                {
                    Skills.LearnSkill(skill.SkillId, skill.Level);
                }
            }
        }

        // ========== DATABASE SAVE ==========
        [Server]
        public void SaveToDatabase()
        {
            if (Stats == null) return;

            var data = new CharacterData
            {
                Id = CharacterId,
                AccountId = AccountId,
                Name = CharacterName,
                Job = Job,
                Level = Level,
                CurrentHp = Stats.CurrentHp,
                CurrentMp = Stats.CurrentMp,
                CurrentSp = Stats.CurrentSp,
                PosX = transform.position.x,
                PosY = transform.position.y,
                PosZ = transform.position.z,
                RotationY = transform.rotation.eulerAngles.y,
                BaseStr = Stats.BaseStrength,
                BaseAgi = Stats.BaseAgility,
                BaseCon = Stats.BaseConstitution,
                BaseSpr = Stats.BaseSpirit,
                MaxHp = Stats.MaxHp,
                MaxMp = Stats.MaxMp,
                MaxSp = Stats.MaxSp
            };

            _ = TOP.Services.DatabaseService.Instance.SaveCharacterAsync(data);
        }

        // ========== COMMANDS (Server Authoritative) ==========
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
        public void CmdUseSkill(int skillId, Vector3 targetPosition, NetworkIdentity target)
        {
            if (Skills != null)
                Skills.UseSkill(skillId, targetPosition, target);
        }

        [Command]
        public void CmdSetTarget(NetworkIdentity target)
        {
            if (Combat != null)
                Combat.SetTarget(target);
        }

        // ========== RPCs ==========
        [ClientRpc]
        public void RpcTakeDamage(int damage, Vector3 hitPosition)
        {
            // Mostrar popup de dano
            var popupManager = Object.FindFirstObjectByType<TOP.Systems.DamagePopupManager>();
            if (popupManager != null)
                popupManager.ShowDamage(damage, hitPosition, false);

            // Flash effect
            var hitFlash = GetComponentInChildren<TOP.UI.HitFlashEffect>();
            if (hitFlash != null)
                hitFlash.Flash();
        }

        [ClientRpc]
        public void RpcHeal(int amount)
        {
            var popupManager = Object.FindFirstObjectByType<TOP.Systems.DamagePopupManager>();
            if (popupManager != null)
                popupManager.ShowHeal(amount, transform.position + Vector3.up * 2f);
        }

        [ClientRpc]
        public void RpcLevelUp()
        {
            Level++;
            var effectManager = Object.FindFirstObjectByType<TOP.Systems.LevelUpEffectManager>();
            if (effectManager != null)
                effectManager.PlayLevelUpEffect(transform.position);
        }

        [ClientRpc]
        public void RpcShowMessage(string message, MessageType type)
        {
            var uiManager = Object.FindFirstObjectByType<UIManager>();
            if (uiManager != null)
                uiManager.ShowMessage(message, type);
        }

        // ========== DEATH / RESPAWN ==========
        [Server]
        public void Die()
        {
            RpcDie();
            // TODO: Respawn timer
            Invoke(nameof(Respawn), 5f);
        }

        [ClientRpc]
        void RpcDie()
        {
            Animation.PlayDeath();
            Movement.enabled = false;
            Combat.enabled = false;
        }

        [Server]
        void Respawn()
        {
            CurrentHp = Stats.MaxHp;
            CurrentMp = Stats.MaxMp;
            CurrentSp = Stats.MaxSp;

            // TODO: Posicao de respawn baseada no mapa
            transform.position = Vector3.zero + Vector3.up;

            RpcRespawn();
        }

        [ClientRpc]
        void RpcRespawn()
        {
            Animation.PlayRespawn();
            Movement.enabled = true;
            Combat.enabled = true;
        }

        void OnDestroy()
        {
            if (isServer)
            {
                SaveToDatabase();
            }
        }
    }

    public enum MessageType
    {
        Info, Warning, Error, Success
    }
}";
        }

        // =================================================================================
        // SCRIPTS - UI HELPERS
        // =================================================================================
        private string GetUIManagerScript()
        {
            return @"using UnityEngine;
using TMPro;
using TOP.Gameplay;

namespace TOP.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header(""Player HUD"")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private UnityEngine.UI.Slider hpBar;
        [SerializeField] private UnityEngine.UI.Slider mpBar;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI mpText;

        [Header(""Messages"")]
        [SerializeField] private GameObject messagePanel;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private float messageDuration = 3f;

        private PlayerController localPlayer;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void SetupLocalPlayer(PlayerController player)
        {
            localPlayer = player;
            playerNameText.text = player.CharacterName;
            UpdateStats();
        }

        void Update()
        {
            if (localPlayer != null)
            {
                UpdateStats();
            }
        }

        void UpdateStats()
        {
            if (localPlayer.Stats == null) return;

            levelText.text = $""Lv. {localPlayer.Level}"";

            hpBar.maxValue = localPlayer.Stats.MaxHp;
            hpBar.value = localPlayer.Stats.CurrentHp;
            hpText.text = $""{localPlayer.Stats.CurrentHp} / {localPlayer.Stats.MaxHp}"";

            mpBar.maxValue = localPlayer.Stats.MaxMp;
            mpBar.value = localPlayer.Stats.CurrentMp;
            mpText.text = $""{localPlayer.Stats.CurrentMp} / {localPlayer.Stats.MaxMp}"";
        }

        public void ShowMessage(string message, MessageType type)
        {
            messageText.text = message;
            messagePanel.SetActive(true);
            CancelInvoke(nameof(HideMessage));
            Invoke(nameof(HideMessage), messageDuration);
        }

        void HideMessage()
        {
            messagePanel.SetActive(false);
        }
    }
}";
        }

        private string GetDamagePopupScript()
        {
            return @"using UnityEngine;
using TMPro;
using System.Collections;

namespace TOP.UI
{
    public class DamagePopup : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float fadeDuration = 1f;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color criticalColor = Color.red;
        [SerializeField] private Color healColor = Color.green;

        public void Setup(int value, bool isCritical, bool isHeal)
        {
            text.text = value.ToString();
            text.color = isHeal ? healColor : (isCritical ? criticalColor : normalColor);
            if (isCritical) text.fontSize *= 1.5f;

            StartCoroutine(Animate());
        }

        IEnumerator Animate()
        {
            float elapsed = 0f;
            Vector3 startPos = transform.position;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                transform.position = startPos + Vector3.up * moveSpeed * elapsed;
                text.alpha = 1f - (elapsed / fadeDuration);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}";
        }

        private string GetCombatFeedbackTypeScript()
        {
            return @"namespace TOP.UI
{
    public enum CombatFeedbackType
    {
        Normal, Critical, Block, Dodge, Miss, Heal, Buff, Debuff
    }
}";
        }

        private string GetHitFlashEffectScript()
        {
            return @"using UnityEngine;
using System.Collections;

namespace TOP.UI
{
    public class HitFlashEffect : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer[] meshRenderers;
        [SerializeField] private Material flashMaterial;
        [SerializeField] private float flashDuration = 0.1f;

        private Material[] originalMaterials;

        void Start()
        {
            if (meshRenderers == null || meshRenderers.Length == 0)
                meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        }

        public void Flash()
        {
            StartCoroutine(FlashCoroutine());
        }

        IEnumerator FlashCoroutine()
        {
            // Store originals
            if (originalMaterials == null)
            {
                originalMaterials = new Material[meshRenderers.Length];
                for (int i = 0; i < meshRenderers.Length; i++)
                    originalMaterials[i] = meshRenderers[i].material;
            }

            // Apply flash
            foreach (var renderer in meshRenderers)
                renderer.material = flashMaterial;

            yield return new WaitForSeconds(flashDuration);

            // Restore
            for (int i = 0; i < meshRenderers.Length; i++)
                meshRenderers[i].material = originalMaterials[i];
        }
    }
}";
        }

        private string GetScreenHitFlashScript()
        {
            return @"using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace TOP.UI
{
    public class ScreenHitFlash : MonoBehaviour
    {
        [SerializeField] private Image flashImage;
        [SerializeField] private float flashDuration = 0.3f;
        [SerializeField] private Color flashColor = new Color(1, 0, 0, 0.3f);

        public void Flash()
        {
            StartCoroutine(FlashCoroutine());
        }

        IEnumerator FlashCoroutine()
        {
            flashImage.color = flashColor;
            float elapsed = 0f;

            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                var c = flashColor;
                c.a = flashColor.a * (1f - elapsed / flashDuration);
                flashImage.color = c;
                yield return null;
            }

            flashImage.color = new Color(0, 0, 0, 0);
        }
    }
}";
        }

        private string GetOneShotAutoDestroyScript()
        {
            return @"using UnityEngine;

namespace TOP.UI
{
    public class OneShotAutoDestroy : MonoBehaviour
    {
        [SerializeField] private float destroyDelay = 2f;
        [SerializeField] private ParticleSystem particles;

        void Start()
        {
            if (particles != null)
                destroyDelay = particles.main.duration + particles.main.startLifetime.constantMax;

            Destroy(gameObject, destroyDelay);
        }
    }
}";
        }

        // =================================================================================
        // SCRIPTS - SIMPLE HELPERS
        // =================================================================================
        private string GetAutoStartScript()
        {
            return @"using UnityEngine;
using Mirror;

public class AutoStart : MonoBehaviour
{
    [SerializeField] private bool startAsServer = false;

    void Start()
    {
        if (startAsServer)
        {
            NetworkManager.singleton.StartServer();
        }
    }
}";
        }

        private string GetDontDestroyOnLoadScript()
        {
            return @"using UnityEngine;

public class DontDestroyOnLoad : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}";
        }

        private string GetDebugGiveItemScript()
        {
            return @"using UnityEngine;
using TOP.Player;

public class DebugGiveItem : MonoBehaviour
{
    [SerializeField] private int itemId = 1001;
    [SerializeField] private int quantity = 1;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            var inv = GetComponent<PlayerInventory>();
            if (inv != null)
                inv.CmdAddItem(itemId, quantity);
        }
    }
}";
        }

        // =================================================================================
        // SCRIPTS - DATABASE
        // =================================================================================
        private string GetQuestDatabaseScript()
        {
            return @"using UnityEngine;
using System.Collections.Generic;
using System.Linq; // CORRIGIDO: Adicionado
using TOP.Core;

namespace TOP.Database
{
    [CreateAssetMenu(fileName = ""QuestDatabase"", menuName = ""TOP/Quest Database"")]
    public class QuestDatabase : ScriptableObject
    {
        public List<QuestData> quests = new List<QuestData>();

        public QuestData GetQuest(int questId)
        {
            return quests.Find(q => q.QuestId == questId);
        }
    }
}";
        }

        private string GetSkillDatabaseScript()
        {
            return @"using UnityEngine;
using System.Collections.Generic;
using System.Linq; // CORRIGIDO: Adicionado
using TOP.Core;

namespace TOP.Database
{
    [CreateAssetMenu(fileName = ""SkillDatabase"", menuName = ""TOP/Skill Database"")]
    public class SkillDatabase : ScriptableObject
    {
        public List<SkillData> skills = new List<SkillData>();

        public SkillData GetSkill(int skillId)
        {
            return skills.Find(s => s.SkillId == skillId);
        }

        public List<SkillData> GetSkillsForJob(byte jobId)
        {
            return skills.FindAll(s => s.RequiredJob == jobId);
        }
    }
}";
        }

        // =================================================================================
        // SCRIPTS - INVENTORY
        // =================================================================================
        private string GetInventoryItemScript()
        {
            return @"using System;

namespace TOP.Inventory
{
    [Serializable]
    public class InventoryItem
    {
        public long Id;
        public int ItemId;
        public int Quantity;
        public ushort SlotIndex;
        public bool IsEquipped;
        public ushort Durability;
    }
}";
        }

        private string GetEquippedItemScript()
        {
            return @"using System;
using TOP.Core;

namespace TOP.Inventory
{
    [Serializable]
    public class EquippedItem
    {
        public EquipmentSlot Slot;
        public long ItemId;
        public int ItemDatabaseId;
        public ushort Durability;
    }
}";
        }

        private string GetInventorySystemScript()
        {
            return @"using System.Collections.Generic;
using UnityEngine;

namespace TOP.Inventory
{
    public class InventorySystem : MonoBehaviour
    {
        [SerializeField] private int maxSlots = 40;
        private InventoryItem[] _slots;

        void Awake()
        {
            _slots = new InventoryItem[maxSlots];
        }

        public bool AddItem(int itemId, int quantity, out ushort slotIndex)
        {
            slotIndex = 0;

            // Procurar slot existente com mesmo item (stackable)
            for (ushort i = 0; i < maxSlots; i++)
            {
                if (_slots[i] != null && _slots[i].ItemId == itemId)
                {
                    // TODO: Verificar max stack
                    _slots[i].Quantity += quantity;
                    slotIndex = i;
                    return true;
                }
            }

            // Procurar slot vazio
            for (ushort i = 0; i < maxSlots; i++)
            {
                if (_slots[i] == null)
                {
                    _slots[i] = new InventoryItem
                    {
                        ItemId = itemId,
                        Quantity = quantity,
                        SlotIndex = i
                    };
                    slotIndex = i;
                    return true;
                }
            }

            return false; // Inventario cheio
        }

        public bool RemoveItem(ushort slotIndex, int quantity)
        {
            if (slotIndex >= maxSlots || _slots[slotIndex] == null)
                return false;

            _slots[slotIndex].Quantity -= quantity;
            if (_slots[slotIndex].Quantity <= 0)
                _slots[slotIndex] = null;

            return true;
        }

        public bool MoveItem(ushort fromSlot, ushort toSlot)
        {
            if (fromSlot >= maxSlots || toSlot >= maxSlots)
                return false;

            var temp = _slots[toSlot];
            _slots[toSlot] = _slots[fromSlot];
            if (_slots[toSlot] != null)
                _slots[toSlot].SlotIndex = toSlot;

            _slots[fromSlot] = temp;
            if (_slots[fromSlot] != null)
                _slots[fromSlot].SlotIndex = fromSlot;

            return true;
        }

        public InventoryItem GetItem(ushort slotIndex)
        {
            if (slotIndex >= maxSlots) return null;
            return _slots[slotIndex];
        }

        public int GetEmptySlotCount()
        {
            int count = 0;
            for (int i = 0; i < maxSlots; i++)
                if (_slots[i] == null) count++;
            return count;
        }
    }
}";
        }

        private string GetItemDatabaseScript()
        {
            return @"using UnityEngine;
using System.Collections.Generic;
using System.Linq; // CORRIGIDO: Adicionado
using TOP.Core;

namespace TOP.Inventory
{
    public class ItemDatabase : MonoBehaviour
    {
        public static ItemDatabase Instance { get; private set; }

        [SerializeField] private List<ItemData> items = new List<ItemData>();
        private Dictionary<int, ItemData> _itemMap;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _itemMap = new Dictionary<int, ItemData>();
            foreach (var item in items)
            {
                if (item != null)
                    _itemMap[item.ItemId] = item;
            }
        }

        public ItemData GetItem(int itemId)
        {
            _itemMap.TryGetValue(itemId, out var item);
            return item;
        }

        public Sprite GetItemIcon(int itemId)
        {
            var item = GetItem(itemId);
            return item?.Icon;
        }

        public string GetItemName(int itemId)
        {
            var item = GetItem(itemId);
            return item?.ItemName ?? ""Unknown"";
        }
    }
}";
        }

        private string GetItemSlotUIScript()
        {
            return @"using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using TOP.Core;

namespace TOP.Inventory
{
    public class ItemSlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private Image background;
        [SerializeField] private GameObject equippedIndicator;

        [Header(""Colors"")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = Color.yellow;
        [SerializeField] private Color selectedColor = Color.cyan;

        public ushort SlotIndex { get; set; }
        public InventoryItem CurrentItem { get; private set; }

        private bool _isDragging = false;
        private Transform _originalParent;

        public System.Action<ushort> OnSlotClicked;
        public System.Action<ushort, ushort> OnItemMoved;
        public System.Action<ushort> OnItemDoubleClicked;

        private float _lastClickTime;
        private const float DOUBLE_CLICK_TIME = 0.3f;

        public void Setup(InventoryItem item)
        {
            CurrentItem = item;

            if (item == null)
            {
                itemIcon.sprite = null;
                itemIcon.enabled = false;
                quantityText.text = """";
                equippedIndicator.SetActive(false);
                return;
            }

            var itemData = ItemDatabase.Instance?.GetItem(item.ItemId);
            if (itemData != null)
            {
                itemIcon.sprite = itemData.Icon;
                itemIcon.enabled = true;
            }

            quantityText.text = item.Quantity > 1 ? item.Quantity.ToString() : """";
            equippedIndicator.SetActive(item.IsEquipped);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                // Context menu
                return;
            }

            float timeSinceLastClick = Time.time - _lastClickTime;
            if (timeSinceLastClick <= DOUBLE_CLICK_TIME)
            {
                OnItemDoubleClicked?.Invoke(SlotIndex);
            }
            else
            {
                OnSlotClicked?.Invoke(SlotIndex);
            }
            _lastClickTime = Time.time;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (CurrentItem == null) return;

            _isDragging = true;
            _originalParent = transform.parent;
            transform.SetParent(transform.root);
            transform.SetAsLastSibling();
            GetComponent<Image>().raycastTarget = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;

            _isDragging = false;
            transform.SetParent(_originalParent);
            transform.localPosition = Vector3.zero;
            GetComponent<Image>().raycastTarget = true;

            // Verificar se soltou em outro slot
            var targetSlot = eventData.pointerCurrentRaycast.gameObject?.GetComponent<ItemSlotUI>();
            if (targetSlot != null && targetSlot != this)
            {
                OnItemMoved?.Invoke(SlotIndex, targetSlot.SlotIndex);
            }
        }

        public void SetSelected(bool selected)
        {
            background.color = selected ? selectedColor : normalColor;
        }

        public void SetHover(bool hover)
        {
            if (background.color != selectedColor)
                background.color = hover ? hoverColor : normalColor;
        }
    }
}";
        }

        private string GetEquipmentSlotUIScript()
        {
            return @"using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using TOP.Core;

namespace TOP.Inventory
{
    public class EquipmentSlotUI : MonoBehaviour, IDropHandler
    {
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI slotNameText;
        [SerializeField] private EquipmentSlot slotType;

        public EquipmentSlot SlotType => slotType;

        public System.Action<EquipmentSlot> OnEquipRequested;
        public System.Action<EquipmentSlot> OnUnequipRequested;

        void Start()
        {
            slotNameText.text = slotType.ToString();
        }

        public void SetItem(ItemData item)
        {
            if (item == null)
            {
                itemIcon.sprite = null;
                itemIcon.enabled = false;
                return;
            }

            itemIcon.sprite = item.Icon;
            itemIcon.enabled = true;
        }

        public void OnDrop(PointerEventData eventData)
        {
            var draggedSlot = eventData.pointerDrag?.GetComponent<ItemSlotUI>();
            if (draggedSlot != null && draggedSlot.CurrentItem != null)
            {
                OnEquipRequested?.Invoke(slotType);
            }
        }
    }
}";
        }

        private string GetInventoryUIScript()
        {
            return @"using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TOP.Player;
using TOP.Core;

namespace TOP.Inventory
{
    public class InventoryUI : MonoBehaviour
    {
        public static InventoryUI Instance { get; private set; }

        [Header(""Inventory"")]
        [SerializeField] private Transform inventoryGrid;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private int slotCount = 40;

        [Header(""Equipment"")]
        [SerializeField] private EquipmentSlotUI[] equipmentSlots;

        [Header(""Info Panel"")]
        [SerializeField] private GameObject itemInfoPanel;
        [SerializeField] private Text itemNameText;
        [SerializeField] private Text itemDescText;

        [Header(""Buttons"")]
        [SerializeField] private Button closeButton;

        private List<ItemSlotUI> _slots = new List<ItemSlotUI>();
        private PlayerInventory _playerInventory;
        private ushort _selectedSlot = ushort.MaxValue;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            closeButton.onClick.AddListener(Hide);

            // Criar slots
            for (ushort i = 0; i < slotCount; i++)
            {
                var slotObj = Instantiate(slotPrefab, inventoryGrid);
                var slot = slotObj.GetComponent<ItemSlotUI>();
                slot.SlotIndex = i;
                slot.OnSlotClicked += OnSlotClicked;
                slot.OnItemMoved += OnItemMoved;
                slot.OnItemDoubleClicked += OnItemDoubleClicked;
                _slots.Add(slot);
            }

            // Setup equipment slots
            foreach (var eqSlot in equipmentSlots)
            {
                eqSlot.OnEquipRequested += OnEquipRequested;
                eqSlot.OnUnequipRequested += OnUnequipRequested;
            }

            gameObject.SetActive(false);
        }

        public void Setup(PlayerInventory inventory)
        {
            _playerInventory = inventory;
            inventory.OnInventoryChanged += RefreshUI;
            RefreshUI();
        }

        void RefreshUI()
        {
            if (_playerInventory == null) return;

            for (int i = 0; i < slotCount; i++)
            {
                var item = _playerInventory.GetItem((ushort)i);
                _slots[i].Setup(item);
            }
        }

        void OnSlotClicked(ushort slotIndex)
        {
            if (_selectedSlot != ushort.MaxValue)
                _slots[_selectedSlot].SetSelected(false);

            _selectedSlot = slotIndex;
            _slots[slotIndex].SetSelected(true);

            // Mostrar info
            var item = _playerInventory?.GetItem(slotIndex);
            if (item != null)
            {
                var itemData = ItemDatabase.Instance?.GetItem(item.ItemId);
                if (itemData != null)
                {
                    itemNameText.text = itemData.ItemName;
                    itemDescText.text = itemData.Description;
                    itemInfoPanel.SetActive(true);
                }
            }
        }

        void OnItemMoved(ushort fromSlot, ushort toSlot)
        {
            _playerInventory?.CmdMoveItem(fromSlot, toSlot);
        }

        void OnItemDoubleClicked(ushort slotIndex)
        {
            var item = _playerInventory?.GetItem(slotIndex);
            if (item == null) return;

            var itemData = ItemDatabase.Instance?.GetItem(item.ItemId);
            if (itemData == null) return;

            if (itemData.Type == ItemType.Consumable)
            {
                _playerInventory?.CmdUseItem(slotIndex);
            }
            else if (itemData.Type == ItemType.Weapon || itemData.Type == ItemType.Armor || itemData.Type == ItemType.Accessory)
            {
                var eqData = itemData as EquipmentData;
                if (eqData != null)
                {
                    _playerInventory?.CmdEquipItem(slotIndex, eqData.Slot);
                }
            }
        }

        void OnEquipRequested(EquipmentSlot slot)
        {
            if (_selectedSlot == ushort.MaxValue) return;
            _playerInventory?.CmdEquipItem(_selectedSlot, slot);
        }

        void OnUnequipRequested(EquipmentSlot slot)
        {
            // TODO: Implementar desequipar
        }

        public void Show()
        {
            gameObject.SetActive(true);
            RefreshUI();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            itemInfoPanel.SetActive(false);
        }

        void OnDestroy()
        {
            if (_playerInventory != null)
                _playerInventory.OnInventoryChanged -= RefreshUI;
        }
    }
}";
        }

        private string GetCloseInventoryButtonScript()
        {
            return @"using UnityEngine;
using UnityEngine.UI;

namespace TOP.Inventory
{
    public class CloseInventoryButton : MonoBehaviour
    {
        void Start()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                InventoryUI.Instance?.Hide();
            });
        }
    }
}";
        }

        private string GetInventoryTestHelperScript()
        {
            return @"using UnityEngine;
using TOP.Player;

namespace TOP.Inventory
{
    public class InventoryTestHelper : MonoBehaviour
    {
        [SerializeField] private int[] testItemIds = { 1001, 1002, 1003 };
        [SerializeField] private KeyCode testKey = KeyCode.T;

        void Update()
        {
            if (Input.GetKeyDown(testKey))
            {
                var inv = GetComponent<PlayerInventory>();
                if (inv != null)
                {
                    foreach (var itemId in testItemIds)
                    {
                        inv.CmdAddItem(itemId, 1);
                    }
                }
            }
        }
    }
}";
        }

        // =================================================================================
        // SCRIPTS - PLAYER COMPONENTS
        // =================================================================================
        private string GetCameraFollowScript()
        {
            return @"using UnityEngine;

namespace TOP.Player
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0, 10, -8);
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private float rotationSpeed = 100f;
        [SerializeField] private float minDistance = 3f;
        [SerializeField] private float maxDistance = 20f;
        [SerializeField] private float zoomSpeed = 5f;

        private float _currentDistance;
        private float _currentRotationX = 45f;
        private float _currentRotationY = 0f;

        void Start()
        {
            _currentDistance = offset.magnitude;
        }

        void LateUpdate()
        {
            if (target == null) return;

            // Zoom com scroll
            float scroll = Input.GetAxis(""Mouse ScrollWheel"");
            _currentDistance = Mathf.Clamp(_currentDistance - scroll * zoomSpeed, minDistance, maxDistance);

            // Rotacao com botao do meio
            if (Input.GetMouseButton(2))
            {
                _currentRotationY += Input.GetAxis(""Mouse X"") * rotationSpeed * Time.deltaTime;
                _currentRotationX -= Input.GetAxis(""Mouse Y"") * rotationSpeed * Time.deltaTime;
                _currentRotationX = Mathf.Clamp(_currentRotationX, 10f, 80f);
            }

            // Calcular posicao
            Quaternion rotation = Quaternion.Euler(_currentRotationX, _currentRotationY, 0);
            Vector3 position = target.position - rotation * Vector3.forward * _currentDistance;
            position.y = Mathf.Max(position.y, target.position.y + 2f);

            transform.position = Vector3.Lerp(transform.position, position, smoothSpeed * Time.deltaTime);
            transform.LookAt(target.position + Vector3.up * 2f);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}";
        }

        private string GetPlayerAnimationScript()
        {
            return @"using UnityEngine;
using Mirror;

namespace TOP.Player
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimation : NetworkBehaviour
    {
        private Animator _animator;
        private static readonly int IsMovingHash = Animator.StringToHash(""IsMoving"");
        private static readonly int IsAttackingHash = Animator.StringToHash(""IsAttacking"");
        private static readonly int AttackTriggerHash = Animator.StringToHash(""Attack"");
        private static readonly int DieHash = Animator.StringToHash(""Die"");
        private static readonly int RespawnHash = Animator.StringToHash(""Respawn"");
        private static readonly int SkillHash = Animator.StringToHash(""Skill"");
        private static readonly int MoveSpeedHash = Animator.StringToHash(""MoveSpeed"");

        void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        [ClientRpc]
        public void RpcSetMoving(bool isMoving, float speed)
        {
            if (_animator == null) return;
            _animator.SetBool(IsMovingHash, isMoving);
            _animator.SetFloat(MoveSpeedHash, speed);
        }

        [ClientRpc]
        public void RpcTriggerAttack()
        {
            if (_animator == null) return;
            _animator.SetTrigger(AttackTriggerHash);
        }

        [ClientRpc]
        public void RpcTriggerSkill(int skillId)
        {
            if (_animator == null) return;
            _animator.SetInteger(SkillHash, skillId);
            _animator.SetTrigger(SkillHash);
        }

        public void PlayDeath()
        {
            if (_animator != null)
                _animator.SetTrigger(DieHash);
        }

        public void PlayRespawn()
        {
            if (_animator != null)
                _animator.SetTrigger(RespawnHash);
        }

        [ClientRpc]
        public void RpcSetBool(string paramName, bool value)
        {
            if (_animator != null)
                _animator.SetBool(paramName, value);
        }

        [ClientRpc]
        public void RpcSetFloat(string paramName, float value)
        {
            if (_animator != null)
                _animator.SetFloat(paramName, value);
        }

        [ClientRpc]
        public void RpcSetTrigger(string paramName)
        {
            if (_animator != null)
                _animator.SetTrigger(paramName);
        }
    }
}";
        }

        private string GetPlayerClassScript()
        {
            return @"using UnityEngine;
using TOP.Core;

namespace TOP.Player
{
    public class PlayerClass : MonoBehaviour
    {
        [SerializeField] private CharacterClassData classData;
        [SerializeField] private byte classId;

        public CharacterClassData ClassData => classData;
        public byte ClassId => classId;

        public void Initialize(byte jobId)
        {
            classId = jobId;
            // TODO: Carregar classData do banco de dados de classes
        }

        public string GetClassName()
        {
            return classData?.ClassName ?? ""Unknown"";
        }
    }
}";
        }

        private string GetPlayerCombatScript()
        {
            return @"using UnityEngine;
using Mirror;
using TOP.Core;
using TOP.Systems;
using TOP.Gameplay;
using System.Linq;

namespace TOP.Player
{
    public class PlayerCombat : NetworkBehaviour
    {
        [Header(""Combat"")]
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

            // Calcular dano
            int damage = CalculateDamage();

            // Aplicar dano no alvo
            var targetStats = _currentTarget.GetComponent<PlayerStats>();
            if (targetStats != null)
            {
                targetStats.TakeDamage(damage);

                // Notificar cliente do atacante
                var controller = GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.RpcTakeDamage(damage, _currentTarget.transform.position);
                }
            }

            // Animacao
            _animation.RpcTriggerAttack();

            // Consumir SP
            _stats.ConsumeSp(5);
        }

        [Server]
        int CalculateDamage()
        {
            int baseDamage = _stats.PhysicalAttack;

            // Bonus de arma
            if (_equipment != null)
            {
                baseDamage += _equipment.GetTotalAttackBonus();
            }

            // Variacao
            float variation = Random.Range(0.9f, 1.1f);
            int finalDamage = Mathf.RoundToInt(baseDamage * variation);

            // Critico
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
}";
        }

        private string GetPlayerConsumablesScript()
        {
            return @"using UnityEngine;
using Mirror;
using TOP.Core;
using TOP.Inventory;
using TOP.Systems;
using TOP.Gameplay;

namespace TOP.Player
{
    public class PlayerConsumables : NetworkBehaviour
    {
        private PlayerStats _stats;
        private PlayerInventory _inventory;

        void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _inventory = GetComponent<PlayerInventory>();
        }

        [Command]
        public void CmdUseItem(ushort slotIndex)
        {
            var item = _inventory.GetItem(slotIndex);
            if (item == null) return;

            var itemData = ItemDatabase.Instance?.GetItem(item.ItemId);
            if (itemData == null || itemData.Type != ItemType.Consumable) return;

            var consumable = itemData as ConsumableData;
            if (consumable == null) return;

            // Aplicar efeitos
            if (consumable.HpRestore > 0)
                _stats.Heal(consumable.HpRestore);

            if (consumable.MpRestore > 0)
                _stats.RestoreMp(consumable.MpRestore);

            if (consumable.SpRestore > 0)
                _stats.RestoreSp(consumable.SpRestore);

            // Aplicar buff se houver
            if (consumable.BuffId > 0)
            {
                var buffManager = Object.FindFirstObjectByType<BuffManager>();
                if (buffManager != null)
                {
                    buffManager.ApplyBuff(netId, consumable.BuffId, consumable.BuffLevel, consumable.Duration);
                }
            }

            // Remover item
            _inventory.RemoveItem(slotIndex, 1);

            // Feedback
            var controller = GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.RpcShowMessage($""Usou {itemData.ItemName}"", MessageType.Success);
            }
        }
    }
}";
        }

        private string GetPlayerDamageDetectorScript()
        {
            return @"using UnityEngine;
using Mirror;

namespace TOP.Player
{
    public class PlayerDamageDetector : NetworkBehaviour
    {
        [SerializeField] private Collider hitbox;

        void OnTriggerEnter(Collider other)
        {
            if (!isServer) return;

            // Detectar ataques de outros jogadores ou mobs
            var attacker = other.GetComponent<PlayerCombat>();
            if (attacker != null && attacker.netId != netId)
            {
                // O dano ja e aplicado pelo atacante no server
            }
        }
    }
}";
        }

        private string GetPlayerEquipmentScript()
        {
            return @"using UnityEngine;
using Mirror;
using System.Collections.Generic;
using TOP.Core;
using TOP.Inventory;

namespace TOP.Player
{
    public class PlayerEquipment : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnEquipmentChanged))] private string _equipmentData = """";

        private readonly Dictionary<EquipmentSlot, EquippedItem> _equippedItems = new Dictionary<EquipmentSlot, EquippedItem>();
        private PlayerStats _stats;

        void Awake()
        {
            _stats = GetComponent<PlayerStats>();
        }

        [Server]
        public void EquipItem(InventoryItem item, EquipmentSlot slot)
        {
            var itemData = ItemDatabase.Instance?.GetItem(item.ItemId) as EquipmentData;
            if (itemData == null) return;

            // Desequipar item atual se houver
            if (_equippedItems.ContainsKey(slot))
            {
                UnequipItem(slot);
            }

            _equippedItems[slot] = new EquippedItem
            {
                Slot = slot,
                ItemId = item.Id,
                ItemDatabaseId = item.ItemId,
                Durability = item.Durability
            };

            // Aplicar stats
            ApplyEquipmentStats(itemData, true);

            // Atualizar sync
            SerializeEquipment();
        }

        [Server]
        public void UnequipItem(EquipmentSlot slot)
        {
            if (!_equippedItems.ContainsKey(slot)) return;

            var equipped = _equippedItems[slot];
            var itemData = ItemDatabase.Instance?.GetItem(equipped.ItemDatabaseId) as EquipmentData;

            if (itemData != null)
                ApplyEquipmentStats(itemData, false);

            _equippedItems.Remove(slot);
            SerializeEquipment();
        }

        [Server]
        void ApplyEquipmentStats(EquipmentData data, bool add)
        {
            int multiplier = add ? 1 : -1;

            _stats.AddBonusStrength(data.StrBonus * multiplier);
            _stats.AddBonusAgility(data.AgiBonus * multiplier);
            _stats.AddBonusConstitution(data.ConBonus * multiplier);
            _stats.AddBonusSpirit(data.SprBonus * multiplier);
            _stats.AddBonusHp(data.HpBonus * multiplier);
            _stats.AddBonusMp(data.MpBonus * multiplier);
            _stats.AddBonusSp(data.SpBonus * multiplier);
            _stats.AddBonusAttack(data.PhysicalAttack * multiplier);
            _stats.AddBonusDefense(data.PhysicalDefense * multiplier);
        }

        [Server]
        void SerializeEquipment()
        {
            // Serializar para string sync
            var list = new List<string>();
            foreach (var kvp in _equippedItems)
            {
                list.Add($""{kvp.Key}:{kvp.Value.ItemDatabaseId}"");
            }
            _equipmentData = string.Join("";"", list);
        }

        void OnEquipmentChanged(string oldValue, string newValue)
        {
            // Atualizar visual no cliente
            UpdateVisuals();
        }

        void UpdateVisuals()
        {
            // TODO: Trocar modelos 3D baseado no equipamento
        }

        public int GetTotalAttackBonus()
        {
            int bonus = 0;
            foreach (var item in _equippedItems.Values)
            {
                var data = ItemDatabase.Instance?.GetItem(item.ItemDatabaseId) as EquipmentData;
                if (data != null)
                    bonus += data.PhysicalAttack;
            }
            return bonus;
        }

        public int GetTotalDefenseBonus()
        {
            int bonus = 0;
            foreach (var item in _equippedItems.Values)
            {
                var data = ItemDatabase.Instance?.GetItem(item.ItemDatabaseId) as EquipmentData;
                if (data != null)
                    bonus += data.PhysicalDefense;
            }
            return bonus;
        }
    }
}";
        }

        private string GetPlayerHotbarScript()
        {
            return @"using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TOP.Inventory;

namespace TOP.Player
{
    public class PlayerHotbar : MonoBehaviour
    {
        [SerializeField] private HotbarSlot[] slots;
        private PlayerInventory _inventory;
        private PlayerSkills _skills;

        void Start()
        {
            _inventory = GetComponent<PlayerInventory>();
            _skills = GetComponent<PlayerSkills>();

            for (int i = 0; i < slots.Length; i++)
            {
                int index = i; // Capture para lambda
                slots[i].GetComponent<Button>().onClick.AddListener(() => OnHotbarSlotClicked(index));
            }
        }

        void Update()
        {
            // Atalhos de teclado 1-8
            for (int i = 0; i < Mathf.Min(slots.Length, 8); i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    OnHotbarSlotClicked(i);
                }
            }
        }

        void OnHotbarSlotClicked(int index)
        {
            var slot = slots[index];
            if (slot.SkillId > 0)
            {
                _skills?.UseSkill(slot.SkillId, Vector3.zero, null);
            }
            else if (slot.ItemSlot >= 0)
            {
                _inventory?.CmdUseItem((ushort)slot.ItemSlot);
            }
        }
    }

    [System.Serializable]
    public class HotbarSlot
    {
        public Image icon;
        public TextMeshProUGUI cooldownText;
        public int SkillId;
        public int ItemSlot = -1;
    }
}";
        }

        private string GetPlayerInventoryScript()
        {
            return @"using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;
using TOP.Inventory;
using TOP.Core;

namespace TOP.Player
{
    public class PlayerInventory : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnInventoryChanged))] private string _inventoryData = """";

        private readonly InventoryItem[] _slots = new InventoryItem[40];
        public event Action OnInventoryChanged;

        void Awake()
        {
            for (int i = 0; i < _slots.Length; i++)
                _slots[i] = null;
        }

        [Server]
        public void AddItem(int itemId, int quantity, ushort slotIndex)
        {
            if (slotIndex >= _slots.Length) return;

            _slots[slotIndex] = new InventoryItem
            {
                ItemId = itemId,
                Quantity = quantity,
                SlotIndex = slotIndex
            };

            SerializeInventory();
        }

        [Command]
        public void CmdAddItem(int itemId, int quantity)
        {
            // Encontrar slot vazio
            for (ushort i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null)
                {
                    AddItem(itemId, quantity, i);
                    return;
                }
            }
        }

        [Command]
        public void CmdMoveItem(ushort fromSlot, ushort toSlot)
        {
            if (fromSlot >= _slots.Length || toSlot >= _slots.Length) return;

            var temp = _slots[toSlot];
            _slots[toSlot] = _slots[fromSlot];
            if (_slots[toSlot] != null)
                _slots[toSlot].SlotIndex = toSlot;

            _slots[fromSlot] = temp;
            if (_slots[fromSlot] != null)
                _slots[fromSlot].SlotIndex = fromSlot;

            SerializeInventory();
        }

        [Command]
        public void CmdEquipItem(ushort inventorySlot, EquipmentSlot targetSlot)
        {
            var item = _slots[inventorySlot];
            if (item == null) return;

            var itemData = ItemDatabase.Instance?.GetItem(item.ItemId) as EquipmentData;
            if (itemData == null) return;

            var equipment = GetComponent<PlayerEquipment>();
            if (equipment != null)
            {
                equipment.EquipItem(item, targetSlot);
                item.IsEquipped = true;
                SerializeInventory();
            }
        }

        [Command]
        public void CmdDropItem(ushort slotIndex, int quantity, Vector3 dropPosition)
        {
            var item = _slots[slotIndex];
            if (item == null || item.Quantity < quantity) return;

            // Criar world item
            var worldItemManager = Object.FindFirstObjectByType<WorldItemManager>();
            if (worldItemManager != null)
            {
                worldItemManager.SpawnWorldItem(item.ItemId, quantity, dropPosition);
            }

            // Remover do inventario
            item.Quantity -= quantity;
            if (item.Quantity <= 0)
                _slots[slotIndex] = null;

            SerializeInventory();
        }

        [Command]
        public void CmdUseItem(ushort slotIndex)
        {
            var consumables = GetComponent<PlayerConsumables>();
            if (consumables != null)
            {
                consumables.CmdUseItem(slotIndex);
            }
        }

        [Server]
        public void RemoveItem(ushort slotIndex, int quantity)
        {
            if (slotIndex >= _slots.Length || _slots[slotIndex] == null) return;

            _slots[slotIndex].Quantity -= quantity;
            if (_slots[slotIndex].Quantity <= 0)
                _slots[slotIndex] = null;

            SerializeInventory();
        }

        public InventoryItem GetItem(ushort slotIndex)
        {
            if (slotIndex >= _slots.Length) return null;
            return _slots[slotIndex];
        }

        [Server]
        void SerializeInventory()
        {
            var list = new List<string>();
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null)
                {
                    list.Add($""{i}:{_slots[i].ItemId}:{_slots[i].Quantity}:{(_slots[i].IsEquipped ? 1 : 0)}"");
                }
            }
            _inventoryData = string.Join("";"", list);
        }

        void OnInventoryChanged(string oldValue, string newValue)
        {
            // Atualizar UI
            OnInventoryChanged?.Invoke();
        }
    }
}";
        }

        private string GetPlayerMovementScript()
        {
            return @"using UnityEngine;
using Mirror;
using UnityEngine.AI;

namespace TOP.Player
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class PlayerMovement : NetworkBehaviour
    {
        [Header(""Movement"")]
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
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask(""Ground"")))
                {
                    CmdMoveTo(hit.point);
                }
            }

            // WASD movement
            float h = Input.GetAxis(""Horizontal"");
            float v = Input.GetAxis(""Vertical"");
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
                // Rotacionar na direcao do movimento
                Vector3 direction = _agent.desiredVelocity.normalized;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }

            // Notificar mudanca de estado
            if (wasMoving != _isMoving)
            {
                _animation.RpcSetMoving(_isMoving, _agent.velocity.magnitude);
            }
        }

        public bool IsMoving => _isMoving;
        public Vector3 TargetPosition => _targetPosition;
    }
}";
        }

        private string GetPlayerRespawnScript()
        {
            return @"using UnityEngine;
using Mirror;

namespace TOP.Player
{
    public class PlayerRespawn : NetworkBehaviour
    {
        [SerializeField] private float respawnDelay = 5f;
        [SerializeField] private Vector3[] respawnPoints;

        private PlayerController _controller;
        private PlayerStats _stats;
        private bool _isDead;

        void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _stats = GetComponent<PlayerStats>();
        }

        [Server]
        public void Die()
        {
            if (_isDead) return;
            _isDead = true;

            _controller.Die();
            Invoke(nameof(Respawn), respawnDelay);
        }

        [Server]
        void Respawn()
        {
            _isDead = false;

            // Escolher ponto de respawn
            Vector3 respawnPos = respawnPoints.Length > 0
                ? respawnPoints[Random.Range(0, respawnPoints.Length)]
                : Vector3.zero;

            transform.position = respawnPos;
            _controller.Respawn();
        }
    }
}";
        }

        private string GetPlayerSkillsScript()
        {
            return @"using UnityEngine;
using Mirror;
using System.Collections.Generic;
using TOP.Core;
using TOP.Database;

namespace TOP.Player
{
    public class PlayerSkills : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnSkillsChanged))] private string _skillsData = """";

        private readonly Dictionary<int, int> _skillLevels = new Dictionary<int, int>();
        private readonly Dictionary<int, float> _skillCooldowns = new Dictionary<int, float>();
        private PlayerStats _stats;
        private PlayerAnimation _animation;
        private PlayerCombat _combat;

        void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _animation = GetComponent<PlayerAnimation>();
            _combat = GetComponent<PlayerCombat>();
        }

        void Update()
        {
            // Atualizar cooldowns
            var keys = new List<int>(_skillCooldowns.Keys);
            foreach (var key in keys)
            {
                if (_skillCooldowns[key] > 0)
                    _skillCooldowns[key] -= Time.deltaTime;
            }
        }

        [Server]
        public void LearnSkill(int skillId, int level)
        {
            _skillLevels[skillId] = level;
            SerializeSkills();
        }

        [Server]
        public void UseSkill(int skillId, Vector3 targetPosition, NetworkIdentity target)
        {
            if (!_skillLevels.ContainsKey(skillId)) return;

            var skillData = SkillDatabase.Instance?.GetSkill(skillId);
            if (skillData == null) return;

            // Verificar cooldown
            if (_skillCooldowns.ContainsKey(skillId) && _skillCooldowns[skillId] > 0)
                return;

            // Verificar MP/SP
            if (_stats.CurrentMp < skillData.MpCost || _stats.CurrentSp < skillData.SpCost)
                return;

            // Consumir recursos
            _stats.ConsumeMp(skillData.MpCost);
            _stats.ConsumeSp(skillData.SpCost);

            // Aplicar cooldown
            _skillCooldowns[skillId] = skillData.Cooldown;

            // Executar skill
            ExecuteSkill(skillData, targetPosition, target);

            // Animacao
            _animation.RpcTriggerSkill(skillId);
        }

        [Server]
        void ExecuteSkill(SkillData data, Vector3 targetPosition, NetworkIdentity target)
        {
            switch (data.TargetType)
            {
                case TargetType.Self:
                    ApplySkillEffect(data, netId);
                    break;

                case TargetType.Single:
                    if (target != null)
                        ApplySkillEffect(data, target.netId);
                    break;

                case TargetType.AoE:
                    // TODO: Area of effect
                    break;

                case TargetType.Line:
                    // TODO: Line projectile
                    break;
            }

            // Spawn projectile se houver
            if (data.ProjectilePrefab != null)
            {
                var projectile = Instantiate(data.ProjectilePrefab, transform.position + Vector3.up, Quaternion.identity);
                var proj = projectile.GetComponent<SkillProjectile>();
                if (proj != null)
                {
                    proj.Initialize(data, targetPosition, target);
                }
                NetworkServer.Spawn(projectile);
            }
        }

        [Server]
        void ApplySkillEffect(SkillData data, uint targetNetId)
        {
            var targetObj = NetworkServer.spawned[targetNetId];
            if (targetObj == null) return;

            var targetStats = targetObj.GetComponent<PlayerStats>();
            if (targetStats == null) return;

            int damage = CalculateSkillDamage(data);

            if (data.DamageType == DamageType.Heal)
            {
                targetStats.Heal(damage);
            }
            else
            {
                targetStats.TakeDamage(damage);
            }
        }

        [Server]
        int CalculateSkillDamage(SkillData data)
        {
            int baseDamage = data.BaseDamage;
            int level = _skillLevels.ContainsKey(data.SkillId) ? _skillLevels[data.SkillId] : 1;

            float multiplier = data.DamageMultiplier * level;
            int finalDamage = Mathf.RoundToInt(baseDamage * multiplier);

            // Add weapon damage
            if (data.DamageType == DamageType.Physical)
                finalDamage += _stats.PhysicalAttack;
            else if (data.DamageType == DamageType.Magic)
                finalDamage += _stats.MagicAttack;

            return Mathf.Max(1, finalDamage);
        }

        [Server]
        void SerializeSkills()
        {
            var list = new List<string>();
            foreach (var kvp in _skillLevels)
            {
                list.Add($""{kvp.Key}:{kvp.Value}"");
            }
            _skillsData = string.Join("";"", list);
        }

        void OnSkillsChanged(string oldValue, string newValue)
        {
            // Atualizar UI de skills
        }

        public bool HasSkill(int skillId) => _skillLevels.ContainsKey(skillId);
        public int GetSkillLevel(int skillId) => _skillLevels.ContainsKey(skillId) ? _skillLevels[skillId] : 0;
        public float GetCooldown(int skillId) => _skillCooldowns.ContainsKey(skillId) ? _skillCooldowns[skillId] : 0;
    }
}";
        }

        private string GetPlayerStatsScript()
        {
            return @"using UnityEngine;
using Mirror;
using TOP.Core;

namespace TOP.Player
{
    public class PlayerStats : NetworkBehaviour, ICharacterStats
    {
        [Header(""Base Stats"")]
        [SyncVar] public int BaseStrength;
        [SyncVar] public int BaseAgility;
        [SyncVar] public int BaseConstitution;
        [SyncVar] public int BaseSpirit;

        [Header(""Current Values"")]
        [SyncVar] public int CurrentHp;
        [SyncVar] public int CurrentMp;
        [SyncVar] public int CurrentSp;

        [Header(""Bonus Stats"")]
        [SyncVar] private int _bonusStr;
        [SyncVar] private int _bonusAgi;
        [SyncVar] private int _bonusCon;
        [SyncVar] private int _bonusSpr;
        [SyncVar] private int _bonusHp;
        [SyncVar] private int _bonusMp;
        [SyncVar] private int _bonusSp;
        [SyncVar] private int _bonusAtk;
        [SyncVar] private int _bonusDef;

        // Properties
        public int Strength => BaseStrength + _bonusStr;
        public int Agility => BaseAgility + _bonusAgi;
        public int Constitution => BaseConstitution + _bonusCon;
        public int Spirit => BaseSpirit + _bonusSpr;

        public int MaxHp => CalculateMaxHp() + _bonusHp;
        public int MaxMp => CalculateMaxMp() + _bonusMp;
        public int MaxSp => CalculateMaxSp() + _bonusSp;

        public int PhysicalAttack => CalculatePhysicalAttack() + _bonusAtk;
        public int MagicAttack => CalculateMagicAttack();
        public int PhysicalDefense => CalculatePhysicalDefense() + _bonusDef;
        public int MagicDefense => CalculateMagicDefense();

        public float MoveSpeed => 5f + (Agility * 0.01f);
        public float AttackSpeed => 1f + (Agility * 0.005f);
        public float CriticalRate => 0.05f + (Agility * 0.002f);
        public float CriticalDamage => 1.5f;

        public float HpRegen => 1f + (Constitution * 0.1f);
        public float MpRegen => 1f + (Spirit * 0.1f);
        public float SpRegen => 2f + (Constitution * 0.05f);

        private float _lastHpRegen;
        private float _lastMpRegen;
        private float _lastSpRegen;

        void Update()
        {
            if (!isServer) return;

            // Regen
            if (Time.time > _lastHpRegen + 1f)
            {
                _lastHpRegen = Time.time;
                Heal(Mathf.RoundToInt(HpRegen));
            }

            if (Time.time > _lastMpRegen + 1f)
            {
                _lastMpRegen = Time.time;
                RestoreMp(Mathf.RoundToInt(MpRegen));
            }

            if (Time.time > _lastSpRegen + 1f)
            {
                _lastSpRegen = Time.time;
                RestoreSp(Mathf.RoundToInt(SpRegen));
            }
        }

        [Server]
        public void Initialize(int str, int agi, int con, int spr, int maxHp, int maxMp, int maxSp)
        {
            BaseStrength = str;
            BaseAgility = agi;
            BaseConstitution = con;
            BaseSpirit = spr;

            CurrentHp = maxHp;
            CurrentMp = maxMp;
            CurrentSp = maxSp;
        }

        [Server]
        public void SetCurrentHpMpSp(int hp, int mp, int sp)
        {
            CurrentHp = Mathf.Clamp(hp, 0, MaxHp);
            CurrentMp = Mathf.Clamp(mp, 0, MaxMp);
            CurrentSp = Mathf.Clamp(sp, 0, MaxSp);
        }

        // CORRIGIDO: Assinatura compativel com ICharacterStats
        [Server]
        public void TakeDamage(int damage, uint attackerId = 0, DamageType damageType = DamageType.Physical)
        {
            int finalDamage = Mathf.Max(1, damage - PhysicalDefense);
            CurrentHp -= finalDamage;

            if (CurrentHp <= 0)
            {
                CurrentHp = 0;
                var controller = GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.Die();
                }
            }
        }

        // CORRIGIDO: Assinatura compativel com ICharacterStats
        [Server]
        public void Heal(int amount)
        {
            CurrentHp = Mathf.Min(CurrentHp + amount, MaxHp);
        }

        // CORRIGIDO: Propriedade IsDead da interface
        public bool IsDead => CurrentHp <= 0;

        [Server]
        public void ConsumeMp(int amount)
        {
            CurrentMp = Mathf.Max(0, CurrentMp - amount);
        }

        [Server]
        public void RestoreMp(int amount)
        {
            CurrentMp = Mathf.Min(CurrentMp + amount, MaxMp);
        }

        [Server]
        public void ConsumeSp(int amount)
        {
            CurrentSp = Mathf.Max(0, CurrentSp - amount);
        }

        [Server]
        public void RestoreSp(int amount)
        {
            CurrentSp = Mathf.Min(CurrentSp + amount, MaxSp);
        }

        // Bonus methods
        public void AddBonusStrength(int value) => _bonusStr += value;
        public void AddBonusAgility(int value) => _bonusAgi += value;
        public void AddBonusConstitution(int value) => _bonusCon += value;
        public void AddBonusSpirit(int value) => _bonusSpr += value;
        public void AddBonusHp(int value) => _bonusHp += value;
        public void AddBonusMp(int value) => _bonusMp += value;
        public void AddBonusSp(int value) => _bonusSp += value;
        public void AddBonusAttack(int value) => _bonusAtk += value;
        public void AddBonusDefense(int value) => _bonusDef += value;

        int CalculateMaxHp() => 100 + (Constitution * 10) + (BaseStrength * 2);
        int CalculateMaxMp() => 50 + (Spirit * 8) + (BaseConstitution * 2);
        int CalculateMaxSp() => 30 + (Constitution * 5) + (BaseAgility * 2);
        int CalculatePhysicalAttack() => 10 + (Strength * 2) + (Agility * 1);
        int CalculateMagicAttack() => 5 + (Spirit * 3);
        int CalculatePhysicalDefense() => 5 + (Constitution * 1) + (Strength * 1);
        int CalculateMagicDefense() => 3 + (Spirit * 2) + (Constitution * 1);
    }
}";
        }

        // =================================================================================
        // SCRIPTS - SYSTEMS
        // =================================================================================
        private string GetBuffManagerScript()
        {
            return @"using UnityEngine;
using System.Collections.Generic;
using System;

namespace TOP.Systems
{
    public class BuffManager : MonoBehaviour
    {
        public static BuffManager Instance { get; private set; }

        private readonly Dictionary<uint, List<ActiveBuff>> _activeBuffs = new Dictionary<uint, List<ActiveBuff>>();

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Update()
        {
            // Atualizar buffs
            var keys = new List<uint>(_activeBuffs.Keys);
            foreach (var key in keys)
            {
                var buffs = _activeBuffs[key];
                for (int i = buffs.Count - 1; i >= 0; i--)
                {
                    buffs[i].RemainingTime -= Time.deltaTime;
                    if (buffs[i].RemainingTime <= 0)
                    {
                        RemoveBuff(key, buffs[i]);
                        buffs.RemoveAt(i);
                    }
                }
            }
        }

        public void ApplyBuff(uint targetNetId, int buffId, int level, float duration)
        {
            if (!_activeBuffs.ContainsKey(targetNetId))
                _activeBuffs[targetNetId] = new List<ActiveBuff>();

            var buff = new ActiveBuff
            {
                BuffId = buffId,
                Level = level,
                Duration = duration,
                RemainingTime = duration,
                AppliedAt = Time.time
            };

            _activeBuffs[targetNetId].Add(buff);

            // TODO: Aplicar efeitos do buff no target
            var target = Mirror.NetworkServer.spawned[targetNetId];
            if (target != null)
            {
                var stats = target.GetComponent<TOP.Player.PlayerStats>();
                if (stats != null)
                {
                    // Aplicar modificadores baseados no buffId
                    ApplyBuffEffects(stats, buffId, level, true);
                }
            }
        }

        void RemoveBuff(uint targetNetId, ActiveBuff buff)
        {
            var target = Mirror.NetworkServer.spawned[targetNetId];
            if (target != null)
            {
                var stats = target.GetComponent<TOP.Player.PlayerStats>();
                if (stats != null)
                {
                    ApplyBuffEffects(stats, buff.BuffId, buff.Level, false);
                }
            }
        }

        void ApplyBuffEffects(TOP.Player.PlayerStats stats, int buffId, int level, bool apply)
        {
            int multiplier = apply ? 1 : -1;

            // TODO: Carregar dados do buff do database
            switch (buffId)
            {
                case 1: // Strength buff
                    stats.AddBonusStrength(10 * level * multiplier);
                    break;
                case 2: // Speed buff
                    // stats.AddBonusMoveSpeed(0.2f * level * multiplier);
                    break;
                case 3: // Defense buff
                    stats.AddBonusDefense(15 * level * multiplier);
                    break;
            }
        }

        public bool HasBuff(uint targetNetId, int buffId)
        {
            if (!_activeBuffs.ContainsKey(targetNetId)) return false;
            return _activeBuffs[targetNetId].Exists(b => b.BuffId == buffId);
        }

        public float GetRemainingTime(uint targetNetId, int buffId)
        {
            if (!_activeBuffs.ContainsKey(targetNetId)) return 0;
            var buff = _activeBuffs[targetNetId].Find(b => b.BuffId == buffId);
            return buff?.RemainingTime ?? 0;
        }
    }

    public class ActiveBuff
    {
        public int BuffId;
        public int Level;
        public float Duration;
        public float RemainingTime;
        public float AppliedAt;
    }
}";
        }

        private string GetCameraControllerScript()
        {
            return @"using UnityEngine;

namespace TOP.Systems
{
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        [SerializeField] private Camera mainCamera;
        [SerializeField] private Transform target;

        [Header(""Orbit Settings"")]
        [SerializeField] private float distance = 10f;
        [SerializeField] private float minDistance = 3f;
        [SerializeField] private float maxDistance = 20f;
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float rotationSpeed = 3f;
        [SerializeField] private float verticalMin = -10f;
        [SerializeField] private float verticalMax = 80f;

        private float _currentRotationX = 45f;
        private float _currentRotationY = 0f;
        private float _targetRotationX;
        private float _targetRotationY;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            if (mainCamera == null)
                mainCamera = Camera.main;
        }

        void LateUpdate()
        {
            if (target == null) return;

            HandleInput();
            UpdatePosition();
        }

        void HandleInput()
        {
            // Zoom
            float scroll = Input.GetAxis(""Mouse ScrollWheel"");
            distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);

            // Rotation
            if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
            {
                _targetRotationY += Input.GetAxis(""Mouse X"") * rotationSpeed;
                _targetRotationX -= Input.GetAxis(""Mouse Y"") * rotationSpeed;
                _targetRotationX = Mathf.Clamp(_targetRotationX, verticalMin, verticalMax);
            }

            // Smooth
            _currentRotationX = Mathf.Lerp(_currentRotationX, _targetRotationX, Time.deltaTime * 5f);
            _currentRotationY = Mathf.Lerp(_currentRotationY, _targetRotationY, Time.deltaTime * 5f);
        }

        void UpdatePosition()
        {
            Quaternion rotation = Quaternion.Euler(_currentRotationX, _currentRotationY, 0);
            Vector3 position = target.position - rotation * Vector3.forward * distance;

            // Raycast para evitar clipping
            if (Physics.Raycast(target.position + Vector3.up, position - target.position, out RaycastHit hit, distance, LayerMask.GetMask(""Default"")))
            {
                position = hit.point + hit.normal * 0.5f;
            }

            mainCamera.transform.position = position;
            mainCamera.transform.LookAt(target.position + Vector3.up * 1.5f);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void Shake(float intensity, float duration)
        {
            StartCoroutine(ShakeCoroutine(intensity, duration));
        }

        System.Collections.IEnumerator ShakeCoroutine(float intensity, float duration)
        {
            float elapsed = 0f;
            Vector3 originalPos = mainCamera.transform.localPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float shake = intensity * (1f - elapsed / duration);
                mainCamera.transform.localPosition = originalPos + UnityEngine.Random.insideUnitSphere * shake;
                yield return null;
            }

            mainCamera.transform.localPosition = originalPos;
        }
    }
}";
        }

        private string GetDamagePopupManagerScript()
        {
            return @"using UnityEngine;
using System.Collections.Generic;
using TOP.UI;

namespace TOP.Systems
{
    public class DamagePopupManager : MonoBehaviour
    {
        public static DamagePopupManager Instance { get; private set; }

        [SerializeField] private GameObject damagePopupPrefab;
        [SerializeField] private GameObject healPopupPrefab;
        [SerializeField] private Transform canvasTransform;
        [SerializeField] private int poolSize = 50;

        private Queue<GameObject> _damagePool = new Queue<GameObject>();
        private Queue<GameObject> _healPool = new Queue<GameObject>();

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            // Pre-warm pool
            for (int i = 0; i < poolSize; i++)
            {
                var dmg = Instantiate(damagePopupPrefab, canvasTransform);
                dmg.SetActive(false);
                _damagePool.Enqueue(dmg);

                var heal = Instantiate(healPopupPrefab, canvasTransform);
                heal.SetActive(false);
                _healPool.Enqueue(heal);
            }
        }

        public void ShowDamage(int damage, Vector3 worldPosition, bool isCritical)
        {
            GameObject popup;
            if (_damagePool.Count > 0)
            {
                popup = _damagePool.Dequeue();
            }
            else
            {
                popup = Instantiate(damagePopupPrefab, canvasTransform);
            }

            popup.SetActive(true);

            // Converter world position para screen position
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
            popup.transform.position = screenPos;

            var dmgPopup = popup.GetComponent<DamagePopup>();
            if (dmgPopup != null)
            {
                dmgPopup.Setup(damage, isCritical, false);
            }

            // Retornar ao pool apos animacao
            StartCoroutine(ReturnToPool(popup, _damagePool, 2f));
        }

        public void ShowHeal(int amount, Vector3 worldPosition)
        {
            GameObject popup;
            if (_healPool.Count > 0)
            {
                popup = _healPool.Dequeue();
            }
            else
            {
                popup = Instantiate(healPopupPrefab, canvasTransform);
            }

            popup.SetActive(true);
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
            popup.transform.position = screenPos;

            var dmgPopup = popup.GetComponent<DamagePopup>();
            if (dmgPopup != null)
            {
                dmgPopup.Setup(amount, false, true);
            }

            StartCoroutine(ReturnToPool(popup, _healPool, 2f));
        }

        System.Collections.IEnumerator ReturnToPool(GameObject obj, Queue<GameObject> pool, float delay)
        {
            yield return new WaitForSeconds(delay);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }
}";
        }

        private string GetLevelUpEffectManagerScript()
        {
            return @"using UnityEngine;
using System.Collections.Generic;

namespace TOP.Systems
{
    public class LevelUpEffectManager : MonoBehaviour
    {
        public static LevelUpEffectManager Instance { get; private set; }

        [SerializeField] private GameObject levelUpEffectPrefab;
        [SerializeField] private AudioClip levelUpSound;
        [SerializeField] private int poolSize = 10;

        private Queue<GameObject> _effectPool = new Queue<GameObject>();

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            for (int i = 0; i < poolSize; i++)
            {
                var effect = Instantiate(levelUpEffectPrefab);
                effect.SetActive(false);
                _effectPool.Enqueue(effect);
            }
        }

        public void PlayLevelUpEffect(Vector3 position)
        {
            GameObject effect;
            if (_effectPool.Count > 0)
            {
                effect = _effectPool.Dequeue();
            }
            else
            {
                effect = Instantiate(levelUpEffectPrefab);
            }

            effect.transform.position = position;
            effect.SetActive(true);

            // Audio
            if (levelUpSound != null)
                AudioSource.PlayClipAtPoint(levelUpSound, position);

            StartCoroutine(ReturnToPool(effect, 3f));
        }

        System.Collections.IEnumerator ReturnToPool(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            obj.SetActive(false);
            _effectPool.Enqueue(obj);
        }
    }
}";
        }

        private string GetSkillProjectileScript()
        {
            return @"using UnityEngine;
using Mirror;
using TOP.Core;

namespace TOP.Systems
{
    public class SkillProjectile : NetworkBehaviour
    {
        [SerializeField] private float speed = 20f;
        [SerializeField] private float maxLifetime = 5f;
        [SerializeField] private ParticleSystem trailEffect;
        [SerializeField] private ParticleSystem impactEffect;

        private SkillData _skillData;
        private Vector3 _targetPosition;
        private NetworkIdentity _target;
        private float _spawnTime;
        private bool _hasHit;

        [Server]
        public void Initialize(SkillData data, Vector3 targetPos, NetworkIdentity target)
        {
            _skillData = data;
            _targetPosition = targetPos;
            _target = target;
            _spawnTime = Time.time;

            // Direcionar para o alvo
            if (target != null)
            {
                _targetPosition = target.transform.position;
            }

            Vector3 direction = (_targetPosition - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        void Update()
        {
            if (!isServer) return;

            if (_hasHit || Time.time > _spawnTime + maxLifetime)
            {
                NetworkServer.Destroy(gameObject);
                return;
            }

            // Mover projectile
            transform.position += transform.forward * speed * Time.deltaTime;

            // Verificar colisao
            float distance = Vector3.Distance(transform.position, _targetPosition);
            if (distance < 1f)
            {
                Hit();
            }

            // Raycast para colisao
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, speed * Time.deltaTime))
            {
                if (hit.collider.gameObject != gameObject)
                {
                    Hit(hit.collider.gameObject);
                }
            }
        }

        [Server]
        void Hit(GameObject hitObject = null)
        {
            if (_hasHit) return;
            _hasHit = true;

            // Aplicar dano no alvo
            if (hitObject != null)
            {
                var targetStats = hitObject.GetComponent<TOP.Player.PlayerStats>();
                if (targetStats != null)
                {
                    int damage = _skillData.BaseDamage;
                    targetStats.TakeDamage(damage);

                    // Mostrar popup
                    var popupManager = Object.FindFirstObjectByType<DamagePopupManager>();
                    if (popupManager != null)
                    {
                        popupManager.ShowDamage(damage, hitObject.transform.position, false);
                    }
                }
            }

            // Efeito de impacto
            RpcPlayImpact(transform.position);

            NetworkServer.Destroy(gameObject);
        }

        [ClientRpc]
        void RpcPlayImpact(Vector3 position)
        {
            if (impactEffect != null)
            {
                var effect = Instantiate(impactEffect, position, Quaternion.identity);
                effect.Play();
                Destroy(effect.gameObject, 2f);
            }
        }

        void OnDestroy()
        {
            if (trailEffect != null)
                trailEffect.Stop();
        }
    }
}";
        }

        private string GetWorldItemScript()
        {
            return @"using UnityEngine;
using Mirror;
using TOP.Core;
using TOP.Inventory;

namespace TOP.Systems
{
    public class WorldItem : NetworkBehaviour
    {
        [SyncVar] public int ItemId;
        [SyncVar] public int Quantity;
        [SyncVar] public string OwnerId; // Quem dropou (protecao)

        [SerializeField] private float pickupRadius = 2f;
        [SerializeField] private float despawnTime = 300f; // 5 minutos
        [SerializeField] private GameObject modelContainer;
        [SerializeField] private ParticleSystem glowEffect;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.2f;

        private float _spawnTime;
        private Vector3 _startPosition;

        void Start()
        {
            _spawnTime = Time.time;
            _startPosition = transform.position;

            if (isServer)
            {
                SetupVisuals();
            }
        }

        void Update()
        {
            // Animacao de flutuacao
            float y = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = _startPosition + Vector3.up * y;
            transform.Rotate(Vector3.up, 50f * Time.deltaTime);

            if (isServer && Time.time > _spawnTime + despawnTime)
            {
                NetworkServer.Destroy(gameObject);
            }
        }

        [Server]
        void SetupVisuals()
        {
            var itemData = ItemDatabase.Instance?.GetItem(ItemId);
            if (itemData == null) return;

            // TODO: Instanciar modelo baseado no item
            // var model = Instantiate(itemData.ModelPrefab, modelContainer.transform);

            // Cor do glow baseado na raridade
            if (glowEffect != null)
            {
                var main = glowEffect.main;
                main.startColor = GetRarityColor(itemData);
            }

            RpcSetupVisuals(ItemId);
        }

        [ClientRpc]
        void RpcSetupVisuals(int itemId)
        {
            var itemData = ItemDatabase.Instance?.GetItem(itemId);
            if (itemData != null && glowEffect != null)
            {
                var main = glowEffect.main;
                main.startColor = GetRarityColor(itemData);
            }
        }

        Color GetRarityColor(ItemData item)
        {
            // TODO: Adicionar campo de raridade no ItemData
            return Color.white;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!isServer) return;

            var player = other.GetComponent<TOP.Player.PlayerInventory>();
            if (player != null)
            {
                // Verificar se pode pegar (protecao de drop)
                // TODO: Implementar timer de protecao

                // Adicionar ao inventario
                player.CmdAddItem(ItemId, Quantity);

                // Destruir world item
                NetworkServer.Destroy(gameObject);
            }
        }
    }
}";
        }

        private string GetWorldItemManagerScript()
        {
            return @"using UnityEngine;
using Mirror;
using System.Collections.Generic;

namespace TOP.Systems
{
    public class WorldItemManager : MonoBehaviour
    {
        public static WorldItemManager Instance { get; private set; }

        [SerializeField] private GameObject worldItemPrefab;
        [SerializeField] private int maxWorldItems = 500;

        private readonly List<GameObject> _activeItems = new List<GameObject>();

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        [Server]
        public void SpawnWorldItem(int itemId, int quantity, Vector3 position)
        {
            if (_activeItems.Count >= maxWorldItems)
            {
                // Remover item mais antigo
                var oldest = _activeItems[0];
                _activeItems.RemoveAt(0);
                NetworkServer.Destroy(oldest);
            }

            var itemObj = Instantiate(worldItemPrefab, position, Quaternion.identity);
            var worldItem = itemObj.GetComponent<WorldItem>();
            if (worldItem != null)
            {
                worldItem.ItemId = itemId;
                worldItem.Quantity = quantity;
            }

            NetworkServer.Spawn(itemObj);
            _activeItems.Add(itemObj);
        }

        [Server]
        public void ClearAllItems()
        {
            foreach (var item in _activeItems)
            {
                if (item != null)
                    NetworkServer.Destroy(item);
            }
            _activeItems.Clear();
        }
    }
}";
        }

        // =================================================================================
        // CREATE SCENES
        // =================================================================================
        private void CreateAllScenes()
        {
            string scenesPath = "Assets/Scenes";
            EnsureFolder(scenesPath);

            // Login Scene
            CreateScene(scenesPath, "LoginScene", scene =>
            {
                // Camera
                var cam = Camera.main;
                if (cam == null)
                {
                    var camObj = new GameObject("Main Camera");
                    camObj.AddComponent<Camera>();
                    camObj.tag = "MainCamera";
                }

                // Light
                var light = new GameObject("Directional Light");
                var dirLight = light.AddComponent<Light>();
                dirLight.type = LightType.Directional;
                dirLight.intensity = 1f;

                // Canvas
                var canvas = new GameObject("Canvas");
                var c = canvas.AddComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.AddComponent<CanvasScaler>();
                canvas.AddComponent<GraphicRaycaster>();

                // Event System
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

                // Login UI Manager
                var loginUI = new GameObject("LoginUIManager");
                loginUI.transform.SetParent(canvas.transform);
                var loginMgr = loginUI.AddComponent<TOP.UI.LoginUIManager>();

                // Criar panels basicos
                CreatePanel(canvas.transform, "LoginPanel", true);
                CreatePanel(canvas.transform, "RegisterPanel", false);
                CreatePanel(canvas.transform, "LoadingPanel", false);
                CreatePanel(canvas.transform, "ErrorPanel", false);

                // Background
                var bg = new GameObject("Background");
                var bgImg = bg.AddComponent<UnityEngine.UI.Image>();
                bgImg.color = new Color(0.1f, 0.1f, 0.2f, 1f);
                bg.transform.SetParent(canvas.transform);
                bg.transform.SetAsFirstSibling();
                var bgRect = bg.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;
            });

            // Character Select Scene
            CreateScene(scenesPath, "CharacterSelectScene", scene =>
            {
                var cam = Camera.main;
                if (cam == null)
                {
                    var camObj = new GameObject("Main Camera");
                    cam = camObj.AddComponent<Camera>();
                    camObj.tag = "MainCamera";
                }
                cam.transform.position = new Vector3(0, 5, -10);
                cam.transform.LookAt(Vector3.zero);

                var light = new GameObject("Directional Light");
                var dirLight = light.AddComponent<Light>();
                dirLight.type = LightType.Directional;

                var canvas = new GameObject("Canvas");
                var c = canvas.AddComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.AddComponent<CanvasScaler>();
                canvas.AddComponent<GraphicRaycaster>();

                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

                // Character Select UI
                var charSelectUI = new GameObject("CharacterSelectUIManager");
                charSelectUI.transform.SetParent(canvas.transform);
                charSelectUI.AddComponent<TOP.UI.CharacterSelectUIManager>();

                // Preview Spawn Point
                var previewPoint = new GameObject("PreviewSpawnPoint");
                previewPoint.transform.position = new Vector3(0, 0, 3);

                CreatePanel(canvas.transform, "CharacterListPanel", true);
                CreatePanel(canvas.transform, "CreateCharacterPanel", false);
                CreatePanel(canvas.transform, "LoadingPanel", false);
                CreatePanel(canvas.transform, "ErrorPanel", false);
            });

            // Game Scene
            CreateScene(scenesPath, "GameScene", scene =>
            {
                // Terrain / Ground
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.localScale = new Vector3(100, 1, 100);
                var groundRenderer = ground.GetComponent<Renderer>();
                if (groundRenderer != null)
                    groundRenderer.material.color = new Color(0.2f, 0.4f, 0.2f);

                // NavMesh Surface (requer pacote: com.unity.ai.navigation)
                var navMesh = ground.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
                navMesh.BuildNavMesh();

                // Camera
                var camObj = new GameObject("Main Camera");
                var cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                cam.transform.position = new Vector3(0, 10, -8);
                cam.transform.LookAt(Vector3.zero);
                camObj.AddComponent<TOP.Player.CameraFollow>();

                // Light
                var light = new GameObject("Directional Light");
                var dirLight = light.AddComponent<Light>();
                dirLight.type = LightType.Directional;
                dirLight.intensity = 1.2f;

                // Canvas (Game HUD)
                var canvas = new GameObject("Canvas");
                var c = canvas.AddComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.AddComponent<CanvasScaler>();
                canvas.AddComponent<GraphicRaycaster>();

                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

                // UI Manager
                var uiMgr = new GameObject("UIManager");
                uiMgr.transform.SetParent(canvas.transform);
                uiMgr.AddComponent<TOP.UI.UIManager>();

                // Systems
                var systems = new GameObject("Systems");
                systems.AddComponent<TOP.Systems.DamagePopupManager>();
                systems.AddComponent<TOP.Systems.LevelUpEffectManager>();
                systems.AddComponent<TOP.Systems.BuffManager>();
                systems.AddComponent<TOP.Systems.CameraController>();
                systems.AddComponent<TOP.Systems.WorldItemManager>();

                // Item Database
                var itemDb = new GameObject("ItemDatabase");
                itemDb.AddComponent<TOP.Inventory.ItemDatabase>();

                // Spawn Points
                var spawnPoints = new GameObject("SpawnPoints");
                for (int i = 0; i < 5; i++)
                {
                    var point = new GameObject($"SpawnPoint_{i}");
                    point.transform.SetParent(spawnPoints.transform);
                    point.transform.position = new Vector3(
                        Random.Range(-20f, 20f), 1f, Random.Range(-20f, 20f));
                }
            });

            Debug.Log("[TOP Setup] Scenes criadas com sucesso!");
        }

        void CreateScene(string path, string name, System.Action<Scene> setup)
        {
            string scenePath = $"{path}/{name}.unity";

            // Criar nova cena
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            setup(newScene);

            // Salvar
            EditorSceneManager.SaveScene(newScene, scenePath);
        }

        void CreatePanel(Transform parent, string name, bool active)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            var img = panel.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0, 0, 0, 0.8f);
            panel.SetActive(active);
        }

        // =================================================================================
        // CREATE PREFABS
        // =================================================================================
        private void CreateAllPrefabs()
        {
            string prefabsPath = "Assets/Prefabs";
            EnsureFolder(prefabsPath);
            EnsureFolder($"{prefabsPath}/Player");
            EnsureFolder($"{prefabsPath}/UI");
            EnsureFolder($"{prefabsPath}/Effects");
            EnsureFolder($"{prefabsPath}/Network");

            // Player Prefab
            var playerObj = new GameObject("Player");
            playerObj.tag = "Player";

            // Components
            playerObj.AddComponent<NetworkIdentity>();
            // CORRIGIDO: NetworkTransform compativel com Mirror atual
            var netTransform = playerObj.AddComponent<NetworkTransformReliable>();
            netTransform.syncDirection = SyncDirection.ServerToClient;
            netTransform.syncPosition = true;
            netTransform.syncRotation = true;

            // Player Controller
            playerObj.AddComponent<TOP.Gameplay.PlayerController>();

            // Movement
            var agent = playerObj.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.speed = 5f;
            agent.acceleration = 20f;
            playerObj.AddComponent<TOP.Player.PlayerMovement>();

            // Stats
            playerObj.AddComponent<TOP.Player.PlayerStats>();

            // Combat
            playerObj.AddComponent<TOP.Player.PlayerCombat>();
            playerObj.AddComponent<TOP.Player.PlayerDamageDetector>();

            // Inventory & Equipment
            playerObj.AddComponent<TOP.Player.PlayerInventory>();
            playerObj.AddComponent<TOP.Player.PlayerEquipment>();
            playerObj.AddComponent<TOP.Player.PlayerConsumables>();

            // Skills
            playerObj.AddComponent<TOP.Player.PlayerSkills>();

            // Animation
            playerObj.AddComponent<TOP.Player.PlayerAnimation>();

            // Class
            playerObj.AddComponent<TOP.Player.PlayerClass>();

            // Respawn
            playerObj.AddComponent<TOP.Player.PlayerRespawn>();

            // Hotbar
            playerObj.AddComponent<TOP.Player.PlayerHotbar>();

            // Visual
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Model";
            capsule.transform.SetParent(playerObj.transform);
            capsule.transform.localPosition = Vector3.up * 1f;
            capsule.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
            DestroyImmediate(capsule.GetComponent<Collider>());

            // Collider
            var col = playerObj.AddComponent<CapsuleCollider>();
            col.center = Vector3.up * 1f;
            col.height = 2f;
            col.radius = 0.5f;

            // Rigidbody
            var rb = playerObj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // Save Prefab
            PrefabUtility.SaveAsPrefabAsset(playerObj, $"{prefabsPath}/Player/Player.prefab");
            DestroyImmediate(playerObj);

            // Network Manager Prefab
            var nmObj = new GameObject("TOPNetworkManager");
            var nm = nmObj.AddComponent<TOP.Network.TOPNetworkManager>();
            nm.playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{prefabsPath}/Player/Player.prefab");
            nm.autoCreatePlayer = false;
            nm.spawnPrefabs.Add(AssetDatabase.LoadAssetAtPath<GameObject>($"{prefabsPath}/Player/Player.prefab"));

            // Transports
            // CORRIGIDO: Verifica se KCP existe, senao usa Telepathy
            var kcpType = System.Type.GetType("kcp2k.KcpTransport, kcp2k");
            if (kcpType != null)
            {
                nmObj.AddComponent(kcpType);
                Debug.Log("[TOP Setup] KCP Transport configurado");
            }
            else
            {
                var telepathyType = System.Type.GetType("Mirror.TelepathyTransport, Mirror");
                if (telepathyType != null)
                {
                    nmObj.AddComponent(telepathyType);
                    Debug.Log("[TOP Setup] Telepathy Transport configurado (KCP nao encontrado)");
                }
            }

            nmObj.AddComponent<DontDestroyOnLoad>();

            PrefabUtility.SaveAsPrefabAsset(nmObj, $"{prefabsPath}/Network/TOPNetworkManager.prefab");
            DestroyImmediate(nmObj);

            // UI Prefabs
            var slotPrefab = new GameObject("ItemSlot");
            var slotRect = slotPrefab.AddComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(60, 60);
            var slotImg = slotPrefab.AddComponent<UnityEngine.UI.Image>();
            slotImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            slotPrefab.AddComponent<TOP.Inventory.ItemSlotUI>();
            PrefabUtility.SaveAsPrefabAsset(slotPrefab, $"{prefabsPath}/UI/ItemSlot.prefab");
            DestroyImmediate(slotPrefab);

            var charCardPrefab = new GameObject("CharacterCard");
            var cardRect = charCardPrefab.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(200, 300);
            var cardImg = charCardPrefab.AddComponent<UnityEngine.UI.Image>();
            cardImg.color = new Color(0.15f, 0.15f, 0.25f, 0.9f);
            charCardPrefab.AddComponent<TOP.UI.CharacterCardUI>();
            PrefabUtility.SaveAsPrefabAsset(charCardPrefab, $"{prefabsPath}/UI/CharacterCard.prefab");
            DestroyImmediate(charCardPrefab);

            // World Item Prefab
            var worldItemObj = new GameObject("WorldItem");
            worldItemObj.AddComponent<NetworkIdentity>();
            var worldItemNetTransform = worldItemObj.AddComponent<NetworkTransformReliable>();
            worldItemNetTransform.syncDirection = SyncDirection.ServerToClient;
            worldItemObj.AddComponent<TOP.Systems.WorldItem>();

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Visual";
            sphere.transform.SetParent(worldItemObj.transform);
            sphere.transform.localScale = Vector3.one * 0.3f;
            DestroyImmediate(sphere.GetComponent<Collider>());

            var trigger = worldItemObj.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1f;

            PrefabUtility.SaveAsPrefabAsset(worldItemObj, $"{prefabsPath}/Effects/WorldItem.prefab");
            DestroyImmediate(worldItemObj);

            Debug.Log("[TOP Setup] Prefabs criados com sucesso!");
        }

        // =================================================================================
        // SETUP MIRROR
        // =================================================================================
        private void SetupMirrorNetwork()
        {
            // Verificar se Mirror esta instalado
            var mirrorType = System.Type.GetType("Mirror.NetworkManager, Mirror");
            if (mirrorType == null)
            {
                Debug.LogWarning("[TOP Setup] Mirror nao encontrado! Instale via Package Manager.");
                EditorUtility.DisplayDialog("Mirror Nao Encontrado",
                    "O package Mirror nao esta instalado.\n\n" +
                    "Instale via: Window > Package Manager > Add package from git URL\n" +
                    "URL: https://github.com/MirrorNetworking/Mirror.git", "OK");
                return;
            }

            // Configurar Network Manager na cena atual se houver
            var existingNM = Object.FindAnyObjectByType<TOP.Network.TOPNetworkManager>();
            if (existingNM == null)
            {
                Debug.Log("[TOP Setup] Arraste o prefab TOPNetworkManager para a cena.");
            }

            Debug.Log("[TOP Setup] Mirror configurado!");
        }
    }
}
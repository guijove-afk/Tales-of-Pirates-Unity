using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TOP.Services;
using TOP.Data;
using TOP.Gameplay;
using TOP.Core;
using TOP.Network;

namespace TOP.Network
{
    public class TOPNetworkManager : NetworkManager
    {
        public static TOPNetworkManager Instance { get; private set; }

        [Header("Tales of Pirates - Config")]
        [SerializeField] private float autoSaveInterval = 60f;
        [SerializeField] private string serverInstanceId = "server_01";

        [Header("Scenes")]
        [SerializeField] private string loginScene = "LoginScene";
        [SerializeField] private string characterSelectScene = "CharacterSelectScene";
        [SerializeField] private string gameScene = "GameScene";

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
            Debug.Log("[TOPNetworkManager] Servidor iniciado");

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

            Debug.Log("[TOPNetworkManager] Servidor fechando - salvando todos...");
            foreach (PlayerConnection playerConn in _connections.Values)
            {
                if (playerConn.State == ConnectionState.InGame && playerConn.PlayerController != null)
                {
                    _ = SavePlayerAsync(playerConn);
                }
            }
        }

        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            base.OnServerConnect(conn);

            PlayerConnection playerConn = new PlayerConnection
            {
                ConnectionId = conn.connectionId,
                State = ConnectionState.Login,
                Connection = conn,
                RateLimiter = new RateLimiter(50, 1f),
                ConnectTime = Time.time
            };

            _connections[conn.connectionId] = playerConn;
            _rateLimiters[conn.connectionId] = playerConn.RateLimiter;

            Debug.Log($"[Server] Cliente conectado: {conn.connectionId}");
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            if (_connections.TryGetValue(conn.connectionId, out PlayerConnection playerConn))
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
            Debug.Log($"[Server] Cliente desconectado: {conn.connectionId}");
        }

        bool CheckRateLimit(int connectionId)
        {
            if (_rateLimiters.TryGetValue(connectionId, out RateLimiter limiter))
                return limiter.CanProcess();
            return true;
        }

        async void OnLoginRequest(NetworkConnectionToClient conn, LoginRequest msg)
        {
            if (!CheckRateLimit(conn.connectionId))
            {
                conn.Send(new LoginResponse { Success = false, ErrorCode = "RATE_LIMITED" });
                return;
            }

            if (!_connections.TryGetValue(conn.connectionId, out PlayerConnection playerConn))
                return;

            if (playerConn.State != ConnectionState.Login)
            {
                conn.Send(new LoginResponse { Success = false, ErrorCode = "INVALID_STATE" });
                return;
            }

            Debug.Log($"[Auth] Login: {msg.Username}");

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

            string token = Guid.NewGuid().ToString("N");
            playerConn.SessionToken = token;

            conn.Send(new LoginResponse
            {
                Success = true,
                SessionToken = token,
                Message = "Login bem-sucedido"
            });

            Debug.Log($"[Auth] OK: {msg.Username} (ID: {accountId})");
        }

        async void OnCharacterListRequest(NetworkConnectionToClient conn, CharacterListRequest msg)
        {
            if (!CheckRateLimit(conn.connectionId)) return;

            if (!_connections.TryGetValue(conn.connectionId, out PlayerConnection playerConn))
                return;

            if (playerConn.State != ConnectionState.CharacterSelect)
            {
                conn.Send(new CharacterListResponse { Success = false, Error = "INVALID_STATE" });
                return;
            }

            List<CharacterPreviewData> characters = await DatabaseService.Instance.GetCharacterListAsync(playerConn.AccountId);

            conn.Send(new CharacterListResponse
            {
                Success = true,
                Characters = characters.ToArray()
            });
        }

        async void OnCreateCharacterRequest(NetworkConnectionToClient conn, CreateCharacterRequest msg)
        {
            if (!CheckRateLimit(conn.connectionId)) return;

            if (!_connections.TryGetValue(conn.connectionId, out PlayerConnection playerConn))
                return;

            if (playerConn.State != ConnectionState.CharacterSelect)
            {
                conn.Send(new CreateCharacterResponse { Success = false, Error = "INVALID_STATE" });
                return;
            }

            if (string.IsNullOrWhiteSpace(msg.Name) || msg.Name.Length < 3)
            {
                conn.Send(new CreateCharacterResponse { Success = false, Error = "NAME_TOO_SHORT" });
                return;
            }

            if (msg.SlotIndex > 2)
            {
                conn.Send(new CreateCharacterResponse { Success = false, Error = "INVALID_SLOT" });
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
                Debug.Log($"[CharCreate] {playerConn.Username} criou '{msg.Name}'");
        }

        async void OnDeleteCharacterRequest(NetworkConnectionToClient conn, DeleteCharacterRequest msg)
        {
            if (!CheckRateLimit(conn.connectionId)) return;

            if (!_connections.TryGetValue(conn.connectionId, out PlayerConnection playerConn))
                return;

            bool success = await DatabaseService.Instance.DeleteCharacterAsync(
                msg.CharacterId, playerConn.AccountId, msg.Password);

            conn.Send(new DeleteCharacterResponse
            {
                Success = success,
                Error = success ? null : "DELETE_FAILED"
            });
        }

        async void OnSelectCharacterRequest(NetworkConnectionToClient conn, SelectCharacterRequest msg)
        {
            if (!CheckRateLimit(conn.connectionId)) return;

            if (!_connections.TryGetValue(conn.connectionId, out PlayerConnection playerConn))
                return;

            if (playerConn.State != ConnectionState.CharacterSelect)
            {
                conn.Send(new SelectCharacterResponse { Success = false, Error = "INVALID_STATE" });
                return;
            }

            CharacterData charData = await DatabaseService.Instance.LoadCharacterAsync(
                msg.CharacterId, playerConn.AccountId);

            if (charData == null)
            {
                conn.Send(new SelectCharacterResponse { Success = false, Error = "CHARACTER_NOT_FOUND" });
                return;
            }

            playerConn.State = ConnectionState.InGame;
            playerConn.CharacterId = msg.CharacterId;

            GameObject playerObj = Instantiate(playerPrefab);

            PlayerController controller = playerObj.GetComponent<PlayerController>();
            if (controller == null)
            {
                Debug.LogError("[TOPNetworkManager] PlayerPrefab sem PlayerController!");
                Destroy(playerObj);
                conn.Send(new SelectCharacterResponse { Success = false, Error = "SERVER_ERROR" });
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
                playerConn.AccountId, msg.CharacterId, "ENTER_WORLD",
                new { map = charData.MapName, pos = new Vector3(charData.PosX, charData.PosY, charData.PosZ) },
                conn.address);

            Debug.Log($"[EnterWorld] {charData.Name} entrou em {charData.MapName}");
        }

        void OnMoveItemRequest(NetworkConnectionToClient conn, MoveItemRequest msg)
        {
            if (!CheckRateLimit(conn.connectionId)) return;
            if (conn.identity == null) return;

            PlayerInventory inv = conn.identity.GetComponent<PlayerInventory>();
            if (inv != null)
            {
                inv.CmdMoveItem(msg.FromSlot, msg.ToSlot);
            }
        }

        void OnEquipItemRequest(NetworkConnectionToClient conn, EquipItemRequest msg)
        {
            if (!CheckRateLimit(conn.connectionId)) return;
            if (conn.identity == null) return;

            PlayerEquipment equip = conn.identity.GetComponent<PlayerEquipment>();
            if (equip != null)
            {
                equip.CmdEquipItem(msg.InventorySlot, msg.TargetSlot);
            }
        }

        void OnDropItemRequest(NetworkConnectionToClient conn, DropItemRequest msg)
        {
            if (!CheckRateLimit(conn.connectionId)) return;
            if (conn.identity == null) return;

            PlayerInventory inv = conn.identity.GetComponent<PlayerInventory>();
            if (inv != null)
            {
                inv.CmdDropItem(msg.SlotIndex, msg.Quantity, msg.DropPosition);
            }
        }

        void OnUseItemRequest(NetworkConnectionToClient conn, UseItemRequest msg)
        {
            if (!CheckRateLimit(conn.connectionId)) return;
            if (conn.identity == null) return;

            PlayerConsumables consumables = conn.identity.GetComponent<PlayerConsumables>();
            if (consumables != null)
            {
                consumables.CmdUseItem(msg.SlotIndex);
            }
        }

        void OnChatMessage(NetworkConnectionToClient conn, ChatMessage msg)
        {
            if (!CheckRateLimit(conn.connectionId)) return;
            if (string.IsNullOrWhiteSpace(msg.Text) || msg.Text.Length > 200) return;

            NetworkServer.SendToAll(new ChatMessage
            {
                Channel = ChatChannel.World,
                Text = msg.Text
            });
        }

        async void AutoSaveAll()
        {
            foreach (PlayerConnection conn in _connections.Values)
            {
                if (conn.State == ConnectionState.InGame && conn.PlayerController != null)
                {
                    try { await SavePlayerAsync(conn); }
                    catch (Exception ex) { Debug.LogError($"[AutoSave] Erro: {ex}"); }
                }
            }
        }

        async System.Threading.Tasks.Task SavePlayerAsync(PlayerConnection playerConn)
        {
            if (playerConn?.PlayerController == null) return;

            CharacterData data = new CharacterData
            {
                Id = playerConn.PlayerController.CharacterId,
                AccountId = playerConn.PlayerController.AccountId,
                Name = playerConn.PlayerController.CharacterName,
                Job = playerConn.PlayerController.Job,
                Level = playerConn.PlayerController.Level,
                CurrentHp = playerConn.PlayerController.CurrentHp,
                CurrentMp = playerConn.PlayerController.CurrentMp,
                CurrentSp = playerConn.PlayerController.CurrentSp,
                PosX = playerConn.PlayerController.transform.position.x,
                PosY = playerConn.PlayerController.transform.position.y,
                PosZ = playerConn.PlayerController.transform.position.z,
                RotationY = playerConn.PlayerController.transform.rotation.eulerAngles.y
            };

            await DatabaseService.Instance.SaveCharacterAsync(data);
        }

        async System.Threading.Tasks.Task SaveAndDisconnectAsync(PlayerConnection playerConn)
        {
            if (playerConn?.PlayerController != null)
            {
                CharacterData data = new CharacterData
                {
                    Id = playerConn.PlayerController.CharacterId,
                    AccountId = playerConn.PlayerController.AccountId,
                    Name = playerConn.PlayerController.CharacterName,
                    PosX = playerConn.PlayerController.transform.position.x,
                    PosY = playerConn.PlayerController.transform.position.y,
                    PosZ = playerConn.PlayerController.transform.position.z,
                    RotationY = playerConn.PlayerController.transform.rotation.eulerAngles.y
                };
                await DatabaseService.Instance.SaveCharacterAsync(data);
            }

            _accountConnections.Remove(playerConn.AccountId);
        }

        void OnClientPing(NetworkConnectionToClient conn, ClientPing msg)
        {
            if (_connections.TryGetValue(conn.connectionId, out PlayerConnection playerConn))
                playerConn.LastPingTime = Time.time;

            conn.Send(new ServerPong
            {
                ClientTime = msg.ClientTime,
                ServerTime = Time.time
            });
        }

        public bool IsAccountOnline(long accountId) => _accountConnections.ContainsKey(accountId);
    }
}

// Assets/Scripts/Network/TOPNetworkManager.cs
using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TOP.Services;
using TOP.Data;
using TOP.Gameplay;

namespace TOP.Network
{
    public class TOPNetworkManager : NetworkManager
    {
        public static TOPNetworkManager Instance { get; private set; }

        [Header("Tales of Pirate - Config")]
        [SerializeField] private float autoSaveInterval = 60f;
        [SerializeField] private float sessionTimeout = 300f;
        [SerializeField] private string serverInstanceId = "server_01";

        [Header("Prefabs")]
        [SerializeField] private GameObject[] characterPreviewPrefabs;

        [Header("Scenes")]
        [Scene, SerializeField] private string loginScene = "LoginScene";
        [Scene, SerializeField] private string characterSelectScene = "CharacterSelect";
        [Scene, SerializeField] private string gameScene = "GameScene";

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
            NetworkServer.RegisterHandler<ClientPing>(OnClientPing);

            InvokeRepeating(nameof(AutoSaveAll), autoSaveInterval, autoSaveInterval);
            InvokeRepeating(nameof(CheckSessionTimeouts), 30f, 30f);
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            CancelInvoke();

            Debug.Log("[TOPNetworkManager] Servidor fechando - salvando todos...");
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
                RateLimiter = new RateLimiter(50, 1f)
            };

            _connections[conn.connectionId] = playerConn;
            _rateLimiters[conn.connectionId] = playerConn.RateLimiter;

            Debug.Log($"[Server] Cliente conectado: {conn.connectionId}");
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            if (_connections.TryGetValue(conn.connectionId, out var playerConn))
            {
                if (playerConn.State == ConnectionState.InGame && playerConn.PlayerController != null)
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
            if (_rateLimiters.TryGetValue(connectionId, out var limiter))
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

            if (!_connections.TryGetValue(conn.connectionId, out var playerConn))
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

            if (!_connections.TryGetValue(conn.connectionId, out var playerConn))
                return;

            if (playerConn.State != ConnectionState.CharacterSelect)
            {
                conn.Send(new CharacterListResponse { Success = false, Error = "INVALID_STATE" });
                return;
            }

            var characters = await DatabaseService.Instance.GetCharacterListAsync(playerConn.AccountId);

            conn.Send(new CharacterListResponse
            {
                Success = true,
                Characters = characters.ToArray()
            });
        }

        async void OnCreateCharacterRequest(NetworkConnectionToClient conn, CreateCharacterRequest msg)
        {
            if (!CheckRateLimit(conn.connectionId)) return;

            if (!_connections.TryGetValue(conn.connectionId, out var playerConn))
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

        async void OnSelectCharacterRequest(NetworkConnectionToClient conn, SelectCharacterRequest msg)
        {
            if (!CheckRateLimit(conn.connectionId)) return;

            if (!_connections.TryGetValue(conn.connectionId, out var playerConn))
                return;

            if (playerConn.State != ConnectionState.CharacterSelect)
            {
                conn.Send(new SelectCharacterResponse { Success = false, Error = "INVALID_STATE" });
                return;
            }

            var charData = await DatabaseService.Instance.LoadCharacterAsync(
                msg.CharacterId, playerConn.AccountId);

            if (charData == null)
            {
                conn.Send(new SelectCharacterResponse { Success = false, Error = "CHARACTER_NOT_FOUND" });
                return;
            }

            playerConn.State = ConnectionState.InGame;
            playerConn.CharacterId = msg.CharacterId;

            GameObject playerObj = Instantiate(playerPrefab);

            var controller = playerObj.GetComponent<PlayerController>();
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

        async void AutoSaveAll()
        {
            foreach (var conn in _connections.Values)
            {
                if (conn.State == ConnectionState.InGame && conn.PlayerController != null)
                {
                    try { await SavePlayerAsync(conn); }
                    catch (Exception ex) { Debug.LogError($"[AutoSave] Erro: {ex}"); }
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
                RotationY = conn.PlayerController.transform.rotation.eulerAngles.y,
                IsOnline = true
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
                    RotationY = conn.PlayerController.transform.rotation.eulerAngles.y,
                    IsOnline = false
                };
                await DatabaseService.Instance.SaveCharacterAsync(data);
            }

            _accountConnections.Remove(conn.AccountId);
        }

        void CheckSessionTimeouts()
        {
            // TODO: Verificar last_ping no banco e kick inativos
        }

        void OnClientPing(NetworkConnectionToClient conn, ClientPing msg)
        {
            if (_connections.TryGetValue(conn.connectionId, out var playerConn))
                playerConn.LastPingTime = Time.time;
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
}
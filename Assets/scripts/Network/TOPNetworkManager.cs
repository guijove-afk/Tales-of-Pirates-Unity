using UnityEngine;
using UnityEngine.AI;
using Mirror;
using UnityEngine.SceneManagement;

public class TOPNetworkManager : NetworkManager
{
    [Header("Tales of Pirates")]
    [SerializeField] private Transform[] spawnPoints;
   // [SerializeField] private GameObject playerPrefab;
    [SerializeField] private string gameScene = "GameScene";
    [SerializeField] private string loginScene = "LoginScene";

    [Header("Character Selection")]
    [SerializeField] private int maxCharactersPerAccount = 3;

    public static TOPNetworkManager Instance => singleton as TOPNetworkManager;
    /// <summary> Limite configurado por conta (para futura seleção de personagens / login ). </summary>
    public int MaxCharactersPerAccount => maxCharactersPerAccount;

    public override void OnStartServer()
    {
        base.OnStartServer();
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Transform spawnPoint = GetSpawnPoint();
        Vector3 pos = spawnPoint.position;
        Quaternion rot = spawnPoint.rotation;

        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 8f, NavMesh.AllAreas))
            pos = hit.position;
        else
            Debug.LogWarning($"[TOP NM] Ponto spawn {spawnPoint.position} pode estar FORA do NavMesh — jogador criado igual; mover pode falhar até haver Bake.", this);

        GameObject player = Instantiate(playerPrefab, pos, rot);
        Debug.Log($"[TOP NM] OnServerAddPlayer conn={conn.connectionId} jogador criado em {pos}", this);

        NetworkServer.AddPlayerForConnection(conn, player);
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (conn.identity != null)
        {
            if (conn.identity.TryGetComponent(out PlayerStats stats))
            {
                SavePlayerData(conn.identity);
            }
        }

        base.OnServerDisconnect(conn);
    }

    private Transform GetSpawnPoint()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
        }

        GameObject spawnObj = new GameObject("SpawnPoint");
        return spawnObj.transform;
    }

    private void SavePlayerData(NetworkIdentity player)
    {
        // Salvar no banco de dados
    }

    public void StartGame()
    {
        ServerChangeScene(gameScene);
    }

    public void ReturnToLogin()
    {
        ServerChangeScene(loginScene);
    }
}
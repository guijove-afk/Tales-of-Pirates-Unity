using UnityEngine;
using Mirror;
using TOP.Gameplay;

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
            Invoke(nameof(RespawnPlayer), respawnDelay);
        }

        [Server]
        void RespawnPlayer()
        {
            _isDead = false;

            Vector3 respawnPos = respawnPoints.Length > 0
                ? respawnPoints[UnityEngine.Random.Range(0, respawnPoints.Length)]
                : Vector3.zero;

            transform.position = respawnPos;
            _controller.RespawnPlayer();
        }
    }
}

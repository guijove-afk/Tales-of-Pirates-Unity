// Assets/Scripts/Services/SecurityLogService.cs
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TOP.Services
{
    /// <summary>
    /// Serviço de logs de segurança - registra ações suspeitas.
    /// 
    /// SEGURANÇA:
    /// - Logs async para não bloquear gameplay
    /// - Buffer em memória + flush periódico
    /// - Categorização por severidade
    /// </summary>
    public class SecurityLogService : MonoBehaviour
    {
        public static SecurityLogService Instance { get; private set; }
        
        [Header("Config")]
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
        
        /// <summary>
        /// Registra evento de segurança.
        /// </summary>
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
                string log = $"[SECURITY] [{severity}] {message} (Char:{characterId}, Acc:{accountId})";
                switch (severity)
                {
                    case LogSeverity.Info: Debug.Log(log); break;
                    case LogSeverity.Warning: Debug.LogWarning(log); break;
                    case LogSeverity.Critical: Debug.LogError(log); break;
                }
            }
            
            // Flush imediato para eventos críticos
            if (severity == LogSeverity.Critical)
                FlushBuffer();
            
            // Flush se buffer cheio
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
                        "SECURITY",
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
                Debug.LogError($"[SecurityLog] Falha ao flush: {ex.Message}");
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
}
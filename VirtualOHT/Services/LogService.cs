using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualOHT.Interfaces;

namespace VirtualOHT.Services
{
    public class LogService : ILogManager
    {
        #region FIELDS
        private readonly Dictionary<string, Action<string>?> _logUpdatedEvents = new();
        private readonly string _logDirectory;
        private static readonly object _logLock = new object();
        #endregion

        #region PROPERTIES
        public string LogDataTime { get; set; }
        #endregion

        #region CONSTRUCTOR
        /// <summary>
        /// Chamber, Event 등 실행 로그를 저장하는 서비스 레이어
        /// </summary>
        /// <param name="logDirectory"></param>
        public LogService(string logDirectory)
        {
            _logDirectory = logDirectory;
        }
        #endregion

        #region COMMAND
        #endregion

        #region METHOD
        /// <summary>
        /// 로그 기록 (날짜별 파일 자동 생성)
        /// </summary>
        public void WriteLog(string messagetype, string message)
        {
            string filePath = GetLogPath();

            string logLine = $"{DateTime.Now:HH:mm:ss} {messagetype} ▶ {message}";

            lock (_logLock)
            {
                File.AppendAllText(filePath, logLine + Environment.NewLine);

                // 파일 전체 내용을 읽어서 이벤트 발행 (실시간 뷰 갱신용)
                string newContent = File.ReadAllText(filePath);
                if (_logUpdatedEvents.ContainsKey("VirtualOHT"))
                    _logUpdatedEvents["VirtualOHT"]?.Invoke(newContent);
            }
        }

        /// <summary>
        /// 로그 기록 (날짜별 파일 자동 생성)
        /// </summary>
        public void LogHostToEquip(string signal)
        {
            string filePath = GetLogPath();

            string logLine = $"[{DateTime.Now:HH:mm:ss}] [H] ->  [E] : {signal}";

            lock (_logLock)
            {
                File.AppendAllText(filePath, logLine + Environment.NewLine);

                // 파일 전체 내용을 읽어서 이벤트 발행 (실시간 뷰 갱신용)
                string newContent = File.ReadAllText(filePath);
                if (_logUpdatedEvents.ContainsKey("VirtualOHT"))
                    _logUpdatedEvents["VirtualOHT"]?.Invoke(newContent);
            }
        }

        public void LogEquipToHost(string signal)
        {
            string filePath = GetLogPath();

            string logLine = $"[{DateTime.Now:HH:mm:ss}] [E] ->  [H] : {signal}";

            lock (_logLock)
            {
                File.AppendAllText(filePath, logLine + Environment.NewLine);

                // 파일 전체 내용을 읽어서 이벤트 발행 (실시간 뷰 갱신용)
                string newContent = File.ReadAllText(filePath);
                if (_logUpdatedEvents.ContainsKey("VirtualOHT"))
                    _logUpdatedEvents["VirtualOHT"]?.Invoke(newContent);
            }
        }

        /// <summary>
        /// 로그 구독 (실시간 뷰 갱신용)
        /// </summary>
        public void Subscribe(Action<string> handler)
        {
            if (!_logUpdatedEvents.ContainsKey("VirtualOHT"))
                _logUpdatedEvents["VirtualOHT"] = null;
            _logUpdatedEvents["VirtualOHT"] += handler;
        }

        /// <summary>
        /// 시간별 로그 파일 경로 반환
        /// </summary>
        public string GetLogPath()
        {
            string processlogDir = Path.Combine(_logDirectory, "VirtualOHT");
            if (!Directory.Exists(processlogDir))
                Directory.CreateDirectory(processlogDir);

            string fileName = $"VirtualOHT_{LogDataTime}.log";
            return Path.Combine(processlogDir, fileName);
        }

        /// <summary>
        /// 현재 시간에 해당하는 폴더 경로 확인 및 생성
        /// </summary>
        /// <param name="time"></param>
        public void SetTime(string time)
        {
            this.LogDataTime = time;
        }
        #endregion


    }
}

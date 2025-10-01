using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualOHT.Interfaces
{
    public partial interface ILogManager
    {
        #region PROPERTIES
        string LogDataTime { get; set; }
        #endregion

        #region METHODS
        void LogHostToEquip(string signal);
        void LogEquipToHost(string signal);
        void WriteLog(string messagetype, string message);

        void Subscribe(Action<string> handler);

        string GetLogPath();

        void SetTime(string time);
        #endregion

        #region EVENTS
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualOHT.Models;
using static VirtualOHT.EnumList.PIOSignalEnum;

namespace VirtualOHT.Interfaces
{
    public interface ICommManager
    {

        bool IsConnected { get; }

        void SetWafer(List<int> slots);
        Task SendSignalAsync(byte action, params byte[] signals);
        void Connect();
        void Disconnect();
        void SetLoadPort(string LoadPort);

        event Action<PioSignalItem>? SignalReceived;
        event Action SignalEnd;
        event Action ClientChange;
    }
}

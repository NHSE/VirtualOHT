using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualOHT.Models
{
    public partial class PioSignalItem : ObservableObject
    {
        [ObservableProperty]
        public bool _l_REQ = false;

        [ObservableProperty]
        public bool _rEADY = false;

        [ObservableProperty]
        public bool _cS_0 = false;

        [ObservableProperty]
        public bool _cS_1 = false;

        [ObservableProperty]
        public bool _vALID = false;

        [ObservableProperty]
        public bool _tR_REQ = false;

        [ObservableProperty]
        public bool _bUSY = false;

        [ObservableProperty]
        public bool _cOMPT = false;

        [ObservableProperty]
        public bool _cONT = true;

        [ObservableProperty]
        public bool _hO_AVBL = true;

        [ObservableProperty]
        public bool _eS = true;
    }

    public class SignalHistory
    {
        public bool CurrentState { get; set; }  // ON/OFF 현재 상태
        public bool EventTriggered { get; set; } // 이번 Tick에 이벤트 발생했는지
    }
}

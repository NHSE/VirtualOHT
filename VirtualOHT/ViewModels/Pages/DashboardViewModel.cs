using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using VirtualOHT.Interfaces;
using VirtualOHT.Models;
using static VirtualOHT.EnumList.PIOSignalEnum;

namespace VirtualOHT.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        #region FIELDS
        public event EventHandler<Wafer> RemoveRequested;
        public event EventHandler<Wafer> AddRequested;
        private readonly ICommManager _commManager;
        private readonly ILogManager _logManager;

        private FileSystemWatcher _logFileWatcher;
        private long lastLogPosition = 0;
        private const int MaxSamples = 200;

        public Dictionary<int, SignalHistory> histories = new();
        #endregion

        #region PROPERTIES
        [ObservableProperty]
        private ObservableCollection<PioSignalItem> _signals = new();

        [ObservableProperty]
        private string? _carrierId;

        [ObservableProperty]
        public List<int> _selectedSlots = new();

        [ObservableProperty]
        private bool _isSetupEnabled = true;

        [ObservableProperty]
        private bool _isCancelEnabled = false;

        [ObservableProperty]
        private bool _isLoadEnabled = true;

        [ObservableProperty]
        private bool _isUnLoadEnabled = true;

        [ObservableProperty]
        private string? _lPState = "Ready";

        [ObservableProperty]
        private bool _isConnectEnabled = true;

        [ObservableProperty]
        private bool _isDisConnectEnabled = false;

        [ObservableProperty]
        private string? _logText;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private bool _isConnected = false;

        [ObservableProperty]
        private bool _isComboBox = true;

        [ObservableProperty]
        private string[] _loadPort = new string[] {"LoadPort 1", "LoadPort 2" };

        [ObservableProperty]
        private string _selectedLoadPort = "LoadPort 1";

        [RelayCommand]
        private void Cancel()
        {
            IsCancelEnabled = false;
            IsSetupEnabled = true;
        }

        public ICommand ConnectCommand => new RelayCommand(() =>
        {
            IsDisConnectEnabled = true;
            IsConnectEnabled = false;
            _commManager.Connect();
        });
        public ICommand DisconnectCommand => new RelayCommand(() =>
        {
            IsDisConnectEnabled = false;
            IsConnectEnabled = true;
            _commManager.Disconnect();
        });

        [RelayCommand]
        private void Load()
        {
            if (IsConnected)
            {
                if(SelectedSlots.Count() == 0)
                {
                    this._logManager.WriteLog("Error", "Not Wafer");
                    return;
                }

                Start_State_Property();

                this._commManager.SendSignalAsync((byte)TransferAction.Load, (byte)PioSignal.VALID);
            }
            else
            {
                this._logManager.WriteLog("Error", "Not connected");
            }
        }

        [RelayCommand]
        private void UnLoad()
        {
            if (IsConnected)
            {
                Start_State_Property();

                this._commManager.SendSignalAsync((byte)TransferAction.UnLoad, (byte)PioSignal.VALID);
            }
            else
            {
                this._logManager.WriteLog("Error", "Not connected");
            }
        }

        public DashboardViewModel(ICommManager commManager, ILogManager logManager)
        {
            this._commManager = commManager;
            this._logManager = logManager;

            this._commManager.ClientChange += OnClientChange;
            this._commManager.SignalReceived += OnSignalReceived;
            this._commManager.SignalEnd += SignalEnd;

            this._logManager.SetTime(DateTime.Now.ToString("yyyyMMddss_HHmmss"));

            this._logManager.Subscribe(OnLogUpdated);

            LoadInitialLogs();
            SetupLogFileWatcher();
        }

        private void OnClientChange()
        {
            this.IsConnected = this._commManager.IsConnected;
        }

        private void Start_State_Property()
        {
            IsRunning = true;
            IsLoadEnabled = false;
            IsUnLoadEnabled = false;
            IsCancelEnabled = false;
            IsComboBox = false;
            IsDisConnectEnabled = false;
            IsConnectEnabled = false;
        }

        private void End_State_Property()
        {
            IsLoadEnabled = true;
            IsUnLoadEnabled = true;
            IsCancelEnabled = true;
            IsComboBox = true;
            IsRunning = false;
            IsDisConnectEnabled = true;
            IsConnectEnabled = false;
        }

        private void SignalEnd()
        {
            End_State_Property();
        }

        private void OnSignalReceived(PioSignalItem signal)
        {
            for (int s = 0; s < 11; s++)
            {
                bool val = s switch
                {
                    0 => signal.L_REQ,
                    1 => signal.U_REQ,
                    2 => signal.READY,
                    3 => signal.CS_0,
                    4 => signal.CS_1,
                    5 => signal.VALID,
                    6 => signal.TR_REQ,
                    7 => signal.BUSY,
                    8 => signal.COMPT,
                    9 => signal.CONT,
                    10 => signal.HO_AVBL,
                    11 => signal.ES,
                    _ => false
                };

                if (!histories.ContainsKey(s))
                    histories[s] = new SignalHistory();

                var h = histories[s];
                if (val && !h.CurrentState)
                    h.EventTriggered = true;

                h.CurrentState = val;
            }
        }

        public void TickSignals()
        {
            var newItem = new PioSignalItem();

            for (int s = 0; s < 11; s++)
            {
                if (!histories.ContainsKey(s)) continue;
                var h = histories[s];
                bool val = h.CurrentState;

                // 단순히 상태 기록
                switch (s)
                {
                    case 0: newItem.L_REQ = val; break;
                    case 1: newItem.U_REQ = val; break;
                    case 2: newItem.READY = val; break;
                    case 3: newItem.CS_0 = val; break;
                    case 4: newItem.CS_1 = val; break;
                    case 5: newItem.VALID = val; break;
                    case 6: newItem.TR_REQ = val; break;
                    case 7: newItem.BUSY = val; break;
                    case 8: newItem.COMPT = val; break;
                    case 9: newItem.CONT = val; break;
                    case 10: newItem.HO_AVBL = val; break;
                    case 11: newItem.ES = val; break;
                }

                h.EventTriggered = false;
            }

            Signals.Add(newItem);
            if (Signals.Count > MaxSamples)
                Signals.RemoveAt(0);
        }

        partial void OnSelectedLoadPortChanged(string value)
        {
            this._commManager.SetLoadPort(value);
        }

        partial void OnSelectedSlotsChanged(List<int> value)
        {
            this._commManager.SetWafer(value);
        }

        /// <summary>
        /// Log 파일 업데이트 시 변수 저장 메서드
        /// </summary>
        /// <param name="newLog"></param>
        private void OnLogUpdated(string newLog)
        {
            // UI 스레드에서 속성 갱신
            App.Current.Dispatcher.Invoke(() => this.LogText = newLog);
        }

        /// <summary>
        /// Log 파일 읽기 설정 메서드
        /// </summary>
        private void SetupLogFileWatcher()
        {
            var logDirectory = @"C:\Logs\VirtualOHT";
            var logFileName = $"VirtualOHT_{DateTime.Now:yyyyMMdd}_{DateTime.Now:HHmmss}.log";

            _logFileWatcher = new FileSystemWatcher
            {
                Path = logDirectory,
                Filter = logFileName,
                NotifyFilter = NotifyFilters.LastWrite
            };

            _logFileWatcher.Changed += OnLogFileChanged;
            _logFileWatcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Log 파일 읽기 메서드
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLogFileChanged(object sender, FileSystemEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    using (var fs = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        fs.Seek(lastLogPosition, SeekOrigin.Begin);
                        using (var reader = new StreamReader(fs))
                        {
                            string newText = reader.ReadToEnd();
                            if (!string.IsNullOrEmpty(newText))
                            {
                                LogText += newText;
                            }
                            lastLogPosition = fs.Position;
                        }
                    }
                }
                catch (IOException)
                {
                    // 파일이 잠겨있을 수 있으니 예외 무시 또는 재시도 로직 추가 가능
                }
            });
        }

        /// <summary>
        /// Log 파일 설정 메서드
        /// </summary>
        private void LoadInitialLogs()
        {
            var logPath = Path.Combine(@"C:\Logs\VirtualOHT", $"VirtualOHT_{DateTime.Now:yyyyMMdd}_{DateTime.Now:HHmmss}.log");
            if (File.Exists(logPath))
            {
                LogText = File.ReadAllText(logPath);
            }
        }
        #endregion
    }
}

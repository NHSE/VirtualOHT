using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using VirtualOHT.EnumList;
using VirtualOHT.Interfaces;
using VirtualOHT.Models;
using static VirtualOHT.EnumList.PIOSignalEnum;

namespace VirtualOHT.Services
{
    public class CommunicationService : ICommManager
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;

        private readonly ILogManager _logManager;

        private List<int> _slots = new();
        private PioSignalItem signal = new();

        public event Action<PioSignalItem>? SignalReceived;
        public event Action SignalEnd;
        public event Action ClientChange;
        public bool IsConnected { get; private set; }

        public CommunicationService(ILogManager logManager)
        {
            _logManager = logManager;
        }

        public void Connect()
        {
            try
            {
                _client = new TcpClient("127.0.0.1", 503);
                if (_client.Connected)
                {
                    _stream = _client.GetStream();
                    IsConnected = true;
                    _logManager.WriteLog("State", "TCP/IP Connected!!!");
                    ClientChange?.Invoke();

                    _cts = new CancellationTokenSource();
                    Task.Run(() => ReceiveLoop(_cts.Token));
                }
            }
            catch
            {
                IsConnected = false;
                ClientChange?.Invoke();
                _logManager.WriteLog("State", "TCP/IP Connect Failed!!!");
            }
        }

        public void Disconnect()
        {
            _cts?.Cancel();
            _stream?.Close();
            _client?.Close();
            _client = null;
            IsConnected = false;

            ClientChange?.Invoke();
        }

        public void SetWafer(List<int> slots) => _slots = slots.ToList();

        public async Task SendSignalAsync(byte action, params byte[] signals)
        {
            if (_stream == null) return;
            await Task.Delay(200);

            signal.CS_0 = true;
            SignalReceived?.Invoke(signal);

            signal.VALID = true;
            SignalReceived?.Invoke(signal);
            var data = new List<byte> { action };
            data.AddRange(signals);
            await _stream.WriteAsync(data.ToArray(), 0, data.Count);
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            try
            {
                if (_stream == null) return;
                var buffer = new byte[1024];

                bool BUSY = false, VALID = false, TR_REQ = false, COMPT = false;

                while (!token.IsCancellationRequested && _client?.Connected == true)
                {
                    int n = await _stream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (n == 0) continue;

                    byte action = buffer[0];
                    byte received = buffer[1];

                    // READY 처리
                    if (received == (byte)PioSignal.READY && !BUSY && !COMPT)
                    {
                        signal.READY = true;
                        SignalReceived?.Invoke(signal);
                        await Task.Delay(200); // 샘플링 딜레이

                        BUSY = true;
                        signal.BUSY = true;
                        _logManager.LogEquipToHost("READY");

                        var response = new List<byte>
                    {
                        (byte)PioSignal.BUSY, 0x01 // LP
                    };
                        foreach (var wafer in _slots)
                            response.Add((byte)wafer);

                        // 대응 신호 보내기
                        await SendSignalAsync(action, response.ToArray());
                    }

                    else if (received == (byte)PioSignal.L_REQ && !BUSY)
                    {
                        await Task.Delay(200); // 샘플링 딜레이
                        signal.L_REQ = true;
                        SignalReceived?.Invoke(signal);
                        await Task.Delay(200); // 샘플링 딜레이

                        TR_REQ = true;
                        signal.TR_REQ = true;
                        _logManager.LogEquipToHost("L_REQ");

                        await SendSignalAsync(action, (byte)PioSignal.READY);
                        _logManager.LogHostToEquip("READY");
                    }
                    // L_REQ 처리 (BUSY ON → COMPT)
                    else if (received == (byte)PioSignal.L_REQ && BUSY)
                    {
                        signal.L_REQ = false;
                        SignalReceived?.Invoke(signal);
                        await Task.Delay(200); // 샘플링 딜레이

                        BUSY = false;
                        TR_REQ = false;
                        COMPT = true;
                        signal.BUSY = false;
                        signal.TR_REQ = false;
                        signal.COMPT = true;
                        _logManager.LogEquipToHost("L_REQ");

                        await SendSignalAsync(action, (byte)PioSignal.COMPT);
                        _logManager.LogHostToEquip("COMPT");
                    }
                    // READY 처리 (COMPT OFF)
                    else if (received == (byte)PioSignal.READY && COMPT)
                    {
                        VALID = false;
                        COMPT = false;

                        signal.READY = false;
                        SignalReceived?.Invoke(signal);
                        await Task.Delay(200); // 샘플링 딜레이

                        signal.VALID = false;
                        signal.COMPT = false;
                        signal.CS_0 = false;
                        _logManager.LogEquipToHost("READY");
                    }

                    // 뷰모델/뷰에 상태 전달
                    SignalReceived?.Invoke(signal);

                    if (!VALID && !COMPT && !TR_REQ && !BUSY)
                    {
                        SignalEnd?.Invoke();
                    }

                    await Task.Delay(200); // 샘플링 딜레이
                }
            }
            catch (Exception ex)
            {
                _logManager.WriteLog("State", "TCP/IP Connect Failed!!!");
                IsConnected = false;
                ClientChange?.Invoke();
            }
        }
    }
}

using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using VirtualOHT.Interfaces;
using VirtualOHT.Models;
using VirtualOHT.ViewModels.Pages;
using VirtualOHT.Views.Windows;
using Wpf.Ui.Abstractions.Controls;

namespace VirtualOHT.Views.Pages
{
    public partial class DashboardPage : INavigableView<DashboardViewModel>
    {
        public DashboardViewModel ViewModel { get; }
        private Dictionary<int, Line> _slotLines = new();
        private readonly DispatcherTimer _timer;

        private Brush[] signalColors = new Brush[]
        {
            Brushes.LimeGreen, // L_REQ
            Brushes.Cyan,      // READY
            Brushes.Orange,    // CS_0
            Brushes.Magenta,   // CS_1
            Brushes.Yellow,    // VALID
            Brushes.Red,       // TR_REQ
            Brushes.LightGreen,// BUSY
            Brushes.LightBlue, // COMPT
            Brushes.Gray,      // CONT
            Brushes.White,     // HO_AVBL
            Brushes.Pink       // ES
        };

        public DashboardPage(DashboardViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _timer.Tick += SignalTimer_Tick;

            ViewModel.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName == nameof(ViewModel.IsRunning))
                {
                    if (!ViewModel.IsRunning)
                    {
                        await Task.Delay(300);  // 0.3초 지연
                        Application.Current.Dispatcher.Invoke(() => _timer.Stop());
                    }
                    else
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            signalCanvas.Children.Clear();
                            ViewModel.Signals.Clear();
                            _timer.Start();
                        });
                    }
                }
                else if (e.PropertyName == nameof(ViewModel.IsConnected))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (ConnectionIndicator.Fill is SolidColorBrush brush && brush.Color == Colors.Gray)
                        {
                            ConnectionIndicator.Fill = new SolidColorBrush(Colors.LightGreen);
                        }
                        else
                        {
                            ConnectionIndicator.Fill = new SolidColorBrush(Colors.Gray);
                        }
                    });
                }
            };

            InitializeComponent();
        }

        private void btnSetup_Click(object sender, RoutedEventArgs e) // command로 빼기
        {
            var carrierSetupWindow = new CarrierSetupWindow();
            var result = carrierSetupWindow.ShowDialog();

            if (result == true)
            {
                var selected = carrierSetupWindow.SelectedWaferSlots;
                // 원하는 방식으로 전달
                ViewModel.SelectedSlots = selected;
                DrawLinesBasedOnSelectedSlots(selected);
                ViewModel.IsSetupEnabled = false;   // Setup 비활성
                ViewModel.IsCancelEnabled = true;   // Cancel 활성
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            mainCanvas.Children.Clear();
        }

        private void SignalTimer_Tick(object sender, EventArgs e)
        {
            ViewModel.TickSignals();
            if (Application.Current.Dispatcher.CheckAccess())
            {
                DrawSignals();
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DrawSignals();
                });
            }
        }

        private readonly string[] signalNames = new string[]
        {
            "L_REQ", "READY", "CS_0", "CS_1", "VALID",
            "TR_REQ", "BUSY", "COMPT", "CONT", "HO_AVBL", "ES"
        };

        private void DrawSignals()
        {
            signalCanvas.Children.Clear();

            double width = signalCanvas.ActualWidth;
            double heightPerSignal = signalCanvas.ActualHeight / 11;
            int count = ViewModel.Signals.Count;
            if (count == 0) return;

            double leftMargin = 50; // 이름 표시 공간 확보
            double xStep = (width - leftMargin) / count;

            for (int s = 0; s < 11; s++)
            {
                // 라인 구분선
                var separator = new Line
                {
                    X1 = leftMargin,
                    X2 = width,
                    Y1 = s * heightPerSignal,
                    Y2 = s * heightPerSignal,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 0.5
                };
                signalCanvas.Children.Add(separator);

                // 라인 이름 표시
                var text = new TextBlock
                {
                    Text = signalNames[s],
                    Foreground = Brushes.White,
                    FontSize = 12
                };
                Canvas.SetLeft(text, 0);
                Canvas.SetTop(text, s * heightPerSignal + heightPerSignal / 4 - 6); // 중앙 정렬
                signalCanvas.Children.Add(text);

                // 신호 라인
                Polyline line = new() { Stroke = signalColors[s], StrokeThickness = 2 };
                bool prevVal = false;
                double prevY = s * heightPerSignal + heightPerSignal * 3 / 4;

                for (int i = 0; i < count; i++)
                {
                    bool val = s switch
                    {
                        0 => ViewModel.Signals[i]._l_REQ,
                        1 => ViewModel.Signals[i]._rEADY,
                        2 => ViewModel.Signals[i]._cS_0,
                        3 => ViewModel.Signals[i]._cS_1,
                        4 => ViewModel.Signals[i]._vALID,
                        5 => ViewModel.Signals[i]._tR_REQ,
                        6 => ViewModel.Signals[i]._bUSY,
                        7 => ViewModel.Signals[i]._cOMPT,
                        8 => ViewModel.Signals[i]._cONT,
                        9 => ViewModel.Signals[i]._hO_AVBL,
                        10 => ViewModel.Signals[i]._eS,
                        _ => false
                    };

                    double y = val ? s * heightPerSignal + heightPerSignal / 4
                                   : s * heightPerSignal + heightPerSignal * 3 / 4;

                    if (i > 0 && val != prevVal)
                    {
                        line.Points.Add(new Point(leftMargin + i * xStep, prevY));
                        line.Points.Add(new Point(leftMargin + i * xStep, y));
                    }

                    line.Points.Add(new Point(leftMargin + i * xStep + xStep, y));

                    prevVal = val;
                    prevY = y;
                }

                signalCanvas.Children.Add(line);
            }
        }

        private void DrawLinesBasedOnSelectedSlots(List<int> selectedSlots)
        {
            mainCanvas.Children.Clear();

            // 선택된 슬롯에 따라 라인 추가
            double TopY = 90;
            double BottomY = 350;
            double XStart = 50;
            double XEnd = 270;
            int SlotCount = 25;
            double GapY = (BottomY - TopY) / (SlotCount - 1);

            foreach (int slotIndex in selectedSlots)
            {
                if (slotIndex < 0 || slotIndex > SlotCount) continue;

                double y = BottomY - slotIndex * GapY;
                var line = new Line
                {
                    X1 = XStart,
                    X2 = XEnd,
                    Y1 = y,
                    Y2 = y,
                    Stroke = Brushes.White,
                    StrokeThickness = 6
                };

                _slotLines[slotIndex] = line;
                mainCanvas.Children.Add(line);
            }
        }
    }
}

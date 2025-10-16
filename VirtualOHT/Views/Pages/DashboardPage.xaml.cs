using System.Collections.ObjectModel;
using System.Reflection.Metadata;
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
    public partial class DashboardPage
    {
        public DashboardViewModel ViewModel { get; }
        private Dictionary<int, Line> _slotLines = new();
        private readonly DispatcherTimer _timer;

        private Brush[] signalColors = new Brush[]
        {
            Brushes.LimeGreen, // L_REQ
            Brushes.LimeGreen, // U_REQ
            Brushes.Cyan,      // READY
            Brushes.Orange,    // CS_0
            Brushes.Orange,   // CS_1
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

            InitializeComponent();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
            _timer.Tick += SignalTimer_Tick;

            ViewModel.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName == nameof(ViewModel.IsRunning))
                {
                    if (Application.Current.Dispatcher.CheckAccess())
                    {
                        if (!ViewModel.IsRunning)
                        {
                            await Task.Delay(500);
                            _timer.Stop();
                            tblog.ScrollToEnd();
                        }
                        else
                        {
                            signalCanvas.Children.Clear();
                            ViewModel.Signals.Clear();
                            _timer.Start();
                        }
                    }
                    else
                    {
                        await Application.Current.Dispatcher.BeginInvoke(async () =>
                        {
                            if (!ViewModel.IsRunning)
                            {
                                await Task.Delay(500);
                                _timer.Stop();
                                tblog.ScrollToEnd();
                            }
                            else
                            {
                                signalCanvas.Children.Clear();
                                ViewModel.Signals.Clear();
                                _timer.Start();
                            }
                        });
                    }
                }
                else if (e.PropertyName == nameof(ViewModel.IsConnected))
                {
                    if (Application.Current.Dispatcher.CheckAccess())
                    {
                        if (ConnectionIndicator.Fill is SolidColorBrush brush && brush.Color == Colors.Gray)
                        {
                            ConnectionIndicator.Fill = new SolidColorBrush(Colors.LightGreen);
                        }
                        else
                        {
                            ConnectionIndicator.Fill = new SolidColorBrush(Colors.Gray);
                        }
                    }
                    else
                    {
                        await Application.Current.Dispatcher.BeginInvoke(() =>
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
                }
            };
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
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    DrawSignals();
                });
            }
        }

        private readonly string[] signalNames = new string[]
        {
            "L_REQ", "U_REQ", "READY", "CS_0", "CS_1", "VALID",
            "TR_REQ", "BUSY", "COMPT", "CONT", "HO_AVBL", "ES"
        };

        private void DrawSignals()
        {
            signalCanvas.Children.Clear();

            double width = signalCanvas.ActualWidth;
            double heightPerSignal = signalCanvas.ActualHeight / signalNames.Length;
            int count = ViewModel.Signals.Count;
            if (count == 0) return;

            double leftMargin = 50;
            double xStep = (width - leftMargin) / count;

            for (int s = 0; s < signalNames.Length; s++)
            {
                // 라인 구분선
                signalCanvas.Children.Add(new Line
                {
                    X1 = leftMargin,
                    X2 = width,
                    Y1 = s * heightPerSignal,
                    Y2 = s * heightPerSignal,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 0.5
                });

                // 라인 이름
                var text = new TextBlock
                {
                    Text = signalNames[s],
                    Foreground = Brushes.White,
                    FontSize = 12
                };
                Canvas.SetLeft(text, 0);
                Canvas.SetTop(text, s * heightPerSignal + heightPerSignal / 4 - 6);
                signalCanvas.Children.Add(text);

                // 신호 라인
                var line = new Polyline { Stroke = signalColors[s], StrokeThickness = 2 };
                bool prevVal = false;
                double prevY = s * heightPerSignal + heightPerSignal * 3 / 4;

                for (int i = 0; i < count; i++)
                {
                    bool val = s switch
                    {
                        0 => ViewModel.Signals[i].L_REQ,
                        1 => ViewModel.Signals[i].U_REQ,
                        2 => ViewModel.Signals[i].READY,
                        3 => ViewModel.Signals[i].CS_0,
                        4 => ViewModel.Signals[i].CS_1,
                        5 => ViewModel.Signals[i].VALID,
                        6 => ViewModel.Signals[i].TR_REQ,
                        7 => ViewModel.Signals[i].BUSY,
                        8 => ViewModel.Signals[i].COMPT,
                        9 => ViewModel.Signals[i].CONT,
                        10 => ViewModel.Signals[i].HO_AVBL,
                        11 => ViewModel.Signals[i].ES,
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
            double TopY = 120;
            double BottomY = 430;
            double XStart = 70;
            double XEnd = 340;
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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VirtualOHT.Views.Pages;
using VirtualOHT.Views.Windows;
using Wpf.Ui;

namespace VirtualOHT.Services
{
    /// <summary>
    /// Managed host of the application.
    /// </summary>
    public class ApplicationHostService : IHostedService
    {
        #region FIELDS
        private readonly IServiceProvider _serviceProvider;
        #endregion

        #region PROPERTIES
        #endregion

        #region CONSTRUCTOR
        public ApplicationHostService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        #endregion

        #region COMMAND
        #endregion

        #region METHOD
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                Application.Current.MainWindow = mainWindow; // WPF의 MainWindow 속성도 설정
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ApplicationHostService: 예외 발생: " + ex.ToString());
                System.Windows.MessageBox.Show(ex.ToString(), "예외 발생");
            }
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        => await Task.CompletedTask;
        #endregion

    }
}

using FFMpegCore;
using System.IO;

namespace FMVideoManagerApp.Services
{
    internal sealed class ApplicationService
    {
        private readonly MainWindow _mainWindow;
        private readonly AuthService _authService;
        private readonly MessageService _messageService;

        public ApplicationService(
            MainWindow mainWindow,
            AuthService authService,
            MessageService messageService)
        {
            _mainWindow = mainWindow;
            _authService = authService;
            _messageService = messageService;
        }

        public async Task InitializeAsync()
        {
            ConfigureFFmpeg();

            _mainWindow.Show();

            await TryRestoreSessionAsync();
        }

        private static void ConfigureFFmpeg()
        {
            GlobalFFOptions.Configure(new FFOptions
            {
                BinaryFolder = Path.Combine(AppContext.BaseDirectory, "Resources", "Exec")
            });
        }

        private async Task TryRestoreSessionAsync()
        {
            try
            {
                if (!_authService.HasSavedToken)
                    return;

                await _authService.LoadCurrentUserAsync();
            }
            catch
            {
                _authService.Logout();
                _messageService.ShowWarning("Session expired. Please log in again.");
            }
        }
    }
}
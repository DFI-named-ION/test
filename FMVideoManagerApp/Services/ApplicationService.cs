using FFMpegCore;
using System.IO;

namespace FMVideoManagerApp.Services
{
    internal sealed class ApplicationService
    {
        private readonly MainWindow _mainWindow;

        public ApplicationService(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public void Initialize()
        {
            GlobalFFOptions.Configure(new FFOptions()
            {
                BinaryFolder = Path.Combine(AppContext.BaseDirectory, "Resources", "Exec")
            });

            _mainWindow.Show();
        }
    }
}
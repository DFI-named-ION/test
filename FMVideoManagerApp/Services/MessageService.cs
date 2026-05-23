using FMVideoManagerApp.Models.AppMessage;

namespace FMVideoManagerApp.Services
{
    public sealed class MessageService
    {
        public event Action<AppMessage>? MessageReceived;

        public void ShowMessage(string message)
        {
            Publish(message, MessageSeverity.Message);
        }

        public void ShowWarning(string message)
        {
            Publish(message, MessageSeverity.Warning);
        }

        public void ShowCriticalWarning(string message)
        {
            Publish(message, MessageSeverity.CriticalWarning);
        }

        public void ShowError(string message)
        {
            Publish(message, MessageSeverity.Error);
        }

        private void Publish(string message, MessageSeverity severity)
        {
            MessageReceived?.Invoke(new AppMessage(message, severity));
        }
    }
}
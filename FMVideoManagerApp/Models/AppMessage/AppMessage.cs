namespace FMVideoManagerApp.Models.AppMessage
{
    public sealed class AppMessage
    {
        private string _message = string.Empty;
        public string Message => _message;

        private MessageSeverity _severity = MessageSeverity.Message;
        public MessageSeverity Severity => _severity;

        private string _detailedMessage = string.Empty;
        public string DetailedMessage => _detailedMessage;
        // private string _details;

        public AppMessage(string message, MessageSeverity severity)
        {
            _message = message;
            _severity = severity;
        }

        public AppMessage(string message, MessageSeverity severity, string detailedMessage)
        {
            _message = message;
            _severity = severity;
            _detailedMessage = detailedMessage;
        }
    }
}
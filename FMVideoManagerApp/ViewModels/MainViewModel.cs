using FMVideoManagerApp.Core;
using FMVideoManagerApp.Models.AppMessage;
using FMVideoManagerApp.Services;
using System.Windows;
using System.Windows.Input;

namespace FMVideoManagerApp.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private IComponentViewModel? _currentComponent;
        public IComponentViewModel? CurrentComponent
        {
            get => _currentComponent;
            private set
            {
                if (_currentComponent != value)
                {
                    _currentComponent = value;
                    OnPropertyChanged(nameof(CurrentComponent));
                }
            }
        }

        private string _message = string.Empty;
        public string Message
        {
            get => _message;
            set
            {
                if (_message != value)
                {
                    _message = value;
                    OnPropertyChanged(nameof(Message));
                }
            }
        }

        private MessageSeverity _severity;
        public MessageSeverity Severity
        {
            get => _severity;
            set
            {
                if (value != _severity)
                {
                    _severity = value;
                    OnPropertyChanged(nameof(Severity));
                }
            }
        }

        private MessageService _messageService;
        private AuthService _authService;

        public ICommand CloseWindowCommand { get; }
        public ICommand OpenFilesCommand { get; }
        public ICommand OpenSettingsCommand { get; }

        private Visibility _leftMenuVisibility = Visibility.Hidden;
        public Visibility LeftMenuVisibility
        {
            get => _leftMenuVisibility;
            set
            {
                if (_leftMenuVisibility != value)
                {
                    _leftMenuVisibility = value;
                    OnPropertyChanged(nameof(LeftMenuVisibility));
                }
            }
        }

        private Dictionary<string, IComponentViewModel> _viewModels = new Dictionary<string, IComponentViewModel>();

        public MainViewModel(MessageService messageService, AuthService authService,
            LogInViewModel logInVM, FileListViewModel fileListVM, SettingsViewModel settingsVM)
        {
            _messageService = messageService;
            _authService = authService;

            _messageService.MessageReceived += OnMessageReceived;
            _authService.OnLogIn += SwitchToFilesView;

            _viewModels.Add("LogIn", logInVM);
            _viewModels.Add("FileList", fileListVM);
            settingsVM.OnColumnsCountChanged += fileListVM.AssingColumnsCount;
            _viewModels.Add("Settings", settingsVM);

            CloseWindowCommand = new RelayCommand((_) => App.Current.MainWindow.Close());
            OpenFilesCommand = new RelayCommand((_) => SwitchToFilesView());
            OpenSettingsCommand = new RelayCommand((_) => SwitchToSettingsView());

            CurrentComponent = _viewModels["LogIn"];
        }

        private void SwitchToFilesView()
        {
            CurrentComponent = _viewModels["FileList"];
            LeftMenuVisibility = Visibility.Visible;
        }

        private void SwitchToSettingsView()
        {
            CurrentComponent = _viewModels["Settings"];
        }

        private void OnMessageReceived(AppMessage message)
        {
            Message = message.Message;
            Severity = message.Severity;
        }
    }
}
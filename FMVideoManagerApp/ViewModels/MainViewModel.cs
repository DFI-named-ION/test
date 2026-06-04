using FMVideoManagerApp.Core;
using FMVideoManagerApp.Data.DTO;
using FMVideoManagerApp.Models.AppMessage;
using FMVideoManagerApp.Services;
using System.Windows;
using System.Windows.Input;

namespace FMVideoManagerApp.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly MessageService _messageService;
        private readonly AuthService _authService;

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

        private readonly Dictionary<string, IComponentViewModel> _viewModels = new();

        public ICommand CloseWindowCommand { get; }
        public ICommand LogOutCommand { get; }
        public ICommand OpenFilesCommand { get; }
        public ICommand OpenHierarchyRelationsCommand { get; }
        public ICommand OpenSettingsCommand { get; }

        public MainViewModel(MessageService messageService, AuthService authService,
            LogInViewModel logInVM, FileListViewModel fileListVM, HierarchyRelationsViewModel hierarchyRelationsVM, SettingsViewModel settingsVM)
        {
            _messageService = messageService;
            _authService = authService;

            _messageService.MessageReceived += OnMessageReceived;

            _authService.LoggedIn += OnLoggedIn;
            _authService.LoggedOut += OnLoggedOut;

            _viewModels.Add("LogIn", logInVM);
            _viewModels.Add("FileList", fileListVM);
            _viewModels.Add("Relations", hierarchyRelationsVM);
            _viewModels.Add("Settings", settingsVM);

            settingsVM.OnColumnsCountChanged += fileListVM.AssingColumnsCount;

            CloseWindowCommand = new RelayCommand(_ => App.Current.MainWindow.Close());
            LogOutCommand = new RelayCommand(_ => _authService.Logout());

            OpenFilesCommand = new RelayCommand(async _ => await SwitchToFilesViewAsync());
            OpenHierarchyRelationsCommand = new RelayCommand(async _ => await SwitchToHierarchyViewAsync());
            OpenSettingsCommand = new RelayCommand(async _ => await SwitchToSettingsViewAsync());

            CurrentComponent = _viewModels["LogIn"];
        }

        private async Task SwitchComponentAsync(string key)
        {
            if (!_viewModels.TryGetValue(key, out IComponentViewModel? nextComponent))
                return;

            if (ReferenceEquals(CurrentComponent, nextComponent))
                return;

            CurrentComponent?.OnDeactivated();

            CurrentComponent = nextComponent;

            await nextComponent.OnActivatedAsync();
        }

        private async void OnLoggedIn(AuthResponse user)
        {
            await SwitchToFilesViewAsync();
            _messageService.ShowMessage($"Logged in as {user.Login}");
        }

        private async void OnLoggedOut()
        {
            await SwitchToLogInViewAsync();
        }

        private async Task SwitchToFilesViewAsync()
        {
            LeftMenuVisibility = Visibility.Visible;
            await SwitchComponentAsync("FileList");
        }

        private async Task SwitchToHierarchyViewAsync()
        {
            LeftMenuVisibility = Visibility.Visible;
            await SwitchComponentAsync("Relations");
        }

        private async Task SwitchToSettingsViewAsync()
        {
            await SwitchComponentAsync("Settings");
        }

        private async Task SwitchToLogInViewAsync()
        {
            LeftMenuVisibility = Visibility.Hidden;
            await SwitchComponentAsync("LogIn");
        }

        private void OnMessageReceived(AppMessage message)
        {
            Message = message.Message;
            Severity = message.Severity;
        }
    }
}
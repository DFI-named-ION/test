using FMVideoManagerApp.Core;
using FMVideoManagerApp.Models;
using FMVideoManagerApp.Services;
using System.Windows.Input;

namespace FMVideoManagerApp.ViewModels
{
    public class LogInViewModel : ObservableObject, IComponentViewModel
    {
        private string _login = string.Empty;
        public string Login
        {
            get => _login;
            set
            {
                _login = value;
                OnPropertyChanged(nameof(Login));
            }
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

        public ICommand LogInCommand { get; }
        public ICommand RegisterCommand { get; }

        private AuthService _authService;
        private MessageService _messageService;

        public LogInViewModel(AuthService authService, MessageService messageService)
        {
            _authService = authService;
            _messageService = messageService;

            LogInCommand = new RelayCommand((_) =>
            {
                try
                {
                    if (!_authService.LogIn(_login, _password))
                        _messageService.ShowWarning("Unsuccessful log in attempt...");
                }
                catch (Exception ex)
                {
                    _messageService.ShowError(ex.Message);
                }
            });
            RegisterCommand = new RelayCommand((_) =>
            {
                try
                {
                    if (!_authService.Register(_login, _password))
                        _messageService.ShowWarning("Unsuccessful register attempt...");

                }
                catch (Exception ex)
                {
                    _messageService.ShowError(ex.Message);
                }
            });
        }
    }
}
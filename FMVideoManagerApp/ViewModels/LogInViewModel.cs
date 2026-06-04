using FMVideoManagerApp.Core;
using FMVideoManagerApp.Models;
using FMVideoManagerApp.Services;
using System.Net.Http;
using System.Windows.Input;

namespace FMVideoManagerApp.ViewModels
{
    public class LogInViewModel : ObservableObject, IComponentViewModel
    {
        private readonly AuthService _authService;
        private readonly MessageService _messageService;

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

        private string _alias = string.Empty;
        public string Alias
        {
            get => _alias;
            set
            {
                _alias = value;
                OnPropertyChanged(nameof(Alias));
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged(nameof(IsBusy));
                }
            }
        }

        public ICommand LogInCommand { get; }
        public ICommand RegisterCommand { get; }

        public LogInViewModel(AuthService authService, MessageService messageService)
        {
            _authService = authService;
            _messageService = messageService;

            LogInCommand = new RelayCommand(async _ => await LogInAsync());
            RegisterCommand = new RelayCommand(async _ => await RegisterAsync());
        }

        private async Task LogInAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;

                if (string.IsNullOrWhiteSpace(Login))
                {
                    _messageService.ShowWarning("Login is required.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(Password))
                {
                    _messageService.ShowWarning("Password is required.");
                    return;
                }

                await _authService.LoginAsync(Login, Password);

                _messageService.ShowMessage("Successfully logged in.");
            }
            catch (HttpRequestException ex)
            {
                _messageService.ShowError($"Cannot connect to API: {ex.Message}");
            }
            catch (Exception ex)
            {
                _messageService.ShowError(ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RegisterAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;

                if (string.IsNullOrWhiteSpace(Login))
                {
                    _messageService.ShowWarning("Login is required.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(Password))
                {
                    _messageService.ShowWarning("Password is required.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(Alias))
                {
                    _messageService.ShowWarning("Alias is required.");
                    return;
                }

                await _authService.RegisterAsync(Login, Password, Alias);

                _messageService.ShowMessage("Successfully registered.");
            }
            catch (HttpRequestException ex)
            {
                _messageService.ShowError($"Cannot connect to API: {ex.Message}");
            }
            catch (Exception ex)
            {
                _messageService.ShowError(ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
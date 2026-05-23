using FMVideoManagerApp.Core;
using FMVideoManagerApp.Data.Repositories;
using FMVideoManagerApp.Models;

namespace FMVideoManagerApp.Services
{
    public sealed class AuthService : ObservableObject
    {
        private IUserRepository _userRepo;

        private AppUser _user = null!;

        private bool _isAuthenticated = false;
        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            internal set
            {
                if (_isAuthenticated != value)
                {
                    _isAuthenticated = value;
                    OnPropertyChanged(nameof(IsAuthenticated));
                }
            }
        }

        public event Action OnLogIn;

        public AuthService(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public bool LogIn(string login, string password)
        {
            try
            {
                var user = _userRepo.FindByLogin(login);

                if (user is null)
                    return false;

                if (!CryptographyService.VerifyPasswordHash(password, user.Password))
                    return false;

                _user = user;
                IsAuthenticated = true;
                OnLogIn?.Invoke();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error during loging in");
            }
        }

        public bool Register(string login, string password, string alias = "")
        {
            try
            {
                string passwordHash = CryptographyService.HashPassword(password);

                var user = new AppUser
                {
                    Login = login,
                    Password = passwordHash,
                    Alias = GenerateAlias()
                };

                _userRepo.Add(user);

                return LogIn(login, password);
            }
            catch (Exception ex)
            {
                throw new Exception("Error during registering");
            }
        }

        public void LogOut()
        {
            _user = null!;
            IsAuthenticated = false;
        }

        public AppUser GetUser()
        {
            return _user;
        }

        private string GenerateAlias()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var alias = new char[12];
            var random = new Random();

            for (int i = 0; i < alias.Length; i++)
            {
                alias[i] = chars[random.Next(chars.Length)];
            }

            return new string(alias);
        }
    }
}
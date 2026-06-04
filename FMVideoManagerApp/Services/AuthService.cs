using FMVideoManagerApp.Data.DTO;

namespace FMVideoManagerApp.Services
{
    public sealed class AuthService
    {
        private readonly ApiClient _apiClient;
        private readonly TokenStore _tokenStore;

        public event Action<AuthResponse>? LoggedIn;
        public event Action? LoggedOut;

        public AuthResponse? CurrentUser { get; private set; }

        public bool IsLoggedIn => CurrentUser != null;
        public bool HasSavedToken => _tokenStore.HasAccessToken;

        public AuthService(ApiClient apiClient, TokenStore tokenStore)
        {
            _apiClient = apiClient;
            _tokenStore = tokenStore;
        }

        public async Task<AuthResponse> LoginAsync(string login, string password)
        {
            AuthResponse response = await _apiClient.LoginAsync(login, password);

            CurrentUser = response;
            _tokenStore.SetAccessToken(response.AccessToken);

            LoggedIn?.Invoke(response);

            return response;
        }

        public async Task<AuthResponse> RegisterAsync(string login, string password, string alias)
        {
            AuthResponse response = await _apiClient.RegisterAsync(login, password, alias);

            CurrentUser = response;
            _tokenStore.SetAccessToken(response.AccessToken);

            LoggedIn?.Invoke(response);

            return response;
        }

        public async Task<AuthResponse> LoadCurrentUserAsync()
        {
            AuthResponse response = await _apiClient.GetMeAsync();

            CurrentUser = response;

            if (!string.IsNullOrWhiteSpace(response.AccessToken))
            {
                _tokenStore.SetAccessToken(response.AccessToken);
            }

            LoggedIn?.Invoke(response);

            return response;
        }

        public long GetCurrentUserId()
        {
            if (CurrentUser == null)
                throw new InvalidOperationException("User is not logged in.");

            return CurrentUser.UserId;
        }

        public void Logout()
        {
            CurrentUser = null;
            _tokenStore.Clear();

            LoggedOut?.Invoke();
        }
    }
}
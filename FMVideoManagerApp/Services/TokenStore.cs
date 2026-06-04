using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FMVideoManagerApp.Services
{
    public sealed class TokenStore
    {
        private static readonly byte[] Entropy =
            Encoding.UTF8.GetBytes("FMVideoManager.TokenStore.v1");

        private readonly string _tokenFilePath;

        public string? AccessToken { get; private set; }

        public bool IsLoggedIn => !string.IsNullOrWhiteSpace(AccessToken);
        public bool HasAccessToken => !string.IsNullOrWhiteSpace(AccessToken);

        public TokenStore()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string directory = Path.Combine(appData, "FM");
            Directory.CreateDirectory(directory);

            _tokenFilePath = Path.Combine(directory, "auth.token");

            Load();
        }

        public void SetAccessToken(string accessToken)
        {
            AccessToken = accessToken;

            byte[] bytes = Encoding.UTF8.GetBytes(accessToken);
            byte[] secureBytes = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);

            File.WriteAllBytes(_tokenFilePath, secureBytes);
        }

        public void Clear()
        {
            AccessToken = null;

            if (File.Exists(_tokenFilePath))
            {
                File.Delete(_tokenFilePath);
            }
        }

        private void Load()
        {
            if (!File.Exists(_tokenFilePath))
                return;

            try
            {
                byte[] secureBytes = File.ReadAllBytes(_tokenFilePath);
                byte[] bytes = ProtectedData.Unprotect(secureBytes, Entropy, DataProtectionScope.CurrentUser);

                AccessToken = Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                AccessToken = null;

                try
                {
                    File.Delete(_tokenFilePath);
                }
                catch {}
            }
        }
    }
}
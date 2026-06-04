using Microsoft.AspNetCore.DataProtection;

namespace FMVideoManagerApi.Data
{
    public sealed class TokenProtector
    {
        private readonly IDataProtector _protector;

        public TokenProtector(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("FMVideoManager.CloudTokens.v1");
        }

        public string Protect(string value)
        {
            return _protector.Protect(value);
        }

        public string Unprotect(string value)
        {
            return _protector.Unprotect(value);
        }
    }
}
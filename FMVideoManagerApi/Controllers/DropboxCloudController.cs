using FMVideoManagerApi.Data;
using FMVideoManagerApi.Data.DTO.Dropbox;
using FMVideoManagerApi.Models;
using FMVideoManagerApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace FMVideoManagerApi.Controllers
{
    [ApiController]
    [Route("api/cloud/dropbox")]
    public sealed class DropboxCloudController : ControllerBase
    {
        private readonly ServerDbContext _db;
        private readonly DropboxOptions _dropboxOptions;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDataProtector _stateProtector;
        private readonly TokenProtector _tokenProtector;
        private readonly DropboxStorageIndexingService _indexingService;

        public DropboxCloudController(ServerDbContext db, IOptions<DropboxOptions> dropboxOptions,
            IHttpClientFactory httpClientFactory, IDataProtectionProvider dataProtectionProvider,
            TokenProtector tokenProtector, DropboxStorageIndexingService indexingService)
        {
            _db = db;
            _dropboxOptions = dropboxOptions.Value;
            _httpClientFactory = httpClientFactory;
            _stateProtector = dataProtectionProvider.CreateProtector("FMVideoManager.DropboxOAuthState.v1");
            _tokenProtector = tokenProtector;
            _indexingService = indexingService;
        }

        [Authorize]
        [HttpPost("connect/start")]
        public ActionResult<StartDropboxConnectResponse> StartConnect()
        {
            if (!TryGetCurrentUserId(out long userId))
                return Unauthorized();

            string stateJson = JsonSerializer.Serialize(new DropboxOAuthState
            {
                UserId = userId,
                CreatedAtUtc = DateTime.UtcNow,
                Nonce = Guid.NewGuid().ToString("N")
            });

            string protectedState = _stateProtector.Protect(stateJson);

            string scopes = string.Join(" ", new[]
            {
                "account_info.read",
                "files.metadata.read",
                "files.content.read"
            });

            string authorizationUrl =
                "https://www.dropbox.com/oauth2/authorize" +
                $"?client_id={Uri.EscapeDataString(_dropboxOptions.AppKey)}" +
                "&response_type=code" +
                $"&redirect_uri={Uri.EscapeDataString(_dropboxOptions.RedirectUri)}" +
                $"&state={Uri.EscapeDataString(protectedState)}" +
                "&token_access_type=offline" +
                $"&scope={Uri.EscapeDataString(scopes)}";

            return Ok(new StartDropboxConnectResponse
            {
                AuthorizationUrl = authorizationUrl
            });
        }

        [AllowAnonymous]
        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state,
            [FromQuery] string? error, CancellationToken cancellationToken)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(error))
                    return Content($"Dropbox connection failed: {error}");

                if (string.IsNullOrWhiteSpace(code))
                    return BadRequest("Missing Dropbox authorization code.");

                if (string.IsNullOrWhiteSpace(state))
                    return BadRequest("Missing OAuth state.");

                DropboxOAuthState oauthState;

                try
                {
                    string stateJson = _stateProtector.Unprotect(state);

                    oauthState = JsonSerializer.Deserialize<DropboxOAuthState>(stateJson)
                        ?? throw new InvalidOperationException("Invalid OAuth state.");
                }
                catch
                {
                    return BadRequest("Invalid OAuth state.");
                }

                if (DateTime.UtcNow - oauthState.CreatedAtUtc > TimeSpan.FromMinutes(10))
                    return BadRequest("OAuth state expired.");

                DropboxTokenResponse tokenResponse = await ExchangeCodeForTokenAsync(code, cancellationToken);

                if (string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
                {
                    return Content("Dropbox connection failed: Dropbox did not return a valid access token.");
                }

                DropboxCurrentAccountResponse account = await GetCurrentAccountAsync(tokenResponse.AccessToken,
                    cancellationToken);

                DateTime now = DateTime.UtcNow;

                CloudProviderAccount? existing = await _db.CloudProviderAccounts
                    .FirstOrDefaultAsync(
                        x =>
                            x.UserId == oauthState.UserId &&
                            x.Provider == CloudProviderType.Dropbox &&
                            x.ProviderAccountId == account.AccountId,
                        cancellationToken);

                DateTime? expiresAtUtc = tokenResponse.ExpiresIn == null
                    ? null
                    : now.AddSeconds(tokenResponse.ExpiresIn.Value);

                if (existing == null)
                {
                    existing = new CloudProviderAccount
                    {
                        UserId = oauthState.UserId,
                        Provider = CloudProviderType.Dropbox,
                        ProviderAccountId = account.AccountId,
                        DisplayName = account.Name?.DisplayName,
                        Email = account.Email,
                        AccessTokenEncrypted = _tokenProtector.Protect(tokenResponse.AccessToken),
                        RefreshTokenEncrypted = string.IsNullOrWhiteSpace(tokenResponse.RefreshToken)
                            ? null
                            : _tokenProtector.Protect(tokenResponse.RefreshToken),
                        TokenExpiresAtUtc = expiresAtUtc,
                        Scopes = tokenResponse.Scope,
                        IsActive = true,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    };

                    _db.CloudProviderAccounts.Add(existing);
                }
                else
                {
                    existing.DisplayName = account.Name?.DisplayName;
                    existing.Email = account.Email;
                    existing.AccessTokenEncrypted = _tokenProtector.Protect(tokenResponse.AccessToken);

                    if (!string.IsNullOrWhiteSpace(tokenResponse.RefreshToken))
                    {
                        existing.RefreshTokenEncrypted = _tokenProtector.Protect(tokenResponse.RefreshToken);
                    }

                    existing.TokenExpiresAtUtc = expiresAtUtc;
                    existing.Scopes = tokenResponse.Scope;
                    existing.IsActive = true;
                    existing.UpdatedAtUtc = now;
                }

                await _db.SaveChangesAsync(cancellationToken);

                return Content("Dropbox connected successfully. You can close this browser tab.");
            }
            catch (OperationCanceledException)
            {
                return Content("Dropbox connection was cancelled. Please close this tab and try again.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                return Content($"Dropbox connection failed: {ex.Message}");
                // return Content("Dropbox connection failed. Please close this tab and try again from the app.");
            }
        }

        [Authorize]
        [HttpPost("accounts/{accountId:long}/index")]
        public async Task<IActionResult> IndexDropboxAccount(long accountId, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out long userId))
                return Unauthorized();

            CloudProviderAccount? account = await _db.CloudProviderAccounts
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == accountId &&
                        x.UserId == userId &&
                        x.Provider == CloudProviderType.Dropbox,
                    cancellationToken);

            if (account == null)
                return NotFound("Dropbox account not found.");

            if (!account.IsActive)
                return BadRequest("Dropbox account is disabled.");

            try
            {
                DropboxIndexResult result = await _indexingService.IndexAsync(account, cancellationToken);

                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return BadRequest("Dropbox indexing was cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                return BadRequest($"Dropbox indexing failed: {ex.Message}");
            }
        }

        private async Task<DropboxTokenResponse> ExchangeCodeForTokenAsync(string code,
            CancellationToken cancellationToken)
        {
            using HttpClient client = _httpClientFactory.CreateClient();

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.dropboxapi.com/oauth2/token");

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["client_id"] = _dropboxOptions.AppKey,
                ["client_secret"] = _dropboxOptions.AppSecret,
                ["redirect_uri"] = _dropboxOptions.RedirectUri
            });

            using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);

            string body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(body);

            DropboxTokenResponse? tokenResponse = JsonSerializer.Deserialize<DropboxTokenResponse>(
                body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return tokenResponse ?? throw new InvalidOperationException("Empty Dropbox token response.");
        }

        private async Task<DropboxCurrentAccountResponse> GetCurrentAccountAsync(string accessToken,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new InvalidOperationException("Dropbox access token is empty.");

            using HttpClient client = _httpClientFactory.CreateClient();

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.dropboxapi.com/2/users/get_current_account");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);

            string body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Dropbox account request failed: {body}");
            }

            DropboxCurrentAccountResponse? account =JsonSerializer.Deserialize<DropboxCurrentAccountResponse>(body);

            return account ?? throw new InvalidOperationException("Empty Dropbox account response.");
        }

        private bool TryGetCurrentUserId(out long userId)
        {
            string? userIdText = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return long.TryParse(userIdText, out userId);
        }
    }
}
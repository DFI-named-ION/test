using FMVideoManagerApp.Data.DTO;
using FMVideoManagerApp.Data.DTO.Hierarchy;
using FMVideoManagerApp.Data.DTO.Tags;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace FMVideoManagerApp.Services
{
    public sealed class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly TokenStore _tokenStore;

        public ApiClient(HttpClient httpClient, TokenStore tokenStore)
        {
            _httpClient = httpClient;
            _tokenStore = tokenStore;
        }

        public async Task<AuthResponse> RegisterAsync(string login, string password, string alias, CancellationToken cancellationToken = default)
        {
            var request = new RegisterRequest
            {
                Login = login,
                Password = password,
                Alias = alias
            };

            using HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/auth/register", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = await ReadErrorMessageAsync(response, cancellationToken);

                throw new InvalidOperationException(errorMessage);
            }

            AuthResponse authResponse =
                await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken)
                ?? throw new InvalidOperationException("Empty register response.");

            _tokenStore.SetAccessToken(authResponse.AccessToken);

            return authResponse;
        }

        public async Task<AuthResponse> LoginAsync(string login, string password, CancellationToken cancellationToken = default)
        {
            var request = new LoginRequest
            {
                Login = login,
                Password = password
            };

            using HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/auth/login", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = await ReadErrorMessageAsync(response, cancellationToken);

                throw new InvalidOperationException(errorMessage);
            }

            AuthResponse authResponse =
                await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken)
                ?? throw new InvalidOperationException("Empty login response.");

            _tokenStore.SetAccessToken(authResponse.AccessToken);

            return authResponse;
        }

        public async Task<AuthResponse> GetMeAsync(CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Get, "api/auth/me");
            using HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _tokenStore.Clear();
                }

                string errorMessage = await ReadErrorMessageAsync(response, cancellationToken);

                throw new InvalidOperationException(errorMessage);
            }

            AuthResponse authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken)
                ?? throw new InvalidOperationException("Empty me response.");

            if (!string.IsNullOrWhiteSpace(authResponse.AccessToken))
            {
                _tokenStore.SetAccessToken(authResponse.AccessToken);
            }

            return authResponse;
        }

        public async Task<RegisterLocalFileResponse> RegisterLocalFileAsync(RegisterLocalFileRequest request, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage httpRequest = CreateAuthorizedRequest(HttpMethod.Post, "api/files/register-local");
            httpRequest.Content = JsonContent.Create(request);
            using HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = await ReadErrorMessageAsync(response, cancellationToken).ConfigureAwait(false);

                throw new InvalidOperationException(errorMessage);
            }

            RegisterLocalFileResponse? result = await response.Content.ReadFromJsonAsync<RegisterLocalFileResponse>(cancellationToken).ConfigureAwait(false);

            return result ?? throw new InvalidOperationException("Empty register local file response.");
        }

        public async Task<List<ServerFileDto>> GetFilesAsync(CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Get, "api/files");
            using HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = await ReadErrorMessageAsync(response, cancellationToken).ConfigureAwait(false);

                throw new InvalidOperationException(errorMessage);
            }

            List<ServerFileDto>? result = await response.Content
                .ReadFromJsonAsync<List<ServerFileDto>>(cancellationToken)
                .ConfigureAwait(false);

            return result ?? new List<ServerFileDto>();
        }

        public async Task<StartDropboxConnectResponse> StartDropboxConnectAsync(CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Post, "api/cloud/dropbox/connect/start");
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = await ReadErrorMessageAsync(response, cancellationToken);

                throw new InvalidOperationException(errorMessage);
            }

            StartDropboxConnectResponse result =
                await response.Content.ReadFromJsonAsync<StartDropboxConnectResponse>(cancellationToken)
                ?? throw new InvalidOperationException("Empty Dropbox connect response.");

            return result;
        }

        public async Task<List<CloudProviderAccountDto>> GetCloudAccountsAsync(CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Get, "api/cloud/accounts");
            using HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = await ReadErrorMessageAsync(response, cancellationToken);

                throw new InvalidOperationException(errorMessage);
            }

            List<CloudProviderAccountDto>? accounts = await response.Content.ReadFromJsonAsync<List<CloudProviderAccountDto>>(cancellationToken);

            return accounts ?? new List<CloudProviderAccountDto>();
        }

        public async Task DeactivateCloudAccountAsync(long accountId, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Post, $"api/cloud/accounts/{accountId}/deactivate");
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = await ReadErrorMessageAsync(response, cancellationToken);

                throw new InvalidOperationException(errorMessage);
            }
        }

        public async Task ActivateCloudAccountAsync(long accountId, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Post, $"api/cloud/accounts/{accountId}/activate");
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = await ReadErrorMessageAsync(response, cancellationToken);

                throw new InvalidOperationException(errorMessage);
            }
        }

        public async Task RemoveCloudAccountAsync(long accountId, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Delete, $"api/cloud/accounts/{accountId}");
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = await ReadErrorMessageAsync(response, cancellationToken);

                throw new InvalidOperationException(errorMessage);
            }
        }

        public async Task<DropboxIndexResult> IndexDropboxAccountAsync(long accountId, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Post, $"api/cloud/dropbox/accounts/{accountId}/index");
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = await ReadErrorMessageAsync(response, cancellationToken);

                throw new InvalidOperationException(errorMessage);
            }

            DropboxIndexResult? result = await response.Content.ReadFromJsonAsync<DropboxIndexResult>(cancellationToken);

            return result ?? throw new InvalidOperationException("Empty Dropbox index response.");
        }

        public async Task<List<HierarchyNodeDto>> GetHierarchyAsync(CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Get, "api/hierarchy");
            using HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = await ReadErrorMessageAsync(response, cancellationToken);

                throw new InvalidOperationException(errorMessage);
            }

            List<HierarchyNodeDto>? result = await response.Content.ReadFromJsonAsync<List<HierarchyNodeDto>>(cancellationToken);

            return result ?? new List<HierarchyNodeDto>();
        }

        public async Task<HierarchyNodeDto> CreateGroupAsync(CreateGroupRequest requestModel, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Post, "api/hierarchy/groups");
            request.Content = JsonContent.Create(requestModel);
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = await ReadErrorMessageAsync(response, cancellationToken);

                throw new InvalidOperationException(errorMessage);
            }

            HierarchyNodeDto? result = await response.Content.ReadFromJsonAsync<HierarchyNodeDto>(cancellationToken);

            return result ?? throw new InvalidOperationException("Empty create group response.");
        }

        public Task<HierarchyNodeDto> CreateGroupAsync(string title, long? parentNodeId = null, string? description = null,
            CancellationToken cancellationToken = default)
        {
            return CreateGroupAsync(
                new CreateGroupRequest
                {
                    Title = title,
                    ParentNodeId = parentNodeId,
                    Description = description
                },
                cancellationToken);
        }

        public async Task RenameNodeAsync(long nodeId, string title, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Patch, $"api/hierarchy/{nodeId}/rename");
            request.Content = JsonContent.Create(new RenameNodeRequest
            {
                Title = title
            });

            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = await ReadErrorMessageAsync(response, cancellationToken);

                throw new InvalidOperationException(errorMessage);
            }
        }

        public async Task UpdateNodeDescriptionAsync(long nodeId, string? description, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Patch, $"api/hierarchy/{nodeId}/description");

            request.Content = JsonContent.Create(new UpdateNodeDescriptionRequest
            {
                Description = description
            });

            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = await ReadErrorMessageAsync(response, cancellationToken);

                throw new InvalidOperationException(errorMessage);
            }
        }

        public async Task UpdateNodeNotesAsync(long nodeId, string? notes, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Patch, $"api/hierarchy/{nodeId}/notes");

            request.Content = JsonContent.Create(new UpdateNodeNotesRequest
            {
                Notes = notes
            });

            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = await ReadErrorMessageAsync(response, cancellationToken);

                throw new InvalidOperationException(errorMessage);
            }
        }

        public async Task MoveNodeAsync(long nodeId, long? newParentNodeId, int? sortOrder = null, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Patch, $"api/hierarchy/{nodeId}/move");

            request.Content = JsonContent.Create(new MoveNodeRequest
            {
                NewParentNodeId = newParentNodeId,
                SortOrder = sortOrder
            });

            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = await ReadErrorMessageAsync(response, cancellationToken);

                throw new InvalidOperationException(errorMessage);
            }
        }

        public async Task DeleteNodeAsync(long nodeId, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Delete, $"api/hierarchy/{nodeId}");
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = await ReadErrorMessageAsync(response, cancellationToken);

                throw new InvalidOperationException(errorMessage);
            }
        }

        public async Task<List<StorageReferenceDto>> GetFileReferencesAsync(long fileNodeId, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Get, $"api/files/{fileNodeId}/references");

            using HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = await ReadErrorMessageAsync(response, cancellationToken);

                throw new InvalidOperationException(errorMessage);
            }

            List<StorageReferenceDto>? result = await response.Content.ReadFromJsonAsync<List<StorageReferenceDto>>(cancellationToken);

            return result ?? new List<StorageReferenceDto>();
        }

        public async Task<List<TagDto>> GetTagsAsync(CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Get, "api/tags");
            using HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
                throw new InvalidOperationException(errorMessage);
            }

            return await response.Content.ReadFromJsonAsync<List<TagDto>>(cancellationToken) ?? new List<TagDto>();
        }

        public async Task<TagDto> CreateTagAsync(CreateTagRequest requestModel, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Post, "api/tags");

            request.Content = JsonContent.Create(requestModel);

            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
                throw new InvalidOperationException(errorMessage);
            }

            return await response.Content.ReadFromJsonAsync<TagDto>(cancellationToken) ?? throw new InvalidOperationException("Empty create tag response.");
        }

        public async Task<List<TagDto>> GetNodeTagsAsync(long nodeId, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Get, $"api/tags/nodes/{nodeId}");
            using HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
                throw new InvalidOperationException(errorMessage);
            }

            return await response.Content.ReadFromJsonAsync<List<TagDto>>(cancellationToken) ?? new List<TagDto>();
        }

        public async Task ApplyTagToNodeAsync(long nodeId, long tagId, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Post, $"api/tags/nodes/{nodeId}/{tagId}");
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
                throw new InvalidOperationException(errorMessage);
            }
        }

        public async Task RemoveTagFromNodeAsync(long nodeId, long tagId, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Delete, $"api/tags/nodes/{nodeId}/{tagId}");
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
                throw new InvalidOperationException(errorMessage);
            }
        }

        private async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(body))
                return $"Request failed with status code {(int)response.StatusCode}.";

            try
            {
                string? parsedString = System.Text.Json.JsonSerializer.Deserialize<string>(body);

                if (!string.IsNullOrWhiteSpace(parsedString))
                    return parsedString;
            }
            catch { }

            return body;
        }

        private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string url)
        {
            var request = new HttpRequestMessage(method, url);

            if (!string.IsNullOrWhiteSpace(_tokenStore.AccessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenStore.AccessToken);
            }

            return request;
        }
    }
}
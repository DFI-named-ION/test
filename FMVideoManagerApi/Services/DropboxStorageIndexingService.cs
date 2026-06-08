using FFMpegCore;
using FMVideoManagerApi.Data;
using FMVideoManagerApi.Data.DTO.Dropbox;
using FMVideoManagerApi.Data.DTO.Indexing;
using FMVideoManagerApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FMVideoManagerApi.Services
{
    public class DropboxStorageIndexingService
    {
        private readonly ServerDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly DropboxOptions _dropboxOptions;
        private readonly TokenProtector _tokenProtector;

        public DropboxStorageIndexingService(ServerDbContext db, IHttpClientFactory httpClientFactory,
            IOptions<DropboxOptions> dropboxOptions, TokenProtector tokenProtector)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
            _dropboxOptions = dropboxOptions.Value;
            _tokenProtector = tokenProtector;
        }

        public async Task<DropboxIndexResult> IndexAsync(CloudProviderAccount account, IProgress<CloudIndexingProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            progress?.Report(new CloudIndexingProgress
            {
                IsIndeterminate = true,
                StatusMessage = "Getting Dropbox access token..."
            });

            string accessToken = await GetValidDropboxAccessTokenAsync(account, cancellationToken);

            DateTime scanStartedAtUtc = DateTime.UtcNow;

            var result = new DropboxIndexResult
            {
                CloudProviderAccountId = account.Id
            };

            progress?.Report(new CloudIndexingProgress
            {
                IsIndeterminate = true,
                StatusMessage = "Reading Dropbox file list..."
            });

            List<DropboxFileEntry> files = await GetDropboxFilesAsync(accessToken, cancellationToken);

            result.FoundFiles = files.Count;

            int total = files.Count;
            int processed = 0;

            progress?.Report(new CloudIndexingProgress
            {
                IsIndeterminate = false,
                TotalFiles = total,
                ProcessedFiles = 0,
                FailedFiles = 0,
                DownloadedFiles = 0,
                IndexedFiles = 0,
                MarkedMissing = 0,
                CurrentFileName = null,
                CurrentFilePath = null,
                StatusMessage = $"Found {total} Dropbox file(s)."
            });

            foreach (DropboxFileEntry file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                progress?.Report(new CloudIndexingProgress
                {
                    IsIndeterminate = false,
                    TotalFiles = total,
                    ProcessedFiles = processed,
                    FailedFiles = result.FailedFiles,
                    DownloadedFiles = result.DownloadedFiles,
                    IndexedFiles = result.IndexedFiles,
                    MarkedMissing = result.MarkedMissing,
                    CurrentFileName = file.Name,
                    CurrentFilePath = file.PathDisplay,
                    StatusMessage = $"Indexing Dropbox file: {file.Name}"
                });

                try
                {
                    await IndexDropboxFileAsync(account, accessToken, file, scanStartedAtUtc, result, progress, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    result.FailedFiles++;

                    await MarkStorageReferenceSyncErrorAsync(account, file, scanStartedAtUtc, ex.Message, cancellationToken);
                }
                finally
                {
                    processed++;

                    progress?.Report(new CloudIndexingProgress
                    {
                        IsIndeterminate = false,
                        TotalFiles = total,
                        ProcessedFiles = processed,
                        FailedFiles = result.FailedFiles,
                        DownloadedFiles = result.DownloadedFiles,
                        IndexedFiles = result.IndexedFiles,
                        MarkedMissing = result.MarkedMissing,
                        CurrentFileName = file.Name,
                        CurrentFilePath = file.PathDisplay,
                        StatusMessage = $"Processed {processed}/{total} Dropbox file(s)."
                    });
                }
            }

            progress?.Report(new CloudIndexingProgress
            {
                IsIndeterminate = true,
                TotalFiles = total,
                ProcessedFiles = processed,
                FailedFiles = result.FailedFiles,
                DownloadedFiles = result.DownloadedFiles,
                IndexedFiles = result.IndexedFiles,
                CurrentFileName = null,
                CurrentFilePath = null,
                StatusMessage = "Marking missing Dropbox references..."
            });

            result.MarkedMissing = await MarkMissingOldReferencesAsync(account, scanStartedAtUtc, cancellationToken);

            account.LastSyncAtUtc = DateTime.UtcNow;
            account.UpdatedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            progress?.Report(new CloudIndexingProgress
            {
                IsIndeterminate = false,
                TotalFiles = total,
                ProcessedFiles = processed,
                FailedFiles = result.FailedFiles,
                DownloadedFiles = result.DownloadedFiles,
                IndexedFiles = result.IndexedFiles,
                MarkedMissing = result.MarkedMissing,
                CurrentFileName = null,
                CurrentFilePath = null,
                StatusMessage = result.FailedFiles == 0
                    ? "Dropbox indexing completed."
                    : $"Dropbox indexing completed with {result.FailedFiles} failed file(s)."
            });

            return result;
        }

        private async Task<List<DropboxFileEntry>> GetDropboxFilesAsync(string accessToken, CancellationToken cancellationToken)
        {
            List<DropboxFileEntry> result = new();

            DropboxListFolderResponse page = await DropboxListFolderAsync(accessToken, cancellationToken);

            AddEntries(page, result);

            while (page.HasMore)
            {
                cancellationToken.ThrowIfCancellationRequested();

                page = await DropboxListFolderContinueAsync(accessToken, page.Cursor, cancellationToken);

                AddEntries(page, result);
            }

            return result;
        }

        private static void AddEntries(DropboxListFolderResponse page, List<DropboxFileEntry> result)
        {
            foreach (DropboxFileEntry entry in page.Entries)
            {
                if (!string.Equals(entry.Tag, "file", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.Equals(Path.GetExtension(entry.Name), ".mp4", StringComparison.OrdinalIgnoreCase))
                    continue;

                result.Add(entry);
            }
        }

        private async Task IndexDropboxFileAsync(CloudProviderAccount account, string accessToken, DropboxFileEntry file, DateTime scanStartedAtUtc,
            DropboxIndexResult result, IProgress<CloudIndexingProgress>? progress, CancellationToken cancellationToken)
        {
            StorageReference storageReference = await UpsertStorageReferenceAsync(account, file, scanStartedAtUtc, cancellationToken);

            string tempFilePath = Path.Combine(Path.GetTempPath(), $"fm_dropbox_{Guid.NewGuid():N}.mp4");

            try
            {
                progress?.Report(new CloudIndexingProgress
                {
                    IsIndeterminate = false,
                    CurrentFileName = file.Name,
                    CurrentFilePath = file.PathDisplay,
                    TotalFiles = result.FoundFiles,
                    DownloadedFiles = result.DownloadedFiles,
                    IndexedFiles = result.IndexedFiles,
                    FailedFiles = result.FailedFiles,
                    MarkedMissing = result.MarkedMissing,
                    StatusMessage = $"Downloading {file.Name}..."
                });

                await DownloadDropboxFileAsync(accessToken, file.Id, tempFilePath, cancellationToken);

                result.DownloadedFiles++;

                progress?.Report(new CloudIndexingProgress
                {
                    IsIndeterminate = false,
                    CurrentFileName = file.Name,
                    CurrentFilePath = file.PathDisplay,
                    TotalFiles = result.FoundFiles,
                    DownloadedFiles = result.DownloadedFiles,
                    IndexedFiles = result.IndexedFiles,
                    FailedFiles = result.FailedFiles,
                    MarkedMissing = result.MarkedMissing,
                    StatusMessage = $"Hashing {file.Name}..."
                });

                string sha256 = CryptographyService.HashFile(new FileInfo(tempFilePath));

                progress?.Report(new CloudIndexingProgress
                {
                    IsIndeterminate = false,
                    CurrentFileName = file.Name,
                    CurrentFilePath = file.PathDisplay,
                    TotalFiles = result.FoundFiles,
                    DownloadedFiles = result.DownloadedFiles,
                    IndexedFiles = result.IndexedFiles,
                    FailedFiles = result.FailedFiles,
                    MarkedMissing = result.MarkedMissing,
                    StatusMessage = $"Reading media info for {file.Name}..."
                });

                MediaMetadata metadata = await ReadMediaMetadataAsync(tempFilePath);

                FileContent content = await UpsertFileContentAsync(sha256, file, metadata, cancellationToken);

                FileItem fileItem = await CreateOrUpdateFileItemAsync(account, storageReference, file, content, cancellationToken);

                storageReference.ContentHash = content.Hash;
                storageReference.FileNodeId = fileItem.NodeId;
                storageReference.State = StorageReferenceState.Active;
                storageReference.LastSeenAtUtc = scanStartedAtUtc;

                result.IndexedFiles++;

                await _db.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempFilePath))
                        File.Delete(tempFilePath);
                }
                catch
                {
                }
            }
        }

        private async Task<FileContent> UpsertFileContentAsync(string sha256, DropboxFileEntry dropboxFile, MediaMetadata metadata,
            CancellationToken cancellationToken)
        {
            FileContent? content = await _db.FileContents.FirstOrDefaultAsync(x => x.Hash == sha256, cancellationToken);

            if (content == null)
            {
                content = new FileContent
                {
                    Hash = sha256,
                    SizeBytes = dropboxFile.Size ?? 0,
                    MimeType = "video/mp4",
                    DurationMs = metadata.DurationMs,
                    Width = metadata.Width,
                    Height = metadata.Height,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _db.FileContents.Add(content);
            }
            else
            {
                if (content.SizeBytes <= 0 && dropboxFile.Size != null)
                    content.SizeBytes = dropboxFile.Size.Value;

                if (string.IsNullOrWhiteSpace(content.MimeType))
                    content.MimeType = "video/mp4";

                if (content.DurationMs == null && metadata.DurationMs != null)
                    content.DurationMs = metadata.DurationMs;

                if (content.Width == null && metadata.Width != null)
                    content.Width = metadata.Width;

                if (content.Height == null && metadata.Height != null)
                    content.Height = metadata.Height;
            }

            return content;
        }

        private async Task<FileItem> CreateOrUpdateFileItemAsync(CloudProviderAccount account, StorageReference storageReference,
            DropboxFileEntry dropboxFile, FileContent content, CancellationToken cancellationToken)
        {
            DateTime now = DateTime.UtcNow;

            if (storageReference.FileNodeId != null)
            {
                FileItem? linkedFileItem = await _db.FileItems
                    .Include(x => x.Node)
                    .FirstOrDefaultAsync(
                        x =>
                            x.NodeId == storageReference.FileNodeId.Value &&
                            x.Node.UserId == account.UserId,
                        cancellationToken);

                if (linkedFileItem != null && linkedFileItem.ContentHash == content.Hash)
                {
                    linkedFileItem.OriginalFilename = dropboxFile.Name;
                    linkedFileItem.UpdatedAtUtc = now;

                    linkedFileItem.Node.UpdatedAtUtc = now;

                    return linkedFileItem;
                }
            }

            FileItem? existingKnownFile = await _db.FileItems
                .Include(x => x.Node)
                .FirstOrDefaultAsync(
                    x =>
                        x.Node.UserId == account.UserId &&
                        x.ContentHash == content.Hash,
                    cancellationToken);

            if (existingKnownFile != null)
            {
                existingKnownFile.UpdatedAtUtc = now;

                return existingKnownFile;
            }

            var node = new HierarchyNode
            {
                UserId = account.UserId,
                ParentNodeId = null,
                NodeType = HierarchyNodeType.File,
                Title = dropboxFile.Name,
                SortOrder = 0,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,

                FileItem = new FileItem
                {
                    ContentHash = content.Hash,
                    OriginalFilename = dropboxFile.Name,
                    Notes = null,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                }
            };

            _db.HierarchyNodes.Add(node);

            await _db.SaveChangesAsync(cancellationToken);

            return node.FileItem!;
        }

        private async Task<StorageReference> UpsertStorageReferenceAsync(CloudProviderAccount account, DropboxFileEntry file, DateTime scanStartedAtUtc,
            CancellationToken cancellationToken)
        {
            string normalizedProviderPath = NormalizeDropboxPath(file.PathLower ?? file.PathDisplay ?? file.Name);

            StorageReference? existing = await _db.StorageReferences
                .FirstOrDefaultAsync(
                    x =>
                        x.CloudProviderAccountId == account.Id &&
                        x.Provider == CloudProviderType.Dropbox &&
                        x.ProviderItemId == file.Id,
                    cancellationToken);

            if (existing == null)
            {
                existing = await _db.StorageReferences
                    .FirstOrDefaultAsync(
                        x =>
                            x.CloudProviderAccountId == account.Id &&
                            x.Provider == CloudProviderType.Dropbox &&
                            x.ProviderPath != null &&
                            x.ProviderPath.ToLower() == normalizedProviderPath,
                        cancellationToken);
            }

            string metadataJson = JsonSerializer.Serialize(new
            {
                dropbox_content_hash = file.ContentHash,
                dropbox_path_lower = file.PathLower,
                dropbox_client_modified = file.ClientModified,
                dropbox_server_modified = file.ServerModified
            });

            if (existing == null)
            {
                existing = new StorageReference
                {
                    UserId = account.UserId,
                    CloudProviderAccountId = account.Id,
                    Provider = CloudProviderType.Dropbox,

                    ProviderItemId = file.Id,
                    ProviderPath = file.PathDisplay ?? file.PathLower ?? file.Name,
                    Name = file.Name,
                    ProviderRevision = file.Rev,

                    MimeType = "video/mp4",
                    SizeBytes = file.Size,
                    ProviderModifiedAtUtc = file.ServerModified,

                    LastSeenAtUtc = scanStartedAtUtc,
                    State = StorageReferenceState.Active,
                    MetadataJson = metadataJson
                };

                _db.StorageReferences.Add(existing);
            }
            else
            {
                existing.ProviderItemId = file.Id;
                existing.ProviderPath = file.PathDisplay ?? file.PathLower ?? file.Name;
                existing.Name = file.Name;
                existing.ProviderRevision = file.Rev;

                existing.MimeType = "video/mp4";
                existing.SizeBytes = file.Size;
                existing.ProviderModifiedAtUtc = file.ServerModified;

                existing.LastSeenAtUtc = scanStartedAtUtc;
                existing.State = StorageReferenceState.Active;
                existing.MetadataJson = metadataJson;
            }

            return existing;
        }

        private async Task MarkStorageReferenceSyncErrorAsync(CloudProviderAccount account, DropboxFileEntry file, DateTime scanStartedAtUtc, string error,
            CancellationToken cancellationToken)
        {
            StorageReference reference = await UpsertStorageReferenceAsync(account, file, scanStartedAtUtc, cancellationToken);

            reference.State = StorageReferenceState.SyncError;
            reference.MetadataJson = JsonSerializer.Serialize(new
            {
                dropbox_content_hash = file.ContentHash,
                dropbox_path_lower = file.PathLower,
                dropbox_client_modified = file.ClientModified,
                dropbox_server_modified = file.ServerModified,
                error
            });

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task<int> MarkMissingOldReferencesAsync(CloudProviderAccount account, DateTime scanStartedAtUtc, CancellationToken cancellationToken)
        {
            List<StorageReference> missingReferences = await _db.StorageReferences
                .Where(x =>
                    x.CloudProviderAccountId == account.Id &&
                    x.Provider == CloudProviderType.Dropbox &&
                    x.MimeType == "video/mp4" &&
                    x.State == StorageReferenceState.Active &&
                    x.LastSeenAtUtc < scanStartedAtUtc)
                .ToListAsync(cancellationToken);

            foreach (StorageReference reference in missingReferences)
            {
                reference.State = StorageReferenceState.Missing;
            }

            return missingReferences.Count;
        }

        private async Task<DropboxListFolderResponse> DropboxListFolderAsync(string accessToken, CancellationToken cancellationToken)
        {
            using HttpClient client = _httpClientFactory.CreateClient();

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.dropboxapi.com/2/files/list_folder");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            request.Content = JsonContent.Create(new
            {
                path = "",
                recursive = true,
                include_deleted = false,
                include_has_explicit_shared_members = false,
                include_mounted_folders = true,
                include_non_downloadable_files = false
            });

            using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);

            string body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(body);

            return JsonSerializer.Deserialize<DropboxListFolderResponse>(body)
                ?? throw new InvalidOperationException("Empty Dropbox list_folder response.");
        }

        private async Task<DropboxListFolderResponse> DropboxListFolderContinueAsync(string accessToken, string cursor, CancellationToken cancellationToken)
        {
            using HttpClient client = _httpClientFactory.CreateClient();

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.dropboxapi.com/2/files/list_folder/continue");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            request.Content = JsonContent.Create(new
            {
                cursor
            });

            using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);

            string body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(body);

            return JsonSerializer.Deserialize<DropboxListFolderResponse>(body)
                ?? throw new InvalidOperationException("Empty Dropbox list_folder/continue response.");
        }

        private async Task DownloadDropboxFileAsync(string accessToken, string dropboxFileId, string targetPath, CancellationToken cancellationToken)
        {
            using HttpClient client = _httpClientFactory.CreateClient();

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://content.dropboxapi.com/2/files/download");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            request.Headers.Add("Dropbox-API-Arg",
                JsonSerializer.Serialize(new
                {
                    path = dropboxFileId
                }));

            using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            string? errorBody = null;

            if (!response.IsSuccessStatusCode)
            {
                errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException(errorBody);
            }

            await using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using FileStream target = File.Create(targetPath);

            await source.CopyToAsync(target, cancellationToken);
        }

        private async Task<MediaMetadata> ReadMediaMetadataAsync(string path)
        {
            try
            {
                var mediaInfo = await FFProbe.AnalyseAsync(path);
                return new MediaMetadata
                {
                    DurationMs = (long)mediaInfo.Duration.TotalMilliseconds,
                    Width = mediaInfo.PrimaryVideoStream?.Width,
                    Height = mediaInfo.PrimaryVideoStream?.Height
                };
            }
            catch
            {
                return new MediaMetadata();
            }
        }

        private async Task<string> GetValidDropboxAccessTokenAsync(CloudProviderAccount account, CancellationToken cancellationToken)
        {
            if (account.TokenExpiresAtUtc != null &&
                account.TokenExpiresAtUtc > DateTime.UtcNow.AddMinutes(2))
            {
                return _tokenProtector.Unprotect(account.AccessTokenEncrypted);
            }

            if (string.IsNullOrWhiteSpace(account.RefreshTokenEncrypted))
            {
                return _tokenProtector.Unprotect(account.AccessTokenEncrypted);
            }

            string refreshToken = _tokenProtector.Unprotect(account.RefreshTokenEncrypted);

            DropboxTokenResponse tokenResponse = await RefreshDropboxAccessTokenAsync(refreshToken, cancellationToken);

            DateTime now = DateTime.UtcNow;

            account.AccessTokenEncrypted = _tokenProtector.Protect(tokenResponse.AccessToken);

            account.TokenExpiresAtUtc = tokenResponse.ExpiresIn == null
                ? null
                : now.AddSeconds(tokenResponse.ExpiresIn.Value);

            account.UpdatedAtUtc = now;

            await _db.SaveChangesAsync(cancellationToken);

            return tokenResponse.AccessToken;
        }

        private async Task<DropboxTokenResponse> RefreshDropboxAccessTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            using HttpClient client = _httpClientFactory.CreateClient();

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.dropboxapi.com/oauth2/token");

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = _dropboxOptions.AppKey,
                ["client_secret"] = _dropboxOptions.AppSecret
            });

            using HttpResponseMessage response = await client.SendAsync(
                request,
                cancellationToken);

            string body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(body);

            DropboxTokenResponse? tokenResponse =
                JsonSerializer.Deserialize<DropboxTokenResponse>(body);

            if (tokenResponse == null)
                throw new InvalidOperationException("Empty Dropbox refresh token response.");

            if (string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
                throw new InvalidOperationException("Dropbox refresh did not return an access token.");

            return tokenResponse;
        }

        private static string NormalizeDropboxPath(string path)
        {
            return path.Trim().Replace('\\', '/').ToLowerInvariant();
        }
    }
}
using FMVideoManagerApi.Data;
using FMVideoManagerApi.Data.DTO;
using FMVideoManagerApi.Data.DTO.Indexing;
using FMVideoManagerApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Security.Claims;
using System.Text.Json;

namespace FMVideoManagerApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/files")]
    public sealed class FilesController : ControllerBase // file repo
    {
        private readonly ServerDbContext _db;

        public FilesController(ServerDbContext db)
        {
            _db = db;
        }

        [HttpPost("register-local")]
        public async Task<ActionResult<RegisterLocalFileResponse>> RegisterLocalFile(RegisterLocalFileRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out long userId))
                return Unauthorized();

            string contentHash = request.ContentHash.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(contentHash))
                return BadRequest("Content hash is required.");

            if (contentHash.Length != 64)
                return BadRequest("Content hash must be a SHA256 hash.");

            if (string.IsNullOrWhiteSpace(request.OriginalFilename))
                return BadRequest("Original filename is required.");

            if (request.SizeBytes <= 0)
                return BadRequest("File size must be greater than zero.");

            if (request.ParentNodeId != null)
            {
                bool parentExists = await _db.HierarchyNodes.AnyAsync(
                    x =>
                        x.Id == request.ParentNodeId.Value &&
                        x.UserId == userId &&
                        x.NodeType == HierarchyNodeType.Group,
                    cancellationToken);

                if (!parentExists)
                    return BadRequest("Parent node must be an existing group owned by the current user.");
            }

            DateTime now = DateTime.UtcNow;

            FileContent? content = await _db.FileContents.FirstOrDefaultAsync(x => x.Hash == contentHash, cancellationToken);

            if (content == null)
            {
                content = new FileContent
                {
                    Hash = contentHash,
                    SizeBytes = request.SizeBytes,
                    MimeType = request.MimeType,
                    DurationMs = request.DurationMs,
                    Width = request.Width,
                    Height = request.Height,
                    CreatedAtUtc = now
                };

                _db.FileContents.Add(content);
            }
            else
            {
                if (content.SizeBytes != request.SizeBytes)
                    return Conflict("Existing content with same hash has different file size.");

                if (content.DurationMs == null && request.DurationMs != null)
                    content.DurationMs = request.DurationMs;

                if (content.Width == null && request.Width != null)
                    content.Width = request.Width;

                if (content.Height == null && request.Height != null)
                    content.Height = request.Height;

                if (string.IsNullOrWhiteSpace(content.MimeType) && !string.IsNullOrWhiteSpace(request.MimeType))
                    content.MimeType = request.MimeType;
            }

            FileItem? existingFileItem = await _db.FileItems
                .Include(x => x.Node)
                .OrderBy(x => x.Node.CreatedAtUtc)
                .FirstOrDefaultAsync(
                    x =>
                        x.ContentHash == contentHash &&
                        x.Node.UserId == userId,
                    cancellationToken);

            FileItem fileItem;
            bool created;

            if (existingFileItem != null)
            {
                fileItem = existingFileItem;
                created = false;
            }
            else
            {
                string title = string.IsNullOrWhiteSpace(request.Title)
                    ? request.OriginalFilename.Trim()
                    : request.Title.Trim();

                var node = new HierarchyNode
                {
                    UserId = userId,
                    ParentNodeId = request.ParentNodeId,
                    NodeType = HierarchyNodeType.File,
                    Title = title,
                    SortOrder = 0,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,

                    FileItem = new FileItem
                    {
                        ContentHash = contentHash,
                        OriginalFilename = request.OriginalFilename.Trim(),
                        Notes = request.Notes,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    }
                };

                _db.HierarchyNodes.Add(node);

                await _db.SaveChangesAsync(cancellationToken);

                fileItem = node.FileItem!;
                created = true;
            }

            await UpsertLocalStorageReferenceAsync(userId, fileItem.NodeId, contentHash, request, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new RegisterLocalFileResponse
            {
                ServerFileItemId = fileItem.NodeId,
                NodeId = fileItem.NodeId,
                ContentHash = contentHash,
                Created = created,
                Title = fileItem.Node.Title
            });
        }

        [HttpGet]
        public async Task<ActionResult<List<ServerFileDto>>> GetFiles(CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out long userId))
                return Unauthorized();

            List<ServerFileDto> files = await _db.FileItems
                .AsNoTracking()
                .Where(x => x.Node.UserId == userId)
                .OrderBy(x => x.Node.SortOrder)
                .ThenBy(x => x.Node.Title)
                .Select(x => new ServerFileDto
                {
                    NodeId = x.NodeId,
                    ServerFileItemId = x.NodeId,

                    ParentNodeId = x.Node.ParentNodeId,

                    Title = x.Node.Title,
                    OriginalFilename = x.OriginalFilename,
                    Notes = x.Notes,

                    ContentHash = x.ContentHash,

                    SizeBytes = x.Content == null
                        ? null
                        : x.Content.SizeBytes,

                    MimeType = x.Content == null
                        ? null
                        : x.Content.MimeType,

                    DurationMs = x.Content == null
                        ? null
                        : x.Content.DurationMs,

                    Width = x.Content == null
                        ? null
                        : x.Content.Width,

                    Height = x.Content == null
                        ? null
                        : x.Content.Height,

                    CreatedAtUtc = x.CreatedAtUtc,
                    UpdatedAtUtc = x.UpdatedAtUtc
                })
                .ToListAsync(cancellationToken);

            return Ok(files);
        }

        [Authorize]
        [HttpGet("{fileNodeId:long}/references")]
        public async Task<ActionResult<List<StorageReferenceDto>>> GetFileReferences(long fileNodeId, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out long userId))
                return Unauthorized();

            FileItem? fileItem = await _db.FileItems
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.NodeId == fileNodeId &&
                        x.Node.UserId == userId,
                    cancellationToken);

            if (fileItem == null)
                return NotFound("File node not found.");

            string contentHash = fileItem.ContentHash;

            List<StorageReferenceDto> references = await _db.StorageReferences
                .AsNoTracking()
                .Where(x =>
                    x.UserId == userId &&
                    x.ContentHash == contentHash)
                .OrderBy(x => x.Provider)
                .ThenBy(x => x.ProviderPath)
                .Select(x => new StorageReferenceDto
                {
                    Id = x.Id,
                    FileNodeId = x.FileNodeId,
                    ContentHash = x.ContentHash,
                    CloudProviderAccountId = x.CloudProviderAccountId,
                    Provider = x.Provider,
                    ProviderItemId = x.ProviderItemId,
                    ProviderPath = x.ProviderPath,
                    Name = x.Name,
                    ProviderRevision = x.ProviderRevision,
                    MimeType = x.MimeType,
                    SizeBytes = x.SizeBytes,
                    ProviderModifiedAtUtc = x.ProviderModifiedAtUtc,
                    LastSeenAtUtc = x.LastSeenAtUtc,
                    State = x.State,
                    AccountDisplayName = x.CloudProviderAccount == null
                        ? null
                        : x.CloudProviderAccount.DisplayName,
                    AccountEmail = x.CloudProviderAccount == null
                        ? null
                        : x.CloudProviderAccount.Email
                })
                .ToListAsync(cancellationToken);

            return Ok(references);
        }

        //[Authorize]
        //[HttpGet("{fileNodeId:long}/references")]
        //public async Task<ActionResult<List<StorageReferenceDto>>> GetFileReferences(long fileNodeId, CancellationToken cancellationToken)
        //{
        //    if (!TryGetCurrentUserId(out long userId))
        //        return Unauthorized();

        //    bool fileExists = await _db.FileItems
        //        .AnyAsync(
        //            x =>
        //                x.NodeId == fileNodeId &&
        //                x.Node.UserId == userId,
        //            cancellationToken);

        //    if (!fileExists)
        //        return NotFound("File node not found.");

        //    List<StorageReferenceDto> references = await _db.StorageReferences
        //        .AsNoTracking()
        //        .Where(x =>
        //            x.UserId == userId &&
        //            x.FileNodeId == fileNodeId)
        //        .OrderBy(x => x.Provider)
        //        .ThenBy(x => x.ProviderPath)
        //        .Select(x => new StorageReferenceDto
        //        {
        //            Id = x.Id,
        //            FileNodeId = x.FileNodeId,
        //            ContentHash = x.ContentHash,
        //            CloudProviderAccountId = x.CloudProviderAccountId,
        //            Provider = x.Provider,
        //            ProviderItemId = x.ProviderItemId,
        //            ProviderPath = x.ProviderPath,
        //            Name = x.Name,
        //            ProviderRevision = x.ProviderRevision,
        //            MimeType = x.MimeType,
        //            SizeBytes = x.SizeBytes,
        //            ProviderModifiedAtUtc = x.ProviderModifiedAtUtc,
        //            LastSeenAtUtc = x.LastSeenAtUtc,
        //            State = x.State,
        //            AccountDisplayName = x.CloudProviderAccount.DisplayName,
        //            AccountEmail = x.CloudProviderAccount.Email
        //        })
        //        .ToListAsync(cancellationToken);

        //    return Ok(references);
        //}

        private async Task UpsertLocalStorageReferenceAsync(long userId, long fileNodeId, string contentHash, RegisterLocalFileRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.LocalPath))
                return;

            if (string.IsNullOrWhiteSpace(request.LocalDeviceId))
                return;

            string normalizedPath = Path.GetFullPath(request.LocalPath).Trim();

            string providerItemId = CreateLocalProviderItemId(request.LocalDeviceId, normalizedPath);

            DateTime now = DateTime.UtcNow;

            StorageReference? existing = await _db.StorageReferences
                .FirstOrDefaultAsync(
                    x =>
                        x.UserId == userId &&
                        x.Provider == CloudProviderType.Local &&
                        x.ProviderItemId == providerItemId,
                    cancellationToken);

            string metadataJson = JsonSerializer.Serialize(new
            {
                local_device_id = request.LocalDeviceId,
                local_path = normalizedPath
            });

            if (existing == null)
            {
                existing = new StorageReference
                {
                    UserId = userId,

                    FileNodeId = fileNodeId,
                    ContentHash = contentHash,

                    CloudProviderAccountId = null,
                    Provider = CloudProviderType.Local,

                    ProviderItemId = providerItemId,
                    ProviderPath = normalizedPath,
                    Name = request.OriginalFilename.Trim(),

                    ProviderRevision = null,
                    MimeType = request.MimeType,
                    SizeBytes = request.SizeBytes,

                    ProviderModifiedAtUtc = null,
                    LastSeenAtUtc = now,
                    State = StorageReferenceState.Active,

                    MetadataJson = metadataJson
                };

                _db.StorageReferences.Add(existing);
            }
            else
            {
                existing.FileNodeId = fileNodeId;
                existing.ContentHash = contentHash;

                existing.ProviderPath = normalizedPath;
                existing.Name = request.OriginalFilename.Trim();

                existing.MimeType = request.MimeType;
                existing.SizeBytes = request.SizeBytes;

                existing.LastSeenAtUtc = now;
                existing.State = StorageReferenceState.Active;

                existing.MetadataJson = metadataJson;
            }
        }

        private static string CreateLocalProviderItemId(string localDeviceId, string normalizedPath)
        {
            string input = $"{localDeviceId}|{normalizedPath.ToLowerInvariant()}";

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input);
            byte[] hash = System.Security.Cryptography.SHA256.HashData(bytes);

            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private bool TryGetCurrentUserId(out long userId)
        {
            string? userIdText = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return long.TryParse(userIdText, out userId);
        }
    }
}
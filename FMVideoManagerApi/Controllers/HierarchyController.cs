using FMVideoManagerApi.Data;
using FMVideoManagerApi.Data.DTO.Hierarchy;
using FMVideoManagerApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FMVideoManagerApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/hierarchy")]
    public sealed class HierarchyController : ControllerBase
    {
        private readonly ServerDbContext _db;

        public HierarchyController(ServerDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<List<HierarchyNodeDto>>> GetHierarchy(CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out long userId))
                return Unauthorized();

            List<HierarchyNodeDto> nodes = await _db.HierarchyNodes
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.ParentNodeId)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.Title)
                .Select(x => new HierarchyNodeDto
                {
                    Id = x.Id,
                    ParentNodeId = x.ParentNodeId,
                    NodeType = x.NodeType,
                    Title = x.Title,
                    SortOrder = x.SortOrder,
                    CreatedAtUtc = x.CreatedAtUtc,
                    UpdatedAtUtc = x.UpdatedAtUtc,

                    File = x.FileItem == null
                        ? null
                        : new FileNodeDto
                        {
                            NodeId = x.FileItem.NodeId,
                            ContentHash = x.FileItem.ContentHash,
                            OriginalFilename = x.FileItem.OriginalFilename,
                            Notes = x.FileItem.Notes,

                            SizeBytes = x.FileItem.Content == null
                                ? null
                                : x.FileItem.Content.SizeBytes,

                            MimeType = x.FileItem.Content == null
                                ? null
                                : x.FileItem.Content.MimeType,

                            DurationMs = x.FileItem.Content == null
                                ? null
                                : x.FileItem.Content.DurationMs,

                            Width = x.FileItem.Content == null
                                ? null
                                : x.FileItem.Content.Width,

                            Height = x.FileItem.Content == null
                                ? null
                                : x.FileItem.Content.Height
                        },

                    Group = x.GroupItem == null
                        ? null
                        : new GroupNodeDto
                        {
                            NodeId = x.GroupItem.NodeId,
                            Description = x.GroupItem.Description,
                            BackgroundColorHex = x.GroupItem.BackgroundColorHex,
                            ForegroundColorHex = x.GroupItem.ForegroundColorHex
                        }
                })
                .ToListAsync(cancellationToken);

            return Ok(nodes);
        }

        [HttpPost("groups")]
        public async Task<ActionResult<HierarchyNodeDto>> CreateGroup(CreateGroupRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out long userId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest("Group title is required.");

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

            var node = new HierarchyNode
            {
                UserId = userId,
                ParentNodeId = request.ParentNodeId,
                NodeType = HierarchyNodeType.Group,
                Title = request.Title.Trim(),
                SortOrder = 0,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,

                GroupItem = new GroupItem
                {
                    Description = request.Description
                }
            };

            _db.HierarchyNodes.Add(node);

            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new HierarchyNodeDto
            {
                Id = node.Id,
                ParentNodeId = node.ParentNodeId,
                NodeType = node.NodeType,
                Title = node.Title,
                SortOrder = node.SortOrder,
                CreatedAtUtc = node.CreatedAtUtc,
                UpdatedAtUtc = node.UpdatedAtUtc,
                Group = new GroupNodeDto
                {
                    NodeId = node.Id,
                    Description = node.GroupItem.Description
                }
            });
        }

        [HttpPatch("{nodeId:long}/rename")]
        public async Task<IActionResult> RenameNode(long nodeId, RenameNodeRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out long userId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest("Title is required.");

            HierarchyNode? node = await _db.HierarchyNodes
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == nodeId &&
                        x.UserId == userId,
                    cancellationToken);

            if (node == null)
                return NotFound("Hierarchy node not found.");

            node.Title = request.Title.Trim();
            node.UpdatedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        [HttpPatch("{nodeId:long}/move")]
        public async Task<IActionResult> MoveNode(long nodeId, MoveNodeRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out long userId))
                return Unauthorized();

            HierarchyNode? node = await _db.HierarchyNodes
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == nodeId &&
                        x.UserId == userId,
                    cancellationToken);

            if (node == null)
                return NotFound("Hierarchy node not found.");

            if (request.NewParentNodeId == nodeId)
                return BadRequest("A node cannot be moved into itself.");

            if (request.NewParentNodeId != null)
            {
                HierarchyNode? newParent = await _db.HierarchyNodes
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id == request.NewParentNodeId.Value &&
                            x.UserId == userId,
                        cancellationToken);

                if (newParent == null)
                    return BadRequest("Parent node not found.");

                if (newParent.NodeType != HierarchyNodeType.Group)
                    return BadRequest("Parent node must be a group.");

                bool wouldCreateCycle = await IsDescendantOfAsync(
                    possibleDescendantId: request.NewParentNodeId.Value,
                    possibleAncestorId: nodeId,
                    userId,
                    cancellationToken);

                if (wouldCreateCycle)
                    return BadRequest("Cannot move a node into its own child.");
            }

            node.ParentNodeId = request.NewParentNodeId;

            if (request.SortOrder != null)
                node.SortOrder = request.SortOrder.Value;

            node.UpdatedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        [Authorize]
        [HttpPost("{nodeId:long}/copy")]
        public async Task<ActionResult<CopyNodeResponse>> CopyNode(long nodeId, MoveNodeRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out long userId))
                return Unauthorized();

            HierarchyNode? sourceNode = await _db.HierarchyNodes
                .Include(x => x.FileItem)
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == nodeId &&
                        x.UserId == userId,
                    cancellationToken);

            if (sourceNode == null)
                return NotFound("Node not found.");

            if (sourceNode.NodeType != HierarchyNodeType.File)
                return BadRequest("Only file nodes can be copied for now.");

            if (sourceNode.FileItem == null)
                return BadRequest("File item is missing.");

            if (request.NewParentNodeId != null)
            {
                bool targetParentExists = await _db.HierarchyNodes.AnyAsync(
                    x =>
                        x.Id == request.NewParentNodeId.Value &&
                        x.UserId == userId &&
                        x.NodeType == HierarchyNodeType.Group,
                    cancellationToken);

                if (!targetParentExists)
                    return BadRequest("Target parent must be an existing group.");
            }

            DateTime now = DateTime.UtcNow;

            string title = sourceNode.ParentNodeId == request.NewParentNodeId
                ? $"{sourceNode.Title} - Copy"
                : sourceNode.Title;

            var copiedNode = new HierarchyNode
            {
                UserId = userId,
                ParentNodeId = request.NewParentNodeId,
                NodeType = HierarchyNodeType.File,
                Title = title,
                SortOrder = request.SortOrder ?? sourceNode.SortOrder,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,

                FileItem = new FileItem
                {
                    ContentHash = sourceNode.FileItem.ContentHash,
                    OriginalFilename = sourceNode.FileItem.OriginalFilename,
                    Notes = sourceNode.FileItem.Notes,
                    // StorageReferences = sourceNode.FileItem.StorageReferences, 
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                }
            };

            _db.HierarchyNodes.Add(copiedNode);

            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new CopyNodeResponse
            {
                NodeId = copiedNode.Id,
                ParentNodeId = copiedNode.ParentNodeId,
                Title = copiedNode.Title
            });
        }

        [HttpPatch("{nodeId:long}/description")]
        public async Task<IActionResult> UpdateNodeDescription(long nodeId, UpdateNodeDescriptionRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out long userId))
                return Unauthorized();

            GroupItem? group = await _db.GroupItems
                .Include(x => x.Node)
                .FirstOrDefaultAsync(
                    x =>
                        x.NodeId == nodeId &&
                        x.Node.UserId == userId,
                    cancellationToken);

            if (group == null)
                return NotFound("Group node not found.");

            group.Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();

            group.Node.UpdatedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        [HttpPatch("{nodeId:long}/notes")]
        public async Task<IActionResult> UpdateNodeNotes(long nodeId, UpdateNodeNotesRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out long userId))
                return Unauthorized();

            FileItem? file = await _db.FileItems
                .Include(x => x.Node)
                .FirstOrDefaultAsync(
                    x =>
                        x.NodeId == nodeId &&
                        x.Node.UserId == userId,
                    cancellationToken);

            if (file == null)
                return NotFound("File node not found.");

            file.Notes = string.IsNullOrWhiteSpace(request.Notes)
                ? null
                : request.Notes.Trim();

            file.UpdatedAtUtc = DateTime.UtcNow;
            file.Node.UpdatedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        [HttpDelete("{nodeId:long}")]
        public async Task<IActionResult> DeleteNode(long nodeId, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out long userId))
                return Unauthorized();

            HierarchyNode? node = await _db.HierarchyNodes
                .Include(x => x.FileItem)
                .Include(x => x.GroupItem)
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == nodeId &&
                        x.UserId == userId,
                    cancellationToken);

            if (node == null)
                return NotFound("Hierarchy node not found.");

            bool hasChildren = await _db.HierarchyNodes.AnyAsync(
                x =>
                    x.UserId == userId &&
                    x.ParentNodeId == nodeId,
                cancellationToken);

            if (hasChildren)
                return BadRequest("Cannot delete a group that contains items.");

            List<StorageReference> references = await _db.StorageReferences
                .Where(x =>
                    x.UserId == userId &&
                    x.FileNodeId == nodeId)
                .ToListAsync(cancellationToken);

            foreach (StorageReference reference in references)
            {
                reference.FileNodeId = null;
            }

            _db.HierarchyNodes.Remove(node);

            await _db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        private async Task<bool> IsDescendantOfAsync(long possibleDescendantId, long possibleAncestorId, long userId, CancellationToken cancellationToken)
        {
            long? currentId = possibleDescendantId;

            while (currentId != null)
            {
                if (currentId.Value == possibleAncestorId)
                    return true;

                currentId = await _db.HierarchyNodes
                    .Where(x =>
                        x.Id == currentId.Value &&
                        x.UserId == userId)
                    .Select(x => x.ParentNodeId)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            return false;
        }

        private bool TryGetCurrentUserId(out long userId)
        {
            string? userIdText = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return long.TryParse(userIdText, out userId);
        }
    }
}
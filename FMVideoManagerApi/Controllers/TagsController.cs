using FMVideoManagerApi.Data;
using FMVideoManagerApi.Data.DTO.Tags;
using FMVideoManagerApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FMVideoManagerApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/tags")]
    public sealed class TagsController : ControllerBase
    {
        private readonly ServerDbContext _db;

        public TagsController(ServerDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<List<TagDto>>> GetTags(CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out long userId))
                return Unauthorized();

            List<TagDto> tags = await _db.Tags
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.Name)
                .Select(x => new TagDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    BackgroundColorHex = x.BackgroundColorHex,
                    ForegroundColorHex = x.ForegroundColorHex
                })
                .ToListAsync(cancellationToken);

            return Ok(tags);
        }

        [HttpPost]
        public async Task<ActionResult<TagDto>> CreateTag(CreateTagRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out long userId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Tag name is required.");

            string name = request.Name.Trim();

            bool exists = await _db.Tags.AnyAsync(
                x =>
                    x.UserId == userId &&
                    x.Name == name,
                cancellationToken);

            if (exists)
                return Conflict("Tag with this name already exists.");

            DateTime now = DateTime.UtcNow;

            var tag = new Tag
            {
                UserId = userId,
                Name = name,
                Description = string.IsNullOrWhiteSpace(request.Description)
                    ? null
                    : request.Description.Trim(),
                BackgroundColorHex = string.IsNullOrWhiteSpace(request.BackgroundColorHex)
                    ? null
                    : request.BackgroundColorHex.Trim(),
                ForegroundColorHex = string.IsNullOrWhiteSpace(request.ForegroundColorHex)
                    ? null
                    : request.ForegroundColorHex.Trim(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            _db.Tags.Add(tag);

            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new TagDto
            {
                Id = tag.Id,
                Name = tag.Name,
                Description = tag.Description,
                BackgroundColorHex = tag.BackgroundColorHex,
                ForegroundColorHex = tag.ForegroundColorHex
            });
        }

        [HttpGet("nodes/{nodeId:long}")]
        public async Task<ActionResult<List<TagDto>>> GetNodeTags(long nodeId, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out long userId))
                return Unauthorized();

            bool nodeExists = await _db.HierarchyNodes.AnyAsync(
                x =>
                    x.Id == nodeId &&
                    x.UserId == userId,
                cancellationToken);

            if (!nodeExists)
                return NotFound("Node not found.");

            List<TagDto> tags = await _db.NodeTags
                .AsNoTracking()
                .Where(x =>
                    x.UserId == userId &&
                    x.NodeId == nodeId)
                .OrderBy(x => x.Tag.Name)
                .Select(x => new TagDto
                {
                    Id = x.Tag.Id,
                    Name = x.Tag.Name,
                    Description = x.Tag.Description,
                    BackgroundColorHex = x.Tag.BackgroundColorHex,
                    ForegroundColorHex = x.Tag.ForegroundColorHex
                })
                .ToListAsync(cancellationToken);

            return Ok(tags);
        }

        [HttpPost("nodes/{nodeId:long}/{tagId:long}")]
        public async Task<IActionResult> ApplyTagToNode(long nodeId, long tagId, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out long userId))
                return Unauthorized();

            bool nodeExists = await _db.HierarchyNodes.AnyAsync(
                x =>
                    x.Id == nodeId &&
                    x.UserId == userId,
                cancellationToken);

            if (!nodeExists)
                return NotFound("Node not found.");

            bool tagExists = await _db.Tags.AnyAsync(
                x =>
                    x.Id == tagId &&
                    x.UserId == userId,
                cancellationToken);

            if (!tagExists)
                return NotFound("Tag not found.");

            bool alreadyApplied = await _db.NodeTags.AnyAsync(
                x =>
                    x.NodeId == nodeId &&
                    x.TagId == tagId &&
                    x.UserId == userId,
                cancellationToken);

            if (alreadyApplied)
                return NoContent();

            _db.NodeTags.Add(new NodeTag
            {
                NodeId = nodeId,
                TagId = tagId,
                UserId = userId,
                CreatedAtUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        [HttpDelete("nodes/{nodeId:long}/{tagId:long}")]
        public async Task<IActionResult> RemoveTagFromNode(long nodeId, long tagId, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out long userId))
                return Unauthorized();

            NodeTag? nodeTag = await _db.NodeTags
                .FirstOrDefaultAsync(
                    x =>
                        x.NodeId == nodeId &&
                        x.TagId == tagId &&
                        x.UserId == userId,
                    cancellationToken);

            if (nodeTag == null)
                return NoContent();

            _db.NodeTags.Remove(nodeTag);

            await _db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        private bool TryGetCurrentUserId(out long userId)
        {
            string? userIdText = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return long.TryParse(userIdText, out userId);
        }
    }
}
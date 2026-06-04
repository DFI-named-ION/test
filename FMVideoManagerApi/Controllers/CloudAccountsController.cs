using FMVideoManagerApi.Data;
using FMVideoManagerApi.Data.DTO;
using FMVideoManagerApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FMVideoManagerApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/cloud/accounts")]
    public sealed class CloudAccountsController : ControllerBase
    {
        private readonly ServerDbContext _db;

        public CloudAccountsController(ServerDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<List<CloudProviderAccountDto>>> GetAccounts(CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out long userId))
                return Unauthorized();

            List<CloudProviderAccountDto> accounts = await _db.CloudProviderAccounts
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.Provider)
                .ThenBy(x => x.DisplayName)
                .Select(x => new CloudProviderAccountDto
                {
                    Id = x.Id,
                    Provider = x.Provider,
                    ProviderAccountId = x.ProviderAccountId,
                    DisplayName = x.DisplayName,
                    Email = x.Email,
                    Scopes = x.Scopes,
                    IsActive = x.IsActive,
                    CreatedAtUtc = x.CreatedAtUtc,
                    UpdatedAtUtc = x.UpdatedAtUtc,
                    LastSyncAtUtc = x.LastSyncAtUtc
                })
                .ToListAsync(cancellationToken);

            return Ok(accounts);
        }

        [HttpPost("{accountId:long}/deactivate")]
        public async Task<IActionResult> DeactivateAccount(long accountId, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out long userId))
                return Unauthorized();

            CloudProviderAccount? account = await _db.CloudProviderAccounts
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == accountId &&
                        x.UserId == userId,
                    cancellationToken);

            if (account == null)
                return NotFound("Cloud account not found.");

            if (!account.IsActive)
                return NoContent();

            account.IsActive = false;
            account.UpdatedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        [HttpPost("{accountId:long}/activate")]
        public async Task<IActionResult> ActivateAccount(long accountId, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out long userId))
                return Unauthorized();

            CloudProviderAccount? account = await _db.CloudProviderAccounts
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == accountId &&
                        x.UserId == userId,
                    cancellationToken);

            if (account == null)
                return NotFound("Cloud account not found.");

            if (account.IsActive)
                return NoContent();

            account.IsActive = true;
            account.UpdatedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        [HttpDelete("{accountId:long}")]
        public async Task<IActionResult> RemoveAccount(long accountId, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out long userId))
                return Unauthorized();

            CloudProviderAccount? account = await _db.CloudProviderAccounts
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == accountId &&
                        x.UserId == userId,
                    cancellationToken);

            if (account == null)
                return NotFound("Cloud account not found.");

            _db.CloudProviderAccounts.Remove(account);

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
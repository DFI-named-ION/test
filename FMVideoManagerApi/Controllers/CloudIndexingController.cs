using FMVideoManagerApi.Data.DTO.Indexing;
using FMVideoManagerApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FMVideoManagerApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/cloud/indexing")]
    public sealed class CloudIndexingController : ControllerBase
    {
        private readonly CloudIndexingJobService _cloudIndexingJobService;

        public CloudIndexingController(
            CloudIndexingJobService cloudIndexingJobService)
        {
            _cloudIndexingJobService = cloudIndexingJobService;
        }

        [HttpGet("jobs/active")]
        public ActionResult<CloudIndexingJobDto> GetActiveCloudIndexingJob()
        {
            if (!TryGetCurrentUserId(out long userId))
                return Unauthorized();

            CloudIndexingJobDto? job = _cloudIndexingJobService.GetActiveJob(userId);

            if (job == null)
                return NotFound("No active cloud indexing job.");

            return Ok(job);
        }

        [HttpGet("jobs/{jobId:guid}")]
        public ActionResult<CloudIndexingJobDto> GetCloudIndexingJob(Guid jobId)
        {
            if (!TryGetCurrentUserId(out long userId))
                return Unauthorized();

            CloudIndexingJobDto? job = _cloudIndexingJobService.GetJob(userId, jobId);

            if (job == null)
                return NotFound("Cloud indexing job not found.");

            return Ok(job);
        }

        [HttpPost("jobs/{jobId:guid}/cancel")]
        public IActionResult CancelCloudIndexingJob(Guid jobId)
        {
            if (!TryGetCurrentUserId(out long userId))
                return Unauthorized();

            bool cancelled = _cloudIndexingJobService.CancelJob(userId, jobId);

            if (!cancelled)
                return NotFound("Cloud indexing job not found.");

            return NoContent();
        }

        private bool TryGetCurrentUserId(out long userId)
        {
            string? userIdText = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return long.TryParse(userIdText, out userId);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using StatsServer.DataAccess;

namespace StatsServer.Controllers
{
    [ApiController]
    [Route("stats")]
    public class StatsController : ControllerBase
    {
        private readonly StatsData _statsData;

        public StatsController(StatsData statsData)
        {
            _statsData = statsData;
        }

        [HttpGet("users")]
        public IActionResult GetAllUsers()
        {
            try
            {
                var totalUsers = _statsData.GetTotalUsers();

                if (totalUsers == 0)
                {
                    return NotFound("No one logged in.");
                }

                return Ok(totalUsers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error: " + ex.Message);
            }
        }
    }
}
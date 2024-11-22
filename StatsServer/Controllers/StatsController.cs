using Microsoft.AspNetCore.Mvc;
using ServerApp.DataAccess;
using StatsServer.DataAccess;
using StatsServer.Domain;

namespace StatsServer.Controllers
{
    [ApiController]
    [Route("stats")]
    public class StatsController : ControllerBase
    {
        private readonly StatsData _statsData;
        private readonly GameRepository _gameRepository;

        public StatsController(StatsData statsData, GameRepository gameRepository)
        {
            _statsData = statsData;
            _gameRepository = gameRepository;
        }


        [HttpGet("users")]
        public Task<IActionResult> GetAllUsers()
        {
            try
            {
                var totalUsers = _statsData.GetTotalLogins();

                if (totalUsers == 0)
                {
                    return Task.FromResult<IActionResult>(NotFound("No one logged in."));
                }

                return Task.FromResult<IActionResult>(Ok(totalUsers));
            }
            catch (Exception ex)
            {
                return Task.FromResult<IActionResult>(StatusCode(500, "Internal Server Error: " + ex.Message));
            }
        }

        [HttpGet("filtered")]
        public Task<IActionResult> GetFilteredGames([FromQuery] FilterGame criteria)
        {
            try
            {
                var filteredGames =  _gameRepository.GetFilteredGames(criteria);

                if (filteredGames.Length == 0)
                {
                    return Task.FromResult<IActionResult>(NotFound("No games found with the specified criteria."));
                }

                return Task.FromResult<IActionResult>(Ok(filteredGames));
            }
            catch (Exception ex)
            {
                return Task.FromResult<IActionResult>(StatusCode(500, "Internal Server Error: " + ex.Message));
            }
        }
    }
}
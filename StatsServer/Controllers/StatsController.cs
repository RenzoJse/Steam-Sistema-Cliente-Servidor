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
                var filteredGames =  _gameRepository.GetFilteredGames(criteria).Result;

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

        [HttpGet("reports")]
        public Task<IActionResult> GetSalesReport()
        {
            try
            {
                var salesReport = _gameRepository.GetSalesReport().Result;

                if (salesReport == null)
                {
                    return Task.FromResult<IActionResult>(NotFound("No sales report available."));
                }

                return Task.FromResult<IActionResult>(Ok(salesReport));
            }
            catch (Exception ex)
            {
                return Task.FromResult<IActionResult>(StatusCode(500, "Internal Server Error: " + ex.Message));
            }
        }

        [HttpPost("reports")]
        public Task<IActionResult> GenerateReport()
        {
            try
            {
                _gameRepository.PostSellsReport();

                return Task.FromResult<IActionResult>(Ok("Report is being generated."));
            }
            catch (Exception ex)
            {
                return Task.FromResult<IActionResult>(StatusCode(500, "Internal Server Error: " + ex.Message));
            }
        }

        [HttpGet("reports/status")]
        public Task<IActionResult> GetSalesReportStatus()
        {
            try
            {
                var salesReport = GameRepository.GetSalesReportStatus().Result;

                if (salesReport == false)
                {
                    return Task.FromResult<IActionResult>(Ok("The report isnt ready."));
                }

                return Task.FromResult<IActionResult>(Ok("The report is ready."));
            }
            catch (Exception ex)
            {
                return Task.FromResult<IActionResult>(StatusCode(500, "Internal Server Error: " + ex.Message));
            }
        }
    }
}
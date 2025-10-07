using Microsoft.AspNetCore.Mvc;
using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    /// <summary>
    /// Status/health-endpoints til monitorerering
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class StatusController : ControllerBase
    {
        private readonly AppDBContext _db;
        public StatusController(AppDBContext db) => _db = db;

        /// <summary>Returnerer OK hvis API kører</summary>
        [HttpGet("healthcheck")]
        public IActionResult HealthCheck() => Ok(new { status = "OK", message = "API'en er kørende!" });

        /// <summary>Tester forbindelse til database med EF Core.</summary>
        [HttpGet("dbhealthcheck")]
        public async Task<IActionResult> DBHealthCheck()
        {
            try
            {
                var canConnect = await _db.Database.CanConnectAsync();
                return canConnect
                    ? Ok(new { status = "OK", message = "Database er kørende!" })
                    : StatusCode(500, new { status = "Error", message = "Kan ikke forbinde til databasen." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "Error", message = "Fejl ved forbindelse til database: " + ex.Message });
            }
        }

        /// <summary>Simpelt ping-endpoint </summary>
        [HttpGet("ping")]
        public IActionResult Ping() => Ok(new { status = "OK", message = "Pong" });
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebMason_final.Server.Data;
using WebMason_final.Server.Models;

namespace WebMason_final.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServerOrderController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ServerOrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateOrder([FromBody] ServerOrder model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return Unauthorized();
            }

            var order = new ServerOrder
            {
                ServerType = model.ServerType,
                OrderDate = DateTime.UtcNow,
                UserId = user.Id,
                User = user
            };

            _context.ServerOrders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Order created successfully" });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var orders = await _context.ServerOrders
                .Where(o => o.UserId == int.Parse(userId))
                .ToListAsync();

            return Ok(orders);
        }
    }
}


using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using WebMason_final.Server.Data;
using WebMason_final.Server.Models;
using Microsoft.AspNetCore.Identity;
using WebMason_final.Server.Utils;
using Org.BouncyCastle.Crypto.Fpe;

namespace WebMason_final.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
        private readonly EmailService _emailService;

        public AuthController(ApplicationDbContext context, IConfiguration configuration, IPasswordHasher<ApplicationUser> passwordHasher, EmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (await _context.Users.AnyAsync(u => u.Username == model.Username || u.Email == model.Email))
            {
                return BadRequest("User already exists.");
            }

            var user = new ApplicationUser
            {
                Username = model.Username,
                Email = model.Email,
                PasswordHash = _passwordHasher.HashPassword(null, model.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Envoyer un e-mail après la création du compte
            await _emailService.SendEmailAsync(model.Email, "Bienvenue sur notre service", "Votre compte WebMason a été créé avec succès.");

            return Ok(new { Message = "User registered successfully" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == model.Username);
            if (user == null || _passwordHasher.VerifyHashedPassword(null, user.PasswordHash, model.Password) == PasswordVerificationResult.Failed)
            {
                return Unauthorized();
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new { Token = tokenString });
        }
    }
}

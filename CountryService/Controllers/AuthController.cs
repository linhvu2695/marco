#nullable disable

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CountryService.Constants;
using CountryService.Dtos;
using CountryService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CountryService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;

        public AuthController(UserManager<IdentityUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationRequestDto requestDto)
        {
            // Validate incoming request
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var userExist = await _userManager.FindByEmailAsync(requestDto.Email);
            if (userExist != null)
            {
                return BadRequest(new AuthResult()
                {
                    Result = false,
                    Errors = new List<string>()
                    {
                        "Email already exist"
                    }
                });
            }

            // Create new user
            var newUser = new IdentityUser()
            {
                Email = requestDto.Email,
                UserName = requestDto.Name,
            };
            var createResult = await _userManager.CreateAsync(newUser, requestDto.Password);
            if (!createResult.Succeeded)
            {
                return BadRequest(new AuthResult()
                {
                    Result = false,
                    Errors = new List<string>()
                    {
                        "Server Error"
                    }
                });
            }

            // Generate JWT Token
            string token = GenerateToken(newUser);
            return Ok(new AuthResult()
            {
                Result = true,
                Token = token
            });
        }

        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequestDto requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResult()
                {
                    Result = false,
                    Errors = new List<string>()
                    {
                        "Invalid payload"
                    }
                });
            }

            // Check if user exist
            var existingUser = await _userManager.FindByEmailAsync(requestDto.Email);
            if (existingUser == null)
            {
                return BadRequest(new AuthResult()
                {
                    Result = false,
                    Errors = new List<string>()
                    {
                        "Invalid payload"
                    }
                });
            }

            // Check if correct password
            var correctPassword = await _userManager.CheckPasswordAsync(existingUser, requestDto.Password);
            if (!correctPassword)
            {
                return BadRequest(new AuthResult()
                {
                    Result = false,
                    Errors = new List<string>()
                    {
                        "Invalid credentials"
                    }
                });
            }

            // Generate JWT Token
            string token = GenerateToken(existingUser);
            return Ok(new AuthResult()
            {
                Result = true,
                Token = token
            });
        }

        private string GenerateToken(IdentityUser user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.UTF8.GetBytes(_configuration[ConnStringKeys.Const.CONFIG_JWT_SECRET]);

            // Token descriptor
            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new []
                {
                    new Claim("Id", user.Id),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Email, value:user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat, DateTime.Now.ToUniversalTime().ToString())
                }),
                Expires = DateTime.Now.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };

            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            return jwtTokenHandler.WriteToken(token);
        }
    }
}
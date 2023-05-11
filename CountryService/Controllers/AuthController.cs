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
using RestSharp;
using RestSharp.Authenticators;

namespace CountryService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        private const string TAG_URL = "#URL#";

        public AuthController(UserManager<IdentityUser> userManager, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationRequestDto requestDto)
        {
            _logger.LogInformation("--> Incoming register request...", DateTime.UtcNow);
            // Validate incoming request
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var userExist = await _userManager.FindByEmailAsync(requestDto.Email);
            if (userExist != null)
            {
                _logger.LogWarning($"Email {requestDto.Email} already exist", DateTime.UtcNow);
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
                _logger.LogWarning($"Fail to create new user for email {requestDto.Email}", DateTime.UtcNow);
                return BadRequest(new AuthResult()
                {
                    Result = false,
                    Errors = new List<string>()
                    {
                        "Unable to create user. Consider using strong password"
                    }
                });
            }
            _logger.LogInformation($"New user created. Username: {requestDto.Name}, Email: {requestDto.Email}", DateTime.UtcNow);

            // Send confirm email
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);
            var emailBody = $"Please confirm your email address <a href =\"{TAG_URL}\">Click here</a>";
            var callbackUrl = Request.Scheme + "://" + Request.Host 
                + Url.Action("ConfirmEmail", "Auth", new {
                    userId = newUser.Id,
                    code = code
                }); // eg: http://localhost:5026/confirmemail/userid=xyz&code=a1b2c3
            emailBody = emailBody.Replace(
                TAG_URL, 
                callbackUrl
            ); 

            bool isEmailSent = SendEmail(emailBody, requestDto.Email);
            if (!isEmailSent)
            {
                _logger.LogWarning($"Fail to send confirmation email to {requestDto.Email}", DateTime.UtcNow);
                return Ok($"User created but not verified. Please request a verification link for email {requestDto.Email}");
            }

            _logger.LogInformation($"Confirmation email sent to {requestDto.Email}", DateTime.UtcNow);
            return Ok($"Email verification sent. Please verify your email at {requestDto.Email}");
        }

        [HttpGet]
        [Route("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail (string userId, string code)
        {
            _logger.LogInformation($"Confirming email for user {userId}",DateTime.UtcNow);
            if (userId.IsNullOrEmpty() || code.IsNullOrEmpty())
            {
                return BadRequest(new AuthResult()
                {
                    Result = false,
                    Errors = new List<string>()
                    {
                        "Invalid email confirmation parameters 1"
                    }
                });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return BadRequest(new AuthResult()
                {
                    Result = false,
                    Errors = new List<string>()
                    {
                        "Invalid email confirmation parameters 2"
                    }
                });
            }

            var result = await _userManager.ConfirmEmailAsync(user, code);
            var status = result.Succeeded ? 
                "Thank you for confirming your email" :
                "Email canot be confirmed. Please try again later";
            
            _logger.LogInformation("Email confirmation result: " + (result.Succeeded ? "Success" : "Fail") , DateTime.UtcNow);
            return Ok(status);
        }


        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequestDto requestDto)
        {
            _logger.LogInformation($"Login {requestDto.Email}...");
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

            // Check if email is confirmed
            if (!existingUser.EmailConfirmed)
            {
                return BadRequest(new AuthResult()
                {
                    Result = false,
                    Errors = new List<string>()
                    {
                        "Email needs to be confirmed"
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

        private bool SendEmail(string body, string receiverEmail)
        {
            // Create client
            var client = new RestClient("https://api.mailgun.net/v3/");

            var apiKey = _configuration[ConnStringKeys.Const.CONFIG_EMAIL_API_KEY];
            var encodedApiKey = Convert.ToBase64String(Encoding.UTF8.GetBytes($"api:{apiKey}"));

            // Create request
            var request = new RestRequest();
            request.AddHeader("Authorization", $"Basic {encodedApiKey}");
            request.AddParameter("domain", _configuration[ConnStringKeys.Const.CONFIG_EMAIL_DOMAIN], ParameterType.UrlSegment);
            request.Resource = "{domain}/messages";
            request.AddParameter("from", $"Marco Polo mailgun@{_configuration[ConnStringKeys.Const.CONFIG_EMAIL_DOMAIN]}");
            request.AddParameter("to",receiverEmail);
            request.AddParameter("subject","Email Verification ");
            request.AddParameter("text", body);
            request.Method = Method.Post;

            var response = client.Execute(request);
            _logger.LogInformation($"Email API response: {response.Content}", DateTime.UtcNow);
            return response.IsSuccessful;
        }
    }
}
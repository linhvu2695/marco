#nullable disable

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CountryService.Constants;
using CountryService.Data;
using CountryService.Dtos;
using CountryService.Global;
using CountryService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly AppDbContext _context;
        private readonly TokenValidationParameters _tokenValidationParameters;

        private const string TAG_URL = "#URL#";

        public AuthController(UserManager<IdentityUser> userManager, IConfiguration configuration, ILogger<AuthController> logger, AppDbContext context, TokenValidationParameters tokenValidationParameters)
        {
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
            _context = context;
            _tokenValidationParameters = tokenValidationParameters;
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
            var genTokenResult = await GenerateJwtToken(existingUser);
            return Ok(genTokenResult);
        }

        [HttpPost]
        [Route("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequestDto tokenRequest)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(new AuthResult()
                {
                    Result = false,
                    Errors = new List<string>()
                    {
                        "Invalid parameters"
                    }
                });
            }

            var result = await VerifyAndGenerateToken(tokenRequest);
            if (result == null)
            {
                return BadRequest(new AuthResult()
                {
                    Result = false,
                    Errors = new List<string>()
                    {
                        "Invalid tokens"
                    }
                });
            }
            return Ok(result);
        }

        private async Task<AuthResult> VerifyAndGenerateToken(TokenRequestDto tokenRequest)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            try
            {
                // Verify JWT Token
                _tokenValidationParameters.ValidateLifetime = false;
                var tokenInVerification = jwtTokenHandler.ValidateToken(tokenRequest.Token, _tokenValidationParameters, out var validatedToken);

                if (!(validatedToken is JwtSecurityToken jwtSecurityToken)) return null;
                var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);
                if (result == false) return null;

                // Check JWT token expiry time
                var utcExpiryDate = long.Parse(tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp).Value);
                var expiryDate = DateTimeTools.UnixTimeStampToDateTime(utcExpiryDate);
                System.Console.WriteLine(expiryDate);
                System.Console.WriteLine(DateTime.UtcNow);
                if (expiryDate > DateTime.UtcNow)
                {
                    return new AuthResult()
                    {
                        Result = false,
                        Errors = new List<string>()
                        {
                            "JWT Token not yet expired"
                        }
                    };
                }
                
                // Check refresh token
                var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == tokenRequest.RefreshToken);
                if (storedToken == null)
                {
                    return new AuthResult()
                    {
                        Result = false,
                        Errors = new List<string>()
                        {
                            "Invalid Token"
                        }
                    };
                }
                if (storedToken.IsRevoked)
                {
                    return new AuthResult()
                    {
                        Result = false,
                        Errors = new List<string>()
                        {
                            "Invalid Token"
                        }
                    };
                }

                // Check Refresh token expiry JwtId
                var jti = tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;
                if (jti != storedToken.JwtId)
                {
                    return new AuthResult()
                    {
                        Result = false,
                        Errors = new List<string>()
                        {
                            "Invalid Token"
                        }
                    };
                }

                // Check Refresh token expiry time
                if (storedToken.ExpiryDate < DateTime.UtcNow)
                {
                    return new AuthResult()
                    {
                        Result = false,
                        Errors = new List<string>()
                        {
                            "Expired Token"
                        }
                    };
                }

                storedToken.IsUsed = true;
                _context.RefreshTokens.Update(storedToken);
                await _context.SaveChangesAsync();

                var dbUser = await _userManager.FindByIdAsync(storedToken.UserId);
                return await GenerateJwtToken(dbUser);
            }
            catch (Exception ex)
            {
                return new AuthResult()
                    {
                        Result = false,
                        Errors = new List<string>()
                        {
                            "Server Error"
                        }
                    };
            }
        }

        private async Task<AuthResult> GenerateJwtToken(IdentityUser user)
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
                Expires = DateTime.UtcNow.Add(TimeSpan.Parse(_configuration[ConnStringKeys.Const.CONFIG_JWT_EXPIRY_TIME])),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };

            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = jwtTokenHandler.WriteToken(token);

            var refreshToken = new RefreshToken()
            {
                JwtId = token.Id,
                Token = RandomStringGeneration(23),
                AddedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddMonths(6),
                IsRevoked = false,
                IsUsed = false,
                UserId = user.Id
            };
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            return new AuthResult()
            {
                Result = true,
                Token = jwtToken,
                RefreshToken = refreshToken.Token
            };
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

        private string RandomStringGeneration(int length)
        {
            var random = new Random();
            var chars = ConstStrings.Const.ALPHANUMERIC_CHARS;
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
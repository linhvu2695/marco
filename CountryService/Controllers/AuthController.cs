#nullable disable

using System.Security.Cryptography;
using System.Text;
using CountryService.Constants;
using CountryService.Data;
using CountryService.Dtos;
using CountryService.Models;
using Microsoft.AspNetCore.Mvc;
using RestSharp;

namespace CountryService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepo _userRepo;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;

        private const string TAG_URL = "#URL#";

        public AuthController(IUserRepo userRepo, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _userRepo = userRepo;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register(UserRegistrationRequest request)
        {
            _logger.LogInformation("--> Incoming register request...", DateTime.UtcNow);
            if (_userRepo.EmailExists(request.Email))
            {
                return BadRequest("Email already exists");
            }

            var passwordEncryptionMethod = Security.Const.PASSWORD_ENCRYPTION_HMACSHA512;
            CreatePasswordHash(request.Password, passwordEncryptionMethod, out byte[] PasswordHash, out byte[] PasswordSalt);

            // Create new user
            var newUser = new User()
            {
                Email = request.Email,
                PasswordHash = PasswordHash,
                PasswordSalt = PasswordSalt,
                PasswordEncryptionMethod = passwordEncryptionMethod,
                VerificationToken = CreateRandomToken()
            };
            _userRepo.CreateUser(newUser);
            _userRepo.SaveChanges();

            // Send confirm email
            var emailBody = $"Please confirm your email address <a href =\"{TAG_URL}\">Click here</a>";
            var callbackUrl = Request.Scheme + "://" + Request.Host 
                + Url.Action("ConfirmEmail", "Auth", new {
                    userId = newUser.Id,
                    code = newUser.VerificationToken
                }); // eg: http://localhost:5026/confirmemail/userid=123&code=A1b2C3d4E5
            emailBody = emailBody.Replace(
                TAG_URL, 
                callbackUrl
            ); 

            bool isEmailSent = SendEmail(emailBody, request.Email);
            if (!isEmailSent)
            {
                _logger.LogWarning($"Fail to send confirmation email to {request.Email}", DateTime.UtcNow);
                return Ok($"User created but not verified. Please request a verification link for email {request.Email}");
            }

            _logger.LogInformation($"Confirmation email sent to {request.Email}", DateTime.UtcNow);
            return Ok($"Email verification sent. Please verify your email at {request.Email}");
        }

        [HttpGet]
        [Route("Login")]
        public async Task<IActionResult> Login(UserLoginRequest request)
        {
            _logger.LogInformation($"Login {request.Email}...");
            var user = _userRepo.GetUserByEmail(request.Email);
            if (user == null)
            {
                return BadRequest("User not found");
            }

            if (user.VerifiedAt == null)
            {
                return BadRequest("User not verified");
            }

            if (!VerifyPasswordHash(request.Password, user.PasswordEncryptionMethod, user.PasswordHash, user.PasswordSalt))
            {
                return BadRequest("Incorrect password");
            }

            return Ok($"Welcome, user {user.Email}");
        }

        [HttpGet]
        [Route("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            _logger.LogInformation($"Confirming email for user {userId}",DateTime.UtcNow);
            int iUserId;
            if (userId == null || code == null || !int.TryParse(userId, out iUserId))
            {
                return BadRequest("Invalid email confirmation parameters");
            }

            var user = _userRepo.GetUserById(iUserId);
            if (user == null || user.VerificationToken != code)
            {
                return BadRequest("Invalid email confirmation parameters");
            }

            user.VerifiedAt = DateTime.Now;
            _userRepo.SaveChanges();
            return Ok("Email Verified");
        }

        private void CreatePasswordHash(string password, string passwordEncryptionMethod, out byte[] passwordHash, out byte[] passwordSalt)
        {
            switch(passwordEncryptionMethod)
            {
                case Security.Const.PASSWORD_ENCRYPTION_HMACSHA512:
                {
                    using (var hmac = new HMACSHA256())
                    {
                        passwordSalt = hmac.Key;
                        passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                    }
                    break;
                }
                default:
                {
                    passwordSalt = Encoding.UTF8.GetBytes(String.Empty);
                    passwordHash = Encoding.UTF8.GetBytes(password);
                    break;
                }
            }
        }

        private bool VerifyPasswordHash(string password, string passwordEncryptionMethod, byte[] passwordHash, byte[] passwordSalt)
        {
            switch(passwordEncryptionMethod)
            {
                case Security.Const.PASSWORD_ENCRYPTION_HMACSHA512:
                {
                    using (var hmac = new HMACSHA256(passwordSalt))
                    {
                        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                        return computedHash.SequenceEqual(passwordHash);
                    }
                }
                default:
                {
                    return Encoding.UTF8.GetString(passwordHash) == password;
                }
            }
        }

        private bool SendEmail(string body, string receiverEmail)
        {
            // Create client
            var client = new RestClient("https://api.mailgun.net/v3/");

            var apiKey = _configuration[AppSettingsKeys.Const.CONFIG_EMAIL_API_KEY];
            var encodedApiKey = Convert.ToBase64String(Encoding.UTF8.GetBytes($"api:{apiKey}"));

            // Create request
            var request = new RestRequest();
            request.AddHeader("Authorization", $"Basic {encodedApiKey}");
            request.AddParameter("domain", _configuration[AppSettingsKeys.Const.CONFIG_EMAIL_DOMAIN], ParameterType.UrlSegment);
            request.Resource = "{domain}/messages";
            request.AddParameter("from", $"Marco Polo mailgun@{_configuration[AppSettingsKeys.Const.CONFIG_EMAIL_DOMAIN]}");
            request.AddParameter("to",receiverEmail);
            request.AddParameter("subject","Email Verification ");
            request.AddParameter("text", body);
            request.Method = Method.Post;

            var response = client.Execute(request);
            _logger.LogInformation($"Email API response: {response.Content}", DateTime.UtcNow);
            return response.IsSuccessful;
        }

        private string CreateRandomToken()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
        }
    }
}
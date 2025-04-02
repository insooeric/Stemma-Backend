using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DotNetEnv;

namespace Stemma.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly bool isDevMode = false;
        private readonly string devMode_Client_ID = "";
        private readonly string devMode_Client_Secret = "";
        //private readonly IConfiguration _configuration;

        public AuthController(IHttpClientFactory httpClientFactory)
        {
            Env.Load();
            _httpClientFactory = httpClientFactory;
            isDevMode = (Environment.GetEnvironmentVariable("DEV_MODE") ?? "false").ToLower().Equals("true");
            devMode_Client_ID = Environment.GetEnvironmentVariable("GITHUB_DEV_CLIENT_ID") ?? "";
            devMode_Client_Secret = Environment.GetEnvironmentVariable("GITHUB_DEV_CLIENT_SECRET") ?? "";
        }

        [HttpGet("github/callback")]
        public async Task<IActionResult> GitHubCallback([FromQuery] string code, [FromQuery] string state)
        {
            var accessToken = await ExchangeCodeForToken(code);
            if (string.IsNullOrEmpty(accessToken))
                return BadRequest("Failed to get GitHub token.");

            var (id, login, email, avatarUrl) = await GetGitHubUserInfo(accessToken);
            if (string.IsNullOrEmpty(id))
                return BadRequest("Failed to retrieve user info.");

            var jwt = GenerateJwtToken(id, login, email, avatarUrl);

            Response.Cookies.Append("token", jwt, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/",
                Expires = DateTime.UtcNow.AddHours(2)
            });

            string redirectUrl = isDevMode ? "http://localhost:3000/" : "https://stemma.vercel.app/";
            string htmlContent = $@"
    <!DOCTYPE html>
    <html>
      <head>
        <meta charset=""UTF-8"">
        <title>Login Successful</title>
        <meta http-equiv=""refresh"" content=""2;url={redirectUrl}"" />
      </head>
<body>
    <div class=""main-content"">
        <span class=""title"">Welcome to</span>
        <div class=""logo-container"">
            <svg xmlns=""http://www.w3.org/2000/svg"" width=""330"" height=""100"" viewBox=""0 0 330 100"">
                <defs>
                    <linearGradient id=""grad1"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""0%"">
                        <stop offset=""0%"" style=""stop-color:#3498db;stop-opacity:1"" />
                        <stop offset=""100%"" style=""stop-color:#9b59b6;stop-opacity:1"" />
                    </linearGradient>
                </defs>
                <text x=""50%"" y=""50%"" text-anchor=""middle"" dominant-baseline=""middle"" font-family=""Arial, sans-serif""
                    font-size=""80"" fill=""url(#grad1)"" stroke=""url(#grad1)"" stroke-width=""6"" stroke-linecap=""round""
                    stroke-linejoin=""round"" letter-spacing=""5"" transform=""translate(0,0)"">
                    Stemma
                </text>
            </svg>
        </div>


        <span class=""description"">
            <p>Login Successful!</p>
            <p>Redirecting to Home...</p>
        </span>
    </div>
    <style>
        body {{
            display: flex;
            flex-direction: column;
            align-items: center;
            background-color: #242424;
            color: white;
        }}

        .main-content {{
            display: flex;
            flex-direction: column;
            align-items: center;
            width: 90%;
            box-sizing: border-box;
        }}

        .title {{
            font-size: 2rem;
            font-weight: bold;
        }}

        .description {{
            display: flex;
            flex-direction: column;
            align-items: center;
            font-size: 1.5rem;
        }}
    </style>
        <script type=""text/javascript"">
          setTimeout(function() {{
            window.location.href = '{redirectUrl}';
          }}, 2000);
        </script>

</body>
    </html>";

            return Content(htmlContent, "text/html");
        }

        [HttpGet("user")]
        public IActionResult GetUser()
        {
            var token = Request.Cookies["token"];
            if (string.IsNullOrEmpty(token))
                return Unauthorized("No token cookie found.");

            try
            {
                var principal = ValidateJwtToken(token);
                if (principal == null)
                    return Unauthorized("Invalid token.");

                var userId = principal.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
                var username = principal.Claims.FirstOrDefault(c => c.Type == "username")?.Value;
                var email = principal.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                var avatarUrl = principal.Claims.FirstOrDefault(c => c.Type == "avatar_url")?.Value;

                return Ok(new
                {
                    id = userId,
                    username = username,
                    email = email,
                    avatarUrl = avatarUrl
                });
            }
            catch
            {
                return Unauthorized();
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("token");

            return Ok(new { message = "Logged out successfully." });
        }


        private async Task<string?> ExchangeCodeForToken(string code)
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token");

            string clientId = Environment.GetEnvironmentVariable("GITHUB_CLIENT_ID") ?? "";
            string clientSecret = Environment.GetEnvironmentVariable("GITHUB_CLIENT_SECRET") ?? "";

            if (isDevMode)
            {
                clientId = devMode_Client_ID;
                clientSecret = devMode_Client_Secret;
            }

            var body = new Dictionary<string, string>
            {
                {"client_id", clientId},
                {"client_secret", clientSecret},
                {"code", code}
            };

            request.Content = new FormUrlEncodedContent(body);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var data = JObject.Parse(json);
            return data["access_token"]?.ToString();
        }

        private async Task<(string id, string login, string email, string avatarUrl)> GetGitHubUserInfo(string accessToken)
        {
            var client = _httpClientFactory.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.UserAgent.ParseAdd("MyApp");

            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode) return (string.Empty, string.Empty, string.Empty, string.Empty);

            var json = await response.Content.ReadAsStringAsync();
            var userObj = JObject.Parse(json);

            var id = userObj["id"]?.ToString() ?? string.Empty;
            var login = userObj["login"]?.ToString() ?? string.Empty;
            var avatarUrl = userObj["avatar_url"]?.ToString() ?? string.Empty;

            var emailRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
            emailRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            emailRequest.Headers.UserAgent.ParseAdd("MyApp");
            var emailResponse = await client.SendAsync(emailRequest);
            if (!emailResponse.IsSuccessStatusCode) return (id, login, string.Empty, avatarUrl);

            var emailJson = await emailResponse.Content.ReadAsStringAsync();
            var emailArr = JArray.Parse(emailJson);
            var primaryEmail = emailArr.FirstOrDefault(e => e["primary"]?.Value<bool>() == true)?["email"]?.ToString() ?? string.Empty;

            return (id, login, primaryEmail, avatarUrl);
        }

        private string GenerateJwtToken(string id, string username, string email, string avatarUrl)
        {
            var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");

            if (string.IsNullOrEmpty(jwtSecret))
            {
                throw new InvalidOperationException("JWT_SECRET environment variable is not set.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("id", id),
                new Claim("username", username),
                new Claim("email", email ?? ""),
                new Claim("avatar_url", avatarUrl ?? "")
            };

            var token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private ClaimsPrincipal ValidateJwtToken(string token)
        {
            var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");

            if (string.IsNullOrEmpty(jwtSecret))
            {
                throw new InvalidOperationException("JWT_SECRET environment variable is not set.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

            var handler = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };

            return handler.ValidateToken(token, parameters, out _);
        }
    }
}

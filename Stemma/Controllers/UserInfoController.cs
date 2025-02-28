using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stemma.Middlewares;
using Stemma.Models;
using System.Text.Json;

namespace Stemma.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserInfoController : ControllerBase
    {
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfileAsync([FromQuery] UserInfoProfileRequest request)
        {
            try
            {
                string githubApiUrl = $"https://api.github.com/users/{request.GithubUserName}";
                string base64Image = "";

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("StemmaApp");

                    var response = await httpClient.GetAsync(githubApiUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(new { Message = $"GitHub API error: {response.ReasonPhrase}" });
                    }

                    string json = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        if (!doc.RootElement.TryGetProperty("avatar_url", out JsonElement avatarUrlElement))
                        {
                            return BadRequest(new { Message = "Avatar URL not found in GitHub response" });
                        }

                        string avatarUrl = avatarUrlElement.GetString() ?? "";

                        IFormFile file = await ProfileHelper.GetAvatarAsFormFileAsync(avatarUrl);

                        byte[] fileBytes;
                        using (var memoryStream = new MemoryStream())
                        {
                            file.CopyTo(memoryStream);
                            fileBytes = memoryStream.ToArray();
                        }

                        base64Image = Convert.ToBase64String(fileBytes);
                    }
                }

                string svgContent = ProfileHelper.GetProfileSvg(base64Image, request.FullName);
                return Content(svgContent, "image/svg+xml");
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Unable to retrieve user from github {ex.Message}" });
            }
        }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Stemma.Middlewares;
using Stemma.Models;
using Stemma.Models.Github;
using Stemma.Redis;
using System.Text.Json;

namespace Stemma.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserInfoController : ControllerBase
    {
        //private readonly IMemoryCache _cache;
        private readonly IRedisService _redisService;

        public UserInfoController(IRedisService redisService)
        {
            _redisService = redisService;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfileAsync([FromQuery] UserInfoProfileRequest request)
        {
            try
            {
                string json = await GitHubHelper.GetData($"https://api.github.com/users/{request.GithubUserName}");
                GithubUserModel? user = JsonSerializer.Deserialize<GithubUserModel>(json);
                if (user == null)
                {
                    throw new Exception("Failed to deserialize GitHub user.");
                }

                IFormFile avatarFile = await ImageHelper.DownloadImageAsFormFileAsync(user.AvatarUrl);

                byte[] fileBytes;
                using (var memoryStream = new MemoryStream())
                {
                    avatarFile.CopyTo(memoryStream);
                    fileBytes = memoryStream.ToArray();
                }

                string base64Image = Convert.ToBase64String(fileBytes);

                user.AvatarUrl = base64Image;

                string svgContent = ProfileHelper.GetProfileSvg(base64Image, request.FullName);
                return Content(svgContent, "image/svg+xml");
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Unable to retrieve user from github {ex.Message}" });
            }
        }

        [HttpGet("statistic")]
        public async Task<IActionResult> GetProjectStatAsync([FromQuery] string GitHubUserName, [FromQuery] string? Items)
        {
            try
            {
                if (string.IsNullOrEmpty(GitHubUserName))
                {
                    return BadRequest(new { Message = $"Github user name is required." });
                }

                List<string> objectNameList = new List<string>();
                if (!string.IsNullOrEmpty(Items))
                {
                    string[] nameArr = Items.Split(",");
                    foreach (string name in nameArr)
                    {
                        string newName = name.Replace(" ", "");
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            return BadRequest(new { Message = "Name of language cannot be null." });
                        }

                        objectNameList.Add(
                            newName
                        );
                    }
                }

                //foreach (string name in objectNameList)
                //{
                //    Console.WriteLine(name);
                //}

                if(objectNameList.Count > 4)
                {
                    return BadRequest(new { Message = "You can only select up to 4 items." });
                }

                if (objectNameList.Distinct(StringComparer.OrdinalIgnoreCase).Count() != objectNameList.Count)
                {
                    return BadRequest("Duplicate found! Each item should be unique.");
                }

                Dictionary<string, int> totalLanguages = new Dictionary<string, int>();

                // !!!MAKE SURE TO UNCOMMENT THIS!!!
                string json = await GitHubHelper.GetData($"https://api.github.com/users/{GitHubUserName}/repos", new string[] { "name" });
                List<GithubRepoModel>? repos = JsonSerializer.Deserialize<List<GithubRepoModel>>(json);
                if (repos == null)
                {
                    return BadRequest(new { Message = "Failed to retrieve repositories from GitHub." });
                }

                //string json = await GitHubHelper.GetData($"https://api.github.com/users/{GitHubUserName}/repos", ["name"]);
                //List<GithubRepoModel> repos = new List<GithubRepoModel>();
                //repos = JsonSerializer.Deserialize<List<GithubRepoModel>>(json);

                // Parallelize the language requests for each repository.
                var languageTasks = repos.Select(async repo =>
                {
                    string detailjson = await GitHubHelper.GetData($"https://api.github.com/repos/{GitHubUserName}/{repo.Name}/languages");
                    return JsonSerializer.Deserialize<Dictionary<string, int>>(detailjson);
                });

                Dictionary<string, int>?[] languagesArray = await Task.WhenAll(languageTasks);
                if (languagesArray.Any(l => l == null))
                {
                    return BadRequest(new { Message = "Failed to retrieve languages for some repositories from GitHub." });
                }

                // Combine language data from all repositories.
                foreach (var repoLanguages in languagesArray)
                {
                    if (repoLanguages == null)
                    {
                        continue;
                    }

                    foreach (var dicObject in repoLanguages)
                    {
                        if (totalLanguages.ContainsKey(dicObject.Key))
                        {
                            totalLanguages[dicObject.Key] += dicObject.Value;
                        }
                        else
                        {
                            totalLanguages[dicObject.Key] = dicObject.Value;
                        }
                    }
                }


                // List<GithubProjectModel> repoDetails = new List<GithubProjectModel>();


                //foreach (GithubRepoModel repo in repos)
                //{
                //    string detailjson = await GitHubHelper.GetData($"https://api.github.com/repos/{GitHubUserName}/{repo.Name}", ["name"]);
                //    GithubProjectModel repodetail = JsonSerializer.Deserialize<GithubProjectModel>(detailjson);
                //    repoDetails.Add(repodetail);
                //}

                //// !!!MAKE SURE TO UNCOMMENT THIS!!!
                //foreach (GithubRepoModel repo in repos)
                //{
                //    string detailjson = await GitHubHelper.GetData($"https://api.github.com/repos/{GitHubUserName}/{repo.Name}/languages");
                //    Dictionary<string, int> repoLanguages = JsonSerializer.Deserialize<Dictionary<string, int>>(detailjson);

                //    foreach (var dicObject in repoLanguages)
                //    {
                //        if (totalLanguages.ContainsKey(dicObject.Key))
                //        {
                //            totalLanguages[dicObject.Key] += dicObject.Value;
                //        }
                //        else
                //        {
                //            totalLanguages[dicObject.Key] = dicObject.Value;
                //        }
                //    }
                //}


                // !!! DUMMY DATA MAKE SURE TO COMMENT THIS !!!
                //totalLanguages = new Dictionary<string, int>
                //{
                //    { "ASP.NET", 363450 },
                //    { "JavaScript", 359920 },
                //    { "C#", 248879 },
                //    { "SCSS", 168195 },
                //    { "ShaderLab", 162022 },
                //    { "HTML", 65762 },
                //    { "TypeScript", 54483 },
                //    { "Java", 28943 },
                //    { "HLSL", 27988 },
                //    { "CSS", 4189 },
                //    { "Dockerfile", 2328 },
                //};


                var sortTotalLanguage = totalLanguages
                    .OrderByDescending(kvp => kvp.Value)
                    .ToDictionary();

                await _redisService.SetLangForUserAsync(GitHubUserName, sortTotalLanguage);

                //if (await _redisService.SetLangForUserAsync(GitHubUserName, sortTotalLanguage) == 1)
                //{
                //    Console.WriteLine("Successfully stored language data in Redis.");
                //} else
                //{
                //    Console.WriteLine("Failed to store language data in Redis.");
                //}

                    //_cache.Set($"{GitHubUserName}-Languages", sortTotalLanguage, TimeSpan.FromMinutes(10));
                    //string serializedLanguages = JsonSerializer.Serialize(sortTotalLanguage);
                    //await _cache.SetStringAsync($"{GitHubUserName}-Languages", serializedLanguages, new DistributedCacheEntryOptions
                    //{
                    //    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    //});

                    //foreach (var dicObject in sortTotalLanguage)
                    //{
                    //    Console.WriteLine($"{dicObject.Key}: {dicObject.Value}");
                    //}
                    //Console.WriteLine("--------------------\n");



                    int total = 0;
                foreach (var lang in sortTotalLanguage)
                {
                    total += lang.Value;
                }


                //Console.WriteLine($"Total: {total.ToString()}");
                //foreach (var dicObject in sortTotalLanguage)
                //{
                //    Console.WriteLine($"{dicObject.Key}: {dicObject.Value}");
                //}
                //Console.WriteLine("--------------------\n");

                Dictionary<string, float> topItemsDic = new Dictionary<string, float>();

                if(objectNameList.Count < 1)
                {
                    foreach (var langObj in sortTotalLanguage.Take(4))
                    {
                        topItemsDic[langObj.Key] = 0.0f;
                    }
                    topItemsDic.Add("Others", 0.0f);
                } else
                {
                    foreach (var langObj in sortTotalLanguage)
                    {
                        if (objectNameList.Contains(langObj.Key))
                        {
                            topItemsDic[langObj.Key] = 0.0f;
                        }
                    }
                    topItemsDic.Add("Others", 0.0f);
                }

                    float occupiedPercentage = 0.0f;
                foreach (var topItem in topItemsDic)
                {
                    if (topItem.Key.Equals("Others"))
                    {
                        continue;
                    }
                    float percentage = totalLanguages[topItem.Key] / (float)total * 100.0f;
                    percentage = (float)Math.Round(percentage, 2);
                    topItemsDic[topItem.Key] = percentage;

                    occupiedPercentage += percentage;
                }


                topItemsDic["Others"] = 100.0f - occupiedPercentage;
                Dictionary<string, float> sortedTopItemsDic = topItemsDic
                    .OrderByDescending(kvp => kvp.Value)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                //foreach (var item in sortedTopItemsDic)
                //{
                //    Console.WriteLine($"Language: {item.Key}, {item.Value}");
                //}



                string svgContent = StatisticHelper.GetStatisticSvg(sortedTopItemsDic);
                return Content(svgContent, "image/svg+xml");
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Unable to retrieve language statistics from user {ex.Message}" });
            }
        }

        [HttpGet("sortedlanguages")]
        public async Task<IActionResult> GetSortedLanguages([FromQuery] string GitHubUserName)
        {
            List<KeyValuePair<string, int>>? serializedLanguages = await _redisService.GetLangFromUserAsync(GitHubUserName);
            if (serializedLanguages.Count > 0)
            {
                return Ok(serializedLanguages);
            }
            return NotFound(new { Message = $"Unable to retrieve language list for user {GitHubUserName}. Either it has expired after 5 minutes or the server failed to retrieve it." });
        }

    }
}

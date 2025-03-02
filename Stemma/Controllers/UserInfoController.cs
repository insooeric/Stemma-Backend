﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stemma.Middlewares;
using Stemma.Models;
using Stemma.Models.Github;
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

                if(objectNameList.Count > 5)
                {
                    return BadRequest(new { Message = "You can only select up to 4 items." });
                }

                if (objectNameList.Distinct(StringComparer.OrdinalIgnoreCase).Count() != objectNameList.Count)
                {
                    return BadRequest("Duplicate found! Each item should be unique.");
                }

                Dictionary<string, int> totalLanguages = new Dictionary<string, int>();

                // !!!MAKE SURE TO UNCOMMENT THIS!!!
                string json = await GitHubHelper.GetData($"https://api.github.com/users/{GitHubUserName}/repos", ["name"]);
                List<GithubRepoModel> repos = new List<GithubRepoModel>();
                repos = JsonSerializer.Deserialize<List<GithubRepoModel>>(json);


                List<GithubProjectModel> repoDetails = new List<GithubProjectModel>();


                foreach (GithubRepoModel repo in repos)
                {
                    string detailjson = await GitHubHelper.GetData($"https://api.github.com/repos/{GitHubUserName}/{repo.Name}", ["name"]);
                    GithubProjectModel repodetail = JsonSerializer.Deserialize<GithubProjectModel>(detailjson);
                    repoDetails.Add(repodetail);
                }

                // !!!MAKE SURE TO UNCOMMENT THIS!!!
                foreach (GithubRepoModel repo in repos)
                {
                    string detailjson = await GitHubHelper.GetData($"https://api.github.com/repos/{GitHubUserName}/{repo.Name}/languages");
                    Dictionary<string, int> repoLanguages = JsonSerializer.Deserialize<Dictionary<string, int>>(detailjson);

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
                    .ToList();



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

    }
}

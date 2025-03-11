﻿using Stemma.Models;
using Stemma.Middlewares;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DotNetEnv;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace Stemma.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BadgeController : ControllerBase
    {
        private readonly string JsonGoogleCred;
        private readonly string BucketName = "badge-bucket";

        public BadgeController()
        {
            try
            {
                Env.Load();

                string privateKey = Environment.GetEnvironmentVariable("GOOGLE_PRIVATE_KEY")?.Replace("\\n", "\n") ?? "";
                var serviceAccountJson = new JObject
                {
                    { "type", Environment.GetEnvironmentVariable("GOOGLE_TYPE") ?? "" },
                    { "project_id", Environment.GetEnvironmentVariable("GOOGLE_PROJECT_ID") ?? "" },
                    { "private_key_id", Environment.GetEnvironmentVariable("GOOGLE_PRIVATE_KEY_ID") ?? "" },
                    { "private_key", privateKey },
                    { "client_email", Environment.GetEnvironmentVariable("GOOGLE_CLIENT_EMAIL") ?? "" },
                    { "client_id", Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? "" },
                    { "auth_uri", Environment.GetEnvironmentVariable("GOOGLE_AUTH_URI") ?? "" },
                    { "token_uri", Environment.GetEnvironmentVariable("GOOGLE_TOKEN_URI") ?? "" },
                    { "auth_provider_x509_cert_url", Environment.GetEnvironmentVariable("GOOGLE_AUTH_PROVIDER_X509_CERT_URL") ?? "" },
                    { "client_x509_cert_url", Environment.GetEnvironmentVariable("GOOGLE_CLIENT_X509_CERT_URL") ?? "" },
                    { "universe_domain", Environment.GetEnvironmentVariable("GOOGLE_UNIVERSE_DOMAIN") ?? "" }
                };

                this.JsonGoogleCred = JsonConvert.SerializeObject(serviceAccountJson);

                if (string.IsNullOrEmpty(JsonGoogleCred))
                {
                    throw new Exception("Error while configuring credentials: Credentials undefined :(");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while configuring credentials: Something went wrong :(", ex);
            }
        }

        [HttpPost("upload-badge")]
        [Authorize] // MAKE SURE TO UNCOMMENT THIS 
        public async Task<IActionResult> UploadBadgeAsync([FromForm] BadgeUploadRequest request)
        {
            try
            {
                if (request.BadgeFile == null || request.BadgeFile.Length <= 0)
                {
                    return BadRequest(new { Message = "Badge image not found" });
                }

                if (string.IsNullOrEmpty(request.BadgeName))
                {
                    return BadRequest(new { Message = "Badge name is required" });
                }

                if (string.IsNullOrEmpty(request.UserId))
                {
                    return BadRequest(new { Message = "User Id is required" });
                }

                if (request.UserId.Equals("-default"))
                {
                    return BadRequest(new { Message = "You are not allowed to change default badge" });
                }

                var validExtensions = new[] { ".png", ".jpg", ".jpeg", ".svg" };
                if (request.BadgeName.Contains("."))
                {
                    return BadRequest(new { Message = "Badge name shouldn't have extension or \".\"" });
                }

                var fileExtension = Path.GetExtension(request.BadgeFile.FileName).ToLower();
                if (!validExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { Message = "Only .png, .jpg, .jpeg, and .svg files are allowed." });
                }

                string userBucketName = request.UserId;
                string fileName = request.BadgeName;

                var credential = GoogleCredential.FromJson(JsonGoogleCred);
                StorageClient storageClient = await StorageClient.CreateAsync(credential);

                string prefix = $"{request.UserId}/";
                var objects = storageClient.ListObjectsAsync(BucketName, prefix);

                if (!await Validator.CheckValidName(Path.GetFileNameWithoutExtension(fileName), JsonGoogleCred))
                {
                    return BadRequest(new { Message = "One of the default badges has the following name. Please choose another name." });
                }

                await foreach (var obj in objects)
                {
                    string fileNameOnly = Path.GetFileNameWithoutExtension(obj.Name.Substring(prefix.Length));
                    if (fileNameOnly.Equals(request.BadgeName, StringComparison.OrdinalIgnoreCase))
                    {
                        return BadRequest(new { Message = "File name already exists." });
                    }
                }

                string finalSVG = ImageHelper.ConvertToSVG(request.BadgeFile, fileName);

                // Console.WriteLine(finalSVG);

                // return BadRequest(new { Message = $"Error: Testing" }); // MAKE SURE TO REMOVE THIS LINE

                string fullPath = $"{userBucketName}/{fileName}.svg";

                string contentType = "image/svg+xml";

                byte[] finalSVGBytes = System.Text.Encoding.UTF8.GetBytes(finalSVG);

                using (var uploadStream = new MemoryStream(finalSVGBytes))
                {
                    await storageClient.UploadObjectAsync(BucketName, fullPath, contentType, uploadStream);
                }

                string gcs_url = $"https://storage.cloud.google.com/{BucketName}/{fullPath}";

                return Ok(new { Message = "File has been uploaded successfully", PublicURL = gcs_url });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Error while uploading badge {ex.Message}" });
            }
        }

        [HttpGet("get-all-default-badge")]
        public async Task<IActionResult> GetAllDefaultBadgeAsync()
        {
            try
            {
                string userBucketName = $"-default";

                var credential = GoogleCredential.FromJson(JsonGoogleCred);
                StorageClient storageClient = await StorageClient.CreateAsync(credential);
                string prefix = $"{userBucketName}";
                var allObjects = storageClient.ListObjectsAsync(BucketName, prefix);

                var badgeList = new List<BadgeObject>();
                await foreach (var storageObject in allObjects)
                {
                    var jsonString = JsonConvert.SerializeObject(storageObject, Formatting.Indented);
                    string fileName = Path.GetFileNameWithoutExtension(storageObject.Name.Replace($"{userBucketName}/", ""));
                    badgeList.Add(new BadgeObject
                    {
                        UserId = userBucketName,
                        BadgeName = fileName,
                        BadgeURL = $"https://stemma.onrender.com/api/badge?badge={fileName}"
                    });
                }

                if (badgeList.Count > 0)
                {
                    badgeList.RemoveAt(0);
                }

                return Ok(new
                {
                    Message = "Retrieved default badges successfully.",
                    Badges = badgeList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Unexpected error: {ex.Message}" });
            }
        }

        [HttpPost("get-all-badge")]
        [Authorize]
        public async Task<IActionResult> GetAllBadgeAsync([FromBody] BadgeGetRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { Message = "Request body is missing." });
                }


                if (string.IsNullOrEmpty(request.UserId))
                {
                    return BadRequest(new { Message = "User ID is required." });
                }

                if (string.IsNullOrEmpty(JsonGoogleCred))
                {
                    return BadRequest(new { Message = "Server configuration error: Missing credentials." });
                }

                string userBucketName = $"{request.UserId}";

                var credential = GoogleCredential.FromJson(JsonGoogleCred);
                StorageClient storageClient = await StorageClient.CreateAsync(credential);
                string prefix = $"{userBucketName}";
                var allObjects = storageClient.ListObjectsAsync(BucketName, prefix);

                var badgeList = new List<BadgeObject>();
                await foreach (var storageObject in allObjects)
                {
                    var jsonString = JsonConvert.SerializeObject(storageObject, Formatting.Indented);
                    // Console.WriteLine(jsonString);
                    string fileName = Path.GetFileNameWithoutExtension(storageObject.Name.Replace($"{userBucketName}/", ""));
                    string fileExtension = Path.GetExtension(storageObject.Name);
                    badgeList.Add(new BadgeObject
                    {
                        UserId = userBucketName,
                        BadgeName = fileName,
                        BadgeURL = $"https://stemma.onrender.com/api/badge?user={userBucketName}&badge={fileName}",
                        ImageType = fileExtension
                    });
                }



                return Ok(new
                {
                    Message = "Retrieved badges successfully.",
                    Badges = badgeList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Unexpected error: {ex.Message}" });
            }
        }

        [HttpPost("update-badge")]
        [Authorize]
        public async Task<IActionResult> UpdateBadgeAsync([FromBody] BadgeUpdateRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { Message = "Request body is missing." });
                }

                if (string.IsNullOrEmpty(request.UserId))
                {
                    return BadRequest(new { Message = "User ID is required." });
                }

                if (string.IsNullOrEmpty(request.OldName))
                {
                    return BadRequest(new { Message = "OldName is required." });
                }

                if (string.IsNullOrEmpty(request.NewName))
                {
                    return BadRequest(new { Message = "NewName is required." });
                }


                if (request.UserId.Equals("-default"))
                {
                    return BadRequest(new { Message = "You are not allowed to update default badge" });
                }

                var credential = GoogleCredential.FromJson(JsonGoogleCred);
                StorageClient storageClient = await StorageClient.CreateAsync(credential);

                string prefix = $"{request.UserId}/";
                var objects = storageClient.ListObjectsAsync(BucketName, prefix);

                if (!await Validator.CheckValidName(Path.GetFileNameWithoutExtension(request.NewName), JsonGoogleCred))
                {
                    return BadRequest(new { Message = $"One of the default badge has the following name. Please choose another name." });
                }

                await foreach (var obj in objects)
                {
                    string fileNameOnly = Path.GetFileNameWithoutExtension(obj.Name.Substring(prefix.Length));

                    if (fileNameOnly.Equals(request.NewName))
                    {
                        return BadRequest(new { Message = $"File name already exists." });
                    }
                }

                string userBucketName = request.UserId;
                string oldObjectPrefix = $"{userBucketName}/{request.OldName}";
                var matchingObjects = storageClient.ListObjectsAsync(BucketName, oldObjectPrefix);
                Google.Apis.Storage.v1.Data.Object? oldBadgeObject = null;

                await foreach (var file in matchingObjects)
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.Name);
                    if (fileNameWithoutExtension.Equals(request.OldName))
                    {
                        oldBadgeObject = file;
                        break;
                    }
                }

                if (oldBadgeObject == null)
                {
                    return NotFound(new { Message = "Badge not found." });
                }

                string oldExtension = Path.GetExtension(oldBadgeObject.Name);
                string newObjectName = $"{userBucketName}/{request.NewName}{oldExtension}";

                try
                {
                    storageClient.CopyObject(
                        sourceBucket: BucketName,
                        sourceObjectName: oldBadgeObject.Name,
                        destinationBucket: BucketName,
                        destinationObjectName: newObjectName
                    );

                    await storageClient.DeleteObjectAsync(BucketName, oldBadgeObject.Name);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { Message = $"Error while copying or deleting badge: {ex.Message}" });
                }

                return Ok(new { Message = "Badge has been updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Unexpected error: {ex.Message}" });
            }
        }


        [HttpDelete("delete-badge")]
        [Authorize]
        public async Task<IActionResult> DeleteBadgeAsync([FromBody] BadgeDeleteRequest request)
        {
            Env.Load();

            try
            {
                if (request == null)
                {
                    return BadRequest(new { Message = "Request body is missing." });
                }

                if (string.IsNullOrEmpty(request.BadgeName))
                {
                    return BadRequest(new { Message = "Badge name is required." });
                }

                if (string.IsNullOrEmpty(request.UserId))
                {
                    return BadRequest(new { Message = "User ID is required." });
                }

                if (request.UserId.Equals("-default"))
                {
                    return BadRequest(new { Message = "You are not allowed to delete default badge" });
                }

                string userBucketName = $"{request.UserId}";
                string fileName = $"{request.BadgeName}";



                if (string.IsNullOrEmpty(JsonGoogleCred))
                {
                    return BadRequest(new { Message = "Server configuration error: Missing credentials." });
                }

                var credential = GoogleCredential.FromJson(JsonGoogleCred);
                StorageClient storageClient = await StorageClient.CreateAsync(credential);

                string prefix = $"{userBucketName}/{fileName}";

                var matchingObjects = storageClient.ListObjectsAsync(BucketName, prefix);
                Google.Apis.Storage.v1.Data.Object? badgeObject = null;

                await foreach (var file in matchingObjects)
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.Name);
                    if (fileNameWithoutExtension.Equals(fileName))
                    {
                        badgeObject = file;
                        break;
                    }
                }

                if (badgeObject == null)
                {
                    return NotFound(new { Message = "Badge not found." });
                }

                try
                {
                    await storageClient.DeleteObjectAsync(BucketName, badgeObject.Name);
                }
                catch (Google.GoogleApiException ex) when (ex.Error.Code == 404)
                {
                    return NotFound(new { Message = "Badge not found during deletion." });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { Message = $"Error while deleting badge: {ex.Message}" });
                }

                return Ok(new { Message = "Badge has been deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Unexpected error: {ex.Message}" });
            }
        }

        [HttpGet("test-svg")]
        public async Task<IActionResult> TestSvgAsync()
        {
            try
            {
                string testSvg = @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""300"" height=""285"" viewBox=""0 0 300 285"" fill=""none"" role=""img"" aria-labelledby=""descId"">
  <title id=""descId"">Circular Image</title>
  <defs>
    <clipPath id=""circleClip"">
      <circle cx=""150"" cy=""142.5"" r=""90"" />
    </clipPath>
    <!-- Adjusted gradient direction -->
    <linearGradient id=""starlightGradient"" x1=""100%"" y1=""0%"" x2=""0%"" y2=""0%"">
      <stop offset=""0%"" stop-color=""yellow"" stop-opacity=""0""/>
      <stop offset=""50%"" stop-color=""yellow"" stop-opacity=""1""/>
      <stop offset=""100%"" stop-color=""yellow"" stop-opacity=""1""/>
    </linearGradient>
    <!-- Define a group for the starlight -->
    <g id=""starlight"">
      <line x1=""0"" y1=""0"" x2=""-60"" y2=""30"" stroke=""url(#starlightGradient)"" stroke-width=""2"" stroke-linecap=""round""/>
    </g>
	
	
  </defs>
  
  <rect data-testid=""card-bg"" x=""0.5"" y=""0.5"" rx=""4.5"" width=""299"" height=""99%"" fill=""#242424"" stroke=""#e4e2e2"" stroke-opacity=""1""/>
  
  <!-- Use the starlight group with animate tags on x and y -->
  <!-- Instance 1 -->
<use href=""#starlight"" transform=""translate(300,0)"">
  <animateTransform attributeName=""transform""
                    type=""translate""
                    values=""0 0; -100 50""
                    keyTimes=""0; 1""
                    additive=""sum""
                    dur=""2s""
                    repeatCount=""indefinite""/>
</use>

</svg>
";
                return Content(testSvg, "image/svg+xml");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Something went wrong :( {ex.Message}" });
            }
        }

        [HttpGet("")]
        // example: https://localhost:32769/api/badge?user=insooeric&badge=auth
        public async Task<IActionResult> GetBadgeAsync([FromQuery] string? user, [FromQuery] string badge, [FromQuery] int? row, [FromQuery] int? col, [FromQuery] bool? fit)
        {
            Env.Load();
            //bool offline_debug = (Environment.GetEnvironmentVariable("OFFLINE_MODE") ?? "").ToLower().Equals("true") ? true : false;
            //Console.WriteLine($"offline debug: {offline_debug}");


            try
            {

                string userFolderName = string.IsNullOrEmpty(user) ? "-default" : user;
                int definedRow = row ?? 0;
                int definedCol = col ?? 0;
                bool defineFitContent = fit ?? false;

                if (definedRow < 0 || definedCol < 0)
                {
                    return BadRequest(new { Message = "Either row or column cannot be less than 1" });
                }


                if (string.IsNullOrEmpty(badge))
                    return BadRequest(new { Message = "Badge name is required." });

                List<ImageObject>? imageList = new List<ImageObject>();

                string[] imageNameArr = badge.Split(",");
                foreach (string imageName in imageNameArr)
                {
                    string newImageName = imageName.Replace(" ", "");
                    if (string.IsNullOrWhiteSpace(imageName))
                    {
                        return BadRequest(new { Message = "Badge name is required." });
                    }

                    imageList.Add(
                        new ImageObject
                        {
                            imageName = newImageName
                        }
                    );
                }

                /* if (offline_debug)
                {
                    foreach (var item in imageList)
                    {
                        switch (item.imageName)
                        {
                            case "cat":
                                item.imageInSvg = @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""40px"" height=""40px"" x=""0"" y=""0"">
  <defs>
    <clipPath id=""clip-cat"">
      <rect width=""100%"" height=""100%"" rx=""8""/>
    </clipPath>
  </defs>
  <image href=""data:image/jpeg;base64,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"" width=""100%"" height=""100%"" clip-path=""url(#clip-cat)"" preserveAspectRatio=""xMidYMid meet""/>
</svg>";
                                break;

                            case "fullstack":
                                item.imageInSvg = @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""133px"" height=""40px"" x=""0"" y=""0"">
  <defs>
    <clipPath id=""clip-readem_full_stack"">
      <rect width=""100%"" height=""100%"" rx=""8""/>
    </clipPath>
  </defs>
  <image href=""data:image/png;base64,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"" width=""100%"" height=""100%"" clip-path=""url(#clip-readem_full_stack)"" preserveAspectRatio=""xMidYMid meet""/>
</svg>";
                                break;

                            case "typescript":
                                item.imageInSvg = @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""40px"" height=""40px"" x=""0"" y=""0"" fill=""none"">
  <defs>
    <clipPath id=""clip-typescript"">
      <rect width=""100%"" height=""100%"" rx=""8""/>
    </clipPath>
  </defs>
  <image href=""data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAQAAAAEACAYAAABccqhmAAAABHNCSVQICAgIfAhkiAAAF+hJREFUeJzt3X9wFOd9BvDn3RPSIVByQraJqFzfQU2MgFrWD+pAHB+QdhJIY8VKcKYw8TF40k6HFDkBVwi7Pk8qUAItYuxMmhkykqd4miaWETa4acYgOa2xYyQhBQwMMeiwKTK2HJ1GIJ+E797+IU7ot3TS7r67t8/nH6PT3ft+8fA+t/vuu+8KJIsnGxdAyiWQWAJgCaT4EwiZAYhZANIBzALgUVsk2UQYwHUAPYC8Dim6IeT/ATgNiVOQOI3KoouKa9SFUF3AlJSdmA9N3A/gfkgsA7AEArNUl0UOInEdkL+HEI2IyTeRkvoW/vneNtVlJcr6AfDEuQxo14sgcD9E7H5IcT8EblddFtEIEh9ByLcA8SaAt5CK3yFY2KO6rPFYNwCeOJcB1/VvQ8Q2QIoHICxcK9FwEhKQv4UQB/Dp7P/Ej+/pVl3SaKw1qILvpKK35ysQYgMg/xoQbtUlEU2fjADiZQhxADPc/43g4j7VFcVZJACkwPbmtUCsDEKsUF0NkYH+F5A7sbPov1QXAqgOgO82zkCW/A4EtgHi80prITLX7xGTlbhQ+Ev8SkRVFaEuAHY0fwmx2M8gcI+yGohUkzgDgc3YWVivonvzA+CJ1hyk3PhXAN8yvW8i6/olRGwrKpa9b2an5gXA9/6QhozwVkCUo39hDhEN1QMpKnDtM/+CZ+/uNaNDcwKg7MR8CLwMIRab0h+RnUn5DiS+bsZqQ83oDlDWtB6aaOXgJ5okIRZDE60oa1pveFeGtby1dRZS+/4NEBsM64Mo6ckD6PY8ZtQpgTEBsL05FyL2MoAFhrRP5CRSvgO41mFX/hm9m9Y/AMpO+CHEYd6cQ6QjieuQ4uuoLDimZ7P6zgGUN66FEL/m4CfSmcAsCPkqtp/4up7N6hcAZU2PALIOAmm6tUlEtwikQaAW5Y1/o1eT+gRAeeMPIOR/ACJFl/aIaAwiBRIHsKPx73RpbdotlJ/4NiBegBmXFIkoLoaY+A4qC16YTiPTC4DtzQ9CRF/jNz+RCvJTQPzVdO4jmPq39o7mQiD6Cgc/kSoiBVIeQtnJoim3MKVPPdnqQ7TvBITImmrHRKQTKT+GxLKpLB1O/Ajg8eMzEb1xmIOfyCKEyILAy3j8+MxEP5p4ALhTfwqB3IQ/R0TGEWIx3Kk/TfRjiQVAeVMAAo8m2gkRmUDgUZQ3BRL7yGQ92bQIMdkI3stPZGU9EFouKvIvTebNkzsCCNanICpfBAc/kdWlQ0Z/ie82zpjMmycXAH2zn+R5P5FdiGW4Te6Y1DsnfMcTrTlw3XiXa/yJbESiF1rs7on2GJz4CMDVt4eDn8hmBNIQE7snftt4yppWQZNHdSuKiMwVk3+JyqLXxvr12EcAQalByJ8YUhQRmUOIfQjKMcf52AHQ1/wdPrSDyOYEctHXuG6sX48dADK23ZCCiMhcEt8f61ejB0BZ01cgxELDCiIi8whRhLKmr4z2q9EDQMTKDC2IiMylydLRXh55FWBH0/2Q8k3DCyIi80hIaNoyVOQ3Dn555BFATOqy1xgRWYiAQCy2eeTLgwXrU9CbEea23kRJSMpupF2bg+DKT+MvDT0C6M34Ggc/UZISIgO9GV8b/NLQABAImFkPEZls2Bi/dQoQbLwNvbgCgUndRkhENiRxA2kz5yC4+Bow+AjgBr7JwU+U5ARmoC/yzfiPtwJAYtSFAkSUbGRx/E83A0AKQD6gqhwiMpGUX+wf8/EAKG+5FxBzlBZFROYQIqt/zMcDQMb8KushIpPdHPP9ASCkX2EpRGS2m2M+PgmYp64SIlIgDwAEtrbOwoy+LgjhUl0REZlEyiiiGZkpSI3dDXDwEzmKEC6kfLJAg4x6VddCRArIqFcDuO8fkUPdwwAgcq57NAh8TnUVRKSAwOc0SHhU10FECkh4NAjJACByIiE9GiDcqusgIhWEWwMkA4DIkaRbgxQ8BSByIik8Gh/9TeRQAmljPxuQiJIeA4DIwRgARA7GACByMAYAkYMxAIgcjAFA5GAMACIHYwAQORgDgMjBGABEDsYAIHIwBgCRgzEAiByMAUDkYAwAIgdjABA5GAOAyMEYAEQOxgAgcjAGAJGDMQCIHIwBQORgDAAiB2MAEDkYA4DIwRgARA7GACByMAYAkYMxAIgcjAFA5GAMACIHYwAQORgDgMjBGABEDsYAIHIwBgCRgzEAiByMAUDkYAwAIgdjABA5GAOAyMEYAEQOxgAgcjAGAJGDMQCIHIwBQORgDAAiB2MAEDkYA4DIwRgARA7GACByMAYAkYMxAIgcjAFA5GAMACIHS1FdAJHRPG4XHvRlIC87HXnZM+GZ6QIA5GWnw+N2jXh/OBJFS3vPwM+hzj6EOvsGXm9t70E4EjWtfiMlfQDMn5OGQH4WBAAhAE0AAqL/vwLQhIAABn4e8buBzwBCiIE/D/3d0Pdp2uA2h7Y/tL9RfnezDbs43xHBle4bOHbhGn4b6lZdzoCHFnng982Gf37/wE+Ex+2C35dx6wXfyPeEI1HUnQmj7kwYh86Gp1mtOkkfAHdnpeGpldmqy0haD94cKLNmfKA8ALyZadjyhdsRKLht1G92PXncLgTysxDIzxoIg+dPfoyGi9YJwclI+gBI0ez0fWpfs1KNHXDjKc71YMvyO4Z+a5tocBg0tHWj6o0PbXNUkPQBoAkGgBnSUsz//5yXnY69a3OUDfzR+H0Z8PsyEOrsw+Ovvo+6M9YOgqS/CpCS9H9Da0h1mfc/2uN2obrEi5ObF1lq8A/mzUzFwfULUF3iNfx0ZDp4BEC6MCto/fMzcHD9AksPqsEC+Vnw+zKw8aWQJecHkv77kUcA5nCZELRbls9F/aaFthn8cd7MVNRvWogty+eqLmWEpB8eLk4CmkIz8F9S/JC/am2OcZ2YoGptDqpLvKrLGCLpTwF4FcAcRp5qHdywwLLn+okK5GcBADbWhtQWclPyHwFw/JvCqKCtLvEmzeCPC+RnWeZIIOkDQOMRgClmGPAvqbrEO/CNmWysEgLJHwCqC3AIvS8Dlq6Ym7SDPy6Qn4XSFWonBpN+fHAS0H7ystPx9CpnLN/euyYHxbkeZf0nfQBw/NtPdcldtrvUNx3VJV54M9OU9M0AIEsJrs5O+O49u/O4XTi4fr6Svh0QAEwAu/C4XZZcLGOGvOx0BFebf9qT9AFg4hJ1mqbSFXc46tB/uKdXzTP96CfphwePAOzByd/+gwXy55janwMCQHUFNBlmbOJhdc8cu4LSI5dN7TPplwKbcZMKTd+WL9yhrO/he/x5M9NwlyfVtP4vhfsQqFVzt2DSBwCPAKzPm5kGb6a5A67q+IdouNg9ZPPP4eL7CZYuv8OwQDh0NozAiyFlm4wKlDdKJT2b5E89qZg/R8011ricz6Ti37/lNaz9qjeu4tC5LsPan4zOT6JoHWcwjad0xVzsXWP8nX5dkShKj1xGTXNHwp/1z89A1Zoc3KvTJF1XJIpAbUj5jkFJfwTwXrgP74X7lNbw+duMDaB3/9hryc0mJsvvm214H63tPSh+4SJCnb1T+nzDxW7kPXcWVWtzpj1Zqfpbf7CkDwCyvocWGbsUtisSndbgH6z0yGW0tEdQXXLXlOqwwrf+YAwAUso/3/hbfQO1IV0Gf1z8FCKRELDSt/5gDABSyuhLf6+3dRvyjVvT3AFv5gw8vWreuO+z4rf+YEm/DoCsLS97pqHt1501bnI0eLQdr7eNPfey7/hVeHefsuzgBxgAlOSMHnyB2kvoGnZYfynch5U/P4/SI5ctd8g/HAOAkpqe5/5jtR8YtL/fM8euIO/ZM7a5KsM5AEpaw7+ZjVJ3JoxvvHABoc6+cRcWWREDgJJWm8Hf/oNZ+Tx/PDwFoKTltI1FpoIBQEnN6XcYToQBQEmtODdTdQmWxgCgpPaoyRts2A0DgJRqaf/E0Pb9vgxTlhvbFQOAlDJjoUz1w17OBYyBAUBKhTqNv1Xbm5mK+sfs91hxMzAASCmjV+rF5WWnMwRGwQAg5aa6k1Ci8rLT0bZtKecEBmEAkHIN49xRpzeP24X6TQtxcP0CZY/jshIGAClX0/xH0/sszvWgbesSPL1qnqNPCxgApFxLew8uKdq3Mbg6G23blmLvmhxHHhEwAMgSprJTr148bhdKV8xF29YlSp/UqwIDgCyh6o0PTbt9dzyB/Cy0bV2C+scWOmKykAFAlhCORFF1/KrqMgb4fRmo37QQbVuX4tH8LNXlGIYBQJahYjJwIt7MVNSUeNH5VF5SThgyAMgyQp29eObYFdVljMrjdiG4OhudT+Ul1TwBA4AspeqND5VdEZis+DxBdYnX9vMEDACylPDNffTtIJCfhfpNC209YcgAIMtpuNiN55s/Vl3GpMUnDO0YBAwAsqRAbci0ewT0MjgI7DJHwAAgy/LvP2+JtQGJ8vsybLOoiAFAlhWORG0bAkD/HMHJzYsmfH6gSgwAsrSW9h5bh0D88mHbVmvehswAIMuzewgAN3cl2rQQe9fkWGoxEQOAbKGlvQfe3adsNzE4XOmKuTi5OdcyDy1hAJBtxOcE7HSJcDTezFSc3LwIW5bPVV0KA4DsJb5QaOMoj+W2m6q1Oagu8SqtgQFAtlTT3IG8587idRO3EzNCID9L6WalDACyrVBnL/z7z9v+aMDvy1AWAgwAsr2a5g54d5/CPgvtJ5Co+LblZmMAUFIIR6IoPXIZvj2nbTtJmJedbvqcAAOAkkqosxeB2hB8e07j0Nmw6nISFsjPQnB1tmn9MQAoKYU6e1F84IItjwieXjXPtHUCDABKaoOPCOwUBAfXLzBlUpABQI4QD4LMH7bgmWNXLH/VwJuZitIVdxjeDwOAHCUciSJ4tB2eH7ZgY+0lS28/tmX5XMOPAhgA5Fjxy4crf37ekhOGHrcLVWvvNLQPBgA5XsPF7iEThlY6PXgo12PoUQADgOim+DyBd/cpy8wTeNwuFOdmGtY+A4BomPg8gVWCYMvy2w1rmwFANAarBEFedrphewsyAIgmMDwIVCjO9RjSLgOAaJLiQXCfgtuQ/b7ZhrTLACBKUHyPQjOPBh70GbOhKAOAaIqCR9uxsfaSKX153C5D7g9gABBNQ01zh3khMFP/9QAaJHp1b5XIQWqaO0xZSaj7PIBErwYhrbcGkshmAi+GDO/D69H5UqCQYQ0QEX1bJXKecCRq+O3G3sxUnVsUEQ2QDAAiHdRZ8Iai8cmIBinsVjU5kMftwtOr5uHk5kWWerTWYC3tn6guITFShFMgwAAgS3s0PwtVa+8cGPjB1dkoPXJZcVUjhTqNnU//rN7BJxDWIPGBvq0S6cM/v3+//JoS75Bv/S3L51rySbtG030dgMQHGoQM6dsq0fR43C5Ul3hRv2kh/GOsgKt+2Gu5UwGj69F9+bGQIQ3AOX1bJZq6p1fNQ9u2pQjkZ437Pm9mqpIHaYwnb541nvibgHMakMIAIOWKcz1o27oUwdXZk/4mVfEgjfFY5ZHfk5dyLgWpCMG6+yJSksvLTsfetTljHupPJH6ksLE2pGNVU2PUHXtxYT33I5AyihvaHzQE7wsD0r4PVSNb8rhd2LsmByc3L5ry4I8L5GcpPxLwz8/AQ4uMuWc/rqW9R7/GhLiMPfdeT7n501sAHtKvdaKxbVk+N6FD/cmIHwk8fuR9fb8pJyEeZkbTeZ1BCwD0B4AUDRCSAUCG8s/PQPXDXgOWtPYL5GchL3smVu4/b2oI7F17pynn/6FOHc/VpWgA4rcDC61Bv5aJhvJmpuHg+gWo37TQsMEfl5edjrZtS01ZJ+Bxu3Bw/YIJr1joRd9TgP4x3x8AO/NaAflH/VonuqVqbY5he9qNxuN2oX7TQhxcv8CwzTSLcz1o27bUtL+XrrcbS7yHnfe1AAMbgggJiP/RrweiW4JH25X0239pcQnqH1uo2wTdo/lZqH9soWkP74yrO9OlZ3P18T+k3HpN/gIQnAcg3bW092Df8avYsnyukv79vgz4fRkIR6JouNiNurNhhDr7JrWyzpuZhgd9s5H3uZkozs00/BRmLA16rgIU8tfxP94KgNT0w+jt6YYQzltkTYYLHm1HcW4m7vKoGUBA/Ck7niGH7S3tPWNOGE738qReXm/r1vNGo2voS30l/sOtAAguvobtJ14F8IhePRHFhSNRBGpDqN9kseW7Nli9V9Os4/SclEew597r8R+HbQoqfqFfT0RDNVzsxr7jXHOWiEvhPtQ0d+jY4tAxPjQA0roPQ0pzn3hAjhI82o5WPS9nJTldJ1AlPkRa9+HBLw0NgODKTwHxkn49Eg3VfypwSfkDN+1A/29/ebB/jN8y8rkAUjwLCaljr0RDtLT3WHJHH6sJ6HmDk4SExM+GvzwyACoLmiDwG/16JhqpprmD8wHj2Hf8Khou6nnpD79BZdHJ4S+P/mSgmKzUr2ei0ZUeuWz4Vtp21Nreo//iqTHG9OgBUFnUAInf6VsB0UiB2hBDYJCum3Mk+t77j9+hsqhhtF+N92xAHgWQKRgC/boiUfj3n9f3pp9+Y47lsQNgV2EdjwLILIHakGkP2bQqQwa/lCewq7BurF+P/3RgTf4DpOT1GjJFTXMHVv78vOMuEXZForjvubNGDP4oNNffj/eW8QOgouhtAPv1rIloPA0Xu+HdfcqUp+1aQWt7D/KMGPz99qMiv3G8N4wfAACQJp6ExEe6lUQ0gXAkiuIDF7AxyRcMHTobhn//eaOeKNQBxHZM9KaJAyBY2AEN/6RLSUQJqGnugHf3qaRbL9AVieIbL1xA8YELxm1dJvAUdv3FhDOrEwcAAFQU/AxSvjbtoogSFI5EUXrkMnx7Tuv/ZBwF9h2/Cu/uU6g7Y+QpjjyKioIRq/5GM7kAgJAQ7g2Q6JxOWURTFershX//efj2nLblJcPnmz+Gb89plB65bOyGpVJ+ALjX9+/yNbFJBgCAnUuvAuKxKRdGpINQZy8CtSH49pzGvuNXcSls3afadEWiAwM/UBsy/OnBAACpre8fq5MjEu6gvLEKwJaEP+dgn78tDeceX2JY+5tfeQ8/ecu587TFuR4E8rPg92Xo/wjtKTh0Noy6M12oO9Np9jMK9mFnYWkiH0iZ+C3DdGAbbpOrAWHcv2iiBNSdCQ+cUxfneuD3zTZ1+7GuSBQNbd2qBv1N8m10iG2JfirxIwAAKG/MhkQjBOZN6fNEJvC4Xciblw6/b/bA/n4P6rDP3+tt3Whp70FLewQN+u7XNzUSVyBQiJ2FCd9BNLUAAIB/bFkM7cab3ESU7CgeDpMV6uxTP9BHI2U3YjO+gB/lvTOVj089AACgvHElIF8FhHta7RDRFMgIINZgZ2H9xO8d3eSvAoxmZ2E9hLYOQGxa7RBRomIQ2rrpDH5gugEAABUFr0CKH0y7HSKavBi+h4qCVyZ+4/imdwowWFnTIxDyeQgY8zA2IgIkegG5AbuKXtSjOf0CAADKTvghxGEIzNK1XSICgB4I7auoyP+tXg3qGwAAUHayCCJ6GAJ36N42kVNJfAQpvorKgiY9m53+HMBwlfedgBYrBOTburdN5EjybWixAr0HP2BEAABAxbL3kZr+AIBnDWmfyCmkfA6p6Q+gYtn7RjSv/ynAcDtOlCCGai4YIkqAlN3QsBEVRbVGdmN8AABAWdOfQciXILDUlP6I7EziFKR4GJUF7xrdlTGnAMNVFryLa58tghQ7APDJkESj6wHkk7j22SIzBj9g1hHAYDvevhNS2wNgnel9E1nXr/DpjO/jx/ea+tBE8wMgrrxxJSSeg0CushqIVJM4B037Wz2v7SdCXQAAwLekCwsa10ETZQD+XGktRGbqP8//ES7k/wK/Esq2PlYbAIOVn/gqIMoBfFF1KUSGkfINQKvEroLDqksBrBQAcTsavwiJMgBrVZdCpBuJV6GJH6Ki4C3VpQxmvQCIe+JcBlKuPQIpNwDiSxAWrpVopBikPAaIA4jOfgk/vseSe5rbY1D1XzlYB4k1EFiluhyiMUkcg8CruBF7AbuXfaC6nInYIwAGC7a50dfxACS+DCG+DCAPZq1nIBpMyigEmiDxGqR2FO45xxH0RVSXlQj7BcBotjfeAyF9gDYfkD5I6QPE7RAyAxCzAKQDmAXAo7hSsocwgOvoX5hzHVJ0A/IjCNEGiDbIWBsgLmJX4TnVhU7X/wNPwE3HPDedBwAAAABJRU5ErkJggg=="" width=""100%"" height=""100%"" clip-path=""url(#clip-typescript)"" preserveAspectRatio=""xMidYMid meet""/>
</svg>";
                                break;
                        }
                    }
                }*/
                /*
                                if (!offline_debug)
                                {*/
                var credential = GoogleCredential.FromJson(JsonGoogleCred);
                StorageClient storageClient = await StorageClient.CreateAsync(credential);


                foreach (var item in imageList)
                {


                    /*                    if (!userFolderName.Equals("-default"))
                                        {*/
                    // if folder name isn't default, find in that folder first
                    if (!userFolderName.Equals("-default"))
                    {
                        // Console.WriteLine($"Checking {userFolderName} for {item.imageName}");
                        string objectLongName = $"{userFolderName}/{item.imageName}.svg";
                        try
                        {
                            item.imageObject = await storageClient.GetObjectAsync(BucketName, objectLongName);
                        }
                        catch (Google.GoogleApiException ex) when (ex.Error.Code == 404)
                        {
                            item.imageObject = null;
                        }

                        if (item.imageObject != null)
                        {
                            item.folderName = userFolderName;

                            // ok, we need to reconfigure this to make sure memoryStream is in initial state
                            using var memoryStream = new MemoryStream();
                            await storageClient.DownloadObjectAsync(item.imageObject, memoryStream);
                            item.imageInByte = memoryStream.ToArray();
                            item.imageInSvg = Encoding.UTF8.GetString(item.imageInByte);
                            //Console.WriteLine($"\nLoaded SVG:\n{item.imageInSvg}\n");

                            string ext = Path.GetExtension(item.imageObject.Name).ToLower(); // should be .svg
                            item.imageExtension = ext;
                        }
                    }
                    // if image object is still null, find in default
                    // since upperblock checkes if userFolderName is not -default
                    // it will automatically fallback to here:
                    // IT"S NOT FKING FALLING BACK <= resolved by adding try catch

                    if (item.imageObject == null)
                    {
                        // Console.WriteLine($"Cannot find in {userFolderName}. Checking Default");
                        string objectLongName = $"-default/{item.imageName}.svg";
                        try
                        {
                            item.imageObject = await storageClient.GetObjectAsync(BucketName, objectLongName);
                        }
                        catch (Google.GoogleApiException ex) when (ex.Error.Code == 404)
                        {
                            item.imageObject = null;
                        }

                        item.folderName = "-default";


                        using var memoryStream = new MemoryStream();
                        await storageClient.DownloadObjectAsync(item.imageObject, memoryStream);
                        item.imageInByte = memoryStream.ToArray();

                        // ok... so we're gonna grab svg
                        item.imageInSvg = Encoding.UTF8.GetString(item.imageInByte);

                        //Console.WriteLine("------------INITIAL SVG-----------");
                        //Console.WriteLine(item.imageInSvg);
                        //Console.WriteLine("----------------------------------");
                        // Console.WriteLine($"\nLoaded SVG:\n{item.imageInSvg}\n");
                        // the thing is svg might have different format. it might not contain rect, width etc...
                        // what if we parse svg to image then add svg container and image tag for it?
                        // the result svg should look the same as non-default svg

                        string tmpSvg = new string(item.imageInSvg);
                        tmpSvg = ImageHelper.FormatSvg(tmpSvg, item.imageName);
                        //Console.WriteLine("------------FORMATTED SVG-----------");
                        //Console.WriteLine(tmpSvg);
                        //Console.WriteLine("----------------------------------");

                        int newHeight = 40;
                        int newWidth = ImageHelper.GetWidthByHeight(newHeight, tmpSvg);
                        item.imageInSvg = ImageHelper.ResizeSVG(tmpSvg, newWidth, newHeight);

                        //Console.WriteLine("------------RESIZED SVG-----------");
                        //Console.WriteLine(item.imageInSvg);
                        //Console.WriteLine("----------------------------------");

                        //Console.WriteLine($"-------------\nOriginal default SVG:\n{tmpSvg}\n------------\n");
                        //string newTmpSvg = tmpSvg;

                        // Console.WriteLine(newTmpSvg);
                        //item.imageInSvg = newTmpSvg;

                        // Console.WriteLine($"\nLoaded SVG:\n{item.imageInSvg}\n");
                        // THAT FUCING WORKS!

                        // if image object is still null, return error
                        if (item.imageObject == null)
                        {
                            return BadRequest(new { Message = $"Could not find the badge named \"{item.imageName}\"" });
                        }

                        string ext = Path.GetExtension(item.imageObject.Name).ToLower(); // should be .svg
                        item.imageExtension = ext;
                    }
                }
                // }



                /*                Console.WriteLine("---------------------------------------------");
                                Console.WriteLine("List of Images");
                                foreach (var image in imageList)
                                {
                                    Console.WriteLine(image.imageInSvg);
                                    Console.WriteLine();
                                }
                                Console.WriteLine("---------------------------------------------");*/


                /*                foreach (var item in imageList)
                                {
                                    Console.WriteLine($"----------------------");
                                    Console.WriteLine($"Object exists? {(item.imageObject != null ? "Yes" : "No")}");
                                    Console.WriteLine($"Image byte exists?: {(item.imageInByte != null ? "Yes" : "No")}");
                                    Console.WriteLine($"Image svg exists?: {(item.imageInSvg != null ? "Yes" : "No")}");
                                    Console.WriteLine($"Image folder: {item.folderName}");
                                    Console.WriteLine($"Image name: {item.imageName}");
                                    Console.WriteLine($"Image type: {item.imageExtension}\n");
                                }*/



                string svgContent = "";

                if (imageList.Count == 1)
                {
                    // svgContent = Encoding.UTF8.GetString(imageList[0].imageInByte);

                    // Console.WriteLine($"\nLoaded SVG:\n{imageList[0].imageInSvg}\n");
                    //svgContent = ImageHelper.ResizeSVG(imageList[0].imageInSvg, ImageHelper.GetWidthByHeight(40, imageList[0].imageInSvg), 40);
                    int newHeight = 40;
                    int newWidth = ImageHelper.GetWidthByHeight(newHeight, imageList[0].imageInSvg);
                    //Console.WriteLine($"New dimension:\nWidth: {newWidth}\nHeight: {newHeight}");
                    svgContent = ImageHelper.ResizeSVG(imageList[0].imageInSvg, newWidth, newHeight); //imageList[0].imageInSvg;
                    // BOOM!
                }
                else if (imageList.Count > 1) // in case multiple images
                {
                    /*                    Console.WriteLine("---------------------------------------------");
                                        Console.WriteLine("Render multiple images");
                                        foreach (var image in imageList)
                                        {
                                            Console.WriteLine(image.imageInSvg);
                                            Console.WriteLine();
                                        }*/
                    // TODO: THIS IS A ROUGH VERSION. NEED TO ADD ENHANCED LOGIC
                    // do it in MultipleSVGCreator.Create()
                    if (definedRow > imageList.Count || definedCol > imageList.Count)
                    {
                        return BadRequest(new { Message = $"Error: both Row and Column cannot exceed number of badges" });
                    }
                    try
                    {
                        svgContent = MultipleSVGCreator.Create(imageList, definedRow, definedCol, defineFitContent);
                    }
                    catch (ArgumentException ex)
                    {
                        return BadRequest(new { Message = $"Error: {ex.Message}" });
                    }
                }

                return Content(svgContent, "image/svg+xml");
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Error while grabbing badge {ex.Message}" });
            }
        }
    }
}

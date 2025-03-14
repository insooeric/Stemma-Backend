using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;

namespace Stemma.Middlewares
{
    public static class Validator
    {
        private static readonly string BucketName = "badge-bucket";
        private const int MaxItems = 20;

        public static async Task<bool> CheckValidName(string name, string JsonGoogleCred)
        {
            // Console.WriteLine($"Checking name \"{name}\"");
            var credential = GoogleCredential.FromJson(JsonGoogleCred);
            StorageClient storageClient = await StorageClient.CreateAsync(credential);

            string prefix = $"-default/";
            var matchingObjects = storageClient.ListObjectsAsync(BucketName, prefix);

            await foreach (var obj in matchingObjects)
            {
                if (Path.GetFileNameWithoutExtension(obj.Name).Equals(name))
                {
                    return false;
                }
                // count++;
            }
            return true;
        }

        public static async Task<bool> CheckMaximumItemsReached(string githubUserName, string JsonGoogleCred)
        {
            try
            {
                var credential = GoogleCredential.FromJson(JsonGoogleCred);
                StorageClient storageClient = await StorageClient.CreateAsync(credential);

                string prefix = $"{githubUserName}/";
                var userBadgeObjects = storageClient.ListObjectsAsync(BucketName, prefix);
                int count = 0;
                await foreach (var obj in userBadgeObjects)
                {
                    if(obj != null)
                    {
                        count++;
                    }
                }

                return (count >= MaxItems) ? true : false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return true;
            }
        }
    }
}

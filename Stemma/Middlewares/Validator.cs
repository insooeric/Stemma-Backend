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

        //public static (bool isValid, string msg) CheckValidGrid(int numOfImage, int row, int col)
        //{
        //    Console.WriteLine($"Checking grid with {numOfImage} images, {row} rows, {col} columns");
        //    // validate row and col equal to 0
        //    if (row == 0 || col == 0)
        //    {
        //        return (false, "Both row and column should be larger than 0");
        //    }

        //    // check under-sized grid
        //    if (row * col < numOfImage)
        //    {
        //        return (false, "Number of items exceeds the number of cell of the grid.");
        //    }

        //    // check over-sized grid
        //    int requiredRows = (numOfImage % col == 0) ? (numOfImage / col) : (numOfImage / col + 1);
        //    // Console.WriteLine(requiredRows);
        //    if (row > requiredRows)
        //    {
        //        return (false, "Grid has extra rows with no images.");
        //    }
        //    return (true, "");
        //}

        public static (bool isValid, string msg) CheckValidTemplate(int numOfItems, List<int> templateRow, List<int> templateCol)
        {
            //Console.WriteLine("Validating templates.");
            //Console.WriteLine($"Number of items: {numOfItems}");
            //Console.Write("TemplateRow: ");
            //foreach (var item in templateRow)
            //{
            //    Console.Write(item + " ");
            //}
            //Console.WriteLine();

            //Console.Write("TemplateCol: ");
            //foreach (var item in templateCol)
            //{
            //    Console.Write(item + " ");
            //}
            //Console.WriteLine();

            if(templateRow.Sum() > numOfItems)
            {
                return (false, "Sum of row exceeds the number of items.");
            }
            if(templateCol.Sum() > numOfItems)
            {
                return (false, "Sum of column exceeds the number of items.");
            }

            if(templateRow.Count() * templateCol.Count() < numOfItems)
            {
                return (false, "Number of items exceeds the number of cell of the grid.");
            }

            if (
                templateRow[0] == 0 || 
                templateRow[templateRow.Count() - 1] == 0 ||
                templateCol[0] == 0 ||
                templateCol[templateCol.Count() - 1] == 0
                )
            {
                return (false, "Grid contains extra space");
            }
            // check over-sized grid

            return (true, "");
        }
    }
}

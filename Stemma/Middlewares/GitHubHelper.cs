using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Stemma.Middlewares
{
    public static class GitHubHelper
    {
        private static readonly HttpClient client = new HttpClient();

        static GitHubHelper()
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("StemmaApp");
        }

        public static async Task<string> GetData(string url)
        {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public static async Task<string> GetData(string url, string[] attr)
        {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string jsonResult = await response.Content.ReadAsStringAsync();

            using (JsonDocument doc = JsonDocument.Parse(jsonResult))
            {
                var root = doc.RootElement;
                using (var stream = new MemoryStream())
                {
                    using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                    {
                        if (root.ValueKind == JsonValueKind.Object)
                        {
                            writer.WriteStartObject();
                            foreach (var key in attr)
                            {
                                if (root.TryGetProperty(key, out JsonElement value))
                                {
                                    writer.WritePropertyName(key);
                                    value.WriteTo(writer);
                                }
                            }
                            writer.WriteEndObject();
                        }
                        else if (root.ValueKind == JsonValueKind.Array)
                        {
                            writer.WriteStartArray();
                            foreach (JsonElement element in root.EnumerateArray())
                            {
                                if (element.ValueKind == JsonValueKind.Object)
                                {
                                    writer.WriteStartObject();
                                    foreach (var key in attr)
                                    {
                                        if (element.TryGetProperty(key, out JsonElement value))
                                        {
                                            writer.WritePropertyName(key);
                                            value.WriteTo(writer);
                                        }
                                    }
                                    writer.WriteEndObject();
                                }
                                else
                                {
                                    element.WriteTo(writer);
                                }
                            }
                            writer.WriteEndArray();
                        }
                        else
                        {
                            return jsonResult;
                        }
                    }
                    return Encoding.UTF8.GetString(stream.ToArray());
                }
            }
        }
    }
}

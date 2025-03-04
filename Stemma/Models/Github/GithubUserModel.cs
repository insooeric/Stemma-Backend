using System;
using System.Text.Json.Serialization;

namespace Stemma.Models.Github
{
    public class GithubUserModel
    {
        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("node_id")]
        public string NodeId { get; set; } = string.Empty;

        [JsonPropertyName("avatar_url")]
        public string AvatarUrl { get; set; } = string.Empty;

        [JsonPropertyName("gravatar_id")]
        public string GravatarId { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("followers_url")]
        public string FollowersUrl { get; set; } = string.Empty;

        [JsonPropertyName("following_url")]
        public string FollowingUrl { get; set; } = string.Empty;

        [JsonPropertyName("gists_url")]
        public string GistsUrl { get; set; } = string.Empty;

        [JsonPropertyName("starred_url")]
        public string StarredUrl { get; set; } = string.Empty;

        [JsonPropertyName("subscriptions_url")]
        public string SubscriptionsUrl { get; set; } = string.Empty;

        [JsonPropertyName("organizations_url")]
        public string OrganizationsUrl { get; set; } = string.Empty;

        [JsonPropertyName("repos_url")]
        public string ReposUrl { get; set; } = string.Empty;

        [JsonPropertyName("events_url")]
        public string EventsUrl { get; set; } = string.Empty;

        [JsonPropertyName("received_events_url")]
        public string ReceivedEventsUrl { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("user_view_type")]
        public string UserViewType { get; set; } = string.Empty;

        [JsonPropertyName("site_admin")]
        public bool SiteAdmin { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("company")]
        public string Company { get; set; } = string.Empty;

        [JsonPropertyName("blog")]
        public string Blog { get; set; } = string.Empty;

        [JsonPropertyName("location")]
        public string Location { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("hireable")]
        public bool? Hireable { get; set; }

        [JsonPropertyName("bio")]
        public string Bio { get; set; } = string.Empty;

        [JsonPropertyName("twitter_username")]
        public string TwitterUsername { get; set; } = string.Empty;

        [JsonPropertyName("public_repos")]
        public int PublicRepos { get; set; }

        [JsonPropertyName("public_gists")]
        public int PublicGists { get; set; }

        [JsonPropertyName("followers")]
        public int Followers { get; set; }

        [JsonPropertyName("following")]
        public int Following { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; } 

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}

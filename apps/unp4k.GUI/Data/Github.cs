using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace unp4k.Data;

internal static class Github
{
    private static Uri unp4kRepoContributorsURL { get; } = new("https://api.github.com/repos/dolkensp/unp4k/stats/contributors");
    private static Uri githubUserPrefixURL { get; } = new("https://api.github.com/users");

    internal class GithubUser
    {
        public required string Name { get; set; }
        public required string Handle { get; set; }
        public required string Bio { get; set; }
        public required string Avatar_URL { get; set; }
        public required string User_URL { get; set; }
        public required int Commits { get; set; }
        public required int Additions { get; set; }
        public required int Deletions { get; set; }
    }

    private static readonly List<GithubUser> Contributors = [];

    internal static async Task<List<GithubUser>> GetContributors()
    {
        if (Contributors.Count is 0)
        {
            using HttpClient client = new();
            using HttpRequestMessage req = new()
            {
                RequestUri = unp4kRepoContributorsURL,
                Method = HttpMethod.Get,
            };
#if DEBUG
            req.Headers.Add("Authorization", $"Bearer {UserSecrets.GetSecret("GithubToken")}");
#endif
            req.Headers.Add("Accept", "application/vnd.github+json");
            req.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
            req.Headers.Add("User-Agent", "Other");
            using HttpResponseMessage contribResp = await client.SendAsync(req);
            try
            {
                contribResp.EnsureSuccessStatusCode();
                JArray? content = null;
                if ((content = JsonConvert.DeserializeObject(await contribResp.Content.ReadAsStringAsync()) as JArray) is not null)
                {
                    foreach (JObject contribUser in content.Cast<JObject>().ToList())
                    {
                        List<JObject> weeks = (contribUser.GetValue("weeks") as JArray).Cast<JObject>().ToList();
                        int commits = weeks.Sum(x => int.Parse(x.GetValue("c").ToString()));
                        int additions = weeks.Sum(x => int.Parse(x.GetValue("a").ToString()));
                        int deletions = weeks.Sum(x => int.Parse(x.GetValue("d").ToString()));

                        using HttpRequestMessage reqAuthor = new()
                        {
                            RequestUri = new Uri($"{githubUserPrefixURL}/{(contribUser.GetValue("author") as JObject).GetValue("login")}"),
                            Method = HttpMethod.Get,
                        };
#if DEBUG
                        reqAuthor.Headers.Add("Authorization", $"Bearer {UserSecrets.GetSecret("GithubToken")}");
#endif
                        reqAuthor.Headers.Add("Accept", "application/vnd.github+json");
                        reqAuthor.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
                        reqAuthor.Headers.Add("User-Agent", "Other");
                        using HttpResponseMessage userResp = await client.SendAsync(reqAuthor);
                        userResp.EnsureSuccessStatusCode();
                        JObject? userCont = null;
                        if ((userCont = JsonConvert.DeserializeObject(await userResp.Content.ReadAsStringAsync()) as JObject) is not null)
                        {
                            Contributors.Add(new GithubUser
                            {
                                Name = userCont.GetValue("name").ToString(),
                                Handle = userCont.GetValue("login").ToString(),
                                Bio = userCont.GetValue("bio").ToString(),
                                Avatar_URL = userCont.GetValue("avatar_url").ToString(),
                                User_URL = userCont.GetValue("html_url").ToString(),
                                Commits = commits,
                                Additions = additions,
                                Deletions = deletions,
                            });
                        }
                    }
                }
            }
            catch (HttpRequestException e)
            {
                Logger.LogError($"Unable to retreive Github Contributors... {e.Message}");
            }
        }
        return Contributors;
    }
}

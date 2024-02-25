using Microsoft.Extensions.Configuration;

namespace unp4k.Data;

public static class UserSecrets
{
    public class Secrets
    {
        public required string GithubToken { get; set; }
    }

    internal static string? GetSecret(string sectionName)
    {
        return new ConfigurationBuilder().AddUserSecrets<Secrets>().Build()[sectionName];
    }
}

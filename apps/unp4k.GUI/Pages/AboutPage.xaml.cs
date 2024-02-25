using unp4k.Components;
using unp4k.Data;

using UraniumUI.Pages;

namespace unp4k.Pages;

public partial class AboutPage : UraniumContentPage
{
	public AboutPage()
	{
		InitializeComponent();
        GetContributors();
    }

	private async void GetContributors()
	{
        foreach (Github.GithubUser user in (await Github.GetContributors()).OrderBy(x => x.Additions).ThenBy(x => x.Deletions).ThenBy(x => x.Commits).Reverse().ToList()) 
            ContributorsList.Add(new GithubContributor(user.User_URL, user.Avatar_URL, user.Name, user.Handle, user.Bio, user.Commits, user.Additions, user.Deletions));
    }

    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        await Browser.Default.OpenAsync(new Uri("https://github.com/dolkensp/unp4k"), new BrowserLaunchOptions { LaunchMode = BrowserLaunchMode.SystemPreferred });
    }
}
namespace unp4k.Components;

public partial class GithubContributor : ContentView
{
	private string User_URL;

	public GithubContributor(string user_url, string avatar_url, string name, string handle, string bio, int commits, int additions, int deletions)
	{
		InitializeComponent();

		User_URL = user_url;
		Avatar.Source = avatar_url;
		Name.Text = name;
		Handle.Text = $"@{handle}";
		Bio.Text = bio;
		Commits.Text = $"Commits: {commits:#,##0}";
		Additions.Text = $" +{additions:#,##0}";
		Deletions.Text = $"-{deletions:#,##0}";
	}

    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        await Browser.Default.OpenAsync(User_URL, new BrowserLaunchOptions { LaunchMode = BrowserLaunchMode.SystemPreferred });
    }
}
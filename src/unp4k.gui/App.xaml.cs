using System.Windows;

namespace unp4k.gui
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private async void Application_Startup(object sender, StartupEventArgs e)
		{
			ArchiveExplorer wnd = new ArchiveExplorer();
			
			wnd.Show();

			if (e.Args.Length == 1)
			{
				await wnd.OpenP4kAsync(e.Args[0]);
			}
		}
	}
}

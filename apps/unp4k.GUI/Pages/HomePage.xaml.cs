namespace unp4k.Pages;

public partial class HomePage : ContentPage
{
	public HomePage()
	{
		InitializeComponent();
	}

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (Globals.P4kFile is null)
        {
            /*
             * Use this for UI calls from other threads. 
             * Application.Current?.Dispatcher.Dispatch(async () =>
             * {
             *
             * });
             */
            await DisplayAlert("p4k Required", "In order to use unp4k, you must link a p4k file.\n\nThis file is found within the Star Citizen install folder!", "Pick a p4k file");
            try
            {
                FileResult? result = await FilePicker.PickAsync(new PickOptions
                {
                    FileTypes = new FilePickerFileType(
                        new Dictionary<DevicePlatform, IEnumerable<string>>
                        {
                        { DevicePlatform.WinUI, new[] { ".p4k" } },
                        { DevicePlatform.macOS, new[] { "p4k" } },
                        }),
                    PickerTitle = "Please select a Star Citizen p4k file to continue!"
                });
                if (result != null)
                {
                    await Initialiser.MAUI.PreInit(new FileInfo(result.FullPath));
                    if (!Globals.InternalExitTrigger) await Initialiser.MAUI.Init();
                    if (!Globals.InternalExitTrigger) await Initialiser.MAUI.PostInit();
                }
                else
                {
                    await DisplayAlert("p4k Required", "Something is wrong with the file you selected!\n\nExiting...", ":(");
                    Globals.InternalExitTrigger = true;
                }
            }
            catch
            {
                await DisplayAlert("p4k Required", "Something went wrong with selecting a p4k file.\n\nTerminating Program... Beep Boop...", ":(");
            }
            if (Globals.InternalExitTrigger) Application.Current?.CloseWindow(Window);
            await Task.Run(Worker.MAUI.Processp4k);
        }
    }
}
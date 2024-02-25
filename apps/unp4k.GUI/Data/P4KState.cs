namespace unp4k.Data;

internal static class P4KState
{
	private static bool isPreInitSet = false;
	private static readonly string[] value = [".p4k"];
	private static readonly string[] valueArray = ["p4k"];

    internal static Action? OnP4KSelectionComplete;

    internal static void SelectP4k(ContentPage page, IDispatcher dispatcher, Window? win = null, string? path = null)
	{
		dispatcher.DispatchAsync(async () =>
		{
            if (path is null)
            {
                try
                {
                    FileResult? result = await FilePicker.PickAsync(new PickOptions
                    {
                        FileTypes = new FilePickerFileType(
                        new Dictionary<DevicePlatform, IEnumerable<string>>
                        {
                            { DevicePlatform.WinUI, value },
                            { DevicePlatform.macOS, valueArray },
                        }),
                        PickerTitle = "Please select a Star Citizen p4k file to continue!"
                    });
                    if (result != null)
                    {
                        await Init(result.FullPath);
                    }
                    else
                    {
                        await page.DisplayAlert("p4k Required", "Something is wrong with the file you selected!\n\nExiting...", ":(");
                        if (Globals.P4kFile is null && win is not null) Application.Current?.CloseWindow(win);
                    }
                }
                catch
                {
                    await page.DisplayAlert("p4k Required", "Something went wrong with selecting a p4k file.\n\nTerminating Program... Beep Boop...", ":(");
                    if (Globals.P4kFile is null && win is not null) Application.Current?.CloseWindow(win);
                }
            }
            else await Init(path);
			await Task.Run(Worker.MAUI.Processp4k);
            OnP4KSelectionComplete?.Invoke();
		});

		async Task Init(string p)
		{
            if (!isPreInitSet)
            {
                await Initialiser.MAUI.PreInit(new FileInfo(p));
                isPreInitSet = true;
            }
            Initialiser.MAUI.Init();
            Initialiser.MAUI.PostInit();
        }
	}
}

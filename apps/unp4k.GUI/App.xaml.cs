using Sharpnado.MaterialFrame;

namespace unp4k;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        // MAUI support is not implemented yet but will soon™️
        //Initializer.Initialize(false, false);
        MainPage = new MainPage();
    }

    protected override void OnStart()
    {
        base.OnStart();
    }

    protected override void OnSleep()
    {
        base.OnSleep();
    }

    protected override void OnResume() 
    {
        base.OnResume();
    }
}

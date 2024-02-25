using Sharpnado.MaterialFrame;

namespace unp4k;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new AppShell();
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

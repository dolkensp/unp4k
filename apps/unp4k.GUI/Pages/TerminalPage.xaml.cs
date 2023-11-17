using System.Collections.ObjectModel;
using System.ComponentModel;

namespace unp4k.Pages;

public partial class TerminalPage : ContentPage
{
    public TerminalPage()
	{
		InitializeComponent();
        BindingContext = new TerminalOutput();
    }
}

public class TermOut
{
    public string Output { get; set; }
}

public class TerminalOutput
{
    public ObservableCollection<TermOut> Out { get; set; } = [new TermOut { Output = "Console Emulation Sample..." }, new TermOut { Output = "123" }];
}
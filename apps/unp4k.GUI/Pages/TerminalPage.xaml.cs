using System.Collections.ObjectModel;

using UraniumUI.Pages;

namespace unp4k.Pages;

public partial class TerminalPage : UraniumContentPage
{
    public TerminalPage()
	{
		InitializeComponent();
        TerminalOutput tout = new();
        BindingContext = tout;
        Logger.OnLog += (clearLevel, level, msg) => 
        {
            tout.Out.Add(new TermOut { Output = msg });
        };
    }
}

public class TermOut
{
    public string Output { get; set; }
}

public class TerminalOutput
{
    public ObservableCollection<TermOut> Out { get; set; } = [];
}
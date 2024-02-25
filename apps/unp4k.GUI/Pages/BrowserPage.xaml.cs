using System.Collections.ObjectModel;
using System.Windows.Input;

using unp4k.Data;

using ICSharpCode.SharpZipLib.Zip;
using UraniumUI;
using UraniumUI.Pages;

namespace unp4k.Pages;

public partial class BrowserPage : UraniumContentPage
{
    public BrowserPage()
	{
		InitializeComponent();

        P4KState.OnP4KSelectionComplete += () =>
        {
            P4kTreeModel model = new();
            Tree.BindingContext = model;
            Tree.ItemsSource = model.Nodes;
            Tree.LoadChildrenCommand = model.LoadChildrenCommand;
            Tree.IsLeafPropertyName = "IsLeaf";
        };
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        Dispatcher.Dispatch(async () =>
        {
            if (Globals.P4kFile is null)
            {
                if (await DisplayAlert("p4k Required", "In order to use unp4k, you must link a p4k file.\n\nThis file is found within the Star Citizen install folder!" +
                "\n\nAlternatively, you can use the p4k file in the default Star Citizen install path." +
                "\n'C:\\Program Files\\Roberts Space Industries\\StarCitizen\\LIVE\\Data.p4k'",
                "Pick a p4k file", "Use default install path")) P4KState.SelectP4k(this, Dispatcher, Window);
                else P4KState.SelectP4k(this, Dispatcher, Window, "C:\\Program Files\\Roberts Space Industries\\StarCitizen\\LIVE\\Data.p4k");
            }
        });
    }

    private void p4kFilePathButton_Clicked(object sender, EventArgs e)
    {
        P4KState.SelectP4k(this, Dispatcher, Window);
    }

    public class P4kTreeModel : UraniumBindableObject
    {
        public ObservableCollection<ObservableP4kEntry> Nodes { get; set; } = [];
        public ICommand? LoadChildrenCommand { get; set; }

        public P4kTreeModel()
        {
            if (Worker.P4K is not null)
            {
                List<string> names = [];
                foreach (ZipEntry zip in Worker.P4K.Entries) names.Add(zip.Name);
                names = new(names.OrderBy(x => x));

                LoadChildrenCommand = new Command<ObservableP4kEntry>(node =>
                {
                    new Task(() => 
                    {
                        List<string> localnames = [];
                        foreach (string n in names.Where(x => x.StartsWith(node.RelativePath is not null ? node.RelativePath + '\\' + node.Name : node.Name)))
                        {
                            string a = n[(n.IndexOf(node.Name) + node.Name.Length + 1)..];
                            if (a.Contains('\\') && !localnames.Contains(a = $"{a[..a.IndexOf('\\')]}\\")) localnames.Add(a);
                            else if (!a.Contains('\\') && !localnames.Contains(a)) localnames.Add(a);
                        }
                        foreach (string sub in localnames.OrderBy(x => !x.EndsWith('\\')))
                        {
                            bool isFolder;
                            string newsub = sub;
                            if (isFolder = sub.EndsWith('\\')) newsub = sub[..(sub.Length - 1)];
                            Dispatcher.DispatchAsync(() =>
                            {
                                node.Children.Add(new ObservableP4kEntry(newsub, isFolder && node.RelativePath is not null ? node.RelativePath + '\\' + node.Name : node.Name, !isFolder, []));
                            });
                        }
                    }).Start();
                });

                foreach (string rootFolder in names.Select(x => x[..x.IndexOf('\\')]).GroupBy(x => x).Select(y => y.First()))
                {
                    Nodes.Add(new ObservableP4kEntry(rootFolder, null, false, []));
                }
            }
        }
    }

    public class ObservableP4kEntry(string name, string? relativePath, bool isFile, ObservableCollection<ObservableP4kEntry> children) : UraniumBindableObject
    {
        private bool isLeaf = isFile;

        public virtual string Name { get; set; } = name;
        public virtual string? RelativePath { get; set; } = relativePath;
        public bool IsLeaf { get => isLeaf; set => SetProperty(ref isLeaf, value); }
        public virtual ObservableCollection<ObservableP4kEntry> Children { get; set; } = children;
    }
}
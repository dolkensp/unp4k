namespace unp4k;
internal static class Utils
{
    internal static void RunProgressBarAction(string keystring, Action action)
    {
        bool loadingTrigger = true;
        new Task(async () =>
        {
            Console.Write($"{keystring}...");
            while (loadingTrigger)
            {
                Console.Write('.');
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }).RunSynchronously();
        action();
        Logger.NewLine();
        loadingTrigger = false;
    }

    internal static bool AskUserInput(string question)
    {
        char? c = null;
        while (c is null)
        {
            Logger.NewLine();
            Console.Write($"{question} y/n: ");
            c = Console.ReadKey().KeyChar.ToString().ToLower()[0];
            if (c is null || c != 'y' && c != 'n')
            {
                Console.Error.WriteLine("Please input y for yes or n for no!");
                c = null;
            }
        }
        Logger.NewLine(2);
        return c is 'y';
    }
}

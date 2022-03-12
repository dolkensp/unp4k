unp4k.Globals.Arguments = args.ToList();

unp4k.Initialiser.PreInit();
unp4k.Initialiser.Init();
unp4k.Initialiser.PostInit();

if (!unp4k.Globals.ExitTrigger) unp4k.Worker.ProcessGameData();
if (!unp4k.Globals.ExitTrigger) unp4k.Worker.ProvideSummary();
if (!unp4k.Globals.ExitTrigger) unp4k.Worker.DoExtraction();
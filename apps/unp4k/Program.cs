unp4k.Globals.Arguments = [.. args];

unp4k.Initialiser.PreInit();
unp4k.Initialiser.Init();
unp4k.Initialiser.PostInit();

if (!unp4k.Globals.InternalExitTrigger) unp4k.Worker.ProcessGameData();
if (!unp4k.Globals.InternalExitTrigger) unp4k.Worker.DoExtraction();
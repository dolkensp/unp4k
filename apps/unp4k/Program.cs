await unp4k.Initialiser.PreInit(args);
await unp4k.Initialiser.Init();
await unp4k.Initialiser.PostInit();

if (!unp4k.Globals.ExitTrigger) await unp4k.Worker.ProcessGameData();
if (!unp4k.Globals.ExitTrigger) await unp4k.Worker.ProvideSummary();
if (!unp4k.Globals.ExitTrigger) await unp4k.Worker.DoExtraction();
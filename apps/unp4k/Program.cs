await unp4k.Initialiser.PreInit(args);
await unp4k.Initialiser.Init();
await unp4k.Initialiser.PostInit();

await unp4k.Worker.ProcessGameData();
await unp4k.Worker.ProvideSummary();
await unp4k.Worker.DoExtraction();
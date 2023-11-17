using unp4k;

Globals.Arguments = [.. args];

Initialiser.PreInit();
Initialiser.Init();
Initialiser.PostInit();

if (!Globals.InternalExitTrigger) Worker.ProcessGameData();
if (!Globals.InternalExitTrigger) Worker.DoExtraction();
using unp4k;

Globals.Arguments = [.. args];

Initialiser.PreInit();
if (!Globals.InternalExitTrigger) Initialiser.Init();
if (!Globals.InternalExitTrigger) Initialiser.PostInit();

if (!Globals.InternalExitTrigger) Worker.Terminal.Processp4k();
if (!Globals.InternalExitTrigger) Worker.Terminal.Extract();
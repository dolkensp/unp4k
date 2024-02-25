using unp4k;

Globals.Arguments = [.. args];

if (await Initialiser.Terminal.PreInit())
{
	if (Initialiser.Terminal.Init())
	{
		if (Initialiser.Terminal.PostInit())
		{
			Worker.Terminal.Processp4k();
			await Worker.Terminal.Extract();
		}
	}
}
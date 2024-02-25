using unp4k;

Globals.Arguments = [.. args];

if (await Initialiser.Terminal.PreInit())
{
	if (await Initialiser.Terminal.Init())
	{
		if (await Initialiser.Terminal.PostInit())
		{
			Worker.Terminal.Processp4k();
			await Worker.Terminal.Extract();
		}
	}
}
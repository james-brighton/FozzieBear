var generator = new JamesBrighton.FozzieBear.AutoUnitTestGenerator
{
	InputFile = Environment.GetCommandLineArgs()[0]
};
var result = generator.Execute();
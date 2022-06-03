using JamesBrighton.FozzieBear;

var generator = new AutoUnitTestGenerator
{
	InputFile = Environment.GetCommandLineArgs()[0]
};
var result = generator.Execute();

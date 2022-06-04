using JamesBrighton.FozzieBear;

var inputFile = Environment.GetCommandLineArgs()[0];
var outputDir = Path.Join(Path.GetDirectoryName(inputFile), "Output");

var generator = new AutoUnitTestGenerator { InputFile = inputFile };
var result = generator.Execute();

foreach (var (FileName, FileContent) in result)
	File.WriteAllLines(Path.Join(outputDir, FileName + ".g.cs"), FileContent);

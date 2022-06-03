using JamesBrighton.FozzieBear;

var inputFile = Environment.GetCommandLineArgs()[0];
var outputDir = Path.Join(Path.GetDirectoryName(inputFile), "Output");

var generator = new AutoUnitTestGenerator { InputFile = inputFile };
var result = generator.Execute();

foreach (var r in result)
{
	var fileContent = r.FileContent;
	var fileName = Path.Join(outputDir, r.FileName + ".g.cs");
	File.WriteAllLines(fileName, fileContent);
}
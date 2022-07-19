# Introduction

FozzieBear is a unit test generator for NUnit. It's capable of automatically creating:

1. Parameter range value tests
2. Output range value tests

## Usage

See the sample application "WockaWocka" to read itself as an assembly (\*.dll) and output the tests.

````csharp
using JamesBrighton.FozzieBear;

var inputFile = Environment.GetCommandLineArgs()[0];
var outputDir = Path.Join(Path.GetDirectoryName(inputFile), "Output");

var generator = new AutoUnitTestGenerator { InputFile = inputFile };
var result = generator.Execute();

foreach (var (FileName, FileContent) in result)
	File.WriteAllLines(Path.Join(outputDir, FileName + ".g.cs"), FileContent);
````

If you want to create your own tests, decorate your classes with the AutoUnitTestAttribute. An example:

````csharp
using JamesBrighton.FozzieBear;

namespace MyNamespace;

[AutoUnitTest]
public class MyClassToTest
{
}
````

You can specify the range of a method's result and skip checking specific methods:

````csharp
using JamesBrighton.FozzieBear;

namespace MyNamespace;

[AutoUnitTest]
public class IntToStringConverter : IValueConverter
{
	[Return(null, typeof(string))]
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not int i)
			return null;
		
		return i.ToString();
	}

	[Skip]
	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
````


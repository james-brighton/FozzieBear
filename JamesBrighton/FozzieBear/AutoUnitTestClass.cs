using System.Xml.Serialization;

namespace JamesBrighton.FozzieBear;

/// <summary>
/// This class represents a class from AutoUnitTest. It contains the declarations of the public constructors, methods and properties.
/// </summary>
[Serializable]
public class AutoUnitTestClass : IEquatable<AutoUnitTestClass>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="AutoUnitTestClass" /> class.
	/// </summary>
	public AutoUnitTestClass()
	{
		FileName = "";
		Name = "";
		Constructors = Array.Empty<string>();
		Methods = Array.Empty<string>();
		StaticMethods = Array.Empty<string>();
		Properties = Array.Empty<string>();
		StaticProperties = Array.Empty<string>();
		ReadOnlyProperties = Array.Empty<string>();
		StaticReadOnlyProperties = Array.Empty<string>();
		WriteOnlyProperties = Array.Empty<string>();
		StaticWriteOnlyProperties = Array.Empty<string>();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AutoUnitTestClass" /> class.
	/// </summary>
	/// <param name="fileName">File name.</param>
	/// <param name="name">Name.</param>
	/// <param name="constructors">Constructors.</param>
	/// <param name="instanceMethods">Instance methods.</param>
	/// <param name="staticMethods">Static methods.</param>
	/// <param name="instanceReadWriteProperties">Instance read/write properties.</param>
	/// <param name="staticReadWriteProperties">Static read/write properties.</param>
	/// <param name="instanceReadOnlyProperties">Instance read-only properties.</param>
	/// <param name="staticReadOnlyProperties">Static read-only properties.</param>
	/// <param name="instanceWriteOnlyProperties">Instance write-only properties.</param>
	/// <param name="staticWriteOnlyProperties">Static write-only properties.</param>
	public AutoUnitTestClass(string fileName, string name, IEnumerable<string> constructors, IEnumerable<string> instanceMethods, IEnumerable<string> staticMethods, IEnumerable<string> instanceReadWriteProperties, IEnumerable<string> staticReadWriteProperties, IEnumerable<string> instanceReadOnlyProperties, IEnumerable<string> staticReadOnlyProperties, IEnumerable<string> instanceWriteOnlyProperties, IEnumerable<string> staticWriteOnlyProperties)
	{
		FileName = fileName;
		Name = name;
		Constructors = constructors.ToArray();
		Methods = instanceMethods.ToArray();
		StaticMethods = staticMethods.ToArray();
		Properties = instanceReadWriteProperties.ToArray();
		StaticProperties = staticReadWriteProperties.ToArray();
		ReadOnlyProperties = instanceReadOnlyProperties.ToArray();
		StaticReadOnlyProperties = staticReadOnlyProperties.ToArray();
		WriteOnlyProperties = instanceWriteOnlyProperties.ToArray();
		StaticWriteOnlyProperties = staticWriteOnlyProperties.ToArray();
	}

	/// <summary>
	/// Gets or sets the name of the file.
	/// </summary>
	/// <value>The name of the file.</value>
	public string FileName { get; set; }
	/// <summary>
	/// Gets or sets the class name.
	/// </summary>
	/// <value>The name.</value>
	public string Name { get; set; }
	/// <summary>
	/// Gets or sets the constructors.
	/// </summary>
	/// <value>The constructors.</value>
	[XmlArrayItem("Definition")]
	public string[] Constructors { get; set; }
	/// <summary>
	/// Gets or sets the instance methods.
	/// </summary>
	/// <value>The instance methods.</value>
	[XmlArrayItem("Definition")]
	public string[] Methods { get; set; }
	/// <summary>
	/// Gets or sets the static methods.
	/// </summary>
	/// <value>The static methods.</value>
	[XmlArrayItem("Definition")]
	public string[] StaticMethods { get; set; }
	/// <summary>
	/// Gets or sets the instance read/write properties.
	/// </summary>
	/// <value>The instance read/write properties.</value>
	[XmlArrayItem("Definition")]
	public string[] Properties { get; set; }
	/// <summary>
	/// Gets or sets the static read/write properties.
	/// </summary>
	/// <value>The static read/write properties.</value>
	[XmlArrayItem("Definition")]
	public string[] StaticProperties { get; set; }
	/// <summary>
	/// Gets or sets the instance read-only properties.
	/// </summary>
	/// <value>The instance read-only properties.</value>
	[XmlArrayItem("Definition")]
	public string[] ReadOnlyProperties { get; set; }
	/// <summary>
	/// Gets or sets the static read-only properties.
	/// </summary>
	/// <value>The static read-only properties.</value>
	[XmlArrayItem("Definition")]
	public string[] StaticReadOnlyProperties { get; set; }
	/// <summary>
	/// Gets or sets the instance write-only properties.
	/// </summary>
	/// <value>The instance write-only properties.</value>
	[XmlArrayItem("Definition")]
	public string[] WriteOnlyProperties { get; set; }
	/// <summary>
	/// Gets or sets the static write-only properties.
	/// </summary>
	/// <value>The static write-only properties.</value>
	[XmlArrayItem("Definition")]
	public string[] StaticWriteOnlyProperties { get; set; }

	/// <summary>
	/// Disables the generation of the attached property (minus Specified) if it's empty.
	/// </summary>
	[XmlIgnore]
	public bool ConstructorsSpecified => Constructors.Length > 0;
	/// <summary>
	/// Disables the generation of the attached property (minus Specified) if it's empty.
	/// </summary>
	[XmlIgnore]
	public bool MethodsSpecified => Methods.Length > 0;
	/// <summary>
	/// Disables the generation of the attached property (minus Specified) if it's empty.
	/// </summary>
	[XmlIgnore]
	public bool StaticMethodsSpecified => StaticMethods.Length > 0;
	/// <summary>
	/// Disables the generation of the attached property (minus Specified) if it's empty.
	/// </summary>
	[XmlIgnore]
	public bool PropertiesSpecified => Properties.Length > 0;
	/// <summary>
	/// Disables the generation of the attached property (minus Specified) if it's empty.
	/// </summary>
	[XmlIgnore]
	public bool StaticPropertiesSpecified => StaticProperties.Length > 0;
	/// <summary>
	/// Disables the generation of the attached property (minus Specified) if it's empty.
	/// </summary>
	[XmlIgnore]
	public bool ReadOnlyPropertiesSpecified => ReadOnlyProperties.Length > 0;
	/// <summary>
	/// Disables the generation of the attached property (minus Specified) if it's empty.
	/// </summary>
	[XmlIgnore]
	public bool StaticReadOnlyPropertiesSpecified => StaticReadOnlyProperties.Length > 0;
	/// <summary>
	/// Disables the generation of the attached property (minus Specified) if it's empty.
	/// </summary>
	[XmlIgnore]
	public bool WriteOnlyPropertiesSpecified => WriteOnlyProperties.Length > 0;
	/// <summary>
	/// Disables the generation of the attached property (minus Specified) if it's empty.
	/// </summary>
	[XmlIgnore]
	public bool StaticWriteOnlyPropertiesSpecified => StaticWriteOnlyProperties.Length > 0;

	/// <summary>
	/// Determines whether the specified <see cref="AutoUnitTestClass" /> is equal to the current <see cref="AutoUnitTestClass" />.
	/// </summary>
	/// <param name="other">The <see cref="AutoUnitTestClass" /> to compare with the current <see cref="AutoUnitTestClass" />.</param>
	/// <returns><c>true</c> if the specified <see cref="AutoUnitTestClass" /> is equal to the current
	/// <see cref="AutoUnitTestClass" />; otherwise, <c>false</c>.</returns>
	public bool Equals(AutoUnitTestClass? other)
	{
		if (other == null) return false;
		return Name.Equals(other.Name, StringComparison.Ordinal) &&
		       FileName.Equals(other.FileName, StringComparison.Ordinal) &&
		       AreEqual(Constructors, other.Constructors) &&
		       AreEqual(Methods, other.Methods) &&
		       AreEqual(StaticMethods, other.StaticMethods) &&
		       AreEqual(Properties, other.Properties) &&
		       AreEqual(StaticProperties, other.StaticProperties) &&
		       AreEqual(ReadOnlyProperties, other.ReadOnlyProperties) &&
		       AreEqual(StaticReadOnlyProperties, other.StaticReadOnlyProperties) &&
		       AreEqual(WriteOnlyProperties, other.WriteOnlyProperties) &&
		       AreEqual(StaticWriteOnlyProperties, other.StaticWriteOnlyProperties);
	}

	/// <summary>
	/// Determines whether the specified <see cref="object" /> is equal to the current <see cref="AutoUnitTestClass" />.
	/// </summary>
	/// <param name="obj">The <see cref="object" /> to compare with the current <see cref="AutoUnitTestClass" />.</param>
	/// <returns><c>true</c> if the specified <see cref="object" /> is equal to the current
	/// <see cref="AutoUnitTestClass" />; otherwise, <c>false</c>.</returns>
	public override bool Equals(object? obj)
	{
		return Equals(obj as AutoUnitTestClass);
	}

	/// <summary>
	/// Serves as a hash function for a <see cref="AutoUnitTestClass" /> object.
	/// </summary>
	/// <returns>A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a hash table.</returns>
	public override int GetHashCode()
	{
		var hash = new HashCode();
		hash.Add(FileName);
		hash.Add(Name);
		hash.Add(Constructors);
		hash.Add(Methods);
		hash.Add(StaticMethods);
		hash.Add(Properties);
		hash.Add(StaticProperties);
		hash.Add(ReadOnlyProperties);
		hash.Add(StaticReadOnlyProperties);
		hash.Add(WriteOnlyProperties);
		hash.Add(StaticWriteOnlyProperties);
		return hash.ToHashCode();
	}

	/// <summary>
	/// Checks if 2 enumerations are contain the same elements.
	/// </summary>
	/// <param name="t1">T1.</param>
	/// <param name="t2">T2.</param>
	/// <typeparam name="T">The 1st type parameter.</typeparam>
	/// <returns><c>true</c>, they are equal, and <c>false</c> otherwise.</returns>
	static bool AreEqual<T>(IReadOnlyCollection<T> t1, IReadOnlyCollection<T> t2)
	{
		return t1.Count == t2.Count && t1.All(t2.Contains);
	}
}

using System.Xml.Serialization;

namespace JamesBrighton.FozzieBear;

/// <summary>
/// This class represents a list of <see cref="AutoUnitTestParameter" /> classes.
/// </summary>
[Serializable]
[XmlRoot("AutoUnitTestClasses")]
public class AutoUnitTestClasses : List<AutoUnitTestClass>, IEquatable<AutoUnitTestClasses>
{
	/// <summary>
	/// Determines whether the specified <see cref="AutoUnitTestClasses" /> is equal to the current <see cref="AutoUnitTestClasses" />.
	/// </summary>
	/// <param name="other">The <see cref="AutoUnitTestClasses" /> to compare with the current <see cref="AutoUnitTestClasses" />.</param>
	/// <returns><c>true</c> if the specified <see cref="AutoUnitTestClasses" /> is equal to the current
	/// <see cref="AutoUnitTestClasses" />; otherwise, <c>false</c>.</returns>
	public bool Equals(AutoUnitTestClasses? other)
	{
		return other != null && EqualityComparer<List<AutoUnitTestClass>>.Default.Equals(this, other);
	}

	/// <summary>
	/// Determines whether the specified <see cref="object" /> is equal to the current <see cref="AutoUnitTestClasses" />.
	/// </summary>
	/// <param name="obj">The <see cref="object" /> to compare with the current <see cref="AutoUnitTestClasses" />.</param>
	/// <returns><c>true</c> if the specified <see cref="object" /> is equal to the current
	/// <see cref="AutoUnitTestClasses" />; otherwise, <c>false</c>.</returns>
	public override bool Equals(object? obj)
	{
		return Equals(obj as AutoUnitTestClasses);
	}

	/// <summary>
	/// Serves as a hash function for a <see cref="AutoUnitTestClasses" /> object.
	/// </summary>
	/// <returns>A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a hash table.</returns>
	public override int GetHashCode()
	{
		unchecked
		{
			var hashCode = -816481761;
			hashCode = hashCode * -1521134295 + EqualityComparer<List<AutoUnitTestClass>>.Default.GetHashCode(this);
			return hashCode;
		}
	}

	/// <summary>
	/// Determines whether a specified instance of <see cref="AutoUnitTestClasses" /> is equal to another
	/// specified <see cref="AutoUnitTestClasses" />.
	/// </summary>
	/// <param name="class1">The first <see cref="AutoUnitTestClasses" /> to compare.</param>
	/// <param name="class2">The second <see cref="AutoUnitTestClasses" /> to compare.</param>
	/// <returns><c>true</c> if <c>class1</c> and <c>class2</c> are equal; otherwise, <c>false</c>.</returns>
	public static bool operator ==(AutoUnitTestClasses? class1, AutoUnitTestClasses? class2)
	{
		return EqualityComparer<AutoUnitTestClasses>.Default.Equals(class1, class2);
	}

	/// <summary>
	/// Determines whether a specified instance of <see cref="AutoUnitTestClasses" /> is not equal to
	/// another specified <see cref="AutoUnitTestClasses" />.
	/// </summary>
	/// <param name="class1">The first <see cref="AutoUnitTestClasses" /> to compare.</param>
	/// <param name="class2">The second <see cref="AutoUnitTestClasses" /> to compare.</param>
	/// <returns><c>true</c> if <c>class1</c> and <c>class2</c> are not equal; otherwise, <c>false</c>.</returns>
	public static bool operator !=(AutoUnitTestClasses? class1, AutoUnitTestClasses? class2)
	{
		return !(class1 == class2);
	}

	/// <summary>Loads the class instance from file.</summary>
	/// <param name="path">Full pathname of the file.</param>
	/// <returns>The instance or null otherwise.</returns>
	public static AutoUnitTestClasses LoadFromFile(string path)
	{
		var result = new AutoUnitTestClasses();
		FileStream stream;
		try
		{
#pragma warning disable IDE0068
			stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
#pragma warning restore IDE0068
		}
		catch (Exception)
		{
			return result;
		}

		result = FromStream<AutoUnitTestClasses>(stream);
		stream.Dispose();
		return result ?? new AutoUnitTestClasses();
	}

	/// <summary>Saves the object to file.</summary>
	/// <param name="path">Full pathname of the file.</param>
	public void SaveToFile(string path)
	{
		FileStream stream;
		try
		{
#pragma warning disable IDE0068
			stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write);
#pragma warning restore IDE0068
		}
		catch (Exception)
		{
			return;
		}

		ToStream(this, stream);
		stream.Dispose();
	}

	/// <summary>Converts this object to a stream.</summary>
	/// <param name="o">Object to process.</param>
	/// <param name="stream">The stream.</param>
	static void ToStream<T>(T o, Stream stream)
	{
		var serializer = new XmlSerializer(typeof(T));
		serializer.Serialize(stream, o);
	}

	/// <summary>Initializes this object from the given stream.</summary>
	/// <param name="stream">The stream.</param>
	/// <returns>A T.</returns>
	static T? FromStream<T>(Stream stream) where T : class, new()
	{
		var serializer = new XmlSerializer(typeof(T));
		try
		{
			return serializer.Deserialize(stream) as T;
		}
		catch (Exception)
		{
			return new T();
		}
	}
}

namespace JamesBrighton.FozzieBear;

/// <summary>
/// This class provides random methods
/// </summary>
public static class RandomGen
{
	/// <summary>
	/// Gets the random value.
	/// </summary>
	/// <typeparam name="T">Type of the value</typeparam>
	/// <returns>The random value.</returns>
	public static T GetValue<T>()
	{
		return AutoUnitTestGeneratorHelper.GetRandomValueT<T>();
	}

	/// <summary>
	/// Gets a random enumeration
	/// </summary>
	/// <typeparam name="TEnum">The type of the enumeration.</typeparam>
	/// <returns>The random enumeration</returns>
	public static TEnum GetEnum<TEnum>() where TEnum : struct, Enum
	{
		var values = Enum.GetValues<TEnum>();
		var result = (TEnum)values.GetValue(random.Next(values.Length))!;
		return result;
	}

	/// <summary>
	/// The random object
	/// </summary>
	static readonly Random random = new();
}

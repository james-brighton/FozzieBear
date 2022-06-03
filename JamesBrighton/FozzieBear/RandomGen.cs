namespace JamesBrighton.FozzieBear;

/// <summary>
///     This class provides random methods
/// </summary>
public static class RandomGen
{
	/// <summary>
	///     The random object
	/// </summary>
	private static readonly Random Random = new();
	/// <summary>
	///     Gets the random value.
	/// </summary>
	/// <typeparam name="T">Type of the value</typeparam>
	/// <returns>The random value.</returns>
	public static T GetValue<T>()
	{
		return AutoUnitTestGeneratorHelper.GetRandomValueT<T>();
	}

	/// <summary>
	///     Gets a random enumeration
	/// </summary>
	/// <typeparam name="TEnum">The type of the enumeration.</typeparam>
	/// <returns>The random enumeration</returns>
	public static TEnum GetEnum<TEnum>() where TEnum : struct, Enum
	{
		var values = Enum.GetValues<TEnum>();
		var result = (TEnum)values.GetValue(Random.Next(values.Length))!;
		return result;
	}
}

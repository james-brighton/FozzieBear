namespace JamesBrighton.FozzieBear;

/// <summary>
///     AutoUnitTest parameter class. Used internally as a helper class for the <see cref="AutoUnitTestGenerator" /> class.
/// </summary>
internal class AutoUnitTestParameter
{
	/// <summary>
	///     Initializes a new instance of the <see cref="AutoUnitTestParameter" /> class.
	/// </summary>
	/// <param name="type">Type.</param>
	/// <param name="value">Value.</param>
	/// <param name="direction">Direction.</param>
	public AutoUnitTestParameter(string type = "", string value = "", string direction = "")
	{
		Type = type;
		Value = value;
		Direction = direction;
	}

	/// <summary>
	///     Gets or sets the type.
	/// </summary>
	/// <value>The type.</value>
	public string Type { get; }

	/// <summary>
	///     Gets or sets the value.
	/// </summary>
	/// <value>The value.</value>
	public string Value { get; set; }

	/// <summary>
	///     Gets or sets the direction.
	/// </summary>
	/// <value>The direction.</value>
	public string Direction { get; set; }

	/// <summary>
	///     Checks if the instance is empty.
	/// </summary>
	/// <returns>True if it is and false otherwise.</returns>
	public bool IsEmpty()
	{
		return string.IsNullOrEmpty(Type) && string.IsNullOrEmpty(Value) && string.IsNullOrEmpty(Direction);
	}
}

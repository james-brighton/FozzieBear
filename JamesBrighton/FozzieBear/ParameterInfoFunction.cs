using System.Reflection;

namespace JamesBrighton.FozzieBear;

/// <summary>
///  Useful functions for reflection
/// </summary>
public static class ParameterInfoFunction
{
	/// <summary>
	/// Checks if the given parameter info instance is nullable.
	/// </summary>
	/// <param name="param">Param to check</param>
	/// <returns>True if it is and false otherwise</returns>
	public static bool IsNullable(ParameterInfo param)
	{
		var result = GetNullableAttributeValue(param);
		// Null = unknown, is yes
		if (result.Length == 0) return true;
		return result[0] switch
		{
			// 1 means: no, not nullable
			1 => false,
			// 2 means: yes!
			2 => true,
			_ => false
		};
	}

	/// <summary>
	/// Reads the new compiler specific nullable attribute of the parameter or its parent scopes.
	/// </summary>
	/// <param name="param">The parameter to check for the availability of a nullable attribute.</param>
	/// <returns>the value of the first attribute to find or <c>null</c> if none is available.</returns>
	static byte[] GetNullableAttributeValue(ParameterInfo param)
	{
		// non reference types will be converted to Nullable<> if they are nullable.
		// C# 8.0 nullable flag just changes the way reference types are handled. If enabled, then they are
		// not implicitly nullable anymore!
		if (param.ParameterType == null || !IsReferenceType(param.ParameterType))
			return Array.Empty<byte>();

		var result = GetNullableAttributeValue(param.GetCustomAttributes(), false);
		if (result.Any()) return result;
		result = GetNullableAttributeValue(param.Member.GetCustomAttributes(), true);
		if (result.Any()) return result;
		return param.Member.DeclaringType != null ? GetNullableAttributeValue(param.Member.DeclaringType.GetCustomAttributes(true), true) : Array.Empty<byte>();
	}

	static bool IsReferenceType(Type t) => t.IsClass || t.IsInterface || t.IsArray || typeof(string).IsAssignableFrom(t);

	static byte[] GetNullableAttributeValue(IEnumerable<object> attributes, bool fromParent)
	{
		if (attributes == null) return Array.Empty<byte>();

		// need to retrieve all attributes and find by class name.
		// see: http://code.fitness/post/2019/02/nullableattribute.html
		// https://github.com/dotnet/roslyn/blob/master/docs/features/nullable-metadata.md
		foreach (var customAttribute in attributes)
		{
			if (customAttribute is not Attribute nullableAttribute)
				continue;

			var isNullableAttribute = string.Equals(customAttribute.GetType().FullName, "System.Runtime.CompilerServices.NullableAttribute", StringComparison.Ordinal);
			var isNullableContextAttribute = fromParent && !isNullableAttribute && string.Equals(customAttribute.GetType().FullName, "System.Runtime.CompilerServices.NullableContextAttribute", StringComparison.Ordinal);
			if (!isNullableAttribute && !isNullableContextAttribute) continue;
			var flagField = isNullableAttribute ? nullableAttribute.GetType().GetRuntimeField("NullableFlags") : nullableAttribute.GetType().GetRuntimeField("Flag");
			var flagsData = flagField?.GetValue(nullableAttribute);
			var flags = flagsData is byte[] flagsDataBytes ? flagsDataBytes : null;
			flags = flags is null && flagsData is byte flagsDataSingleByte ? new[] { flagsDataSingleByte } : flags;
			return flags?.Length > 0 ? flags : Array.Empty<byte>();
		}

		return Array.Empty<byte>();
	}
}

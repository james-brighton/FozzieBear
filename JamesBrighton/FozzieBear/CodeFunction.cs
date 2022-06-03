using System.Reflection;

namespace Mercury.Common.Function;

/// <summary>
/// This class contains general code (generation) functions.
/// </summary>
public static class CodeFunction
{
	/// <summary>
	/// Get the friendly name of a type.
	/// </summary>
	/// <param name="type">Type.</param>
	/// <returns>Friendly name or "" otherwise.</returns>
	public static string GetFriendlyName(Type type)
	{
		var @namespace = type.Namespace + ".";
		var result = @namespace + PrependDeclaringType(type, type.Name);
		if (result.EndsWith("&", StringComparison.Ordinal))
			result = result[..^1];
		var typeParameters = type.GetGenericArguments();
		if (!typeParameters.Any()) return GetCSharpName(result);
		var backTick = result.IndexOf('`');
		if (backTick > 0)
		{
			result = result.Remove(backTick);
		}
		result += "<";
		for (var i = 0; i < typeParameters.Length; i++)
		{
			var typeParameter = typeParameters[i];
			var typeParamName = typeParameter.IsGenericParameter ? typeParameter.Name : GetFriendlyName(typeParameter);
			result += i == 0 ? typeParamName : $",{typeParamName}";
		}
		result += ">";

		return GetCSharpName(result);
	}

	/// <summary>
	/// Gets the dictionary with C# names and their corresponding .NET names.
	/// </summary>
	/// <returns>THe dictionary.</returns>
	public static Dictionary<string, string> GetCSharpNames() => CSharpNames;

	/// <summary>
	/// Check if two given types are equal
	/// </summary>
	/// <param name="t1">First type.</param>
	/// <param name="t2">Second type.</param>
	/// <returns>True if they are equal and false otherwise.</returns>
	public static bool TypeEquals(Type t1, Type t2)
	{
		if (t1.Namespace?.Equals(t2.Namespace, StringComparison.Ordinal) == false) return false;
		if (!t1.Name.Equals(t2.Name, StringComparison.Ordinal)) return false;
		if (t1.IsGenericType != t2.IsGenericType) return false;

		if (!t1.IsGenericType) return true;
		var typeParameters1 = t1.GetGenericArguments();
		var typeParameters2 = t2.GetGenericArguments();
		if (typeParameters1.Length != typeParameters2.Length) return false;

		return !typeParameters1.Where((t, i) => !TypeEquals(t, typeParameters2[i])).Any();
	}

	/// <summary>
	/// Gets the name of the method.
	/// </summary>
	/// <param name="methodInfo">Method info.</param>
	/// <returns>The method name or "" otherwise.</returns>
	public static string GetFriendlyName(MethodInfo methodInfo)
	{
		var parms = methodInfo.GetParameters().Select(p => (p.IsOut ? "out " : p.ParameterType.IsByRef ? "ref " : "") + GetFriendlyName(p.ParameterType));
		var paramNames = string.Join(", ", parms);
		var genericArgNames = string.Join(", ", methodInfo.GetGenericArguments().Select(p => p.Name));
		if (!string.IsNullOrEmpty(genericArgNames))
			genericArgNames = string.Concat("<", genericArgNames, ">");
		return $"{GetFriendlyName(methodInfo.ReturnType)} {string.Concat(methodInfo.Name, genericArgNames, "(", paramNames, ")")}";
	}


	/// <summary>
	/// Prepends the given name with the surrounding type's name.
	/// </summary>
	/// <param name="type">Type to get the surrounding type from.</param>
	/// <param name="name">The current name.</param>
	/// <returns>The prepended name.</returns>
	static string PrependDeclaringType(MemberInfo type, string name)
	{
		while (true)
		{
			if (type.DeclaringType == null) return name;
			var type1 = type;
			type = type.DeclaringType;
			name = type1.DeclaringType != null ? $"{type1.DeclaringType.Name}.{name}" : name;
		}
	}

	/// <summary>
	/// Gets the C# name of a given type name.
	/// </summary>
	/// <param name="typeName">Type name.</param>
	/// <returns>The C# type name or the original .NET name if not found.</returns>
	static string GetCSharpName(string typeName)
	{
		var (key, value) = CSharpNames.FirstOrDefault(x => x.Value == typeName);
		return value != typeName ? typeName : key;
	}

	/// <summary>
	/// C# names and their .NET names
	/// </summary>
	static readonly Dictionary<string, string> CSharpNames = new()
	{
		{ "bool", "System.Boolean" },
		{ "byte", "System.Byte" },
		{ "sbyte", "System.SByte" },
		{ "char", "System.Char" },
		{ "decimal", "System.Decimal" },
		{ "double", "System.Double" },
		{ "float", "System.Single" },
		{ "int", "System.Int32" },
		{ "uint", "System.UInt32" },
		{ "long", "System.Int64" },
		{ "ulong", "System.UInt64" },
		{ "object", "System.Object" },
		{ "short", "System.Int16" },
		{ "ushort", "System.UInt16" },
		{ "string", "System.String" }
	};
}

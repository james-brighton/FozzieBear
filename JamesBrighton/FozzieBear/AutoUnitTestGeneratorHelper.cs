using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using Mercury.Common.Function;

namespace JamesBrighton.FozzieBear;

/// <summary>
///     This class represents auto unit test generator helper.
/// </summary>
internal static class AutoUnitTestGeneratorHelper
{
    /// <summary>
    ///     Random number generator
    /// </summary>
    private static readonly Random Random = new();
    /// <summary>
    ///     Gets the primitive types
    /// </summary>
    /// <returns>The types</returns>
    public static Type[] GetPrimitiveTypes()
    {
        return new[]
        {
            typeof(bool),
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(IntPtr),
            typeof(UIntPtr),
            typeof(char),
            typeof(double),
            typeof(float)
        };
    }

    /// <summary>
    /// Check if the member's assembly is in debug mode
    /// </summary>
    /// <param name="member">Member to check</param>
    /// <returns>True if it is, and false otherwise</returns>
    public static bool IsAssemblyDebugBuild(MemberInfo member)
    {
        var assembly = member.DeclaringType?.Assembly;
        if (assembly == null) return false;
        return IsAssemblyDebugBuild(assembly);
    }

    /// <summary>
    ///     An object extension method that executes a method and returns the result.
    /// </summary>
    /// <param name="obj">The obj to act on.</param>
    /// <param name="methodName">Name of the method.</param>
    /// <param name="parameters">(Optional) Parameters to supply the method.</param>
    /// <returns>The result of the method or null in case of an error.</returns>
    public static object? InvokeMethod(object? obj, string methodName, object?[]? parameters = null)
    {
        if (obj == null) return null;

        var method = Array.Find(
            obj.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), m =>
                m.Name == methodName && (parameters == null || m.GetParameters().Length == parameters.Length));
        return method?.Invoke(obj, BindingFlags.InvokeMethod, null, parameters, CultureInfo.CurrentCulture);
    }

    /// <summary>
    ///     Gets a random number of items, shuffled
    /// </summary>
    /// <param name="list">List to get items from</param>
    /// <param name="count">Number of items to get and shuffle</param>
    /// <typeparam name="T">Type of items in the list</typeparam>
    /// <returns>The shuffled list or an empty list otherwise</returns>
    public static IEnumerable<T> GetRandomItems<T>(IReadOnlyList<T> list, int count)
    {
        if (count <= 0)
            return new List<T>();

        if (count > list.Count)
            count = list.Count;

        var result = new List<T>();
        var copy = new List<T>();
        copy.AddRange(list);
        for (var i = 0; i < count; i++)
        {
            var index = Random.Next(0, copy.Count);
            result.Add(copy[index]);
            copy.RemoveAt(index);
        }

        return result;
    }

    /// <summary>
    ///     Checks if a given type implements the given interface
    /// </summary>
    /// <param name="type">Type.</param>
    /// <param name="interfaceType">Interface type to check against.</param>
    /// <returns>True if it does and false otherwise.</returns>
    public static bool ImplementsInterface(Type type, Type interfaceType)
    {
        if (ImplementsInterfaces.TryGetValue((type, interfaceType), out var v1))
            return v1;

        if (TypeEquals(type, interfaceType))
        {
            if (!ImplementsInterfaces.TryGetValue((type, interfaceType), out _))
                ImplementsInterfaces.Add((type, interfaceType), true);
            return true;
        }
        var typeArgs = type.GetGenericArguments();
        if (typeArgs?.Length > 0 && interfaceType.IsGenericTypeDefinition)
        {
            var interfaceArgs = interfaceType.GetGenericArguments();
            if (interfaceArgs != null && interfaceArgs.Length == typeArgs.Length)
                interfaceType = interfaceType.MakeGenericType(typeArgs);
        }
        if (TypeEquals(type, interfaceType))
        {
            if (!ImplementsInterfaces.TryGetValue((type, interfaceType), out _))
                ImplementsInterfaces.Add((type, interfaceType), true);
            return true;
        }

        if (ImplementsInterfaces.TryGetValue((type, interfaceType), out var v2))
            return v2;

        var interfaces = type.GetInterfaces();
        var result = interfaces.Any(t => TypeEquals(t, interfaceType));
        ImplementsInterfaces.Add((type, interfaceType), result);
        return result;
    }

    /// <summary>
    ///     Gets the initialize type.
    /// </summary>
    /// <param name="originalType">Original type.</param>
    /// <param name="currentType">Current type.</param>
    /// <returns>The initializable type.</returns>
    public static Type? GetInitializableType(Type originalType, Type currentType)
    {
        if (Initializes.TryGetValue((originalType, currentType), out var v))
            return v;
        Type? result;
        try
        {
            result = currentType.IsGenericTypeDefinition &&
                   currentType.GetGenericArguments().Length == originalType.GetGenericArguments().Length &&
                   GenericArgumentsAreInitializable(originalType)
                ? currentType.MakeGenericType(originalType.GetGenericArguments())
                : currentType;
        }
        catch (ArgumentException)
        {
            result = null;
        }
        Initializes.Add((originalType, currentType), result);
        return result;
    }

    private static readonly Dictionary<(Type, Type), Type?> Initializes = new();

    /// <summary>
    ///     Get the full name of a type.
    /// </summary>
    /// <param name="type">Type.</param>
    /// <returns>Full name.</returns>
    public static string GetFullName(Type type)
    {
        if (Names.TryGetValue(type, out var v))
            return v;
        var result = CodeFunction.GetFriendlyName(type);
        Names.Add(type, result);
        return result;
    }

    /// <summary>
    ///     Gets the name of the constructor.
    /// </summary>
    /// <returns>The constructor name.</returns>
    /// <param name="constructorInfo">Constructor info.</param>
    public static string GetConstructorName(MethodBase constructorInfo)
    {
        if (constructorInfo.DeclaringType == null) return "";
        var paramNames = string.Join(", ",
            from p in constructorInfo.GetParameters()
            let s = p.IsOut ? "out " : p.ParameterType.IsByRef ? "ref " : ""
            select s + GetFullName(p.ParameterType));
        return string.Concat(GetFullName(constructorInfo.DeclaringType), "(", paramNames, ")");
    }

    /// <summary>
    ///     Gets all enumerations of a given enum type.
    /// </summary>
    /// <param name="type">Type.</param>
    /// <returns>The enumerations.</returns>
    public static List<AutoUnitTestParameter> GetEnumParams(Type type)
    {
        var fullName = GetFullName(type);
        var values = Enum.GetValues(type);
        var first = Enum.GetName(type, (int)values.GetValue(0)!);
        var last = Enum.GetName(type, (int)values.GetValue(values.Length - 1)!);
        var m = typeof(RandomGen).GetMethod(nameof(RandomGen.GetEnum), BindingFlags.Public | BindingFlags.Static);
        if (m == null) return new List<AutoUnitTestParameter>();
        m = m.MakeGenericMethod(type);
        var v = m.Invoke(null, null);
        var random = Enum.GetName(type, v!);
        var result = new List<AutoUnitTestParameter>
        {
            new(fullName, $"{fullName}.{first}"),
            new(fullName, $"{fullName}.{last}"),
            new(fullName, $"{fullName}.{random}")
        };
        return DistinctBy(result, x => x.Value).ToList();
    }

    /// <summary>
    ///     Generates the C# method declaration for the given property.
    /// </summary>
    /// <param name="prop">The property.</param>
    /// <param name="counter">Counter for the method name generation.</param>
    /// <param name="needsInstance">Check if this method needs an instance.</param>
    /// <param name="fullName">Full name of the enclosing type.</param>
    /// <param name="constructorParamDeclarations">List of the constructor's parameter declarations</param>
    /// <param name="constructorParamList">String with the list of the constructor's parameter list</param>
    /// <returns>The C# code.</returns>
    public static List<string> GenerateMethodDeclaration(PropertyInfo prop, ref Dictionary<string, int> counter,
        bool needsInstance, string fullName, IEnumerable<string> constructorParamDeclarations,
        string constructorParamList)
    {
        var method = new List<string>
        {
            "\t\t[Test]",
            $"\t\tpublic void {prop.Name}{IntToHex(GetMethodNumber(prop.Name, ref counter), 4)}()",
            "\t\t{"
        };
        if (!needsInstance) return method;
        method.AddRange(constructorParamDeclarations);
        method.Add($"\t\t\tvar instance = new {fullName}({constructorParamList});");
        return method;
    }

    /// <summary>
    ///     Gets all combinations for the given primitive type.
    /// </summary>
    /// <param name="type">Type.</param>
    /// <returns>The combinations.</returns>
    public static List<AutoUnitTestParameter> GetPrimitiveParams(Type type)
    {
        var result = new List<AutoUnitTestParameter>();
        var fullName = GetFullName(type);

        if (type == typeof(bool))
        {
            result.Add(new AutoUnitTestParameter(fullName, "false"));
            result.Add(new AutoUnitTestParameter(fullName, "true"));
            result.AddRange(StaticFields(type).Select(x => new AutoUnitTestParameter(fullName, x)));
            return result;
        }

        result.Add(new AutoUnitTestParameter(fullName, $"default({fullName})"));
        result.AddRange(StaticFields(type).Select(x => new AutoUnitTestParameter(fullName, x)));
        result.Add(new AutoUnitTestParameter(fullName, GetRandomPrimitiveValue(type)));
        return DistinctBy(result, x => x.Value).ToList();
    }

    /// <summary>
    ///     Gets all combinations for the given string type.
    /// </summary>
    /// <param name="type">Type.</param>
    /// <param name="isNullable">Type is nullable (true) or not (false)</param>
    /// <returns>The combinations.</returns>
    public static List<AutoUnitTestParameter> GetStringParams(Type type, bool isNullable)
    {
        var fullName = GetFullName(type);
        var result = new List<AutoUnitTestParameter>();
        if (isNullable)
            result.Add(new AutoUnitTestParameter(fullName, $"default({fullName})"));
        result.Add(new AutoUnitTestParameter(fullName, "\"\""));
        result.Add(new AutoUnitTestParameter(fullName, "\" \""));
        result.Add(new AutoUnitTestParameter(fullName, "\"\\r\\n\""));
        result.Add(new AutoUnitTestParameter(fullName, $"\"{GetRandomString(16, 128)}\""));
        result.AddRange(StaticFields(type).Select(x => new AutoUnitTestParameter(fullName, x)));
        return DistinctBy(result, x => x.Value).ToList();
    }

    /// <summary>
    ///     Gets all combinations for the given object type.
    /// </summary>
    /// <param name="type">Type.</param>
    /// <param name="isNullable">Type is nullable (true) or not (false)</param>
    /// <returns>The combinations.</returns>
    public static List<AutoUnitTestParameter> GetObjectParams(Type type, bool isNullable)
    {
        var fullName = GetFullName(type);
        var result = new List<AutoUnitTestParameter>();
        if (isNullable)
            result.Add(new AutoUnitTestParameter(fullName, $"default({fullName})"));
        result.Add(new AutoUnitTestParameter(fullName, $"new {fullName}()"));

        var randomPrimitiveTypeList = new List<AutoUnitTestParameter>
        {
            new(fullName, "1"),
            new(fullName, "1.0d"),
            new(fullName, "1.0f"),
            new(fullName, "false")
        };
        var index = Random.Next(0, randomPrimitiveTypeList.Count);
        result.Add(randomPrimitiveTypeList[index]);
        result.Add(new AutoUnitTestParameter(fullName, "\"\""));
        return DistinctBy(result, x => x.Value).ToList();
    }

    /// <summary>
    ///     Gets all combinations for the given type 'type'.
    /// </summary>
    /// <param name="type">Type.</param>
    /// <param name="isNullable">Type is nullable (true) or not (false)</param>
    /// <returns>The combinations.</returns>
    public static List<AutoUnitTestParameter> GetTypeParams(Type type, bool isNullable)
    {
        var fullName = GetFullName(type);
        var result = new List<AutoUnitTestParameter>();
        if (isNullable)
            result.Add(new AutoUnitTestParameter(fullName, $"default({fullName})"));
        result.Add(new AutoUnitTestParameter(fullName, "typeof(object)"));
        return result;
    }

    /// <summary>
    ///     Gets all combinations for the given date/time offset type.
    /// </summary>
    /// <param name="type">Type.</param>
    /// <param name="isNullable">Type is nullable (true) or not (false)</param>
    /// <returns>The combinations.</returns>
    public static List<AutoUnitTestParameter> GetDateTimeOffsetParams(Type type, bool isNullable)
    {
        var fullName = GetFullName(type);
        var result = new List<AutoUnitTestParameter>();
        if (isNullable)
            result.Add(new AutoUnitTestParameter(fullName, $"default({fullName})"));
        result.Add(new AutoUnitTestParameter(fullName, $"new {fullName}()"));
        return result;
    }

    /// <summary>
    ///     Gets all combinations for the given delegate type.
    /// </summary>
    /// <param name="type">Type.</param>
    /// <returns>The combinations.</returns>
    public static List<AutoUnitTestParameter> GetDelegateParams(Type type)
    {
        var fullName = GetFullName(type);
        var method = Array.Find(type.GetMethods(), x => x.Name.Equals("Invoke", StringComparison.Ordinal));
        if (method == null) return new List<AutoUnitTestParameter>();
        var args = method.GetParameters();
        var paramList = "";
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].IsOut)
                paramList += "out ";
            else if (args[i].ParameterType.IsByRef)
                paramList += "ref ";
            paramList += $"{GetFullName(args[i].ParameterType)} p{IntToHex(i, 2)}";
            if (i < args.Length - 1) paramList += ", ";
        }

        var initParamList = "";
        var initArgs = args.Where(p => p.IsOut || p.ParameterType.IsByRef).ToArray();
        for (var i = 0; i < initArgs.Length; i++)
        {
            if (i == 0) initParamList = " ";
            initParamList += $"p{IntToHex(Array.FindIndex(args, p => p == initArgs[i]), 2)} = default;";
            if (i < initArgs.Length - 1) initParamList += " ";
        }

        return new List<AutoUnitTestParameter>
        {
            new(fullName,
                $"new {fullName}(({paramList}) => {{{initParamList} return{(method.ReturnType != typeof(void) ? $" default({GetFullName(method.ReturnType)})" : "")}; }})")
        };
    }

    /// <summary>
    ///     Generates the array inits for the given name (full name).
    /// </summary>
    /// <param name="fullName">Name of the array.</param>
    /// <param name="isNullable">Type is nullable (true) or not (false)</param>
    /// <returns>List with inits.</returns>
    public static List<AutoUnitTestParameter> GenerateArrayInits(string fullName, bool isNullable)
    {
        var result = new List<AutoUnitTestParameter>();
        if (isNullable)
            result.Add(new AutoUnitTestParameter(fullName + "[]", "null"));
        result.Add(new AutoUnitTestParameter(fullName + "[]", $"new {fullName}[0]"));
        result.Add(new AutoUnitTestParameter(fullName + "[]", $"new {fullName}[1]")
        );
        return result;
    }

    /// <summary>
    ///     Gets the referenced assemblies for the given assembly.
    /// </summary>
    /// <param name="assembly">Assembly to get references for.</param>
    /// <param name="excludeFiles">List of files to exclude.</param>
    /// <returns>List of assemblies</returns>
    public static IList<Assembly> GetAllAssemblies(Assembly assembly, IEnumerable<string> excludeFiles)
    {
        var result = new List<Assembly>();
        GetAllAssemblies(assembly, excludeFiles, result);
        return result;
    }

    /// <summary>
    ///     Converts an integer to a hexadecimal represented string.
    /// </summary>
    /// <param name="i">The integer to convert.</param>
    /// <param name="length">The length of the value.</param>
    /// <returns>The converted value.</returns>
    public static string IntToHex(int i, int length)
    {
        var result = i.ToString("X");
        while (result.Length < length)
            result = $"0{result}";
        return result;
    }

    /// <summary>
    ///     Gets the random value.
    /// </summary>
    /// <typeparam name="T">Type of the value</typeparam>
    /// <returns>The random value.</returns>
    public static T GetRandomValueT<T>()
    {
        if (typeof(T) == typeof(bool))
        {
            object obj = Random.Next(0, 2) == 1;
            return (T)obj;
        }
        var bytes = new byte[GetManagedSize(typeof(T))];
        for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)Random.Next(0, 256);
        var result = ToT<T>(bytes);
        return result;
    }

    /// <summary>
    ///     Gets the random value.
    /// </summary>
    /// <typeparam name="T">Type of the value</typeparam>
    /// <returns>The random value.</returns>
    public static string GetRandomValueTAsString<T>()
    {
        var result = GetRandomValueT<T>();
        return string.Format(CultureInfo.InvariantCulture, "{0}", result);
    }

    /// <summary>
    ///     Gets the name of the unit test file.
    /// </summary>
    /// <returns>The unit test file name.</returns>
    /// <param name="type">Type of the class.</param>
    /// <param name="name">Name of the class.</param>
    public static string GetUnitTestFileName(Type type, string name)
    {
        return $"{type.Namespace}.UnitTest.{name}";
    }

    /// <summary>
    ///     Determines whether the beginning of this string instance matches the specified string when compared using the
    ///     specified comparison option.
    /// </summary>
    /// <param name="obj">This string</param>
    /// <param name="value">The string to compare.</param>
    /// <param name="comparisonType">One of the enumeration values that determines how this string and value are compared.</param>
    /// <returns>true if this instance begins with value; otherwise, false.</returns>
    public static bool StartsWith(string obj, string value, StringComparison comparisonType)
    {
        return obj.StartsWith(value, comparisonType);
    }

    /// <summary>
    ///     Creates the C# class file's header.
    /// </summary>
    /// <param name="type">Type of the C# class.</param>
    /// <param name="name">Name of the C# class.</param>
    /// <returns>The header.</returns>
    public static List<string> CreateHeader(Type type, string name)
    {
        return new List<string>
        {
            "using NUnit.Framework;",
            "",
            "#pragma warning disable IDE0017, IDE0018, IDE0034, IDE0059, RCS1021, RCS1036, RCS1118, RCS1163, RCS1196, RCS1204",
            $"namespace {type.Namespace}.UnitTest",
            "{",
            $"\tpublic partial class {name}",
            "\t{"
        };
    }

    /// <summary>
    ///     Gets the name of the method.
    /// </summary>
    /// <returns>The method name.</returns>
    /// <param name="methodInfo">Method info.</param>
    public static string GetMethodName(MethodInfo methodInfo)
    {
        return CodeFunction.GetFriendlyName(methodInfo);
    }

    /// <summary>
    ///     Gets the random culture info parameter.
    /// </summary>
    /// <returns>The random culture info parameter.</returns>
    public static string GetRandomCultureInfoParam()
    {
        var cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
        return cultures[Random.Next(0, cultures.Length)].Name;
    }

    /// <summary>
    ///     Checks if the a given type is a delegate.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if it is and false otherwise.</returns>
    public static bool IsDelegate(Type type)
    {
        return typeof(MulticastDelegate).IsAssignableFrom(type.BaseType);
    }

    /// <summary>
    ///     Gets the random value.
    /// </summary>
    /// <returns>The random value.</returns>
    /// <param name="type">Type.</param>
    public static string GetRandomValue(Type type)
    {
        var m = typeof(AutoUnitTestGenerator)
            .GetMethod("GetRandomValueT", BindingFlags.Static | BindingFlags.NonPublic)
            ?.MakeGenericMethod(type);
        var res = m?.Invoke(null, null);
        return (string)res!;
    }

    /// <summary>
    ///     Check if 2 types are equal
    /// </summary>
    /// <param name="t1">First type.</param>
    /// <param name="t2">Second type.</param>
    /// <returns>True if they're equal and false otherwise.</returns>
    public static bool TypeEquals(Type t1, Type t2)
    {
        return CodeFunction.TypeEquals(t1, t2);
    }

    /// <summary>
    ///     Returns the cartesian product from a 2D list.
    /// </summary>
    /// <param name="list">The 2D list.</param>
    /// <typeparam name="T">The 1st type parameter.</typeparam>
    /// <returns>The cartesian product.</returns>
    public static IEnumerable<IEnumerable<T>> Cartesian<T>(IEnumerable<IEnumerable<T>> list)
    {
        IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };

        return list.Aggregate(
            emptyProduct,
            (accumulator, sequence) => accumulator.SelectMany(a => sequence, (a, item) => a.Concat(new[] { item })));
    }

    /// <summary>
    ///     Gets a random primitive value
    /// </summary>
    /// <param name="type">Type of the primitive</param>
    /// <returns>The value as string</returns>
    private static string GetRandomPrimitiveValue(Type type)
    {
        var m = typeof(RandomGen).GetMethod(nameof(RandomGen.GetValue), BindingFlags.Public | BindingFlags.Static);
        if (m == null) return "";
        m = m.MakeGenericMethod(type);
        var v = m.Invoke(null, null);
        m = typeof(AutoUnitTestGeneratorHelper).GetMethod(nameof(PrimitiveToString), BindingFlags.NonPublic | BindingFlags.Static);
        if (m == null) return "";
        m = m.MakeGenericMethod(type);
        var r = m.Invoke(null, new[] { v });
        return r as string ?? "";
    }

    /// <summary>
    ///     Converts a primitive value to string
    /// </summary>
    /// <param name="value">Value to convert</param>
    /// <typeparam name="T">Type of value</typeparam>
    /// <returns>The string</returns>
    private static string PrimitiveToString<T>(T value)
    {
        return value switch
        {
            bool b => b ? "true" : "false",
            char c => $"'\\u{string.Format("{0:x4}", (int)c)}'",
            double d => double.IsNaN(d) ? "double.NaN" : double.IsNegativeInfinity(d) ? "double.NegativeInfinity" : double.IsPositiveInfinity(d) ? "double.PositiveInfinity" : $"{d.ToString(CultureInfo.InvariantCulture)}d",
            float f => float.IsNaN(f) ? "float.NaN" : float.IsNegativeInfinity(f) ? "float.NegativeInfinity" : float.IsPositiveInfinity(f) ? "float.PositiveInfinity" : $"{f.ToString(CultureInfo.InvariantCulture)}f",
            IConvertible convertable => convertable.ToString(CultureInfo.InvariantCulture),
            _ => value?.ToString() ?? ""
        };
    }

    /// <summary>
    /// Gets the referenced assemblies for the given assembly.
    /// </summary>
    /// <param name="assembly">Assembly to get references for.</param>
    /// <param name="excludeFiles">List of files to exclude.</param>
    /// <param name="result">List of assemblies to add to.</param>
    private static void GetAllAssemblies(Assembly assembly, IEnumerable<string> excludeFiles, List<Assembly> result)
    {
        var directoryName = assembly.GetName().CodeBase ?? ".\\";
        directoryName = Path.GetDirectoryName(new Uri(directoryName).LocalPath) ?? ".\\";

        var assemblyNames = assembly.GetReferencedAssemblies();
        foreach (var assemblyName in assemblyNames)
        {
            if (result.Any(x => x.FullName == assemblyName.FullName)) continue;
            var fileName = Path.GetFullPath(Path.Combine(directoryName ?? ".\\", assemblyName.Name + ".dll"));
            if (!File.Exists(fileName)) continue;
            if (excludeFiles.Any(x => x.Equals(fileName, StringComparison.OrdinalIgnoreCase))) continue;

            try
            {
                var a = Assembly.LoadFrom(fileName);
                var path = new Uri(a.GetName().CodeBase ?? "").LocalPath;
                // Outside current dir?
                if (!path.StartsWith(directoryName!, StringComparison.Ordinal)) continue;
                if (!result.Exists(x => x.FullName == a.FullName)) result.Add(a);
                GetAllAssemblies(a, excludeFiles, result);
            }
            catch (FileNotFoundException)
            {
            }
            catch (FileLoadException)
            {
            }
        }

        if (!result.Exists(x => x.FullName == assembly.FullName)) result.Insert(0, assembly);
    }

    /// <summary>
    ///     Gets a random string.
    /// </summary>
    /// <param name="minLength">Minimum length.</param>
    /// <param name="maxLength">Maximum length (inclusive).</param>
    /// <returns>The random string.</returns>
    private static string GetRandomString(int minLength, int maxLength)
    {
        var length = Random.Next(minLength, maxLength + 1);
        return GetRandomString(length);
    }

    /// <summary>
    ///     Gets a random string.
    ///     <param name="length">Length of the string.</param>
    /// </summary>
    /// <returns>The random string.</returns>
    private static string GetRandomString(int length)
    {
        var bytes = new byte[length];
        for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)Random.Next(1, 128);
        return AddEscapeSequences(Encoding.ASCII.GetString(bytes));
    }

    /// <summary>
    ///     Adds escape sequences to a given string.
    /// </summary>
    /// <param name="value">String to process.</param>
    /// <returns>String with escape sequences.</returns>
    private static string AddEscapeSequences(string value)
    {
        var result = "";

        foreach (var c in value)
            switch (c)
            {
                case '\0':
                    result += "\\0";
                    break;
                case '\"':
                    result += "\\\"";
                    break;
                case '\\':
                    result += "\\\\";
                    break;
                case '\a':
                    result += "\\a";
                    break;
                case '\b':
                    result += "\\b";
                    break;
                case '\f':
                    result += "\\f";
                    break;
                case '\n':
                    result += "\\n";
                    break;
                case '\r':
                    result += "\\r";
                    break;
                case '\t':
                    result += "\\t";
                    break;
                case '\v':
                    result += "\\v";
                    break;
                default:
                    {
                        if (!IsVisible(c))
                            result += $"\\u{IntToHex(c, 4)}";
                        else
                            result += c;
                        break;
                    }
            }

        return result;
    }

    /// <summary>
    ///     Checks if a character is visible
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns>True if it is visible and false otherwise.</returns>
    private static bool IsVisible(char c)
    {
        return !char.IsControl(c) || char.IsWhiteSpace(c);
    }

    /// <summary>
    ///     Converts a byte array to type T.
    /// </summary>
    /// <typeparam name="T">Type of the value</typeparam>
    /// <param name="bytes">Array to convert</param>
    /// <returns>The converted value.</returns>
    private static T ToT<T>(IEnumerable bytes)
    {
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        var structure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T))!;
        handle.Free();
        return structure;
    }

    /// <summary>
    ///     Gets the size of a type in bytes
    /// </summary>
    /// <param name="type">The type</param>
    /// <returns>The size.</returns>
    private static int GetManagedSize(Type type)
    {
        var method = new DynamicMethod("GetManagedSizeImpl", typeof(uint), Array.Empty<Type>(),
            typeof(AutoUnitTestGenerator), false);

        var gen = method.GetILGenerator();
        gen.Emit(OpCodes.Sizeof, type);
        gen.Emit(OpCodes.Ret);

        var func = (Func<uint>)method.CreateDelegate(typeof(Func<uint>));
        return checked((int)func());
    }

    /// <summary>
    ///     Check if the given type's generic arguments are initializable.
    /// </summary>
    /// <param name="type">Type.</param>
    /// <returns>True if they are and false otherwise.</returns>
    private static bool GenericArgumentsAreInitializable(Type type)
    {
        return type.IsGenericType && type.GetGenericArguments().All(a => !a.IsAbstract && a.IsPublic);
    }

    /// <summary>
    ///     Get a list of public static fields.
    /// </summary>
    /// <param name="type">Type.</param>
    /// <returns>List of public static fields.</returns>
    private static IEnumerable<string> StaticFields(Type type)
    {
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static).Where(x => x.FieldType == type);
        return fields.Select(x => $"{GetFullName(type)}.{x.Name}").ToList();
    }

    /// <summary>
    /// Returns distinct elements from a sequence according to a specified key selector function and using a specified comparer to compare keys.
    /// </summary>
    /// <param name="source">The sequence to remove duplicate elements from.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <param name="comparer">An IEqualityComparer{T} to compare keys.</param>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <typeparam name="TKey">The type of key to distinguish elements by.</typeparam>
    /// <returns>An IEnumerable{T} that contains distinct elements from the source sequence.</returns>
    private static IEnumerable<TSource> DistinctBy<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

        return _(); IEnumerable<TSource> _()
        {
            var knownKeys = new HashSet<TKey>(comparer);
            foreach (var element in source)
            {
                if (knownKeys.Add(keySelector(element)))
                    yield return element;
            }
        }
    }

    /// <summary>
    ///     Gets the .NET name of a given C# name.
    /// </summary>
    /// <param name="typeName">C# name.</param>
    /// <returns>The .NET name.</returns>
    public static string GetDotNetName(string typeName)
    {
        return CodeFunction.GetCSharpNames().TryGetValue(typeName, out var val) ? val : typeName;
    }

    /// <summary>
    ///     Converts a name to a class name.
    /// </summary>
    /// <param name="name">Name to convert.</param>
    /// <returns>Converted name or "" otherwise.</returns>
    public static string NameToClassName(string name)
    {
        var result = "";
        if (string.IsNullOrEmpty(name)) return result;

        for (var i = 0; i < name.Length; i++)
        {
            switch (name[i])
            {
                case '_':
                case '.':
                    continue;
            }

            if (i == 0 || name[i - 1] == '_')
                result += char.ToUpper(name[i]);
            else
                result += name[i];
        }

        return result;
    }

    /// <summary>
    ///     Converts a name to an upper camel-case name.
    /// </summary>
    /// <param name="name">String to process.</param>
    /// <returns>The result.</returns>
    public static string NameToUpperCamelCase(string name)
    {
        var result = "";
        for (var i = 0; i < name.Length; i++)
        {
            if (name[i] == '_') continue;
            if (i == 0 || name[i - 1] == '_')
                result += char.ToUpper(name[i]);
            else
                result += char.ToLower(name[i]);
        }

        return result;
    }

    /// <summary>
    ///     Gets the method number appendix.
    /// </summary>
    /// <param name="name">Name of the method.</param>
    /// <param name="counter">Reference counter.</param>
    /// <returns>The method number.</returns>
    public static int GetMethodNumber(string name, ref Dictionary<string, int> counter)
    {
        if (counter.TryGetValue(name, out var num))
        {
            counter[name] = num + 1;
            return num;
        }

        counter[name] = 1;
        return 0;
    }

    /// <summary>
    ///     Gets the exceptions for a given member thru the <see cref="JamesBrighton.FozzieBear.ThrowsExceptionAttribute" />
    ///     attribute.
    /// </summary>
    /// <param name="member">Member.</param>
    /// <returns>The exceptions.</returns>
    public static IEnumerable<Type> GetExceptions(MemberInfo member)
    {
        var attribute = member.GetCustomAttributes().FirstOrDefault(a =>
            GetFullName(a.GetType())
                .Equals("JamesBrighton.FozzieBear.ThrowsExceptionAttribute", StringComparison.Ordinal));
        if (attribute == null)
            return new List<Type>();
        if (InvokeMethod(attribute, "GetTypeExceptions") is List<Type> result1 && result1.Count > 0)
            return result1;
        if (InvokeMethod(attribute, "GetStringExceptions") is List<string> result2 && result2.Count > 0)
            return GetExceptions(result2);
        return new List<Type>();
    }

    /// <summary>
    ///     Gets the returns for a given member thru the <see cref="JamesBrighton.FozzieBear.ReturnAttribute" />
    ///     attribute.
    /// </summary>
    /// <param name="member">Member.</param>
    /// <returns>The returns.</returns>
    public static string GetReturns(MemberInfo member)
    {
        var attribute = member.GetCustomAttributes(true).FirstOrDefault(a =>
            GetFullName(a.GetType())
                .Equals("JamesBrighton.FozzieBear.ReturnAttribute", StringComparison.Ordinal));
        var result = (attribute == null
            ? new List<string>()
            : InvokeMethod(attribute, "GetReturns") is IEnumerable<object?> list
                ? list
                : new List<object?>()).Select(x => "(" + ReturnToString(x) + ")");
        return StringsToString(result, " || ");
    }

    /// <summary>
    ///     Gets the returns for a given class and method thru the <see cref="JamesBrighton.FozzieBear.ReturnForAttribute" />
    ///     attribute.
    /// </summary>
    /// <param name="classType">Type of the class holding the attribute.</param>
    /// <param name="method">Method.</param>
    /// <returns>The returns.</returns>
    public static string GetReturnsFor(Type classType, MethodInfo method)
    {
        var attributes = classType.GetCustomAttributes(true).Where(a =>
            GetFullName(a.GetType())
                .Equals("JamesBrighton.FozzieBear.ReturnForAttribute", StringComparison.Ordinal));
        foreach (var attribute in attributes)
        {
            if (InvokeMethod(attribute, "GetMethodName") is not string methodName) continue;

            var friendlyName = CodeFunction.GetFriendlyName(method);
            if (!string.Equals(friendlyName, methodName, StringComparison.Ordinal)) continue;
            var result = (InvokeMethod(attribute, "GetReturns") is IEnumerable<object?> list ? list : new List<object?>()).Select(x => "(" + ReturnToString(x) + ")");
            return StringsToString(result, " || ");
        }
        return "";
    }

    /// <summary>
    ///     Check if the given member is marked as a member to skip in the automatic unit test.
    /// </summary>
    /// <param name="member">Member.</param>
    /// <returns>True to skip and false otherwise.</returns>
    public static bool SkipMember(MemberInfo member)
    {
        var attribute = member.GetCustomAttributes().FirstOrDefault(a =>
            GetFullName(a.GetType()).Equals("JamesBrighton.FozzieBear.SkipAttribute", StringComparison.Ordinal));
        if (attribute != null)
            return true;

        var attributes = member.DeclaringType?.GetCustomAttributes().Where(a => GetFullName(a.GetType()).Equals("JamesBrighton.FozzieBear.SkipAttribute", StringComparison.Ordinal)).ToList() ?? new List<Attribute>();
        var memberName = "";
        foreach (var attr in attributes)
        {
            var parameters = InvokeMethod(attr, "GetParameters") as List<string> ?? new List<string>();
            if (parameters.Count == 0)
                continue;

            if (string.IsNullOrEmpty(memberName))
                memberName = member is MethodInfo method ? CodeFunction.GetFriendlyName(method) : member.Name;
            var contains = parameters.Any(x => string.Equals(x, memberName, StringComparison.Ordinal));
            if (contains) return true;
        }
        return false;
    }

    /// <summary>
    ///     Check if the given class type is marked as a class to skip in the automatic unit test.
    /// </summary>
    /// <param name="type">Class type to check.</param>
    /// <returns>True to skip and false otherwise.</returns>
    public static bool SkipClass(Type type)
    {
        var attributes = type.GetCustomAttributes().Where(a => GetFullName(a.GetType()).Equals("JamesBrighton.FozzieBear.SkipAttribute", StringComparison.Ordinal)).ToList();
        if (attributes.Count == 1)
        {
            var parameters = InvokeMethod(attributes[0], "GetParameters") as List<string> ?? new List<string>();
            return parameters.Count == 0;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    ///     Gets the parameters.
    /// </summary>
    /// <param name="parameters">Parameters to use.</param>
    /// <param name="paramPrefix">Prefix for the variable names.</param>
    /// <param name="declarations">List of parameter/variable declarations.</param>
    /// <returns>The list with parameters (for a method).</returns>
    public static string GetParams(IEnumerable<AutoUnitTestParameter> parameters, string paramPrefix,
        ICollection<string> declarations)
    {
        var paramList = "";
        for (var i = 0; i < parameters.Count(); i++)
        {
            var p = parameters.ElementAt(i);
            var constructorParamName = $"{paramPrefix}{IntToHex(i, 2)}";
            declarations.Add($"\t\t\t{p.Type} {constructorParamName};");
            declarations.Add($"\t\t\ttry {{ {constructorParamName} = {p.Value}; }} catch {{ return; }}");
            paramList += (!string.IsNullOrEmpty(p.Direction) ? p.Direction + " " : "") + constructorParamName;
            if (i < parameters.Count() - 1) paramList += ", ";
        }

        return paramList;
    }

    /// <summary>
    ///     Gets the core of a type.
    /// </summary>
    /// <param name="type">Type.</param>
    /// <returns>The core type.</returns>
    public static Type GetCoreType(Type type)
    {
        var elementType = type.GetElementType();
        return type.IsByRef && !type.IsArray && !type.IsPointer ? elementType ?? type : type;
    }

    /// <summary>
    /// Check if the assembly is in debug mode
    /// </summary>
    /// <param name="assembly">Assembly to check</param>
    /// <returns>True if it is, and false otherwise</returns>
    private static bool IsAssemblyDebugBuild(Assembly assembly)
    {
        var result = false;

        if (AssembliesInDebug.TryGetValue(assembly, out var v))
            return v;

        foreach (var attribute in assembly.GetCustomAttributes(false))
        {
            if (attribute is not DebuggableAttribute debuggableAttribute || !debuggableAttribute.IsJITTrackingEnabled)
                continue;

            result = true;
            break;
        }
        AssembliesInDebug.Add(assembly, result);
        return result;
    }

    /// <summary>
    ///     Gets the types of the exceptions.
    /// </summary>
    /// <returns>The exceptions.</returns>
    private static IEnumerable<Type> GetExceptions(IReadOnlyCollection<string> exceptions)
    {
        var result = new List<Type>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            foreach (var t in exceptions.Select(e => GetType(assembly, e)).Where(t => t != null && !result.Any(x => TypeEquals(x, t))))
                result.Add(t!);
        return result;
    }

    /// <summary>
    ///     Gets the Type with the specified name in the given assembly, performing a case-sensitive search.
    /// </summary>
    /// <param name="assembly">Assembly to search in.</param>
    /// <param name="typeName">The full name of the type to search for.</param>
    /// <returns>The type or null otherwise.</returns>
    private static Type? GetType(Assembly assembly, string typeName)
    {
        return Array.Find(assembly.GetTypes(), type => GetFullName(type).Equals(typeName, StringComparison.Ordinal));
    }

    /// <summary>
    ///     Converts a list of strings to 1 string using the given terminator.
    /// </summary>
    /// <returns>The result.</returns>
    /// <param name="list">The list.</param>
    /// <param name="terminator">Terminator.</param>
    private static string StringsToString(IEnumerable<string?> list, string terminator)
    {
        var result = new StringBuilder();
        using var enumerator = list.GetEnumerator();
        var i = 0;
        while (enumerator.MoveNext())
        {
            var value = enumerator.Current;
            if (value == null) continue;
            if (i > 0)
                result.Append(terminator);
            result.Append(value);
            i++;
        }

        return result.ToString();
    }

    /// <summary>
    /// Returns a given object to a result string
    /// </summary>
    /// <param name="o">Given object to convert</param>
    /// <returns>The result string or an empty one otherwise</returns>
    private static string ReturnToString(object? o)
    {
        if (o  == null) return "result == null";
        if (o is string s) return s;
        if (o is Type t) return "result is " + CodeFunction.GetFriendlyName(t);
        if (o is Enum e) return "result == " + CodeFunction.GetFriendlyName(e.GetType()) + "." + Enum.GetName(e.GetType(), e);

        var m = typeof(AutoUnitTestGeneratorHelper).GetMethod(nameof(PrimitiveToString), BindingFlags.NonPublic | BindingFlags.Static);
        if (m == null) return "";
        m = m.MakeGenericMethod(o.GetType());
        var r = m.Invoke(null, new[] { o });
        if (r is not string ss) return "";
        return "result == " + ss;
    }

    /// <summary>
    /// List with assemblies in debug (cache)
    /// </summary>
    private static readonly Dictionary<Assembly, bool> AssembliesInDebug = new();
    /// <summary>
    /// Cache of implemented interfaces
    /// </summary>
    private static readonly Dictionary<(Type, Type), bool> ImplementsInterfaces = new();
    /// <summary>
    /// Types and names cache
    /// </summary>
    private static readonly Dictionary<Type, string> Names = new();
}

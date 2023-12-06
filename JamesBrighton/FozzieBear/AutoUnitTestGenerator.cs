using System.Collections;
using System.Globalization;
using System.Reflection;

namespace JamesBrighton.FozzieBear;

/// <summary>
///     This class represents auto unit test generator.
/// </summary>
#if DEBUG
[AutoUnitTest]
#endif
public class AutoUnitTestGenerator
{
    /// <summary>
    ///     The loaded assemblies.
    /// </summary>
    private IList<Assembly> assemblies = new List<Assembly>();

    /// <summary>
    ///     Gets or sets the input file.
    /// </summary>
    /// <value>The input file.</value>
    public string InputFile { get; set; } = "";

    /// <summary>
    ///     Gets or sets the exclude files.
    /// </summary>
    /// <value>The exclude files.</value>
    public IList<string> ExcludeFiles { get; set; } = new List<string>();

    /// <summary>
    ///     Gets or sets generate result output flag.
    /// </summary>
    /// <value>The generate result output flag.</value>
    public bool OutputResult { get; set; }

    /// <summary>
    ///     Generates the tests.
    /// </summary>
    /// <returns>List with name and file content.</returns>
    [Return(typeof(List<(string FileName, List<string> FileContent)>))]
    public IEnumerable<(string FileName, List<string> FileContent)> Execute()
    {
        var result = new List<(string FileName, List<string> FileContent)>();
        if (string.IsNullOrEmpty(InputFile)) return result;

        var names = new List<string>();
        var files = new List<List<string>>();
        var fullInputFile = Path.GetFullPath(InputFile);
        assemblies = AutoUnitTestGeneratorHelper.GetAllAssemblies(Assembly.LoadFrom(fullInputFile), ExcludeFiles);

        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes().Where(t =>
                    t.IsClass || !t.IsInterface || !t.IsAbstract || !t.IsEnum || !t.IsValueType || !t.IsValueType)
                .OrderBy(t => t.Name).ToList();
            foreach (var t in types)
            {
                var attribute = Array.Find(t.GetCustomAttributes(false), a => string.Equals(a.GetType().FullName, "JamesBrighton.FozzieBear.AutoUnitTestAttribute", StringComparison.Ordinal));
                if (attribute == null) continue;
                var parameters = AutoUnitTestGeneratorHelper.InvokeMethod(attribute, "GetParameters") as List<string> ?? new List<string>();
                var n2 = new List<string>();
                var classes = Generate(t, parameters, ref n2);
                if (!classes.Any()) continue;
                files.AddRange(classes);
                names.AddRange(n2);
            }
        }

        assemblies.Clear();

        for (var i = 0; i < files.Count; i++)
        {
            var file = files[i];
            var name = names[i];
            result.Add((name, file));
        }
        return result;
    }

    /// <summary>
    ///     Gets the derived classes for a given type.
    /// </summary>
    /// <param name="type">Type.</param>
    /// <param name="skipType">Type to skip. Can be null (do not skip).</param>
    /// <returns>The derived classes.</returns>
    private IReadOnlyList<Type> GetDerivedClasses(Type type, Type? skipType)
    {
        if (DerivedClasses.TryGetValue((type, skipType), out var v))
            return v;
        var result = new List<Type>();

        var realType = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        foreach (var assembly in assemblies)
            foreach (var currentType in assembly.GetTypes())
            {
                if (!currentType.IsSubclassOf(realType) && (!realType.IsInterface || !AutoUnitTestGeneratorHelper.ImplementsInterface(currentType, realType))) continue;
                var tt = AutoUnitTestGeneratorHelper.GetInitializableType(type, currentType);
                if (tt?.IsGenericTypeDefinition == false && !tt.IsAbstract && tt.IsPublic &&
                    !AutoUnitTestGeneratorHelper.SkipClass(tt) &&
                    result.IndexOf(tt) < 0)
                    if (skipType == null || skipType != tt)
                        result.Add(tt);
            }

        DerivedClasses.Add(((type, skipType)), result);
        return result;
    }

    /// <summary>
    ///     Generate tests for the given class' type.
    /// </summary>
    /// <param name="classType">Class type to use.</param>
    /// <param name="parameters">List of parameters the class type should use. Eg. list of template arguments.</param>
    /// <param name="names">List of class names generated.</param>
    /// <returns>List of generated files.</returns>
    private IEnumerable<List<string>> Generate(Type classType, IReadOnlyCollection<string> parameters,
        ref List<string> names)
    {
        var classNames = new List<string>();
        var result = new List<List<string>>();
        var types = new List<Type>();
        if (parameters.Count > 0)
        {
            ExpandGeneric(classType, parameters, types, classNames);
        }
        else
        {
            types.Add(classType);
            classNames.Add(classType.Name);
        }

        for (var i = 0; i < types.Count; i++)
        {
            var type = types[i];
            var name = classNames[i];

            var instanceMethods = GetMethods(type, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            var staticMethods = GetMethods(type, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            var instanceGetProperties = GetGetProperties(type);
            var staticGetProperties = GetStaticGetProperties(type);
            var instanceSetProperties = GetSetProperties(type);
            var staticSetProperties = GetStaticSetProperties(type);

            var list = AutoUnitTestGeneratorHelper.CreateHeader(type, name);

            var counter = new Dictionary<string, int>();

            list.AddRange(GenerateMethods(type, instanceMethods, true, ref counter));
            list.AddRange(GenerateMethods(type, staticMethods, false, ref counter));
            list.AddRange(GenerateGetProperties(type, instanceGetProperties, true, ref counter));
            list.AddRange(GenerateGetProperties(type, staticGetProperties, false, ref counter));
            list.AddRange(GenerateSetProperties(type, instanceSetProperties, true, ref counter));
            list.AddRange(GenerateSetProperties(type, staticSetProperties, false, ref counter));

            list.Add("\t}");
            list.Add("}");
            list.Add("#pragma warning restore IDE0017, IDE0018, IDE0034, IDE0059, RCS1021, RCS1036, RCS1118, RCS1163, RCS1196, RCS1204");
            result.Add(list);
            names.Add(AutoUnitTestGeneratorHelper.GetUnitTestFileName(type, name));
        }

        return result;
    }

    /// <summary>
    ///     Gets the methods.
    /// </summary>
    /// <param name="type">Type.</param>
    /// <param name="bindingAttr">The binding attributes used to control the search.</param>
    /// <returns>The methods.</returns>
    private IEnumerable<MethodInfo> GetMethods(IReflect type, BindingFlags bindingAttr)
    {
        var result = new List<MethodInfo>();

        var methods = type.GetMethods(bindingAttr).Where(m =>
        {
            if (m.DeclaringType == null) return false;
            return AutoUnitTestGeneratorHelper.IsAssemblyDebugBuild(m) && !m.IsSpecialName &&
                   !IsExcluded(m) && !AutoUnitTestGeneratorHelper.SkipMember(m);
        }).OrderBy(m => m.Name).ToList();

        foreach (var m in methods)
        {
            var attribute = m.GetCustomAttributes().FirstOrDefault(a => string.Equals(a.GetType().FullName, "JamesBrighton.FozzieBear.InvokeAttribute", StringComparison.Ordinal));
            if (attribute == null || !m.IsGenericMethod)
            {
                result.Add(m);
                continue;
            }
            if (AutoUnitTestGeneratorHelper.InvokeMethod(attribute, "GetTypeParameters") is not List<Type> parameters) return result;
            result.AddRange(parameters.Select(t => m.MakeGenericMethod(t)));
            if (AutoUnitTestGeneratorHelper.InvokeMethod(attribute, "GetStringParameters") is not List<string> parameters2) return result;
            result.AddRange(parameters2.Select(GetParameterTypes).Select(parameterTypes => m.MakeGenericMethod(parameterTypes.ToArray())));
        }

        return result;
    }

    /// <summary>
    ///     Gets the static set properties.
    /// </summary>
    /// <returns>The static set properties.</returns>
    /// <param name="type">Type.</param>
    private IEnumerable<PropertyInfo> GetStaticSetProperties(IReflect type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Static).Where(IsSetProperty).OrderBy(p => p.Name).ToList();
    }

    /// <summary>
    ///     Gets the instance set properties.
    /// </summary>
    /// <returns>The set properties.</returns>
    /// <param name="type">Type.</param>
    private IEnumerable<PropertyInfo> GetSetProperties(IReflect type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(IsSetProperty).OrderBy(p => p.Name).ToList();
    }

    /// <summary>
    ///     Checks if the given property is a set property.
    /// </summary>
    /// <param name="p">Property to check</param>
    /// <returns>True if it is and false otherwise.</returns>
    private bool IsSetProperty(PropertyInfo p)
    {
        if (p.DeclaringType == null) return false;
        return AutoUnitTestGeneratorHelper.IsAssemblyDebugBuild(p) && !p.IsSpecialName &&
               p.CanWrite && p.GetSetMethod(true)?.IsPublic == true && !IsExcluded(p) && !AutoUnitTestGeneratorHelper.SkipMember(p);
    }

    /// <summary>
    ///     Gets the static get properties.
    /// </summary>
    /// <returns>The static get properties.</returns>
    /// <param name="type">Type.</param>
    private IEnumerable<PropertyInfo> GetStaticGetProperties(IReflect type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Static).Where(IsGetProperty).OrderBy(p => p.Name).ToList();
    }

    /// <summary>
    ///     Gets the instance get properties.
    /// </summary>
    /// <returns>The get properties.</returns>
    /// <param name="type">Type.</param>
    private IEnumerable<PropertyInfo> GetGetProperties(IReflect type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(IsGetProperty).OrderBy(p => p.Name).ToList();
    }

    /// <summary>
    ///     Checks if the given property is a get property.
    /// </summary>
    /// <param name="p">Property to check</param>
    /// <returns>True if it is and false otherwise.</returns>
    private bool IsGetProperty(PropertyInfo p)
    {
        if (p.DeclaringType == null) return false;
        return AutoUnitTestGeneratorHelper.IsAssemblyDebugBuild(p) && !p.IsSpecialName &&
               p.CanRead && p.GetGetMethod(true)?.IsPublic == true && !IsExcluded(p) && !AutoUnitTestGeneratorHelper.SkipMember(p);
    }

    /// <summary>
    ///     Gets all combinations for the given array type.
    /// </summary>
    /// <param name="type">Type.</param>
    /// <param name="isNullable">Type is nullable (true) or not (false)</param>
    /// <returns>The combinations.</returns>
    private List<AutoUnitTestParameter> GetArrayParams(Type type, bool isNullable)
    {
        var elementType = type.GetElementType();
        if (elementType == null) return new List<AutoUnitTestParameter>();
        if (elementType.IsPrimitive || elementType.IsEnum || elementType == typeof(string) ||
            elementType.IsValueType || elementType.IsClass && !elementType.IsAbstract)
        {
            var fullName = AutoUnitTestGeneratorHelper.GetFullName(elementType);
            var result = AutoUnitTestGeneratorHelper.GenerateArrayInits(fullName, isNullable);
            return result;
        }

        if (!elementType.IsAbstract && !elementType.IsInterface) return new List<AutoUnitTestParameter>();
        {
            var result = new List<AutoUnitTestParameter>();
            var classes = GetDerivedClasses(elementType, null);
            // Don't need all classes
            var classes2 = AutoUnitTestGeneratorHelper.GetRandomItems(classes, 3);
            foreach (var @class in classes2)
            {
                var fullName = AutoUnitTestGeneratorHelper.GetFullName(@class);
                result.AddRange(AutoUnitTestGeneratorHelper.GenerateArrayInits(fullName, isNullable));
            }

            return result;
        }
    }

    /// <summary>
    ///     Expands the given generic type.
    /// </summary>
    /// <param name="classType">Generic class type.</param>
    /// <param name="parameters">Parameters for the generic class.</param>
    /// <param name="types">Resulting types.</param>
    /// <param name="names">Resulting names.</param>
    private void ExpandGeneric(Type classType, IEnumerable<string> parameters, ICollection<Type> types, ICollection<string> names)
    {
        foreach (var param in parameters)
        {
            var parameterList = GetParameterTypes(param);
            var concreteType = classType.MakeGenericType(parameterList.ToArray());
            types.Add(concreteType);
            var name = classType.Name;
            var backTick = name.IndexOf('`');
            if (backTick > 0)
                name = name.Remove(backTick);
            names.Add(name + AutoUnitTestGeneratorHelper.NameToClassName(param));
        }
    }

    /// <summary>
    ///     Gets the types of the given parameters for a to be determined generics class.
    /// </summary>
    /// <param name="parameters">Parameters.</param>
    /// <returns>The parameter types.</returns>
    private List<Type> GetParameterTypes(string parameters)
    {
        var parameterArray = parameters.Split(',');

        return parameterArray.Select(p => GetType(AutoUnitTestGeneratorHelper.GetDotNetName(p))).Where(type => type != null).ToList();
    }

    /// <summary>
    ///     Get the type for the given name.
    /// </summary>
    /// <param name="typeName">Name of the type.</param>
    /// <returns>The type or null if not found.</returns>
    private Type GetType(string typeName)
    {
        foreach (var assembly in assemblies)
            foreach (var t in assembly.GetTypes())
                if (AutoUnitTestGeneratorHelper.GetFullName(t) == typeName)
                    return t;
        return Type.GetType(typeName)!;
    }

    /// <summary>
    ///     Generates the C# methods for the given class and its attached methods.
    /// </summary>
    /// <param name="type">Class type.</param>
    /// <param name="methods">Methods.</param>
    /// <param name="needsInstance">True if an instance variable is required and false otherwise.</param>
    /// <param name="counter">Counter for the method name generation.</param>
    /// <returns>The C# code.</returns>
    private IEnumerable<string> GenerateMethods(Type type, IEnumerable<MethodInfo> methods, bool needsInstance,
        ref Dictionary<string, int> counter)
    {
        if (!needsInstance) return GenerateMethods(type, methods, ref counter, new List<string>(), "", needsInstance);
        var result = new List<string>();
        foreach (var c in type.GetConstructors())
            foreach (var paramSet in GetAllCombinations(c.GetParameters()))
            {
                var constructorParamDeclarations = new List<string>();
                var constructorParamList = AutoUnitTestGeneratorHelper.GetParams(paramSet, "ctorParam", constructorParamDeclarations);
                result.AddRange(GenerateMethods(type, methods, ref counter, constructorParamDeclarations,
                    constructorParamList, needsInstance));
            }

        return result;
    }

    /// <summary>
    ///     Generates the C# methods for the given class and its attached methods.
    /// </summary>
    /// <param name="type">Class type.</param>
    /// <param name="methods">Methods.</param>
    /// <param name="counter">Counter for the method name generation.</param>
    /// <param name="constructorParamDeclarations">List of the constructor's parameter declarations</param>
    /// <param name="constructorParamList">String with the list of the constructor's parameter list</param>
    /// <param name="needsInstance">True if an instance variable is required and false otherwise.</param>
    /// <returns>The C# code.</returns>
    private IEnumerable<string> GenerateMethods(Type type, IEnumerable<MethodInfo> methods,
        ref Dictionary<string, int> counter, IReadOnlyCollection<string> constructorParamDeclarations,
        string constructorParamList, bool needsInstance)
    {
        var result = new List<string>();
        var fullName = AutoUnitTestGeneratorHelper.GetFullName(type);
        var isDisposable = AutoUnitTestGeneratorHelper.ImplementsInterface(type, typeof(IDisposable));
        foreach (var m in methods)
        {
            var exceptionTypes = AutoUnitTestGeneratorHelper.GetExceptions(m);
            var returns = AutoUnitTestGeneratorHelper.GetReturns(m);
            if (string.IsNullOrEmpty(returns))
                returns = AutoUnitTestGeneratorHelper.GetReturnsFor(type, m);
            foreach (var paramSet in GetAllCombinations(m.GetParameters()))
            {
                var isAwaitable = m.ReturnType == typeof(Task) || GenericTypeIs(m.ReturnType, typeof(Task<>));
                var methodResult = (m.ReturnType != typeof(void) && m.ReturnType != typeof(Task))
                    ? GetFullReturnName(m.ReturnType) + " result = "
                    : "";
                if (isAwaitable)
                    methodResult += "await ";
                var method = new List<string>
                {
                    "\t\t[Test]",
                    $"\t\tpublic {(isAwaitable ? "async System.Threading.Tasks.Task " : "void ")}{m.Name}{AutoUnitTestGeneratorHelper.IntToHex(AutoUnitTestGeneratorHelper.GetMethodNumber(m.Name, ref counter), 4)}()",
                    "\t\t{"
                };
                if (needsInstance)
                {
                    method.AddRange(constructorParamDeclarations);
                    method.Add($"\t\t\tvar instance = new {fullName}({constructorParamList});");
                }

                var methodParamList = AutoUnitTestGeneratorHelper.GetParams(paramSet, needsInstance ? "methodParam" : "param", method);
                var jump = "";
                if (exceptionTypes.Any())
                {
                    jump = "\t";
                    method.Add("\t\t\ttry");
                    method.Add("\t\t\t{");
                }

                var genericArgNames = string.Join(", ", m.GetGenericArguments().Select(AutoUnitTestGeneratorHelper.GetFullName));
                if (!string.IsNullOrEmpty(genericArgNames))
                    genericArgNames = string.Concat("<", genericArgNames, ">");

                method.Add(
                    $"{jump}\t\t\t{methodResult}{(needsInstance ? "instance" : fullName)}.{m.Name}{genericArgNames}({methodParamList});");
                if (OutputResult && m.ReturnType != typeof(void) && string.IsNullOrEmpty(returns))
                {
                    if (m.ReturnType.IsValueType)
                        method.Add($"\t\t\tglobal::System.Console.WriteLine(\"{fullName}.{m.Name}\\t\" + result.ToString());");
                    else
                        method.Add($"\t\t\tglobal::System.Console.WriteLine(\"{fullName}.{m.Name}\\t\" + (result?.ToString() ?? \"null\"));");
                }

                if (!string.IsNullOrEmpty(returns)) method.Add($"{jump}\t\t\tAssert.That({returns}, Is.True);");
                if (m.ReturnType != typeof(void) && AutoUnitTestGeneratorHelper.ImplementsInterface(m.ReturnType, typeof(IDisposable)) && !isAwaitable) method.Add($"{jump}\t\t\tresult?.Dispose();");
                if (exceptionTypes.Any())
                {
                    method.Add("\t\t\t}");
                    method.AddRange(exceptionTypes.Select(e => $"\t\t\tcatch ({AutoUnitTestGeneratorHelper.GetFullName(e)}) {{}}"));
                }

                if (needsInstance && isDisposable && !isAwaitable) method.Add("\t\t\tinstance?.Dispose();");
                method.Add("\t\t}");
                method.Add("");
                result.AddRange(method);
            }
        }

        return result;
    }

    /// <summary>
    ///     Generates the C# get-properties for the given class and its attached properties.
    /// </summary>
    /// <param name="type">Class type.</param>
    /// <param name="properties">Properties.</param>
    /// <param name="needsInstance">True if an instance variable is required and false otherwise.</param>
    /// <param name="counter">Counter for the method name generation.</param>
    /// <returns>The C# code.</returns>
    private IEnumerable<string> GenerateGetProperties(Type type, IEnumerable<PropertyInfo> properties, bool needsInstance,
        ref Dictionary<string, int> counter)
    {
        if (!needsInstance) return GenerateGetProperties(type, properties, ref counter, new List<string>(), "", needsInstance);
        var result = new List<string>();
        foreach (var c in type.GetConstructors())
            foreach (var paramSet in GetAllCombinations(c.GetParameters()))
            {
                var paramDeclarations = new List<string>();
                var paramList = AutoUnitTestGeneratorHelper.GetParams(paramSet, "ctorParam", paramDeclarations);
                result.AddRange(GenerateGetProperties(type, properties, ref counter, paramDeclarations, paramList, needsInstance));
            }

        return result;
    }

    /// <summary>
    ///     Generates the C# get-properties for the given class and its attached properties.
    /// </summary>
    /// <param name="type">Class type.</param>
    /// <param name="properties">Properties.</param>
    /// <param name="counter">Counter for the method name generation.</param>
    /// <param name="constructorParamDeclarations">List of the constructor's parameter declarations</param>
    /// <param name="constructorParamList">String with the list of the constructor's parameter list</param>
    /// <param name="needsInstance">True if an instance variable is required and false otherwise.</param>
    /// <returns>The C# code.</returns>
    private IEnumerable<string> GenerateGetProperties(Type type, IEnumerable<PropertyInfo> properties,
        ref Dictionary<string, int> counter, IReadOnlyCollection<string> constructorParamDeclarations,
        string constructorParamList, bool needsInstance)
    {
        var result = new List<string>();
        var fullName = AutoUnitTestGeneratorHelper.GetFullName(type);
        var isDisposable = AutoUnitTestGeneratorHelper.ImplementsInterface(type, typeof(IDisposable));
        foreach (var prop in properties)
            foreach (var propertySet in GetAllCombinations(prop))
            {
                var propResult = prop.PropertyType != typeof(void)
                    ? AutoUnitTestGeneratorHelper.GetFullName(prop.PropertyType) + " propResult = "
                    : "";
                var method = AutoUnitTestGeneratorHelper.GenerateMethodDeclaration(prop, ref counter, needsInstance, fullName, constructorParamDeclarations, constructorParamList);

                method.Add($"\t\t\t{propResult}{(needsInstance ? "instance" : fullName)}{propertySet.Value};");
                if (prop.PropertyType != typeof(void) && AutoUnitTestGeneratorHelper.ImplementsInterface(prop.PropertyType, typeof(IDisposable))) method.Add("\t\t\tpropResult?.Dispose();");
                if (needsInstance && isDisposable) method.Add("\t\t\tinstance?.Dispose();");
                method.Add("\t\t}");
                method.Add("");
                result.AddRange(method);
            }

        return result;
    }

    /// <summary>
    ///     Generates the C# set-properties for the given class and its attached properties.
    /// </summary>
    /// <param name="type">Class type.</param>
    /// <param name="properties">Properties.</param>
    /// <param name="needsInstance">True if an instance variable is required and false otherwise.</param>
    /// <param name="counter">Counter for the method name generation.</param>
    /// <returns>The C# code.</returns>
    private IEnumerable<string> GenerateSetProperties(Type type, IEnumerable<PropertyInfo> properties, bool needsInstance,
        ref Dictionary<string, int> counter)
    {
        if (!needsInstance) return GenerateSetProperties(type, properties, ref counter, new List<string>(), "", needsInstance);
        var result = new List<string>();
        foreach (var c in type.GetConstructors())
            foreach (var paramSet in GetAllCombinations(c.GetParameters()))
            {
                var paramDeclarations = new List<string>();
                var paramList = AutoUnitTestGeneratorHelper.GetParams(paramSet, "ctorParam", paramDeclarations);
                result.AddRange(GenerateSetProperties(type, properties, ref counter, paramDeclarations, paramList, needsInstance));
            }

        return result;
    }

    /// <summary>
    ///     Generates the C# set-properties for the given class and its attached properties.
    /// </summary>
    /// <param name="type">Class type.</param>
    /// <param name="properties">Properties.</param>
    /// <param name="counter">Counter for the method name generation.</param>
    /// <param name="constructorParamDeclarations">List of the constructor's parameter declarations</param>
    /// <param name="constructorParamList">String with the list of the constructor's parameter list</param>
    /// <param name="needsInstance">True if an instance variable is required and false otherwise.</param>
    /// <returns>The C# code.</returns>
    private IEnumerable<string> GenerateSetProperties(Type type, IEnumerable<PropertyInfo> properties,
        ref Dictionary<string, int> counter, IReadOnlyCollection<string> constructorParamDeclarations,
        string constructorParamList, bool needsInstance)
    {
        var result = new List<string>();
        var fullName = AutoUnitTestGeneratorHelper.GetFullName(type);

        var isDisposable = AutoUnitTestGeneratorHelper.ImplementsInterface(type, typeof(IDisposable));
        foreach (var prop in properties)
        {
            var exceptionTypes = AutoUnitTestGeneratorHelper.GetExceptions(prop);
            foreach (var propertySet in GetTypeVariations(null, prop.PropertyType, 0))
                foreach (var indexerSet in GetAllIndexCombinations(prop))
                {
                    var method = AutoUnitTestGeneratorHelper.GenerateMethodDeclaration(prop, ref counter, needsInstance, fullName, constructorParamDeclarations, constructorParamList);

                    var p = $".{prop.Name}";
                    if (!indexerSet.IsEmpty()) p = indexerSet.Value;
                    var jump = "";
                    if (exceptionTypes.Any())
                    {
                        jump = "\t";
                        method.Add("\t\t\ttry");
                        method.Add("\t\t\t{");
                    }

                    method.Add($"{jump}\t\t\t{(needsInstance ? "instance" : fullName)}{p} = {propertySet.Value};");
                    if (exceptionTypes.Any())
                    {
                        method.Add("\t\t\t}");
                        method.AddRange(exceptionTypes.Select(e => $"\t\t\tcatch ({AutoUnitTestGeneratorHelper.GetFullName(e)})"));
                        method.Add("\t\t\t{");
                        method.Add("\t\t\t}");
                    }

                    if (needsInstance && isDisposable) method.Add("\t\t\tinstance?.Dispose();");
                    method.Add("\t\t}");
                    method.Add("");
                    result.AddRange(method);
                }
        }

        return result;
    }

    /// <summary>
    ///     Gets all combinations for the given struct type.
    /// </summary>
    /// <param name="type">Type.</param>
    /// <returns>The combinations.</returns>
    private List<AutoUnitTestParameter> GetStructParams(Type type)
    {
        var fullName = AutoUnitTestGeneratorHelper.GetFullName(type);
        var result = new List<AutoUnitTestParameter>
        {
            new(fullName, $"new {GetConstructor(type, 0)}")
        };
        return result;
    }

    /// <summary>
    ///     Gets a mock implementation for the given interface type.
    /// </summary>
    /// <param name="enclosingConstructor">Type for calls this method in its constructor. Null means 'doesn't matter'.</param>
    /// <param name="type">Type.</param>
    /// <param name="level">Level of the call. Used to stop endless recursion.</param>
    /// <param name="isNullable">Type is nullable (true) or not (false)</param>
    /// <returns>The combinations.</returns>
    private List<AutoUnitTestParameter> GetInterfaceParams(Type? enclosingConstructor, Type type, int level, bool isNullable)
    {
        var result = new List<AutoUnitTestParameter>();
        // Endless recursion detection
        if (level >= 4)
            return result;
        var fullName = AutoUnitTestGeneratorHelper.GetFullName(type);

        if (isNullable)
            result.Add(new AutoUnitTestParameter(fullName, $"({fullName})null"));
        var list = new List<AutoUnitTestParameter>();
        list.AddRange(from @class in GetDerivedClasses(type, enclosingConstructor)
                      select GetConstructor(@class, level + 1)
            into c
                      where !string.IsNullOrEmpty(c)
                      select new AutoUnitTestParameter(fullName, $"new {c}"));
        // Don't need all items
        result.AddRange(AutoUnitTestGeneratorHelper.GetRandomItems(list, 3));

        if (!type.IsInterface) return result;

        if (AutoUnitTestGeneratorHelper.ImplementsInterface(type, typeof(IEnumerable)) || type == typeof(IEnumerable))
        {
            if (AutoUnitTestGeneratorHelper.ImplementsInterface(type, typeof(IDictionary<,>)) || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary))
            {
                var args = type.GetGenericArguments();
                result.Add(new AutoUnitTestParameter(fullName,
                    $"JamesBrighton.FozzieBear.InterfaceProxy.Create<{fullName}>((string methodName, object[] args, ref object result) => {{if (methodName != \"System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<{AutoUnitTestGeneratorHelper.GetFullName(args[0])},{AutoUnitTestGeneratorHelper.GetFullName(args[1])}>> GetEnumerator()\") return false; result = System.Collections.Immutable.ImmutableDictionary<{AutoUnitTestGeneratorHelper.GetFullName(args[0])},{AutoUnitTestGeneratorHelper.GetFullName(args[1])}>.Empty.GetEnumerator(); return true;}})"));
            }
            else
            {
                var args = type.GetGenericArguments();
                result.Add(args.Length > 0
                    ? new AutoUnitTestParameter(fullName,
                        $"JamesBrighton.FozzieBear.InterfaceProxy.Create<{fullName}>((string methodName, object[] args, ref object result) => {{if (methodName != \"System.Collections.Generic.IEnumerator<{AutoUnitTestGeneratorHelper.GetFullName(args[0])}> GetEnumerator()\") return false; result = System.Linq.Enumerable.Empty<{AutoUnitTestGeneratorHelper.GetFullName(args[0])}>().GetEnumerator(); return true;}})")
                    : new AutoUnitTestParameter(fullName,
                        $"JamesBrighton.FozzieBear.InterfaceProxy.Create<{fullName}>((string methodName, object[] args, ref object result) => {{if (methodName != \"System.Collections.IEnumerator GetEnumerator()\") return false; result = new System.Collections.ArrayList().GetEnumerator(); return true;}})"));
            }
        }
        else
        {
            result.Add(new AutoUnitTestParameter(fullName, $"JamesBrighton.FozzieBear.InterfaceProxy.Create<{fullName}>()"));
        }
        return result;
    }

    /// <summary>
    ///     Gets all combinations for the given class type.
    /// </summary>
    /// <param name="type">Type.</param>
    /// <param name="isNullable">Type is nullable (true) or not (false)</param>
    /// <returns>The combinations.</returns>
    private List<AutoUnitTestParameter> GetClassParams(Type type, bool isNullable)
    {
        var fullName = AutoUnitTestGeneratorHelper.GetFullName(type);

        var result = new List<AutoUnitTestParameter>();

        if (isNullable)
            result.Add(new AutoUnitTestParameter(fullName, $"({fullName})null"));
        var c = GetConstructor(type, 0);
        if (string.IsNullOrEmpty(c)) return result;
        result.Add(new AutoUnitTestParameter(fullName, $"new {c}"));
        return result;
    }

    /// <summary>
    ///     Gets the constructor as a C# code declaration for the given type.
    /// </summary>
    /// <param name="type">Type.</param>
    /// <param name="level">Level of the call.</param>
    /// <returns>The constructor or an empty string if not found.</returns>
    private string GetConstructor(Type type, int level)
    {
        var fullName = AutoUnitTestGeneratorHelper.GetFullName(type);

        if (AutoUnitTestGeneratorHelper.IsDelegate(type))
        {
            var method = Array.Find(type.GetMethods(), x => x.Name.Equals("Invoke", StringComparison.Ordinal));
            if (method == null) return "";
            var args = method.GetParameters();
            var l = "";
            for (var i = 0; i < args.Length; i++)
            {
                l += $"p{AutoUnitTestGeneratorHelper.IntToHex(i, 2)}";
                if (i < args.Length - 1) l += ", ";
            }

            return $"{fullName}(({l}) => {{ return default({AutoUnitTestGeneratorHelper.GetFullName(method.ReturnType)}); }})";
        }

        if (AutoUnitTestGeneratorHelper.TypeEquals(type, typeof(CultureInfo)))
        {
            var param = AutoUnitTestGeneratorHelper.GetRandomCultureInfoParam();
            return $"{fullName}(\"{param}\")";
        }

        if (IsStruct(type))
        {
            // A struct always has a parameterless constructor
            return $"{fullName}()";
        }

        var constructors = type.GetConstructors().Where(x => !x.GetParameters().Any(y => y.ParameterType.IsPointer))
            .OrderBy(x => x.GetParameters().Length);
        if (!constructors.Any()) return "";

        var list = "";
        var pars = constructors.ElementAt(0).GetParameters();
        for (var i = 0; i < pars.Length; i++)
        {
            var param = pars[i];
            var vars = GetParameterVariations(type, level, param);
            if (vars.Count == 0) return "";

            list += vars[0].Value;
            if (i < pars.Length - 1) list += ", ";
        }

        return $"{fullName}({list})";
    }

    /// <summary>
    ///     Get all the indexer combinations for a given set property
    /// </summary>
    /// <param name="property">Property.</param>
    /// <returns>The combinations.</returns>
    private IEnumerable<AutoUnitTestParameter> GetAllIndexCombinations(PropertyInfo property)
    {
        if (property.SetMethod == null) return new List<AutoUnitTestParameter>();
        if (property.SetMethod.GetParameters().Length <= 1)
            return new List<AutoUnitTestParameter> { new() };

        var param = property.SetMethod.GetParameters()[0];
        var type = param.ParameterType;
        var isNullable = ParameterInfoFunction.IsNullable(param);
        var result = GetTypeVariations(null, type, 0, isNullable);
        foreach (var r in result)
            r.Value = $"[{r.Value}]";
        return result;
    }

    /// <summary>
    ///     Gets all the combinations for a given get property
    /// </summary>
    /// <param name="property">Property.</param>
    /// <returns>The combinations.</returns>
    private IEnumerable<AutoUnitTestParameter> GetAllCombinations(PropertyInfo property)
    {
        if (property.GetMethod == null) return new List<AutoUnitTestParameter>();
        if (!property.GetMethod.GetParameters().Any())
            return new List<AutoUnitTestParameter> { new(AutoUnitTestGeneratorHelper.GetFullName(property.PropertyType), $".{property.Name}") };

        var param = property.GetMethod.GetParameters()[0];
        var type = param.ParameterType;
        var isNullable = ParameterInfoFunction.IsNullable(param);
        var result = GetTypeVariations(null, type, 0, isNullable);
        foreach (var r in result)
            r.Value = $"[{r.Value}]";
        return result;
    }

    /// <summary>
    ///     Gets all possible combinations of a given parameter list.
    /// </summary>
    /// <param name="parameters">Parameters.</param>
    /// <returns>The combinations.</returns>
    private IEnumerable<IEnumerable<AutoUnitTestParameter>> GetAllCombinations(IEnumerable<ParameterInfo> parameters)
    {
        var cases = new List<IEnumerable<AutoUnitTestParameter>>();
        foreach (var parameter in parameters)
        {
            var vars = GetParameterVariations(null, 0, parameter);
            cases.Add(vars);
        }
        return AutoUnitTestGeneratorHelper.Cartesian(cases);
    }

    /// <summary>
    ///     Gets parameter variations for a given parameter
    /// </summary>
    /// <param name="enclosingConstructor">Type for calls this method in its constructor. Null means 'doesn't matter'.</param>
    /// <param name="level">Level of the call.</param>
    /// <param name="param">Given parameter.</param>
    /// <returns>All variations.</returns>
    private List<AutoUnitTestParameter> GetParameterVariations(Type? enclosingConstructor, int level, ParameterInfo param)
    {
        var isNullable = ParameterInfoFunction.IsNullable(param);
        var typeVariations = GetTypeVariations(enclosingConstructor, param.ParameterType, level, isNullable);
        foreach (var p in typeVariations)
            if (param.IsOut)
                p.Direction = "out";
            else if (param.ParameterType.IsByRef)
                p.Direction = "ref";
            else
                p.Direction = "";
        if (!param.IsOut)
            return typeVariations;

        var result = new List<AutoUnitTestParameter>();
        if (typeVariations.Count > 0)
            result.Add(typeVariations[0]);
        return result;
    }

    /// <summary>
    ///     Gets parameter variations for a given type
    /// </summary>
    /// <param name="enclosingConstructor">Type for calls this method in its constructor. Null means 'doesn't matter'.</param>
    /// <param name="type">Given type.</param>
    /// <param name="level">Level of the call.</param>
    /// <param name="isNullable">Type is nullable (true) or not (false)</param>
    /// <returns>All variations.</returns>
    private List<AutoUnitTestParameter> GetTypeVariations(Type? enclosingConstructor, Type type, int level, bool isNullable = true)
    {
        var coreType = AutoUnitTestGeneratorHelper.GetCoreType(type);
        List<AutoUnitTestParameter> result = new();
        if (coreType.IsPrimitive)
            result = AutoUnitTestGeneratorHelper.GetPrimitiveParams(coreType);
        else if (coreType.IsEnum)
            result = AutoUnitTestGeneratorHelper.GetEnumParams(coreType);
        else if (coreType == typeof(string))
            result = AutoUnitTestGeneratorHelper.GetStringParams(coreType, isNullable);
        else if (coreType == typeof(object))
            result = AutoUnitTestGeneratorHelper.GetObjectParams(coreType, isNullable);
        else if (coreType == typeof(Type))
            result = AutoUnitTestGeneratorHelper.GetTypeParams(coreType, isNullable);
        else if (coreType == typeof(DateTimeOffset))
            result = AutoUnitTestGeneratorHelper.GetDateTimeOffsetParams(coreType, isNullable);
        else if (AutoUnitTestGeneratorHelper.IsDelegate(coreType))
            result = AutoUnitTestGeneratorHelper.GetDelegateParams(coreType);
        else if (coreType.IsValueType)
            result = GetStructParams(coreType);
        else if (type.IsArray)
            result = GetArrayParams(type, isNullable);
        else if (type.IsInterface || type.IsAbstract)
            result = GetInterfaceParams(enclosingConstructor, type, level, isNullable);
        else if (type.IsClass && !type.IsAbstract) result = GetClassParams(type, isNullable);

        return result;
    }

    /// <summary>
    ///     Checks if the given member is to be excluded (based on its assembly file name)
    /// </summary>
    /// <param name="m">Member to check</param>
    /// <returns>True if it is and false otherwise.</returns>
    private bool IsExcluded(MemberInfo m)
    {
        var fileName = m.DeclaringType?.Assembly.Location ?? "";
        foreach (var excludeFile in ExcludeFiles)
        {
            if (excludeFile.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    ///     Checks if the given concrete type is of a given generic type.
    /// </summary>
    /// <param name="concreteType">Given concrete type.</param>
    /// <param name="genericType">Given generic type.</param>
    /// <returns>True if it is and false otherwise.</returns>
    private static bool GenericTypeIs(Type concreteType, Type genericType)
    {
        return concreteType.IsGenericType && concreteType.GetGenericTypeDefinition() == genericType;
    }

    /// <summary>
    ///     Get the full name of a return type.
    /// </summary>
    /// <param name="type">Type.</param>
    /// <returns>Full name.</returns>
    private static string GetFullReturnName(Type type)
    {
        if (type == typeof(Task)) return "void";
        if (!GenericTypeIs(type, typeof(Task<>))) return AutoUnitTestGeneratorHelper.GetFullName(type);
        var arguments = type.GetGenericArguments();
        if (arguments.Length == 0) return "";
        return AutoUnitTestGeneratorHelper.GetFullName(arguments[0]);
    }

	/// <summary>
	///     Checks if a type is a struct
	/// </summary>
	/// <param name="type">Type.</param>
	/// <returns>True if it is and false otherwise.</returns>
	private static bool IsStruct(Type type)
	{
		return type?.IsValueType == true && !type.IsEnum && !type.IsPrimitive;
	}

    /// <summary>
    /// Derived classes cache
    /// </summary>
    private readonly Dictionary<(Type, Type?), IReadOnlyList<Type>> DerivedClasses = new();
}

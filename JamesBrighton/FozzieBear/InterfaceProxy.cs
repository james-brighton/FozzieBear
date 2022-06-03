#nullable disable

using System.Reflection;
using System.Reflection.Emit;
using Mercury.Common.Function;

namespace JamesBrighton.FozzieBear;

/// <summary>
/// This class creates implementations of interfaces, allowing dynamic behaviour to be specified by its method handler delegate.
/// </summary>
public class InterfaceProxy
{
	/// <summary>
	/// This delegate represents the method handler. The method name, the arguments of the method (as an object array) and the result (as an object) are the arguments. Return true if the method was handled and false otherwise. False means the default value of the given return type is used.
	/// </summary>
	public delegate bool MethodHandler(string methodName, object[] args, ref object result);

	/// <summary>
	/// Create the implementation of the specified interface type and providing the optional method handler.
	/// </summary>
	/// <typeparam name="TInterface">Type of the interface to construct</typeparam>
	/// <param name="methodHandler">Method handler.</param>
	/// <returns>The instance or null otherwise.</returns>
	public static TInterface Create<TInterface>(MethodHandler methodHandler = null) where TInterface : class
	{
		var result = Create(typeof(TInterface), methodHandler);
		return result as TInterface;
	}

	/// <summary>
	/// Create the implementation of the specified interface type and providing the optional method handler.
	/// </summary>
	/// <param name="interfaceType">Interface type.</param>
	/// <param name="methodHandler">Method handler.</param>
	/// <returns>The instance or null otherwise.</returns>
	public static object Create(Type interfaceType, MethodHandler methodHandler = null)
	{
		if (interfaceType?.IsInterface != true || !interfaceType.IsPublic || interfaceType.IsGenericTypeDefinition) return null;
		var type = CreateType(interfaceType, typeof(InterfaceProxy));
		var result = type != null ? Activator.CreateInstance(type) : null;
		if (result == null || result is not InterfaceProxy i) return result;
		i.methodHandler = methodHandler;
		return result;
	}

	/// <summary>
	/// Tries to invoke the member. This is an internal method used to trigger the method handler.
	/// </summary>
	/// <param name="methodName">Method name.</param>
	/// <param name="args">Arguments.</param>
	/// <param name="result">Result.</param>
	/// <returns>True if the method was handled and false otherwise.</returns>
	protected bool TryInvokeMember(string methodName, object[] args, out object result)
	{
		result = null;
		return methodHandler != null && methodHandler(methodName, args, ref result);
	}

	/// <summary>
	/// Initializes the <see cref="JamesBrighton.FozzieBear.InterfaceProxy" /> class.
	/// </summary>
	static InterfaceProxy()
	{
		var typeFromHandle = typeof(InterfaceProxy);
		tryInvokeMemberInfo = Array.Find(typeFromHandle.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic), x => x.Name.Equals("TryInvokeMember", StringComparison.Ordinal));
		var guid = Guid.NewGuid().ToString();
		var assemblyName = new AssemblyName($"{typeFromHandle.Namespace}.{typeFromHandle.Name}_{guid}");
		var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
		moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name + ".dll");
		cache = new Dictionary<string, Type>();
	}

	/// <summary>
	/// Creates the implementation type for the given interface type and the base class.
	/// </summary>
	/// <param name="interfaceType">Interface type.</param>
	/// <param name="baseType">Base type to use. The implementation will use this as its parent</param>
	/// <returns>The type or null otherwise.</returns>
	static Type CreateType(Type interfaceType, Type baseType)
	{
		var name = $"{nameof(InterfaceProxy)}+{GetFullName(interfaceType)}";
		if (cache.TryGetValue(name, out var type))
			return type;
		var typeBuilder = moduleBuilder.DefineType(name, TypeAttributes.NotPublic);
		typeBuilder.SetParent(baseType);
		typeBuilder.AddInterfaceImplementation(interfaceType);
		CreateConstructorBaseCalls(baseType, typeBuilder);
		ImplementInterface(new List<string>(), new List<Type>
		{
			interfaceType
		}, interfaceType, typeBuilder);
		var result = typeBuilder.CreateType();
		cache.Add(name, result);
		return result;

	}

	/// <summary>
	/// Get the full name of a type.
	/// </summary>
	/// <param name="type">Type.</param>
	/// <returns>Full name.</returns>
	static string GetFullName(Type type)
	{
		return CodeFunction.GetFriendlyName(type);
	}

	/// <summary>
	/// Implements the interface recursively.
	/// </summary>
	/// <param name="usedMethods">List of methods already implemented.</param>
	/// <param name="implementedInterfaceList">List of already implemented interfaces.</param>
	/// <param name="interfaceType">Interface type to implement.</param>
	/// <param name="typeBuilder">The type builder to use.</param>
	static void ImplementInterface(ICollection<string> usedMethods, ICollection<Type> implementedInterfaceList, Type interfaceType, TypeBuilder typeBuilder)
	{
		GenerateMethods(usedMethods, interfaceType, typeBuilder);
		var interfaces = interfaceType.GetInterfaces();
		foreach (var type in interfaces)
			if (!implementedInterfaceList.Contains(type))
			{
				ImplementInterface(usedMethods, implementedInterfaceList, type, typeBuilder);
				implementedInterfaceList.Add(type);
			}
	}

	/// <summary>
	/// Emits the method's body.
	/// </summary>
	/// <param name="methodInfo">Method info.</param>
	/// <param name="methodBuilder">Method builder.</param>
	static void EmitInvokeMethod(MethodInfo methodInfo, MethodBuilder methodBuilder)
	{
		var iLGenerator = methodBuilder.GetILGenerator();
		var name = GetMethodName(methodInfo);

		// var result = object
		// var args = object[]
		// var result = bool
		iLGenerator.DeclareLocal(typeof(object), true);
		iLGenerator.DeclareLocal(typeof(object[]), true);
		iLGenerator.DeclareLocal(typeof(bool), true);

		if (methodInfo.ReturnType != typeof(void))
		{
			iLGenerator.DeclareLocal(methodInfo.ReturnType, true);
			if (IsStruct(methodInfo.ReturnType))
				iLGenerator.DeclareLocal(methodInfo.ReturnType, true);
		}
		var parameters = methodInfo.GetParameters();
		SetOutParameters(iLGenerator, parameters);

		var objConstructor = typeof(object).GetConstructor(Type.EmptyTypes);
		// var result = new object
		if (objConstructor != null)
			iLGenerator.Emit(OpCodes.Newobj, objConstructor);
		iLGenerator.Emit(OpCodes.Stloc_0);
		// var args = new object[parameterCount]
		iLGenerator.Emit(OpCodes.Ldc_I4, parameters.Length);
		iLGenerator.Emit(OpCodes.Newarr, typeof(object));
		iLGenerator.Emit(OpCodes.Stloc_1);

		CopyParametersToArgs(iLGenerator, parameters);

		// call tryInvokeMethod(methodName, args, result) and store the returned value (bool)
		iLGenerator.Emit(OpCodes.Ldarg_0);
		iLGenerator.Emit(OpCodes.Ldstr, name);
		iLGenerator.Emit(OpCodes.Ldloc_1);
		iLGenerator.Emit(OpCodes.Ldloca_S, 0);

		iLGenerator.EmitCall(OpCodes.Call, tryInvokeMemberInfo, null);
		iLGenerator.Emit(OpCodes.Stloc_2);
		iLGenerator.Emit(OpCodes.Ldloc_2);
		var falseLabel = iLGenerator.DefineLabel();
		var continueLabel = iLGenerator.DefineLabel();
		// jump to default value if tryInvokeMethod returned false
		iLGenerator.Emit(OpCodes.Brfalse_S, falseLabel);

		CopyArgsToParameters(iLGenerator, parameters);
		if (methodInfo.ReturnType == typeof(void))
		{
			// Done if no return type of the function
			iLGenerator.MarkLabel(falseLabel);
			iLGenerator.Emit(OpCodes.Ret);
			return;
		}

		// copy result (object) to the return value
		iLGenerator.Emit(OpCodes.Ldloc_0);
		if (methodInfo.ReturnType.IsValueType)
			iLGenerator.Emit(OpCodes.Unbox_Any, methodInfo.ReturnType);
		else if (methodInfo.ReturnType.IsClass || methodInfo.ReturnType.IsInterface)
			iLGenerator.Emit(OpCodes.Castclass, methodInfo.ReturnType);
		iLGenerator.Emit(OpCodes.Stloc_3);
		iLGenerator.Emit(OpCodes.Br_S, continueLabel);
		iLGenerator.MarkLabel(falseLabel);
		// tryInvokeMethod returned false. Let's load default values.
		LoadDefaultValue(iLGenerator, methodInfo.ReturnType);
		if (IsStruct(methodInfo.ReturnType))
		{
			iLGenerator.Emit(OpCodes.Ldloca_S, 4);
			iLGenerator.Emit(OpCodes.Initobj, methodInfo.ReturnType);
			iLGenerator.Emit(OpCodes.Ldloc_S, 4);
		}
		iLGenerator.Emit(OpCodes.Stloc_3);
		iLGenerator.MarkLabel(continueLabel);
		iLGenerator.Emit(OpCodes.Ldloc_3);
		iLGenerator.Emit(OpCodes.Ret);
	}

	/// <summary>
	/// Copies the method's args array back to the out/ref parameters. 
	/// </summary>
	/// <param name="iLGenerator">The IL generator.</param>
	/// <param name="parameters">Parameters.</param>
	static void CopyArgsToParameters(ILGenerator iLGenerator, IReadOnlyList<ParameterInfo> parameters)
	{
		for (var i = 0; i < parameters.Count; i++)
		{
			// foreach args[i] copy back to parameters[i] if it's a reference parameter (or out)
			var p = parameters[i];
			var elementType = p.ParameterType.IsByRef ? p.ParameterType.GetElementType() : p.ParameterType;
			if (elementType == null) continue;
			if (!p.ParameterType.IsByRef) continue;
			iLGenerator.Emit(i < 4 ? OpCodes.Ldarg : OpCodes.Ldarg_S, i + 1);
			iLGenerator.Emit(OpCodes.Ldloc_1);
			iLGenerator.Emit(OpCodes.Ldc_I4, i);
			iLGenerator.Emit(OpCodes.Ldelem_Ref);
			if (elementType.IsGenericParameter || elementType.IsValueType)
				iLGenerator.Emit(OpCodes.Unbox_Any, elementType);
			else if (elementType.IsClass)
				iLGenerator.Emit(OpCodes.Castclass, elementType);

			if (elementType.IsGenericParameter || IsStruct(elementType))
				iLGenerator.Emit(OpCodes.Stobj, elementType);
			else if (storeMap.ContainsKey(elementType))
			{
				iLGenerator.Emit(storeMap[elementType]);
			}
			else
			{
				iLGenerator.Emit(OpCodes.Stind_Ref);
			}
		}
	}

	/// <summary>
	/// Copies the method's parameters to the args array.
	/// </summary>
	/// <param name="iLGenerator">The IL generator.</param>
	/// <param name="parameters">Parameters.</param>
	static void CopyParametersToArgs(ILGenerator iLGenerator, IReadOnlyList<ParameterInfo> parameters)
	{
		for (var i = 0; i < parameters.Count; i++)
		{
			// foreach parameter[i] set it to args[i]
			var p = parameters[i];
			var elementType = p.ParameterType.IsByRef ? p.ParameterType.GetElementType() : p.ParameterType;
			if (elementType == null) continue;

			iLGenerator.Emit(OpCodes.Ldloc_1);
			iLGenerator.Emit(OpCodes.Ldc_I4, i);
			iLGenerator.Emit(i < 4 ? OpCodes.Ldarg : OpCodes.Ldarg_S, i + 1);
			if (p.ParameterType.IsByRef)
			{
				if (elementType.IsGenericParameter || IsStruct(elementType))
					iLGenerator.Emit(OpCodes.Ldobj, elementType);
				else if (loadMap.ContainsKey(elementType))
				{
					var instruction = loadMap[elementType];
					iLGenerator.Emit(instruction);
				}
				else
				{
					var instruction = OpCodes.Ldind_Ref;
					iLGenerator.Emit(instruction);
				}
			}
			if (elementType.IsGenericParameter || elementType.IsValueType)
				iLGenerator.Emit(OpCodes.Box, elementType);
			iLGenerator.Emit(OpCodes.Stelem_Ref);
		}
	}

	/// <summary>
	/// Sets the out parameters to their default values.
	/// </summary>
	/// <param name="iLGenerator">The IL generator.</param>
	/// <param name="parameters">Parameters.</param>
	static void SetOutParameters(ILGenerator iLGenerator, IReadOnlyList<ParameterInfo> parameters)
	{
		for (var i = 0; i < parameters.Count; i++)
		{
			// foreach out variable: set it to 0, null or default
			var p = parameters[i];
			if (!p.IsOut) continue;
			iLGenerator.Emit(i < 4 ? OpCodes.Ldarg : OpCodes.Ldarg_S, i + 1);
			var elementType = p.ParameterType.GetElementType();
			if (elementType == null) continue;

			if (elementType.IsGenericParameter || IsStruct(elementType))
			{
				iLGenerator.Emit(OpCodes.Initobj, elementType);
			}
			else
			{
				LoadDefaultValue(iLGenerator, elementType);
				var instruction = OpCodes.Stind_Ref;
				if (storeMap.ContainsKey(elementType))
					instruction = storeMap[elementType];
				iLGenerator.Emit(instruction);
			}
		}
	}

	/// <summary>
	/// Loads the default value for the given type. It will not do anything for structs.
	/// </summary>
	/// <param name="iLGenerator">The IL generator.</param>
	/// <param name="type">Type.</param>
	static void LoadDefaultValue(ILGenerator iLGenerator, Type type)
	{
		if (type.IsGenericParameter) return;
		if (type.IsValueType)
		{
			if (type == typeof(bool) || type == typeof(byte) || type == typeof(sbyte) || type == typeof(ushort) || type == typeof(short) || type == typeof(uint) || type == typeof(int))
				iLGenerator.Emit(OpCodes.Ldc_I4, 0);
			else if (type == typeof(ulong) || type == typeof(long))
				iLGenerator.Emit(OpCodes.Ldc_I8, 0L);
			else if (type == typeof(float))
				iLGenerator.Emit(OpCodes.Ldc_R4, 0.0f);
			else if (type == typeof(double))
				iLGenerator.Emit(OpCodes.Ldc_R8, 0.0d);
			else if (type == typeof(IntPtr) && Environment.Is64BitProcess)
				iLGenerator.Emit(OpCodes.Ldc_I8, 0L);
			else if (type == typeof(IntPtr))
				iLGenerator.Emit(OpCodes.Ldc_I4, 0);
			else if (type.IsEnum)
				iLGenerator.Emit(OpCodes.Ldc_I4, 0);
		}
		else if (type.IsClass || type.IsInterface)
			iLGenerator.Emit(OpCodes.Ldnull);
	}

	/// <summary>
	/// Generates the methods for the given interface type.
	/// </summary>
	/// <param name="usedMethods">Used methods.</param>
	/// <param name="interfaceType">Interface type.</param>
	/// <param name="typeBuilder">Type builder.</param>
	static void GenerateMethods(ICollection<string> usedMethods, Type interfaceType, TypeBuilder typeBuilder)
	{
		var methods = interfaceType.GetMethods();
		foreach (var method in methods)
		{
			var parameters = method.GetParameters();
			var genericArguments = method.GetGenericArguments();
			var nameWithParams = GetMethodName(method);
			if (usedMethods.Contains(nameWithParams)) continue;
			usedMethods.Add(nameWithParams);
			var methodBuilder = typeBuilder.DefineMethod(method.Name,
				MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Virtual,
				method.ReturnType, (from pi in parameters
					select pi.ParameterType).ToArray());
			if (genericArguments.Any())
				methodBuilder.DefineGenericParameters((from s in genericArguments
					select s.Name).ToArray());
			EmitInvokeMethod(method, methodBuilder);
			typeBuilder.DefineMethodOverride(methodBuilder, method);
		}
	}

	/// <summary>
	/// Gets the name of the method as a C# definition.
	/// </summary>
	/// <returns>The method name.</returns>
	/// <param name="methodInfo">Method info.</param>
	static string GetMethodName(MethodInfo methodInfo)
	{
		return CodeFunction.GetFriendlyName(methodInfo);
	}

	/// <summary>
	/// Creates the constructor base calls.
	/// </summary>
	/// <param name="baseClass">Base class.</param>
	/// <param name="typeBuilder">The type builder.</param>
	static void CreateConstructorBaseCalls(Type baseClass, TypeBuilder typeBuilder)
	{
		var constructors = baseClass.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		foreach (var constructor in constructors)
		{
			var parameters = constructor.GetParameters();
			if (parameters.Length != 0 && parameters.Last().IsDefined(typeof(ParamArrayAttribute), false)) break;
			var parameterTypes = (from p in parameters select p.ParameterType).ToArray();
			var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, constructor.CallingConvention, parameterTypes);
			checked
			{
				for (var i = 0; i < parameters.Length; i++)
				{
					var parameter = parameters[i];
					var parameterBuilder = constructorBuilder.DefineParameter(i + 1, parameter.Attributes, parameter.Name);
					if ((parameter.Attributes & ParameterAttributes.HasDefault) != 0)
						parameterBuilder.SetConstant(parameter.RawDefaultValue);
				}

				var iLGenerator = constructorBuilder.GetILGenerator();
				iLGenerator.Emit(OpCodes.Nop);
				iLGenerator.Emit(OpCodes.Ldarg_0);
				for (var j = 1; j <= parameters.Length; j++) iLGenerator.Emit(OpCodes.Ldarg, j);
				iLGenerator.Emit(OpCodes.Call, constructor);
				iLGenerator.Emit(OpCodes.Ret);
			}
		}
	}

	/// <summary>
	/// Checks if a type is a struct
	/// </summary>
	/// <param name="type">Type.</param>
	/// <returns>True if it is and false otherwise.</returns>
	static bool IsStruct(Type type)
	{
		return type?.IsValueType == true && !type.IsEnum && !type.IsPrimitive;
	}

	/// <summary>
	/// The method handler.
	/// </summary>
	MethodHandler methodHandler;
	/// <summary>
	/// The try invoke member info.
	/// </summary>
	static readonly MethodInfo tryInvokeMemberInfo;
	/// <summary>
	/// The type cache.
	/// </summary>
	static readonly Dictionary<string, Type> cache;
	/// <summary>
	/// The module builder.
	/// </summary>
	static readonly ModuleBuilder moduleBuilder;

	/// <summary>
	/// The store map for the primitive types.
	/// </summary>
	static readonly Dictionary<Type, OpCode> storeMap = new()
	{
		[typeof(bool)] = OpCodes.Stind_I1,
		[typeof(sbyte)] = OpCodes.Stind_I1,
		[typeof(byte)] = OpCodes.Stind_I1,

		[typeof(ushort)] = OpCodes.Stind_I2,
		[typeof(short)] = OpCodes.Stind_I2,

		[typeof(uint)] = OpCodes.Stind_I4,
		[typeof(int)] = OpCodes.Stind_I4,

		[typeof(IntPtr)] = Environment.Is64BitProcess ? OpCodes.Stind_I8 : OpCodes.Stind_I4,
		[typeof(ulong)] = OpCodes.Stind_I8,
		[typeof(long)] = OpCodes.Stind_I8,

		[typeof(float)] = OpCodes.Stind_R4,
		[typeof(double)] = OpCodes.Stind_R8
	};

	/// <summary>
	/// The load map for the primitive types.
	/// </summary>
	static readonly Dictionary<Type, OpCode> loadMap = new()
	{
		[typeof(bool)] = OpCodes.Ldind_I1,
		[typeof(sbyte)] = OpCodes.Ldind_I1,
		[typeof(byte)] = OpCodes.Ldind_I1,

		[typeof(ushort)] = OpCodes.Ldind_I2,
		[typeof(short)] = OpCodes.Ldind_I2,

		[typeof(uint)] = OpCodes.Ldind_I4,
		[typeof(int)] = OpCodes.Ldind_I4,

		[typeof(IntPtr)] = Environment.Is64BitProcess ? OpCodes.Ldind_I8 : OpCodes.Ldind_I4,
		[typeof(ulong)] = OpCodes.Ldind_I8,
		[typeof(long)] = OpCodes.Ldind_I8,

		[typeof(float)] = OpCodes.Ldind_R4,
		[typeof(double)] = OpCodes.Ldind_R8
	};
}

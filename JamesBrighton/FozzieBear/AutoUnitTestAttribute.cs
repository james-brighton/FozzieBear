namespace JamesBrighton.FozzieBear;

/// <summary>Attribute for specifying a class can automatically be unit tested.</summary>
[AttributeUsage(AttributeTargets.Class)]
public class AutoUnitTestAttribute : Attribute
{
	/// <summary>
	///     The parameters.
	/// </summary>
	private readonly List<string> parameters;
	/// <summary>
	///     Initializes a new instance of the <see cref="AutoUnitTestAttribute" /> class.
	/// </summary>
	public AutoUnitTestAttribute()
	{
		parameters = new List<string>();
	}

	/// <summary>
	///     Initializes a new instance of the <see cref="AutoUnitTestAttribute" /> class.
	/// </summary>
	/// <param name="parameters">List of parameters.</param>
	public AutoUnitTestAttribute(params string[] parameters)
	{
		this.parameters = new List<string>(parameters);
	}

	/// <summary>
	///     Gets the parameters.
	/// </summary>
	/// <returns>The parameters.</returns>
	public List<string> GetParameters()
	{
		return parameters;
	}
}

/// <summary>Attribute for specifying which exception a method may throw.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
public class ThrowsExceptionAttribute : Attribute
{
	/// <summary>
	///     The exceptions.
	/// </summary>
	private readonly List<string> exceptions;
	/// <summary>
	///     Initializes a new instance of the <see cref="ThrowsExceptionAttribute" /> class. It adds the
	///     <see cref="Exception" /> class to the list of exceptions.
	/// </summary>
	public ThrowsExceptionAttribute()
	{
		exceptions = new List<string> { "System.Exception" };
	}

	/// <summary>
	///     Initializes a new instance of the <see cref="ThrowsExceptionAttribute" /> class.
	/// </summary>
	/// <param name="exceptions">List of parameters.</param>
	public ThrowsExceptionAttribute(params string[] exceptions)
	{
		this.exceptions = new List<string>(exceptions);
	}

	/// <summary>
	///     Gets the types of the exceptions.
	/// </summary>
	/// <returns>The exceptions.</returns>
	public List<string> GetExceptions()
	{
		return exceptions;
	}
}

/// <summary>Attribute for specifying which returns a method may have.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
public class ReturnAttribute : Attribute
{
	/// <summary>
	///     The returns.
	/// </summary>
	private readonly List<string> returns;
	/// <summary>
	///     Initializes a new instance of the <see cref="ReturnAttribute" /> class.
	/// </summary>
	public ReturnAttribute()
	{
		returns = new List<string>();
	}

	/// <summary>
	///     Initializes a new instance of the <see cref="ReturnAttribute" /> class.
	/// </summary>
	/// <param name="returns">List of return statements.</param>
	public ReturnAttribute(params string[] returns)
	{
		this.returns = new List<string>(returns);
	}

	/// <summary>
	///     Gets the types of the exceptions.
	/// </summary>
	/// <returns>The exceptions.</returns>
	public IEnumerable<string> GetReturns()
	{
		return returns;
	}
}

/// <summary>Attribute for specifying which returns a method may have. This attribute must be set a class level</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ReturnForAttribute : Attribute
{
	/// <summary>
	///     The class name
	/// </summary>
	private readonly string methodName;
	/// <summary>
	///     The returns.
	/// </summary>
	private readonly List<string> returns;
	/// <summary>
	///     Initializes a new instance of the <see cref="ReturnForAttribute" /> class.
	/// </summary>
	/// <param name="methodName">Name of the method</param>
	public ReturnForAttribute(string methodName)
	{
		this.methodName = methodName;
		returns = new List<string>();
	}

	/// <summary>
	///     Initializes a new instance of the <see cref="ReturnForAttribute" /> class.
	/// </summary>
	/// <param name="methodName">Name of the method</param>
	/// <param name="returns">List of return statements.</param>
	public ReturnForAttribute(string methodName, params string[] returns)
	{
		this.methodName = methodName;
		this.returns = new List<string>(returns);
	}

	/// <summary>
	///     Gets the method name
	/// </summary>
	/// <returns>The method name</returns>
	public string GetMethodName()
	{
		return methodName;
	}

	/// <summary>
	///     Gets the types of the exceptions.
	/// </summary>
	/// <returns>The exceptions.</returns>
	public IEnumerable<string> GetReturns()
	{
		return returns;
	}
}

/// <summary>Attribute for marking the class, method or property as not applicable for an automatic unit test.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
public class SkipAttribute : Attribute
{
	/// <summary>
	///     The parameters.
	/// </summary>
	private readonly List<string> parameters;
	/// <summary>
	///     Initializes a new instance of the <see cref="SkipAttribute" /> class.
	/// </summary>
	public SkipAttribute()
	{
		parameters = new List<string>();
	}

	/// <summary>
	///     Initializes a new instance of the <see cref="SkipAttribute" /> class.
	/// </summary>
	/// <param name="parameters">List of parameters.</param>
	public SkipAttribute(params string[] parameters)
	{
		this.parameters = new List<string>(parameters);
	}

	/// <summary>
	///     Gets the parameters.
	/// </summary>
	/// <returns>The parameters.</returns>
	public List<string> GetParameters()
	{
		return parameters;
	}
}

/// <summary>Attribute for marking the method or property with a given set of parameters.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
public class InvokeAttribute : Attribute
{
	/// <summary>
	///     The parameters.
	/// </summary>
	private readonly List<string> parameters;
	/// <summary>
	///     Initializes a new instance of the <see cref="InvokeAttribute" /> class.
	/// </summary>
	public InvokeAttribute()
	{
		parameters = new List<string>();
	}

	/// <summary>
	///     Initializes a new instance of the <see cref="InvokeAttribute" /> class.
	/// </summary>
	/// <param name="parameters">List of parameters.</param>
	public InvokeAttribute(params string[] parameters)
	{
		this.parameters = new List<string>(parameters);
	}

	/// <summary>
	///     Gets the parameters.
	/// </summary>
	/// <returns>The parameters.</returns>
	public List<string> GetParameters()
	{
		return parameters;
	}
}

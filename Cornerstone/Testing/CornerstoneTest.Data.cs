#region References

using System;
using Cornerstone.Extensions;
using Cornerstone.Generators;
using Cornerstone.Generators.CodeGenerators;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Testing;

public abstract partial class CornerstoneTest
{
	#region Methods

	/// <summary>
	/// Get a test dispatcher.
	/// </summary>
	/// <returns> The dispatcher for testing. </returns>
	public IDispatcher GetDispatcher()
	{
		return new TestDispatcher();
	}

	/// <summary>
	/// Get a test runtime information.
	/// </summary>
	/// <param name="update"> An optional update. </param>
	/// <returns> The runtime information. </returns>
	public IRuntimeInformation GetRuntimeInformation(Action<IRuntimeInformation> update = null)
	{
		var response = RuntimeInformation.Copy();
		update?.Invoke(response);
		return response;
	}

	/// <summary>
	/// Generate asserts for an object.
	/// </summary>
	/// <typeparam name="T"> The type of the object. </typeparam>
	/// <param name="instance"> The object instance. </param>
	protected void GenerateAsserts<T>(T instance)
	{
		var properties = instance.GetCachedProperties();
		var builder = new TextBuilder();

		foreach (var property in properties)
		{
			var expected = CSharpCodeWriter.GenerateCode(property.GetValue(instance));
			builder.AppendLine($"AreEqual({expected}, actual.{property.Name});");
		}

		var code = builder.ToString();
		code.Dump();

		CopyToClipboard(code);
	}
	
	/// <summary>
	/// Generate asserts for an object.
	/// </summary>
	/// <typeparam name="T"> The type of the object. </typeparam>
	protected void GenerateSampleCode<T>()
	{
		var instance = Activator.CreateInstance<T>();
		var properties = instance.GetCachedProperties();

		foreach (var property in properties)
		{
			property.SetValue(instance, ValueGenerator.GenerateValue(property.PropertyType));
		}

		var code = CSharpCodeWriter.GenerateCode(instance);
		code.Dump();

		CopyToClipboard(code);
	}

	#endregion
}
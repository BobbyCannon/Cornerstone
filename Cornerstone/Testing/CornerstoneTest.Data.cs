#region References

using System.Linq;
using Cornerstone.Data;
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
	public virtual IDispatcher GetDispatcher()
	{
		return new TestDispatcher();
	}

	/// <summary>
	/// Get a test runtime information.
	/// </summary>
	/// <returns> The runtime information. </returns>
	public virtual IRuntimeInformation GetRuntimeInformation()
	{
		var response = RuntimeInformationData.GetSample();
		return response;
	}

	/// <summary>
	/// Generate asserts for an object.
	/// </summary>
	/// <typeparam name="T"> The type of the object. </typeparam>
	/// <param name="instance"> The object instance. </param>
	/// <param name="settings"> Optional include / exclude settings. </param>
	protected void GenerateAsserts<T>(T instance, IncludeExcludeSettings settings = null)
	{
		settings ??= IncludeExcludeSettings.Empty;
		var builder = new TextBuilder();
		var properties = instance
			.GetCachedProperties()
			.Where(x => !x.IsIndexer());
		var codeSettings = new CodeWriterSettings
		{
			TextFormat = TextFormat.Spaced,
			OutputMode = CodeWriterMode.Instance
		};

		foreach (var property in properties)
		{
			if (!settings.ShouldProcessProperty(property.Name))
			{
				continue;
			}

			var expected = CSharpCodeWriter.GenerateCode(property.GetValue(instance), codeSettings);
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
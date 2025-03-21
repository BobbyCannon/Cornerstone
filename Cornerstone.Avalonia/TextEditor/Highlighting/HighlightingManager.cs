#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;
using Cornerstone.Avalonia.TextEditor.Highlighting.Resources;
using Cornerstone.Avalonia.TextEditor.Highlighting.Xshd;
using Cornerstone.Avalonia.TextEditor.Utils;
using Cornerstone.Collections;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Highlighting;

/// <summary>
/// Manages a list of syntax highlighting definitions.
/// </summary>
/// <remarks>
/// All members on this class (including instance members) are thread-safe.
/// </remarks>
public class HighlightingManager : IHighlightingDefinitionReferenceResolver
{
	#region Fields

	private readonly ISpeedyList<TextEditorControl> _editors;
	private readonly Dictionary<string, IHighlightingDefinition> _highlightingsByExtension;
	private readonly Dictionary<string, IHighlightingDefinition> _highlightingsByName;
	private readonly object _lockObj;

	#endregion

	#region Constructors

	protected HighlightingManager()
	{
		_editors = new SpeedyList<TextEditorControl>();
		_highlightingsByExtension = new(StringComparer.OrdinalIgnoreCase);
		_highlightingsByName = new(StringComparer.OrdinalIgnoreCase);
		_lockObj = new();
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets a copy of all highlightings.
	/// </summary>
	public IReadOnlySet<IHighlightingDefinition> HighlightingDefinitions
	{
		get
		{
			lock (_lockObj)
			{
				return _highlightingsByName.Values.ToReadOnlySet();
			}
		}
	}

	/// <summary>
	/// Gets the default HighlightingManager instance.
	/// The default HighlightingManager comes with built-in highlightings.
	/// </summary>
	public static HighlightingManager Instance => DefaultHighlightingManager.Instance;

	#endregion

	#region Methods

	/// <summary>
	/// Gets a highlighting definition by name.
	/// Returns null if the definition is not found.
	/// </summary>
	public IHighlightingDefinition GetDefinition(string name)
	{
		lock (_lockObj)
		{
			return _highlightingsByName.GetValueOrDefault(name);
		}
	}

	/// <summary>
	/// Gets a highlighting definition by extension.
	/// Returns null if the definition is not found.
	/// </summary>
	public IHighlightingDefinition GetDefinitionByExtension(string extension)
	{
		lock (_lockObj)
		{
			return _highlightingsByExtension.GetValueOrDefault(extension);
		}
	}

	public void RegisterForUpdates(TextEditorControl editor)
	{
		_editors.Add(editor);
	}

	/// <summary>
	/// Registers a highlighting definition.
	/// </summary>
	/// <param name="name"> The name to register the definition with. </param>
	/// <param name="extensions"> The file extensions to register the definition for. </param>
	/// <param name="highlighting"> The highlighting definition. </param>
	public void RegisterHighlighting(string name, string[] extensions, IHighlightingDefinition highlighting)
	{
		if (highlighting == null)
		{
			throw new ArgumentNullException(nameof(highlighting));
		}

		lock (_lockObj)
		{
			_highlightingsByName.AddOrUpdate(name, highlighting);

			if (extensions != null)
			{
				foreach (var ext in extensions)
				{
					_highlightingsByExtension.AddOrUpdate(ext, highlighting);
				}
			}

			OnRegisteredHighlighting(name, highlighting);
		}
	}

	/// <summary>
	/// Registers a highlighting definition.
	/// </summary>
	/// <param name="name"> The name to register the definition with. </param>
	/// <param name="extensions"> The file extensions to register the definition for. </param>
	/// <param name="resourceName"> The name of the resource. </param>
	/// <param name="xmlDefinition"> The XML of the highlighting definition. </param>
	public void RegisterHighlighting(string name, string[] extensions, string resourceName, string xmlDefinition)
	{
		XshdSyntaxDefinition xshd;
		using (var s = new StringReader(xmlDefinition))
		using (var reader = XmlReader.Create(s))
		{
			// in release builds, skip validating the built-in highlightings
			xshd = HighlightingLoader.LoadXshd(reader);
		}
		var definition = HighlightingLoader.Load(resourceName, xshd, this);
		RegisterHighlighting(name, extensions, definition);
	}

	/// <summary>
	/// Registers a highlighting definition.
	/// </summary>
	/// <param name="name"> The name to register the definition with. </param>
	/// <param name="extensions"> The file extensions to register the definition for. </param>
	/// <param name="resourceName"> The name of the resource. </param>
	/// <param name="lazyLoadedHighlighting"> A function that loads the highlighting definition. </param>
	public void RegisterHighlighting(string name, string[] extensions, string resourceName, Func<IHighlightingDefinition> lazyLoadedHighlighting)
	{
		if (lazyLoadedHighlighting == null)
		{
			throw new ArgumentNullException(nameof(lazyLoadedHighlighting));
		}
		RegisterHighlighting(name, extensions, new DelayLoadedHighlightingDefinition(name, extensions, resourceName, lazyLoadedHighlighting));
	}

	public void UnregisterForUpdates(TextEditorControl editor)
	{
		_editors.Remove(editor);
	}

	private void OnRegisteredHighlighting(string name, IHighlightingDefinition highlighting)
	{
		foreach (var editor in _editors)
		{
			if (editor.SyntaxHighlighting?.Name == name)
			{
				editor.SyntaxHighlighting = highlighting;
			}
		}
	}

	#endregion

	#region Classes

	internal sealed class DefaultHighlightingManager : HighlightingManager
	{
		#region Constructors

		public DefaultHighlightingManager()
		{
			ResourceLoader.RegisterBuiltInHighlightings(this);
		}

		#endregion

		#region Properties

		// ReSharper disable once MemberHidesStaticFromOuterClass
		public new static DefaultHighlightingManager Instance { get; } = new();

		#endregion

		#region Methods

		// Registering a built-in highlighting
		internal void RegisterHighlighting(string name, string[] extensions, string resourceName)
		{
			try
			{
				RegisterHighlighting(name, extensions, resourceName, LoadHighlighting(resourceName));
			}
			catch (HighlightingDefinitionInvalidException ex)
			{
				throw new InvalidOperationException("The built-in highlighting '" + name + "' is invalid.", ex);
			}
		}

		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode",
			Justification = "LoadHighlighting is used only in release builds")]
		private Func<IHighlightingDefinition> LoadHighlighting(string resourceName)
		{
			IHighlightingDefinition loadHighlighting()
			{
				XshdSyntaxDefinition xshd;
				using (var s = ResourceLoader.OpenStream(resourceName))
				using (var reader = XmlReader.Create(s))
				{
					// in release builds, skip validating the built-in highlightings
					xshd = HighlightingLoader.LoadXshd(reader);
				}
				return HighlightingLoader.Load(resourceName, xshd, this);
			}

			return loadHighlighting;
		}

		#endregion
	}

	private sealed class DelayLoadedHighlightingDefinition : IHighlightingDefinition
	{
		#region Fields

		private IHighlightingDefinition _definition;
		private readonly string[] _extensions;
		private Func<IHighlightingDefinition> _lazyLoadingFunction;
		private readonly object _lockObj = new();
		private readonly string _name;
		private Exception _storedException;

		#endregion

		#region Constructors

		public DelayLoadedHighlightingDefinition(string name, string[] extensions, string resourceName, Func<IHighlightingDefinition> lazyLoadingFunction)
		{
			_name = name;
			_extensions = extensions;
			_lazyLoadingFunction = lazyLoadingFunction;

			ResourceName = resourceName;
		}

		#endregion

		#region Properties

		/// <inheritdoc />
		public string[] Extensions => _extensions ?? GetDefinition().Extensions;

		public HighlightingRuleSet MainRuleSet => GetDefinition().MainRuleSet;

		public string Name => _name ?? GetDefinition().Name;

		public IEnumerable<HighlightingColor> NamedHighlightingColors => GetDefinition().NamedHighlightingColors;

		public IDictionary<string, string> Properties => GetDefinition().Properties;

		/// <inheritdoc />
		public string ResourceName { get; }

		#endregion

		#region Methods

		public HighlightingColor GetNamedColor(string name)
		{
			return GetDefinition().GetNamedColor(name);
		}

		public HighlightingRuleSet GetNamedRuleSet(string name)
		{
			return GetDefinition().GetNamedRuleSet(name);
		}

		public override string ToString()
		{
			return Name;
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
			Justification = "The exception will be rethrown")]
		private IHighlightingDefinition GetDefinition()
		{
			Func<IHighlightingDefinition> func;
			lock (_lockObj)
			{
				if (_definition != null)
				{
					return _definition;
				}
				func = _lazyLoadingFunction;
			}
			Exception exception = null;
			IHighlightingDefinition def = null;
			try
			{
				using (var busyLock = BusyManager.Enter(this))
				{
					if (!busyLock.Success)
					{
						throw new InvalidOperationException("Tried to create delay-loaded highlighting definition recursively. Make sure the are no cyclic references between the highlighting definitions.");
					}
					def = func();
				}
				if (def == null)
				{
					throw new InvalidOperationException("Function for delay-loading highlighting definition returned null");
				}
			}
			catch (Exception ex)
			{
				exception = ex;
			}
			lock (_lockObj)
			{
				_lazyLoadingFunction = null;
				if ((_definition == null) && (_storedException == null))
				{
					_definition = def;
					_storedException = exception;
				}
				if (_storedException != null)
				{
					throw new HighlightingDefinitionInvalidException("Error delay-loading highlighting definition", _storedException);
				}
				return _definition;
			}
		}

		#endregion
	}

	#endregion
}
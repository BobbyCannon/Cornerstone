#region References

using System;
using System.Collections.Generic;
using System.Text;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Text;

/// <summary>
/// Builds text with Unicode box-drawing tree connectors.
/// </summary>
[SourceReflection]
public class AsciiWriter
{
	#region Constants

	public const string TreeBranch = "├── ";
	public const string TreeContinue = "│   ";
	public const string TreeGap = "    ";
	public const string TreeLastBranch = "└── ";

	#endregion

	#region Fields

	private readonly StringBuilder _builder;

	#endregion

	#region Constructors

	public AsciiWriter()
		: this(new StringBuilder())
	{
	}

	public AsciiWriter(int capacity)
		: this(new StringBuilder(capacity))
	{
	}

	public AsciiWriter(StringBuilder builder)
	{
		_builder = builder ?? throw new ArgumentNullException(nameof(builder));
	}

	#endregion

	#region Methods

	public AsciiWriter Append(string value)
	{
		_builder.Append(value);
		return this;
	}

	public AsciiWriter AppendLine()
	{
		_builder.AppendLine();
		return this;
	}

	public AsciiWriter AppendLine(string value)
	{
		_builder.AppendLine(value);
		return this;
	}

	public AsciiWriter AppendTree<T>(T node, Func<T, IReadOnlyList<T>> children, Func<T, T, string> format)
	{
		return AppendTree(node, children, format, include: null);
	}

	public AsciiWriter AppendTree<T>(T node, Func<T, IReadOnlyList<T>> children, Func<T, T, string> format, Func<T, bool> include)
	{
		AppendTreeNode(node, default, children, format, include, string.Empty, true, isRoot: true);
		return this;
	}

	public AsciiWriter AppendTreeNode(string prefix, bool isLast, string text)
	{
		_builder.Append(prefix);
		_builder.Append(isLast ? TreeLastBranch : TreeBranch);
		_builder.Append(text);
		_builder.AppendLine();
		return this;
	}

	public static string TreeChildPrefix(string prefix, bool isLast)
	{
		return prefix + (isLast ? TreeGap : TreeContinue);
	}

	public override string ToString()
	{
		return _builder.ToString();
	}

	private void AppendTreeNode<T>(
		T node,
		T parent,
		Func<T, IReadOnlyList<T>> children,
		Func<T, T, string> format,
		Func<T, bool> include,
		string prefix,
		bool isLast,
		bool isRoot)
	{
		if ((include != null) && !include(node))
		{
			return;
		}

		var line = format(node, parent);
		if (isRoot)
		{
			_builder.AppendLine(line);
		}
		else
		{
			AppendTreeNode(prefix, isLast, line);
		}

		var list = children(node);
		if ((list == null) || (list.Count == 0))
		{
			return;
		}

		var lastKept = -1;
		for (var i = 0; i < list.Count; i++)
		{
			if ((include == null) || include(list[i]))
			{
				lastKept = i;
			}
		}

		if (lastKept < 0)
		{
			return;
		}

		var childPrefix = isRoot ? string.Empty : TreeChildPrefix(prefix, isLast);
		for (var i = 0; i < list.Count; i++)
		{
			if ((include != null) && !include(list[i]))
			{
				continue;
			}

			AppendTreeNode(list[i], node, children, format, include, childPrefix, i == lastKept, isRoot: false);
		}
	}

	#endregion
}
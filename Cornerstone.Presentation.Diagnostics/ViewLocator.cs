#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Text;

#endregion

#pragma warning disable IL2057

namespace Cornerstone.Presentation.Diagnostics;

public class ViewLocator : IDataTemplate
{
	#region Methods

	public Control Build(object data)
	{
		if (data is null)
		{
			return null;
		}

		var name = data.GetType().FullName!.Replace("ViewModel", "View");
		var type = Type.GetType(name);

		if (type != null)
		{
			return (Control) SourceReflector.CreateInstance(type)!;
		}
		return new TextBlock { Text = name };
	}

	public bool Match(object data)
	{
		return data is ViewModel;
	}

	#endregion
}
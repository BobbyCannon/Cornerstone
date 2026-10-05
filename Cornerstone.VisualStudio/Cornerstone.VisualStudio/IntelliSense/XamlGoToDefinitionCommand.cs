#region References

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cornerstone.VisualStudio.Core.Completion;
using Cornerstone.VisualStudio.Models;
using Cornerstone.VisualStudio.Protocol;
using Cornerstone.VisualStudio.Services;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Serilog;
using Constants = Microsoft.VisualStudio.OLE.Interop.Constants;

#endregion

namespace Cornerstone.VisualStudio.IntelliSense;

internal static class XamlGoToDefinitionCommand
{
	#region Fields

	private static readonly object InFlight;
	private static readonly object HandingOff;

	#endregion

	#region Constructors

	static XamlGoToDefinitionCommand()
	{
		InFlight = new object();
		HandingOff = new object();
	}

	#endregion

	#region Methods

	public static int Exec(ref Guid group, uint commandId, ITextView textView, CompletionEngine engine)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		if (!IsCommand(group, commandId))
		{
			return (int) Constants.OLECMDERR_E_NOTSUPPORTED;
		}

		Execute(textView, engine, null);
		return VSConstants.S_OK;
	}

	public static bool IsHandingOff(ITextView textView)
	{
		return (textView != null) && textView.Properties.ContainsProperty(HandingOff);
	}

	public static void Execute(ITextView textView, CompletionEngine engine, Action visualStudioFallback)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		if (textView?.TextBuffer == null)
		{
			return;
		}

		// The document pane and the text-view filter can both see F12.
		if (textView.Properties.ContainsProperty(InFlight))
		{
			return;
		}

		textView.Properties.AddProperty(InFlight, null);

		textView.TextBuffer.Properties.TryGetProperty(typeof(XamlBufferMetadata), out XamlBufferMetadata metadata);
		textView.TextBuffer.Properties.TryGetProperty("AssemblyName", out string assemblyName);

		var snapshot = textView.TextSnapshot;
		var caret = textView.Caret.Position.BufferPosition.Position;
		caret = Math.Max(0, Math.Min(caret, snapshot.Length));
		var xml = snapshot.GetText();

		engine ??= new CompletionEngine();
		var paths = metadata?.AssemblyPaths;
		List<string> pathCopy = null;
		if ((paths != null) && (paths.Count > 0))
		{
			pathCopy = Copy(paths);
		}

		ResolveAndNavigateAsync(textView, engine, metadata, xml, caret, assemblyName, pathCopy, visualStudioFallback).FireAndForget();
	}

	private static void ClearInFlight(ITextView textView)
	{
		try
		{
			if ((textView == null) || textView.IsClosed)
			{
				return;
			}

			if (textView.Properties.ContainsProperty(InFlight))
			{
				textView.Properties.RemoveProperty(InFlight);
			}
		}
		catch (Exception)
		{
			// The view can close while the host call is still running.
		}
	}

	private static async Task ResolveAndNavigateAsync(
		ITextView textView,
		CompletionEngine engine,
		XamlBufferMetadata metadata,
		string xml,
		int caret,
		string assemblyName,
		List<string> paths,
		Action visualStudioFallback)
	{
		var completed = false;
		try
		{
			completed = await ResolveAndNavigateCoreAsync(
				textView,
				engine,
				metadata,
				xml,
				caret,
				assemblyName,
				paths).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			Log.Logger.Debug(ex, "CXAML Go To Definition did not complete");
		}
		finally
		{
			ClearInFlight(textView);
		}

		if (!completed)
		{
			HandOff(textView, visualStudioFallback);
		}
	}

	private static async Task<bool> ResolveAndNavigateCoreAsync(
		ITextView textView,
		CompletionEngine engine,
		XamlBufferMetadata metadata,
		string xml,
		int caret,
		string assemblyName,
		List<string> paths)
	{
		XamlGoToDefinitionTarget target;
		var loaded = metadata?.CompletionMetadata;
		if (loaded != null)
		{
			// Metadata is already in this process. A name lookup does not need the editor host.
			target = XamlGoToDefinitionResolver.Resolve(engine, loaded, xml, caret, assemblyName);
		}
		else if ((paths != null) && (paths.Count > 0))
		{
			try
			{
				var response = await EditorHostSession.GoToDefinitionAsync(xml, caret, assemblyName, paths).ConfigureAwait(false);
				if (!string.IsNullOrEmpty(response.Error))
				{
					Log.Logger.Debug("CXAML Go To Definition failed on the editor host: {Error}", response.Error);
					return false;
				}

				target = FromResponse(response);
			}
			catch (Exception ex)
			{
				Log.Logger.Debug(ex, "CXAML Go To Definition failed on the editor host");
				return false;
			}
		}
		else
		{
			target = XamlGoToDefinitionResolver.Resolve(engine, null, xml, caret, assemblyName);
		}

		Log.Logger.Debug(
			"CXAML Go To Definition: kind={Kind} type={Type} member={Member} class={Class} method={Method} offset={Offset} caret={Caret}",
			target.Kind, target.TypeFullName, target.MemberName, target.ClassName, target.MethodName, target.DocumentOffset, caret);

		if (target.Kind == XamlGoToDefinitionKind.None)
		{
			return false;
		}

		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
		if (textView.IsClosed)
		{
			return false;
		}

		return await XamlGoToDefinitionNavigator.NavigateAsync(target, textView).ConfigureAwait(true);
	}

	private static void HandOff(ITextView textView, Action visualStudioFallback)
	{
		if ((visualStudioFallback == null) || (textView == null) || textView.IsClosed)
		{
			return;
		}

		ThreadHelper.JoinableTaskFactory.Run(async () =>
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			if (textView.IsClosed)
			{
				return;
			}

			textView.Properties.AddProperty(HandingOff, null);
			try
			{
				visualStudioFallback();
			}
			catch (Exception ex)
			{
				Log.Logger.Debug(ex, "Visual Studio Go To Definition hand-off failed");
			}
			finally
			{
				if (textView.Properties.ContainsProperty(HandingOff))
				{
					textView.Properties.RemoveProperty(HandingOff);
				}
			}
		});
	}

	private static XamlGoToDefinitionTarget FromResponse(GoToDefinitionResponseMessage response)
	{
		var kind = (XamlGoToDefinitionKind) response.Kind;
		if (kind == XamlGoToDefinitionKind.Type)
		{
			return XamlGoToDefinitionTarget.ForType(response.TypeFullName);
		}

		if (kind == XamlGoToDefinitionKind.Member)
		{
			return XamlGoToDefinitionTarget.ForMember(response.TypeFullName, response.MemberName);
		}

		if (kind == XamlGoToDefinitionKind.ClassName)
		{
			return XamlGoToDefinitionTarget.ForClass(response.ClassName);
		}

		if (kind == XamlGoToDefinitionKind.MethodName)
		{
			return XamlGoToDefinitionTarget.ForMethod(response.MethodName, response.ClassName);
		}

		if (kind == XamlGoToDefinitionKind.StyleClass)
		{
			return XamlGoToDefinitionTarget.ForStyleClass(response.ClassName, response.DocumentOffset);
		}

		return XamlGoToDefinitionTarget.None;
	}

	private static List<string> Copy(System.Collections.Generic.IReadOnlyList<string> paths)
	{
		var copy = new List<string>();
		foreach (var path in paths)
		{
			copy.Add(path);
		}

		return copy;
	}

	public static CompletionEngine GetEngine()
	{
		var componentModel = Package.GetGlobalService(typeof(SComponentModel)) as IComponentModel;
		return componentModel?.GetService<CompletionEngineSource>()?.CompletionEngine ?? new CompletionEngine();
	}

	public static bool IsCommand(Guid group, uint commandId)
	{
		if (group == VSConstants.GUID_VSStandardCommandSet97)
		{
			return (commandId == (uint) VSConstants.VSStd97CmdID.GotoDefn) ||
				(commandId == (uint) VSConstants.VSStd97CmdID.GotoDecl);
		}

		if (group == VSConstants.VsStd12)
		{
			return commandId == (uint) VSConstants.VSStd12CmdID.PeekDefinition;
		}

		return false;
	}

	public static int QueryStatus(ref Guid group, uint count, OLECMD[] commands)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		if ((commands == null) || (count == 0))
		{
			return (int) Constants.OLECMDERR_E_NOTSUPPORTED;
		}

		var handled = false;
		for (var i = 0; i < count; i++)
		{
			if (IsCommand(group, commands[i].cmdID))
			{
				commands[i].cmdf = (uint) (OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
				handled = true;
			}
		}

		return handled ? VSConstants.S_OK : (int) Constants.OLECMDERR_E_NOTSUPPORTED;
	}

	#endregion
}

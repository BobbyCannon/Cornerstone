#region References

using System;
using System.Collections;
using System.Collections.Generic;
using Cornerstone.Testing;
using Microsoft.Build.Framework;

#endregion

namespace Cornerstone.Presentation.UnitTests.BuildTasks;

/// <summary>
/// This is fake BuildEngine using for testing build task
/// at moment it manages only <see cref="BuildErrorEventArgs" /> and <see cref="BuildWarningEventArgs" />
/// other messages are ignored/>
/// </summary>
internal class UnitTestBuildEngine : IBuildEngine, IDisposable
{
	#region Fields

	private readonly bool _assertOnDispose;
	private readonly List<UnitTestBuildEngineMessage> _errors = new();
	private readonly bool _treatWarningAsError;

	#endregion

	#region Constructors

	private UnitTestBuildEngine(bool continueOnError,
		bool treatWarningAsError,
		bool assertOnDispose)
	{
		ContinueOnError = continueOnError;
		_treatWarningAsError = treatWarningAsError;
		_assertOnDispose = assertOnDispose;
	}

	#endregion

	#region Properties

	public int ColumnNumberOfTaskNode => 0;

	public bool ContinueOnError { get; }

	public IReadOnlyList<UnitTestBuildEngineMessage> Errors => _errors;

	public int LineNumberOfTaskNode => 0;

	public string ProjectFileOfTaskNode => string.Empty;

	#endregion

	#region Methods

	public bool BuildProjectFile(string projectFileName,
		string[] targetNames,
		IDictionary globalProperties,
		IDictionary targetOutputs)
	{
		throw new NotImplementedException();
	}

	public void Dispose()
	{
		if (_assertOnDispose && (_errors.Count > 0))
		{
			CornerstoneTest.Fail("There is one o more errors.");
		}
	}

	public void LogCustomEvent(CustomBuildEventArgs e)
	{
	}

	public void LogErrorEvent(BuildErrorEventArgs e)
	{
		var message = UnitTestBuildEngineMessage.From(e);
		_errors.Add(message);
		if (!ContinueOnError)
		{
			CornerstoneTest.Fail(message.Message);
		}
	}

	public void LogMessageEvent(BuildMessageEventArgs e)
	{
	}

	public void LogWarningEvent(BuildWarningEventArgs e)
	{
		if (_treatWarningAsError)
		{
			var message = UnitTestBuildEngineMessage.From(e);
			_errors.Add(message);
			if (!ContinueOnError)
			{
				CornerstoneTest.Fail(message.Message);
			}
		}
	}

	/// <summary>
	/// Start new instance of <see cref="UnitTestBuildEngine" />
	/// </summary>
	/// <param name="continueOnError"> if it is <c> false </c> immediately assert error </param>
	/// <param name="treatWarningAsError"> if it is <c> true </c> treat warning as error </param>
	/// <param name="assertOnDispose"> if it is <c> true </c> assert on dispose if there are any errors. </param>
	/// <returns> </returns>
	public static UnitTestBuildEngine Start(bool continueOnError = false,
		bool treatWarningAsError = false,
		bool assertOnDispose = false)
	{
		return new(continueOnError, treatWarningAsError, assertOnDispose);
	}

	#endregion
}
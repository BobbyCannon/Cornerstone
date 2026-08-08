#region References

using System;
using System.Diagnostics;
using Cornerstone.Text;

#endregion

namespace Cornerstone.FileSystem.Sync;

public class SyncLog : TextWriter
{
	#region Constructors

	public SyncLog()
		: base(new StringBuffer(), new TextSettings())
	{
	}

	public SyncLog(IStringBuffer buffer)
		: base(buffer, new TextSettings())
	{
	}

	#endregion

	#region Methods

	public void AddFinalReportEntry(string title, string elapsedTime)
	{
		Append(elapsedTime);
		Append(": ");
		AppendLine(title);
	}

	public IDisposable MeasureTime(string operationTitle)
	{
		return new Disposable(this, operationTitle);
	}

	#endregion

	#region Classes

	private class Disposable : IDisposable
	{
		#region Fields

		private readonly SyncLog _log;
		private readonly Stopwatch _stopwatch;
		private readonly string _title;

		#endregion

		#region Constructors

		public Disposable(SyncLog log, string operationTitle)
		{
			_stopwatch = Stopwatch.StartNew();
			_log = log;
			_title = operationTitle;
		}

		#endregion

		#region Methods

		public void Dispose()
		{
			_log.AddFinalReportEntry(_title, _stopwatch.Elapsed.ToString(@"hh\:mm\:ss\.fffffff"));
		}

		#endregion
	}

	#endregion
}
#region References

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

#endregion

namespace Cornerstone.Platforms.Windows;

/// <summary>
/// Application Instance Manager
/// </summary>
public static class ApplicationInstanceManager
{
	#region Methods

	/// <summary>
	/// Creates the single instance.
	/// </summary>
	/// <param name="name"> The name. </param>
	/// <param name="args"> The arguments. </param>
	/// <param name="callback"> The callback. </param>
	/// <returns> </returns>
	public static void CreateInstance(string name, string[] args, EventHandler<InstanceCallbackEventArgs> callback)
	{
		using (var process = Process.GetCurrentProcess())
		{
			var eventName = $"{name}-{process.Id}";
			InstanceProxy.CommandLineArgs = args;

			// register wait handle for this instance (process)
			var eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);
			ThreadPool.RegisterWaitForSingleObject(eventWaitHandle, WaitOrTimerCallback, callback, Timeout.Infinite, false);
			eventWaitHandle.Close();

			// register shared type (used to pass data between processes)
			RegisterRemoteType(eventName);
		}
	}

	/// <summary>
	/// Creates the single instance.
	/// </summary>
	/// <param name="name"> The name. </param>
	/// <param name="args"> The arguments. </param>
	/// <param name="callback"> The callback. </param>
	/// <returns> </returns>
	public static void NotifyOrCreateInstance(string name, string[] args, EventHandler<InstanceCallbackEventArgs> callback)
	{
		InstanceProxy.CommandLineArgs = args;

		using (var currentProcess = Process.GetCurrentProcess())
		{
			var process = ProcessService.Where(name).FirstOrDefault(x => (x.Id != currentProcess.Id) && (x.Arguments.IndexOf("isolated") < 0));

			if (process != null)
			{
				var eventName = $"{name}-{process.Id}";

				if (EventWaitHandle.TryOpenExisting(eventName, out var eventWaitHandle))
				{
					// pass console arguments to shared object
					if (UpdateRemoteObject(eventName))
					{
						// invoke (signal) wait handle on other process
						eventWaitHandle?.Set();

						// kill current process
						Environment.Exit(0);
					}
				}
			}

			CreateInstance(name, args, callback);
		}
	}

	/// <summary>
	/// Registers the remote type.
	/// </summary>
	/// <param name="uri"> The URI. </param>
	private static void RegisterRemoteType(string uri)
	{
		// register remote channel (net-pipes)
		//var serverChannel = new IpcServerChannel(Environment.MachineName + uri);
		//ChannelServices.RegisterChannel(serverChannel, true);

		//// register shared type
		//RemotingConfiguration.RegisterWellKnownServiceType(typeof(InstanceProxy), uri, WellKnownObjectMode.Singleton);

		//// close channel, on process exit
		//using (var process = Process.GetCurrentProcess())
		//{
		//	process.Exited += delegate { ChannelServices.UnregisterChannel(serverChannel); };
		//}
	}

	/// <summary>
	/// Updates the remote object.
	/// </summary>
	/// <param name="uri"> The remote URI. </param>
	private static bool UpdateRemoteObject(string uri)
	{
		try
		{
			//// register net-pipe channel
			//var clientChannel = new IpcClientChannel();
			//ChannelServices.RegisterChannel(clientChannel, true);

			//// get shared object from other process
			//var proxy = Activator.GetObject(typeof(InstanceProxy), string.Format("ipc://{0}{1}/{1}", Environment.MachineName, uri)) as InstanceProxy;

			//// pass current command line args to proxy
			//proxy?.SetCommandLineArgs(InstanceProxy.CommandLineArgs);

			//// close current client channel
			//ChannelServices.UnregisterChannel(clientChannel);

			return true;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Wait Or Timer Callback Handler
	/// </summary>
	/// <param name="state"> The state. </param>
	/// <param name="timedOut"> if set to <c> true </c> [timed out]. </param>
	private static void WaitOrTimerCallback(object state, bool timedOut)
	{
		// cast to event handler
		var callback = state as EventHandler<InstanceCallbackEventArgs>;

		// invoke event handler on other process
		callback?.Invoke(state, new InstanceCallbackEventArgs(InstanceProxy.CommandLineArgs));
	}

	#endregion

	#region Classes

	/// <summary>
	/// </summary>
	public class InstanceCallbackEventArgs : EventArgs
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceCallbackEventArgs" /> class.
		/// </summary>
		/// <param name="commandLineArgs"> The command line args. </param>
		internal InstanceCallbackEventArgs(string[] commandLineArgs)
		{
			CommandLineArgs = commandLineArgs;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the command line args.
		/// </summary>
		/// <value> The command line args. </value>
		public string[] CommandLineArgs { get; }

		/// <summary>
		/// Gets a value indicating whether this instance is first instance.
		/// </summary>
		/// <value>
		/// <c> true </c> if this instance is first instance; otherwise, <c> false </c>.
		/// </value>
		public bool IsFirstInstance { get; private set; }

		#endregion
	}

	/// <summary>
	/// shared object for processes
	/// </summary>
	[Serializable]
	internal class InstanceProxy : MarshalByRefObject
	{
		#region Properties

		/// <summary>
		/// Gets the command line args.
		/// </summary>
		/// <value> The command line args. </value>
		public static string[] CommandLineArgs { get; internal set; }

		#endregion

		#region Methods

		/// <summary>
		/// Sets the command line args.
		/// </summary>
		/// <param name="commandLineArgs"> The command line args. </param>
		public void SetCommandLineArgs(string[] commandLineArgs)
		{
			CommandLineArgs = commandLineArgs;
		}

		#endregion
	}

	#endregion
}
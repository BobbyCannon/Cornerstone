#region References

using System.ComponentModel;
using System.Threading;
using System.Windows.Input;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Net;
using Cornerstone.Presentation;
using Cornerstone.Profiling;

#endregion

namespace Sample.Shared;

public class WebClientTest : Bindable
{
	#region Fields

	private readonly BackgroundWorker _worker;

	#endregion

	#region Constructors

	//public WebClientTest(ObservableCollection<string> log, IDispatcher dispatcher)
	public WebClientTest(SpeedyList<string> log, IDispatcher dispatcher)
		: base(dispatcher)
	{
		_worker = new BackgroundWorker();
		_worker.DoWork += WorkerOnDoWork;
		_worker.RunWorkerCompleted += WorkerOnRunWorkerCompleted;
		_worker.WorkerSupportsCancellation = true;

		Log = log;
		Count = new Counter(dispatcher);
		Delay = 10;

		// Commands
		StartCommand = new RelayCommand(_ => Start());
	}

	#endregion

	#region Properties

	public Counter Count { get; }

	public int Delay { get; set; }

	public bool IsRunning { get; set; }

	//public ObservableCollection<string> Log { get; }
	public SpeedyList<string> Log { get; }

	public ICommand StartCommand { get; }

	#endregion

	#region Methods

	private void Start()
	{
		if (_worker.IsBusy)
		{
			_worker.CancelAsync();
			return;
		}

		_worker.RunWorkerAsync();

		IsRunning = true;
	}

	private void WorkerOnDoWork(object sender, DoWorkEventArgs e)
	{
		var worker = (BackgroundWorker) sender;
		//using var client = new HttpClient();
		//client.BaseAddress = new Uri("https://becomeepic.com");
		using var client = new WebClient("https://becomeepic.com", 5000);

		while (!worker.CancellationPending)
		{
			//var value = client.GetStringAsync("/").AwaitResults();
			using var httpResponseMessage = client.Get("/");
			var value = httpResponseMessage.Content.ReadAsStringAsync().AwaitResults();

			this.Dispatch(() =>
			{
				Log.Add($"{Count.Value} - Length {value.Length}");

				//while (Log.Count > 20)
				//{
				//	Log.RemoveAt(0);
				//}
				Count.Increment();
			});

			Thread.Sleep(Delay);
		}
	}

	private void WorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
	{
		IsRunning = false;
	}

	#endregion
}
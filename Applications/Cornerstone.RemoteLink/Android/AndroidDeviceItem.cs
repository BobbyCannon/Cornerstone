#region References

using Cornerstone.Data;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.RemoteLink.Android;

[Notifiable(["*"])]
public partial class AndroidDeviceItem : ViewModel
{
	#region Constructors

	public AndroidDeviceItem()
	{
	}

	public AndroidDeviceItem(string serial, string state, string model)
	{
		Serial = serial ?? string.Empty;
		State = state ?? string.Empty;
		Model = string.IsNullOrWhiteSpace(model) ? serial : model;
	}

	#endregion

	#region Properties

	public string DisplayLabel => string.IsNullOrWhiteSpace(Model) || (Model == Serial)
		? $"{Serial} ({State})"
		: $"{Model}  {Serial} ({State})";

	[Notify]
	public partial string Model { get; set; }

	[Notify]
	public partial string Serial { get; set; }

	[Notify]
	public partial string State { get; set; }

	#endregion
}

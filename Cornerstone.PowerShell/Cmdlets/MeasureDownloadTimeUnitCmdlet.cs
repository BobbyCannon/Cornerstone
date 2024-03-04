#region References

using System.Management.Automation;
using Cornerstone.Data.Bytes;
using Cornerstone.Data.Times;
using Cornerstone.PowerShell.Documentation;
using Cornerstone.Text;
using Cornerstone.Text.Human;

#endregion

namespace Cornerstone.PowerShell.Cmdlets;

[CmdletGroup(CmdletGroups.Utility)]
[Cmdlet(VerbsDiagnostic.Measure, "DownloadTime")]
[CmdletDescription("Calculate the download time base on size and speed.")]
[CmdletExample(Code = "Convert-NumberUnits -Value 16 -ValueUnit Bit -OutputUnit Byte\r\n2 bytes\r\n", Remarks = "")]
public class MeasureDownloadTimeUnitCmdlet : PSCmdlet
{
	#region Constructors

	public MeasureDownloadTimeUnitCmdlet()
	{
		OutputMaxUnit = TimeUnit.Second;
		OutputMinUnit = TimeUnit.Millisecond;
		OutputUnitFormat = WordFormat.Full;
	}

	#endregion

	#region Properties

	[Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The size of data.")]
	public decimal Data { get; set; }

	[Parameter(Mandatory = true, Position = 1, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The data unit value.")]
	public ByteUnit DataUnit { get; set; }

	[Parameter(Mandatory = false)]
	public TimeUnit OutputMaxUnit { get; set; }

	[Parameter(Mandatory = false)]
	public TimeUnit OutputMinUnit { get; set; }

	[Parameter(Mandatory = false)]
	public WordFormat OutputUnitFormat { get; set; }

	[Parameter(Mandatory = true, Position = 2, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The rate of transfer value.")]
	public decimal Rate { get; set; }

	[Parameter(Mandatory = true, Position = 3, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The rate unit value.")]
	public ByteUnit RateUnit { get; set; }

	#endregion

	#region Methods

	protected override void ProcessRecord()
	{
		var downloadTime = new DownloadTime(ByteSize.From(Data, DataUnit), ByteSize.From(Rate, RateUnit));
		var options = new HumanizeOptions
		{
			MaxUnit = OutputMaxUnit,
			MinUnit = OutputMinUnit,
			WordFormat = OutputUnitFormat,
			Precision = 3
		};
		WriteObject(downloadTime.Humanize(options));
	}

	#endregion
}
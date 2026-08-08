#region References

using System.Management.Automation;
using Cornerstone.Data.Bytes;
using Cornerstone.PowerShell.Documentation;
using Cornerstone.Text;

#endregion

namespace Cornerstone.PowerShell.Cmdlets;

[CmdletGroup(CmdletGroups.Utility)]
[Cmdlet(VerbsData.Convert, "NumberUnits")]
[CmdletDescription("Convert the number from one unit to another.")]
[CmdletExample(Code = "Convert-NumberUnits -Value 16 -ValueUnit Bit -OutputUnit Byte\r\n2 bytes\r\n", Remarks = "")]
public class ConvertNumberUnitCmdlet : PSCmdlet
{
	#region Constructors

	public ConvertNumberUnitCmdlet()
	{
		OutputUnitFormat = WordFormat.Full;
	}

	#endregion

	#region Properties

	[Parameter(Mandatory = true, Position = 2, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The desired output unit.")]
	public ByteUnit OutputUnit { get; set; }

	[Parameter(Mandatory = false, Position = 3, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The desired output unit word format.")]
	public WordFormat OutputUnitFormat { get; set; }

	[Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The number value.")]
	public decimal Value { get; set; }

	[Parameter(Mandatory = true, Position = 1, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The number's unit value.")]
	public ByteUnit ValueUnit { get; set; }

	#endregion

	#region Methods

	protected override void ProcessRecord()
	{
		var byteValue = ByteSize.From(Value, ValueUnit);
		WriteObject(byteValue.ToUnitString(OutputUnit));
	}

	#endregion
}
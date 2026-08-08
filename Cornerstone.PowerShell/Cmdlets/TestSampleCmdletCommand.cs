#region References

using Cornerstone.PowerShell.Documentation;
using System.Management.Automation;

#endregion

namespace Cornerstone.PowerShell.Cmdlets;

[Cmdlet(VerbsDiagnostic.Test, "SampleCmdlet")]
[CmdletDescription("Sample Cmdlet", "This is a sample cmdlet.")]
[OutputType(typeof(FavoriteStuff))]
public class TestSampleCmdletCommand : PSCmdlet
{
	#region Properties

	[Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
	public int FavoriteNumber { get; set; }

	[Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
	[ValidateSet("Cat", "Dog", "Horse")]
	public string FavoritePet { get; set; } = "Dog";

	#endregion

    #region Methods

    // This method gets called once for each cmdlet in the pipeline when the pipeline starts executing
    protected override void BeginProcessing()
    {
        WriteVerbose("Begin!");
    }

    // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
    protected override void EndProcessing()
    {
        WriteVerbose("End!");
    }

    // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
    protected override void ProcessRecord()
    {
        WriteObject(new FavoriteStuff
        {
            FavoriteNumber = FavoriteNumber,
            FavoritePet = FavoritePet
        });
    }

    #endregion
}

public class FavoriteStuff
{
	#region Properties

	public int FavoriteNumber { get; set; }

	public string FavoritePet { get; set; }

	#endregion
}
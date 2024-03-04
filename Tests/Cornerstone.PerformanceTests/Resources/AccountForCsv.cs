#region References

using Cornerstone.Attributes;

#endregion

namespace Cornerstone.PerformanceTests.Resources;

/// <summary>
/// "first_name","last_name","company_name","address","city","county","state","zip","phone1","phone2","email","web"
/// </summary>
public class AccountForCsv
{
	#region Properties

	[CsvHeader("address")]
	public string Address { get; set; }

	[CsvHeader("city")]
	public string City { get; set; }

	[CsvHeader("company_name")]
	public string CompanyName { get; set; }

	[CsvHeader("country")]
	public string Country { get; set; }

	[CsvHeader("email")]
	public string EmailAddress { get; set; }

	[CsvHeader("first_name")]
	public string FirstName { get; set; }

	[CsvHeader("last_name")]
	public string LastName { get; set; }

	[CsvHeader("phone1")]
	public string Phone1 { get; set; }

	[CsvHeader("phone2")]
	public string Phone2 { get; set; }

	[CsvHeader("state")]
	public string State { get; set; }

	[CsvHeader("web")]
	public string Web { get; set; }

	[CsvHeader("zip")]
	public int Zip { get; set; }

	#endregion

	#region Methods

	public static AccountForCsv GetFirstLineOfCsv()
	{
		return new AccountForCsv
		{
			FirstName = "James",
			LastName = "Butt",
			CompanyName = "Benton, John B Jr",
			Address = "6649 N Blue Gum St",
			City = "New Orleans",
			Country = "Orleans",
			State = "LA",
			Zip = 70116,
			Phone1 = "504-621-8927",
			Phone2 = "504-845-1427",
			EmailAddress = "jbutt@gmail.com",
			Web = "http://www.bentonjohnbjr.com"
		};
	}

	#endregion
}
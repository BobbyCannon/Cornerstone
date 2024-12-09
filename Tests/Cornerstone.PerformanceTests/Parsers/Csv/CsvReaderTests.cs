#region References

using System.IO;
using Cornerstone.Convert;
using Cornerstone.Parsers.Csv;
using Cornerstone.PerformanceTests.Resources;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.PerformanceTests.Parsers.Csv;

[TestClass]
public class CsvReaderTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void StreamParse()
	{
		var accountsPath = $@"{UnitTestsDirectory}\Resources\Accounts.csv";
		var textToStream = File.ReadAllText(accountsPath);
		using var stringReader = new StringReader(textToStream);
		var options = new CsvConverterSettings { HasHeader = true, Delimiter = ',' };
		var csvReader = new CsvReader(stringReader, options);

		AreEqual(0, csvReader.FieldsCount);
		IsTrue(csvReader.Read());
		AreEqual(12, csvReader.FieldsCount);
		AreEqual("first_name", csvReader[0]);
		AreEqual("last_name", csvReader[1]);
		AreEqual("company_name", csvReader[2]);
		AreEqual("address", csvReader[3]);
		AreEqual("city", csvReader[4]);
		AreEqual("county", csvReader[5]);
		AreEqual("state", csvReader[6]);
		AreEqual("zip", csvReader[7]);
		AreEqual("phone1", csvReader[8]);
		AreEqual("phone2", csvReader[9]);
		AreEqual("email", csvReader[10]);
		AreEqual("web", csvReader[11]);

		IsTrue(csvReader.Read());
		var firstRow = AccountForCsv.GetFirstLineOfCsv();

		AreEqual(firstRow.FirstName, csvReader["first_name"]);
		AreEqual(firstRow.LastName, csvReader["last_name"]);
		AreEqual(firstRow.CompanyName, csvReader["company_name"]);
		AreEqual(firstRow.Address, csvReader["address"]);
		AreEqual(firstRow.City, csvReader["city"]);
		AreEqual(firstRow.Country, csvReader["county"]);
		AreEqual(firstRow.State, csvReader["state"]);
		AreEqual(firstRow.Zip, csvReader["zip"].ConvertTo<int>());
		AreEqual(firstRow.Phone1, csvReader["phone1"]);
		AreEqual(firstRow.Phone2, csvReader["phone2"]);
		AreEqual(firstRow.EmailAddress, csvReader["email"]);
		AreEqual(firstRow.Web, csvReader["web"]);
	}

	#endregion
}
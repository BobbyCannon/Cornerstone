#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using Cornerstone.Extensions;
using Cornerstone.PowerShell.Documentation;
using Microsoft.Web.Administration;
using Microsoft.Win32;

#endregion

namespace Cornerstone.PowerShell.Cmdlets;

[CmdletGroup(CmdletGroups.Environment)]
[Cmdlet(VerbsCommon.New, "Website")]
[CmdletDescription("Add a new (or update) a website.")]
[CmdletExample(Code = "", Remarks = "Binding Example: *:443:btb.becomeepic.com:*.becomeepic.com")]
public class NewWebsiteCmdlet : PSCmdlet
{
	#region Properties

	[Parameter(Mandatory = true, Position = 1, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The bindings for the site.")]
	public string Bindings { get; set; }

	[Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The name of the IIS website.")]
	public string Name { get; set; }

	#endregion

	#region Methods

	public void AddOrUpdateWebsite()
	{
		var inetpubPath = GetInetpubPath();
		using var server = new ServerManager();
		var sitePool = server.ApplicationPools.FirstOrDefault(pool => pool.Name == Name);

		if (sitePool == null)
		{
			sitePool = server.ApplicationPools.Add(Name);
			sitePool.AutoStart = true;
			sitePool.ManagedPipelineMode = ManagedPipelineMode.Integrated;
			// sitePool.ManagedRuntimeVersion = "V4.0";
			sitePool.ProcessModel.IdentityType = ProcessModelIdentityType.ApplicationPoolIdentity;
			sitePool.Enable32BitAppOnWin64 = false;
			server.CommitChanges();
		}

		var site = server.Sites.FirstOrDefault(x => x.Name == Name);
		if (site == null)
		{
			var mySitePath = Path.Combine(inetpubPath, Name);
			site = server.Sites.Add(Name, mySitePath, 80);
			UpdateBindings(site);
			site.ApplicationDefaults.ApplicationPoolName = sitePool.Name;
		}
		else
		{
			UpdateBindings(site);
		}

		server.CommitChanges();
	}

	public static string GetInetpubPath()
	{
		const string keyName = @"SOFTWARE\Microsoft\InetStp";
		const string valueName = "PathWWWRoot";

		try
		{
			using var key = Registry.LocalMachine.OpenSubKey(keyName);
			var value = key?.GetValue(valueName);
			if (value != null)
			{
				var response = value.ToString();
				var path = "inetpub";
				var index = response.IndexOf(path);
				return index < 0 ? response : response.Substring(0, index + path.Length);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error reading registry: {ex.Message}");
		}
		return null;
	}

	public static void RemoveWebsite(string name)
	{
		using var server = new ServerManager();
		var site = server.Sites.FirstOrDefault(x => x.Name == name);
		if (site != null)
		{
			site.Stop();
			server.Sites.Remove(site);
			server.CommitChanges();
		}

		var sitePool = server.ApplicationPools.FirstOrDefault(pool => pool.Name == name);
		if (sitePool != null)
		{
			server.ApplicationPools.Remove(sitePool);
			server.CommitChanges();
		}
	}

	public static bool TryParseBinding(string bindingString, out IDictionary<string, string> binding)
	{
		// Regular expression to match the binding pattern
		var pattern = bindingString.Split(':');

		if (pattern.Length >= 3)
		{
			var ip = string.IsNullOrWhiteSpace(pattern[0]) ? "*" : pattern[0];
			var port = pattern[1];
			var host = pattern[2];

			binding = new Dictionary<string, string>
			{
				{ "ip", ip },
				{ "port", port },
				{ "host", host }
			};

			if (pattern.Length >= 4)
			{
				binding.Add("ssl", pattern[3]);
			}

			return true;
		}

		binding = null;
		return false;
	}

	protected override void ProcessRecord()
	{
		AddOrUpdateWebsite();
		//WriteObject(downloadTime.Humanize(options));
	}

	private void UpdateBindings(Site site)
	{
		site.Bindings.Clear();

		var bindings = Bindings.Split(';');

		foreach (var binding in bindings)
		{
			if (!TryParseBinding(binding, out var dictionary))
			{
				continue;
			}

			var newBinding = site.Bindings.CreateElement("binding");
			newBinding.Protocol = dictionary.ContainsKey("ssl") ? "https" : "http";
			newBinding.BindingInformation = $"{dictionary["ip"]}:{dictionary["port"]}:{dictionary["host"]}";

			if (dictionary.TryGetValue("ssl", out var ssl))
			{
				newBinding.CertificateHash = ssl.FromHexStringToByteArray();
				newBinding.CertificateStoreName = "WebHosting";
			}
			site.Bindings.Add(newBinding);
		}
	}

	#endregion
}
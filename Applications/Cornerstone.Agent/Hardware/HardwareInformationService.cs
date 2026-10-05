#region References

using System;
using System.Collections.Generic;
using Cornerstone.Data.Bytes;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Agent.Hardware;

/// <summary>
/// Detects available inference backends and rough hardware capacity.
/// Used by <see cref="Keystone.Processors.ModelsProcessor"/> when configuring native libs.
/// </summary>
public class HardwareInformationService
{
	#region Constants

	private const string ManagementScope = "root\\cimv2";

	#endregion

	#region Fields

	private static readonly WmiLightQueryProvider _provider;
	private readonly IRuntimeInformation _runtimeInformation;

	#endregion

	#region Constructors

	public HardwareInformationService(IRuntimeInformation runtimeInformation)
	{
		_runtimeInformation = runtimeInformation;

		AvailableBackends = [];
		RecommendedBackend = ExecutionBackend.Cpu;
		SelectedBackend = ExecutionBackend.Cpu;
		StatusMessage = string.Empty;
	}

	static HardwareInformationService()
	{
		_provider = new WmiLightQueryProvider(null);
	}

	#endregion

	#region Properties

	public List<ExecutionBackend> AvailableBackends { get; set; }
	public long AvailableRamBytes { get; set; }
	public long GpuMemoryBytes { get; set; }
	public string GpuName { get; set; }
	public bool HasCuda { get; set; }
	public bool HasNpu { get; set; }
	public bool HasVulkan { get; set; }
	public DateTime LastUpdated { get; private set; }
	public ExecutionBackend RecommendedBackend { get; set; }
	public ExecutionBackend SelectedBackend { get; set; }
	public string StatusMessage { get; set; }
	public long TotalRamBytes { get; set; }

	#endregion

	#region Methods

	public void LoadLifecycle()
	{
		if (LastUpdated != DateTime.MinValue)
		{
			return;
		}

		try
		{
			PerformDetection();
			LastUpdated = DateTime.UtcNow;
		}
		catch (Exception ex)
		{
			GpuName = "Unknown : " + ex.Message;
			StatusMessage = "Hardware detection had issues";
		}
	}

	private string BuildStatusMessage()
	{
		var totalRam = ByteSize.FromBytes(TotalRamBytes).ToString();
		var parts = new List<string> { $"RAM: {totalRam}" };
		if (!string.IsNullOrEmpty(GpuName))
		{
			parts.Add($"GPU: {GpuName}");
		}

		var backends = new List<string>();
		if (HasCuda)
		{
			backends.Add("CUDA");
		}
		if (HasVulkan)
		{
			backends.Add("Vulkan");
		}

		if (backends.Count > 0)
		{
			parts.Add(string.Join(" / ", backends) + " ✓");
		}

		return string.Join(" | ", parts);
	}

	private ExecutionBackend DetermineRecommendedBackend()
	{
		if (HasCuda)
		{
			return ExecutionBackend.Cuda;
		}
		if (HasVulkan && (GpuMemoryBytes > (2L * 1024 * 1024 * 1024)) &&
			(GpuName.Contains("Arc", StringComparison.OrdinalIgnoreCase) ||
				GpuName.Contains("Radeon", StringComparison.OrdinalIgnoreCase)))
		{
			return ExecutionBackend.Vulkan;
		}
		return ExecutionBackend.Cpu;
	}

	private static long GetActualVramForGpu(string gpuName, long reportedVram)
	{
		if (gpuName.Contains("5090", StringComparison.OrdinalIgnoreCase))
		{
			return 32L * 1024 * 1024 * 1024;
		}
		if (gpuName.Contains("5080", StringComparison.OrdinalIgnoreCase))
		{
			return 16L * 1024 * 1024 * 1024;
		}
		if (gpuName.Contains("5070 Ti", StringComparison.OrdinalIgnoreCase) || gpuName.Contains("5070Ti", StringComparison.OrdinalIgnoreCase))
		{
			return 16L * 1024 * 1024 * 1024;
		}
		if (gpuName.Contains("5070", StringComparison.OrdinalIgnoreCase))
		{
			return 12L * 1024 * 1024 * 1024;
		}
		if (gpuName.Contains("4090", StringComparison.OrdinalIgnoreCase))
		{
			return 24L * 1024 * 1024 * 1024;
		}
		if (gpuName.Contains("4080", StringComparison.OrdinalIgnoreCase))
		{
			return 16L * 1024 * 1024 * 1024;
		}
		if (gpuName.Contains("4070 Ti", StringComparison.OrdinalIgnoreCase) || gpuName.Contains("4070Ti", StringComparison.OrdinalIgnoreCase))
		{
			return 16L * 1024 * 1024 * 1024;
		}
		if (gpuName.Contains("4070", StringComparison.OrdinalIgnoreCase))
		{
			return 12L * 1024 * 1024 * 1024;
		}
		if (gpuName.Contains("3090", StringComparison.OrdinalIgnoreCase))
		{
			return 24L * 1024 * 1024 * 1024;
		}
		if (gpuName.Contains("3080 Ti", StringComparison.OrdinalIgnoreCase) || gpuName.Contains("3080Ti", StringComparison.OrdinalIgnoreCase))
		{
			return 12L * 1024 * 1024 * 1024;
		}
		if (gpuName.Contains("3080", StringComparison.OrdinalIgnoreCase))
		{
			return 10L * 1024 * 1024 * 1024;
		}
		if ((reportedVram >= 4290772992) && (reportedVram <= 4294967296))
		{
			return 8L * 1024 * 1024 * 1024;
		}

		return reportedVram;
	}

	private (string Name, long MemoryBytes) GetPrimaryGpu()
	{
		try
		{
			var query = "SELECT Name, AdapterRAM FROM Win32_VideoController";
			var controllers = _provider.Query(ManagementScope, query);

			string bestName = null;
			long bestMemory = 0;
			var foundNvidia = false;
			var foundAmd = false;

			foreach (var c in controllers)
			{
				var name = c["Name"]?.ToString()?.Trim() ?? "";
				if (string.IsNullOrEmpty(name))
				{
					continue;
				}

				if (name.Contains("Microsoft Basic Display", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				long ram = 0;
				var ramObj = c["AdapterRAM"];
				if (ramObj is uint u)
				{
					ram = u;
				}
				else if (ramObj is ulong ul)
				{
					ram = (long) ul;
				}

				ram = GetActualVramForGpu(name, ram);

				var isNvidia = name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase);
				var isAmd = name.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
					name.Contains("Radeon", StringComparison.OrdinalIgnoreCase);

				if (isNvidia && !foundNvidia)
				{
					bestName = name;
					bestMemory = ram;
					foundNvidia = true;
				}
				else if (isAmd && !foundNvidia && !foundAmd)
				{
					bestName = name;
					bestMemory = ram;
					foundAmd = true;
				}
				else if (!foundNvidia && !foundAmd && (bestName == null))
				{
					bestName = name;
					bestMemory = ram;
				}
			}

			return (bestName ?? "Unknown", bestMemory);
		}
		catch
		{
			return ("Unknown", 0);
		}
	}

	private void PerformDetection()
	{
		TotalRamBytes = (long) _runtimeInformation.DeviceMemory.Bytes;
		AvailableRamBytes = TotalRamBytes - Environment.WorkingSet;

		var gpu = GetPrimaryGpu();
		GpuName = gpu.Name ?? "Unknown";
		GpuMemoryBytes = gpu.MemoryBytes;

		HasCuda = GpuName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase);
		HasVulkan = GpuName.Contains("AMD", StringComparison.OrdinalIgnoreCase)
			|| GpuName.Contains("Radeon", StringComparison.OrdinalIgnoreCase);

		HasNpu = false;

		AvailableBackends.Add(ExecutionBackend.Cpu);
		if (HasCuda)
		{
			AvailableBackends.Add(ExecutionBackend.Cuda);
		}
		if (HasVulkan)
		{
			AvailableBackends.Add(ExecutionBackend.Vulkan);
		}

		RecommendedBackend = DetermineRecommendedBackend();
		SelectedBackend = RecommendedBackend;
		StatusMessage = BuildStatusMessage();
	}

	#endregion
}

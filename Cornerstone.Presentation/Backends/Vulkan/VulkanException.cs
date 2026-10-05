using System;
using Cornerstone.Presentation.Vulkan.Interop;
using Cornerstone.Presentation.Vulkan.UnmanagedInterop;

namespace Cornerstone.Presentation.Vulkan;

public class VulkanException : Exception
{
    public VulkanException(string message) : base(message)
    {
        
    }

    public VulkanException(string funcName, int res) : this(funcName, (VkResult)res)
    {
        
    }
    
    internal VulkanException(string funcName, VkResult res) : base($"{funcName} returned {res}")
    {

    }

    public static void ThrowOnError(string funcName, int res) => ((VkResult)res).ThrowOnError(funcName);
}

internal static class VulkanExceptionExtensions
{
    public static void ThrowOnError(this VkResult res, string funcName)
    {
        if (res != VkResult.VK_SUCCESS)
            throw new VulkanException(funcName, res);
    }
}
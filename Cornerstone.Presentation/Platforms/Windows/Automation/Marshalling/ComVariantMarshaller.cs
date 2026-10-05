global using ComVariantMarshaller = Cornerstone.Presentation.Platforms.Windows.Automation.Marshalling.ComVariantMarshaller;
using System.Runtime.InteropServices.Marshalling;

namespace Cornerstone.Presentation.Platforms.Windows.Automation.Marshalling;

[CustomMarshaller(typeof(object), MarshalMode.Default, typeof(ComVariantMarshaller))]
internal static class ComVariantMarshaller
{
    public static ComVariant ConvertToUnmanaged(object? managed) => ComVariant.Create(managed);

    public static object? ConvertToManaged(ComVariant unmanaged) => unmanaged.AsObject();

    public static void Free(ComVariant unmanaged) => unmanaged.Dispose();
}

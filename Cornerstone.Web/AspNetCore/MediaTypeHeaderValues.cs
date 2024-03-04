#region References

using Microsoft.Net.Http.Headers;

#endregion

namespace Cornerstone.Web.AspNetCore;

internal static class MediaTypeHeaderValues
{
	#region Fields

	public static readonly MediaTypeHeaderValue ApplicationAnyJsonSyntax
		= MediaTypeHeaderValue.Parse("application/*+json").CopyAsReadOnly();

	public static readonly MediaTypeHeaderValue ApplicationJson
		= MediaTypeHeaderValue.Parse("application/json").CopyAsReadOnly();

	public static readonly MediaTypeHeaderValue ApplicationJsonPatch
		= MediaTypeHeaderValue.Parse("application/json-patch+json").CopyAsReadOnly();

	public static readonly MediaTypeHeaderValue TextJson
		= MediaTypeHeaderValue.Parse("text/json").CopyAsReadOnly();

	#endregion
}
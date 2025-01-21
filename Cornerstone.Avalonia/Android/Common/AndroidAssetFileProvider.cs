#region References

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Android.Content.Res;
using Avalonia.Platform;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

#endregion

namespace Cornerstone.Avalonia.Android.Common;

internal class AndroidAssetFileProvider : IFileProvider
{
	#region Fields

	private readonly Assembly _assembly;

	private readonly AssetManager _assetManager;
	private readonly string _contentRootDir;

	#endregion

	#region Constructors

	public AndroidAssetFileProvider(Assembly assembly, string contentRootDir)
	{
		var assets = AndroidApplication.Context.Assets;
		if (assets is null)
		{
			throw new ArgumentNullException(nameof(assets));
		}

		_assetManager = assets;
		_assembly = assembly;
		_contentRootDir = contentRootDir;
	}

	#endregion

	#region Methods

	public IDirectoryContents GetDirectoryContents(string subpath)
	{
		return new AndroidAssetDirectoryContents();
	}

	public IFileInfo GetFileInfo(string subpath)
	{
		return new AndroidAssetFileInfo(_assetManager, Path.Combine(_contentRootDir, subpath));
	}

	public IChangeToken Watch(string filter)
	{
		return NullChangeToken.Singleton;
	}

	#endregion

	#region Classes

	private sealed class AndroidAssetDirectoryContents : IDirectoryContents
	{
		#region Properties

		public bool Exists => false;

		#endregion

		#region Methods

		public IEnumerator<IFileInfo> GetEnumerator()
		{
			throw new NotImplementedException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException();
		}

		#endregion
	}

	private sealed class AndroidAssetFileInfo : IFileInfo
	{
		#region Fields

		private readonly AssetManager _assetManager;
		private readonly string _filePath;
		private readonly Lazy<bool> _lazyAssetExists;
		private readonly Lazy<long> _lazyAssetLength;

		#endregion

		#region Constructors

		public AndroidAssetFileInfo(AssetManager assetManager, string filePath)
		{
			_assetManager = assetManager;
			_filePath = filePath;
			Name = Path.GetFileName(filePath);

			_lazyAssetExists = new Lazy<bool>(() =>
			{
				try
				{
					using var stream = _assetManager.Open(_filePath);
					return true;
				}
				catch
				{
					return false;
				}
			});

			_lazyAssetLength = new Lazy<long>(() =>
			{
				try
				{
					// The stream returned by AssetManager.Open() is not seekable, so we have to read
					// the contents to get its length. In practice, Length is never called by BlazorWebView,
					// so it's here "just in case."
					using var stream = _assetManager.Open(_filePath);
					var buffer = ArrayPool<byte>.Shared.Rent(4096);
					long length = 0;
					while (length != (length += stream.Read(buffer)))
					{
						// just read the stream to get its length; we don't need the contents here
					}
					ArrayPool<byte>.Shared.Return(buffer);
					return length;
				}
				catch
				{
					return -1;
				}
			});
		}

		#endregion

		#region Properties

		public bool Exists => _lazyAssetExists.Value;
		public bool IsDirectory => false;
		public DateTimeOffset LastModified { get; } = DateTimeOffset.FromUnixTimeSeconds(0);
		public long Length => _lazyAssetLength.Value;
		public string Name { get; }
		public string PhysicalPath => null!;

		#endregion

		#region Methods

		public Stream CreateReadStream()
		{
			return AssetLoader.Open(new Uri(_filePath));
		}

		#endregion
	}

	#endregion
}
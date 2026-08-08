#region References

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Avalonia.MediaPlayer;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;

#endregion

namespace Cornerstone.Avalonia.Platforms.Windows;

[SourceReflection]
[SupportedOSPlatform("Windows")]
internal class MediaPlayerAdapter : BaseMediaPlayerAdapter
{
	#region Constants

	private const uint MfVideoArModeNone = 0;
	private const uint MfVideoArModePreservePicture = 1;
	private const int PwRenderFullContent = 0x00000002;
	private const int SrcCopy = 0x00CC0020;
	private const ushort VtI8 = 20;

	#endregion

	#region Fields

	private string _currentUrl;
	private bool _fillMode;
	private IntPtr _mediaPlayer;
	private NativeControlHost _nativeHost;
	private IMFPMediaPlayer _player;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public MediaPlayerAdapter()
	{
		var hr = MFStartup(0x10070, 0);
		if (hr != 0)
		{
			throw new Exception($"MFStartup failed: HRESULT {hr:X8}");
		}
	}

	#endregion

	#region Properties

	public override TimeSpan Duration
	{
		get
		{
			if (_player == null)
			{
				return TimeSpan.Zero;
			}

			var positionType = Guid.Empty;
			var pv = new PropVariant();
			return _player.GetDuration(ref positionType, ref pv) == 0
				? TimeSpan.FromTicks(pv.Int64Value)
				: TimeSpan.Zero;
		}
	}

	public override bool IsMuted
	{
		get => (_player != null) && (_player.GetMute(out var mute) == 0) && mute;
		set => _player?.SetMute(value);
	}

	public override TimeSpan Position
	{
		get
		{
			if (_player == null)
			{
				return TimeSpan.Zero;
			}

			var positionType = Guid.Empty;
			var pv = new PropVariant();
			return _player.GetPosition(ref positionType, ref pv) == 0
				? TimeSpan.FromTicks(pv.Int64Value)
				: TimeSpan.Zero;
		}
		set
		{
			if (_player == null)
			{
				return;
			}

			var positionType = Guid.Empty;
			var pv = new PropVariant { Vt = VtI8, Int64Value = value.Ticks };
			_player.SetPosition(ref positionType, ref pv);
		}
	}

	public override MediaPlaybackState State
	{
		get
		{
			if ((_player == null) || (_player.GetState(out var state) != 0))
			{
				return MediaPlaybackState.Stopped;
			}

			return state switch
			{
				MfpMediaPlayerState.Playing => MediaPlaybackState.Playing,
				MfpMediaPlayerState.Paused => MediaPlaybackState.Paused,
				_ => MediaPlaybackState.Stopped
			};
		}
	}

	public override double Volume
	{
		get => (_player != null) && (_player.GetVolume(out var volume) == 0) ? volume : 1d;
		set => _player?.SetVolume((float) Math.Clamp(value, 0d, 1d));
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override async Task<NativeSurfaceSnapshot> CaptureSnapshotAsync(NativeSurfaceSnapshotOptions options = null)
	{
		if (_nativeHost == null)
		{
			return NativeSurfaceSnapshot.Failed("Media player host is not available.");
		}

		try
		{
			var hwnd = await _nativeHost.GetHwndAsync().ConfigureAwait(true);
			if (hwnd == IntPtr.Zero)
			{
				return NativeSurfaceSnapshot.Failed("Media player HWND is not available.");
			}

			if (!GetClientRect(hwnd, out var rect))
			{
				return NativeSurfaceSnapshot.Failed("Could not read media player client size.");
			}

			var width = Math.Max(1, rect.Right - rect.Left);
			var height = Math.Max(1, rect.Bottom - rect.Top);
			if ((width <= 1) || (height <= 1))
			{
				return NativeSurfaceSnapshot.Failed("Media player has no measurable size.");
			}

			var png = CaptureHwndToPng(hwnd, width, height);
			if (png is not { Length: > 0 })
			{
				return NativeSurfaceSnapshot.Failed("Failed to encode media player snapshot.");
			}

			return NativeSurfaceSnapshotHelper.ProcessPng(png, width, height, options);
		}
		catch (Exception ex)
		{
			return NativeSurfaceSnapshot.Failed(ex.Message);
		}
	}

	public override void Initialize(NativeControlHost nativeHost)
	{
		_nativeHost = nativeHost ?? throw new ArgumentNullException(nameof(nativeHost));

		base.Initialize(nativeHost);
	}

	public override void Pause()
	{
		_player?.Pause();
		OnStateChanged();
	}

	public override async void Play(string url)
	{
		if (string.IsNullOrEmpty(url))
		{
			return;
		}

		try
		{
			// Get HWND from Avalonia NativeControlHost child.
			var hwnd = await _nativeHost.GetHwndAsync();
			if (hwnd == IntPtr.Zero)
			{
				throw new Exception("Failed to retrieve NativeControlHost HWND.");
			}

			if ((_player != null) && string.Equals(url, _currentUrl, StringComparison.Ordinal))
			{
				// Same source is still loaded (e.g. it finished playing) - restart from the beginning.
				Position = TimeSpan.Zero;
				_player.Play();
				OnStateChanged();
				return;
			}

			ReleasePlayer();

			var hrCreate = MFPCreateMediaPlayer(url, false, 0, IntPtr.Zero, hwnd, out _mediaPlayer);
			if ((hrCreate != 0) || (_mediaPlayer == IntPtr.Zero))
			{
				throw new Exception(FormatCreatePlayerError(hrCreate, url));
			}

			_player = (IMFPMediaPlayer) Marshal.GetObjectForIUnknown(_mediaPlayer);
			_currentUrl = url;

			// Default: preserve aspect (width or height wins). Fill only when SetVideoStretch(true).
			_player.SetAspectRatioMode(_fillMode ? MfVideoArModeNone : MfVideoArModePreservePicture);
			_player.UpdateVideo();

			_player.Play();

			OnMediaOpened();
			OnStateChanged();
		}
		catch (Exception ex)
		{
			// async void: surface clearly; callers cannot await this path.
			Console.WriteLine($"MediaPlayerAdapter.Play failed: {ex.Message}");
			ReleasePlayer();
			OnStateChanged();
			throw;
		}
	}

	public override void PlayFile(string filePath)
	{
		var fileUrl = $"file://localhost/{filePath.Replace("\\", "/")}";
		Play(fileUrl);
	}

	public override void Resume()
	{
		_player?.Play();
		OnStateChanged();
	}

	public override void SetVideoStretch(bool fill)
	{
		_fillMode = fill;

		if (_player == null)
		{
			return;
		}

		_player.SetAspectRatioMode(fill ? MfVideoArModeNone : MfVideoArModePreservePicture);
		_player.UpdateVideo();
	}

	public override void Stop()
	{
		ReleasePlayer();
		OnStateChanged();
	}

	public override void UpdateVideoLayout()
	{
		_player?.UpdateVideo();
	}

	/// <inheritdoc/>
	protected override void Dispose(bool disposing)
	{
		ReleasePlayer();
		MFShutdown();
		base.Dispose(disposing);
	}

	private static byte[] CaptureHwndToPng(IntPtr hwnd, int width, int height)
	{
		var hdcWindow = GetDC(hwnd);
		if (hdcWindow == IntPtr.Zero)
		{
			return null;
		}

		var hdcMem = CreateCompatibleDC(hdcWindow);
		var hBitmap = CreateCompatibleBitmap(hdcWindow, width, height);
		var old = SelectObject(hdcMem, hBitmap);

		try
		{
			// Prefer PrintWindow so layered/video content is included when the host supports it.
			if (!PrintWindow(hwnd, hdcMem, PwRenderFullContent))
			{
				BitBlt(hdcMem, 0, 0, width, height, hdcWindow, 0, 0, SrcCopy);
			}

			var bmi = new BitmapInfo
			{
				BmiHeader = new BitmapInfoHeader
				{
					BiSize = Marshal.SizeOf<BitmapInfoHeader>(),
					BiWidth = width,
					BiHeight = -height, // top-down
					BiPlanes = 1,
					BiBitCount = 32,
					BiCompression = 0
				}
			};

			var pixelCount = width * height;
			var pixels = new byte[pixelCount * 4];
			if (GetDIBits(hdcMem, hBitmap, 0, (uint) height, pixels, ref bmi, 0) == 0)
			{
				return null;
			}

			using var writeable = new WriteableBitmap(
				new PixelSize(width, height),
				new Vector(96, 96),
				PixelFormat.Bgra8888,
				AlphaFormat.Opaque);

			using (var fb = writeable.Lock())
			{
				var srcStride = width * 4;
				if (fb.RowBytes == srcStride)
				{
					Marshal.Copy(pixels, 0, fb.Address, pixels.Length);
				}
				else
				{
					for (var y = 0; y < height; y++)
					{
						Marshal.Copy(pixels, y * srcStride, fb.Address + (y * fb.RowBytes), srcStride);
					}
				}
			}

			using var stream = new MemoryStream();
			writeable.Save(stream);
			return stream.ToArray();
		}
		finally
		{
			SelectObject(hdcMem, old);
			DeleteObject(hBitmap);
			DeleteDC(hdcMem);
			ReleaseDC(hwnd, hdcWindow);
		}
	}

	private static string FormatCreatePlayerError(int hr, string url)
	{
		// HRESULT_FROM_WIN32(ERROR_ACCESS_DENIED) — common when the URL is blocked (HTTP 403),
		// the path is not readable, or Media Foundation cannot open the source.
		if (unchecked((uint) hr) == 0x80070005)
		{
			return $"Media Foundation could not open the media (access denied, HRESULT {hr:X8}). "
				+ "Check that the URL is reachable (many sample hosts return 403) or open a local file. "
				+ $"Source: {url}";
		}

		return $"MFPCreateMediaPlayer failed: HRESULT {hr:X8}. Source: {url}";
	}

	private void ReleasePlayer()
	{
		if (_player != null)
		{
			try
			{
				_player.Stop();
				_player.Shutdown();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error stopping media player: {ex.Message}");
			}

			Marshal.ReleaseComObject(_player);
			_player = null;
		}

		if (_mediaPlayer != IntPtr.Zero)
		{
			Marshal.Release(_mediaPlayer);
			_mediaPlayer = IntPtr.Zero;
		}

		_currentUrl = null;
	}

	[DllImport("gdi32.dll")]
	private static extern bool BitBlt(IntPtr hdcDest, int x, int y, int width, int height, IntPtr hdcSrc, int xSrc, int ySrc, int rop);

	[DllImport("gdi32.dll")]
	private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);

	[DllImport("gdi32.dll")]
	private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteDC(IntPtr hdc);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteObject(IntPtr hObject);

	[DllImport("user32.dll")]
	private static extern IntPtr GetDC(IntPtr hWnd);

	[DllImport("gdi32.dll")]
	private static extern int GetDIBits(IntPtr hdc, IntPtr hbm, uint start, uint lines, byte[] bits, ref BitmapInfo bmi, uint usage);

	[DllImport("user32.dll")]
	private static extern bool GetClientRect(IntPtr hWnd, out Rect rect);

	[DllImport("mfplay.dll")]
	private static extern int MFPCreateMediaPlayer(
		[MarshalAs(UnmanagedType.LPWStr)] string pwszURL,
		[MarshalAs(UnmanagedType.Bool)] bool fStartPlayback,
		uint creationOptions,
		IntPtr pCallback,
		IntPtr hWnd,
		out IntPtr ppPlayer);

	[DllImport("mfplat.dll")]
	private static extern int MFShutdown();

	[DllImport("mfplat.dll")]
	private static extern int MFStartup(uint Version, uint dwFlags);

	[DllImport("user32.dll")]
	private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, int nFlags);

	[DllImport("user32.dll")]
	private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

	[DllImport("gdi32.dll")]
	private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

	#endregion

	#region Enumerations

	private enum MfpMediaPlayerState
	{
		Empty = 0,
		Stopped = 1,
		Playing = 2,
		Paused = 3,
		Shutdown = 4
	}

	#endregion

	#region Structures

	[StructLayout(LayoutKind.Sequential)]
	private struct BitmapInfo
	{
		public BitmapInfoHeader BmiHeader;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct BitmapInfoHeader
	{
		public int BiSize;
		public int BiWidth;
		public int BiHeight;
		public short BiPlanes;
		public short BiBitCount;
		public int BiCompression;
		public int BiSizeImage;
		public int BiXPelsPerMeter;
		public int BiYPelsPerMeter;
		public int BiClrUsed;
		public int BiClrImportant;
	}

	// PROPVARIANT is 16 bytes on 32-bit but 24 bytes on 64-bit Windows. The explicit size keeps the managed layout in sync with the native
	// one so duration/position (VT_I8 at offset 8) marshal correctly and seek requests are well-formed.
	[StructLayout(LayoutKind.Explicit, Size = 24)]
	private struct PropVariant
	{
		[FieldOffset(0)]
		public ushort Vt;

		[FieldOffset(8)]
		public long Int64Value;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct Rect
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;
	}

	#endregion

	#region Interfaces

	[ComImport]
	[Guid("A714590A-58AF-430a-85BF-44F5EC838D85")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IMFPMediaPlayer
	{
		// Declaration order defines the COM vtable slots and MUST match mfplay.h exactly. Methods that are not called are declared as
		// opaque parameterless stubs so their slot is preserved without needing full marshaling signatures.
		[PreserveSig] int Play();
		[PreserveSig] int Pause();
		[PreserveSig] int Stop();
		[PreserveSig] int FrameStep();
		[PreserveSig] int SetPosition(ref Guid guidPositionType, ref PropVariant value);
		[PreserveSig] int GetPosition(ref Guid guidPositionType, ref PropVariant value);
		[PreserveSig] int GetDuration(ref Guid guidPositionType, ref PropVariant value);
		[PreserveSig] int SetRate();
		[PreserveSig] int GetRate();
		[PreserveSig] int GetSupportedRates();
		[PreserveSig] int GetState(out MfpMediaPlayerState state);
		[PreserveSig] int CreateMediaItemFromUrl();
		[PreserveSig] int CreateMediaItemFromObject();
		[PreserveSig] int SetMediaItem();
		[PreserveSig] int ClearMediaItem();
		[PreserveSig] int GetMediaItem();
		[PreserveSig] int GetVolume(out float volume);
		[PreserveSig] int SetVolume(float volume);
		[PreserveSig] int GetBalance();
		[PreserveSig] int SetBalance();
		[PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
		[PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute);
		[PreserveSig] int GetNativeVideoSize();
		[PreserveSig] int GetIdealVideoSize();
		[PreserveSig] int SetVideoSourceRect();
		[PreserveSig] int GetVideoSourceRect();
		[PreserveSig] int SetAspectRatioMode(uint mode);
		[PreserveSig] int GetAspectRatioMode();
		[PreserveSig] int GetVideoWindow();
		[PreserveSig] int UpdateVideo();
		[PreserveSig] int SetBorderColor();
		[PreserveSig] int GetBorderColor();
		[PreserveSig] int InsertEffect();
		[PreserveSig] int RemoveEffect();
		[PreserveSig] int RemoveAllEffects();
		[PreserveSig] int Shutdown();
	}

	#endregion
}
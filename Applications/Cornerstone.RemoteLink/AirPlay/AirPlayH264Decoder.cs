#region References

using System;
using System.Runtime.InteropServices;
using System.Threading;
using H264Data = Cornerstone.RemoteLink.AirPlay.Models.Mirroring.H264Data;

#endregion

namespace Cornerstone.RemoteLink.AirPlay;

/// <summary>
/// Decodes Annex-B H.264 via the Windows H.264 MFT, calling COM through native vtables.
/// On current Windows LTSC builds, QueryInterface for IMFTransform returns E_NOINTERFACE;
/// the IUnknown identity vtable is the transform.
/// </summary>
internal sealed class AirPlayH264Decoder : IDisposable
{
	#region Constants

	private const int MfENotAccepting = unchecked((int) 0xC00D36B5);
	private const int MfETransformNeedMoreInput = unchecked((int) 0xC00D6D72);
	private const int MfETransformStreamChange = unchecked((int) 0xC00D6D61);

	private const int MfVersion = 0x00020070;
	private const int MftInputStatusAcceptData = 1;
	private const int MftMessageCommandFlush = 0;
	private const int MftMessageNotifyBeginStreaming = 0x10000000;
	private const int MftMessageNotifyStartOfStream = 0x10000003;
	private const int MftOutputStreamProvidesSamples = 0x00000100;

	#endregion

	#region Fields

	private static readonly Guid ClsidCmsH264DecoderMft = new("62CE7E72-4C71-4d20-B15D-452831A87D9D");
	private static readonly Guid IidIUnknown = new("00000000-0000-0000-C000-000000000046");
	private static readonly Guid MfLowLatency = new("9c27891a-ed7a-40e1-88e8-b22727a024ee");

	private static readonly Guid MfMediaTypeVideo = new("73646976-0000-0010-8000-00AA00389B71");
	private static readonly Guid MfMtDefaultStride = new("644b4e48-1e02-4516-b0eb-c01ca9d49ac6");
	private static readonly Guid MfMtFrameSize = new("1652c33d-d6b2-4012-b834-72030849a37d");
	private static readonly Guid MfMtGeometricAperture = new("06674cf9-d26f-4a82-a117-328d2b76ffa6");
	private static readonly Guid MfMtInterlaceMode = new("e2724bb8-e676-4806-b4b2-a8d6efb44cc0");
	private static readonly Guid MfMtMajorType = new("48eba18e-f8c9-4687-bf11-0a74c9f96a8f");
	private static readonly Guid MfMtMinimumDisplayAperture = new("d2e7558c-dc1f-403f-9a72-d28bb73eb369");
	private static readonly Guid MfMtSubtype = new("f7e34c9a-42e8-4714-b74b-cb29d72c35e5");
	private static readonly Guid MfVideoFormatH264 = new("34363248-0000-0010-8000-00AA00389B71");
	private static readonly Guid MfVideoFormatH264Es = new("3F40F4F0-5622-4FF8-B6D8-A617D1338787");
	private static readonly Guid MfVideoFormatNv12 = new("3231564E-0000-0010-8000-00AA00389B71");
	private static readonly Guid MftCategoryVideoDecoder = new("d6c02d4b-6833-45b4-971a-05a4b04bab91");
	private byte[] _bgra;
	private int _cachedCodedHeight;
	private int _cachedCropX;
	private int _cachedCropY;
	private int _cachedHeight;
	private int _cachedStride;
	private int _cachedWidth;
	private int _copiedFrames;
	private int _decodeHeight;
	private int _decodeWidth;
	private int _disposed;

	private readonly object _gate = new();
	private GetInputStatusDlg _getInputStatus;
	private GetOutputCurrentTypeDlg _getOutputCurrentType;
	private GetOutputStreamInfoDlg _getOutputStreamInfo;
	private bool _layoutCached;
	private ProcessInputDlg _processInput;
	private ProcessMessageDlg _processMessage;
	private ProcessOutputDlg _processOutput;
	private long _sampleTime;
	private bool _started;
	private IntPtr _transform;

	#endregion

	#region Properties

	public int DecodedFrames { get; private set; }

	public string LastError { get; private set; }

	public int Packets { get; private set; }

	public int PictureHeight { get; private set; }

	public int PictureWidth { get; private set; }

	#endregion

	#region Methods

	public bool CopyPixels(ref byte[] destination, out int width, out int height)
	{
		lock (_gate)
		{
			width = _decodeWidth;
			height = _decodeHeight;
			if ((_bgra == null) || (width <= 0) || (height <= 0))
			{
				return false;
			}

			var needed = width * height * 4;
			if (_copiedFrames == DecodedFrames)
			{
				return (destination != null) && (destination.Length >= needed);
			}

			if ((destination == null) || (destination.Length < needed))
			{
				destination = new byte[needed];
			}

			Buffer.BlockCopy(_bgra, 0, destination, 0, needed);
			_copiedFrames = DecodedFrames;
			return true;
		}
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
		{
			return;
		}

		lock (_gate)
		{
			ReleaseTransform();
		}
	}

	public void Reset()
	{
		lock (_gate)
		{
			if (_disposed != 0)
			{
				return;
			}

			ReleaseTransform();
			Packets = 0;
			DecodedFrames = 0;
			LastError = null;
			_bgra = null;
			_decodeWidth = 0;
			_decodeHeight = 0;
			PictureWidth = 0;
			PictureHeight = 0;
			_sampleTime = 0;
			_layoutCached = false;
			_copiedFrames = -1;
		}
	}

	public void Submit(H264Data data)
	{
		if ((_disposed != 0) || (data.Data == null) || (data.Length < 5))
		{
			return;
		}

		lock (_gate)
		{
			if (_disposed != 0)
			{
				return;
			}

			var width = data.Width > 0 ? data.Width : PictureWidth;
			var height = data.Height > 0 ? data.Height : PictureHeight;
			if ((width <= 0) || (height <= 0))
			{
				return;
			}

			Packets++;
			if (PictureChanged(width, height))
			{
				ReleaseTransform();
				_bgra = null;
				_decodeWidth = 0;
				_decodeHeight = 0;
				_layoutCached = false;
				LastError = null;
			}

			PictureWidth = width;
			PictureHeight = height;

			if (!_started && !TryStart())
			{
				return;
			}

			TryDecode(data.Data, data.Length);
		}
	}

	private static int ActivateObject(IntPtr activate, ref Guid iid, out IntPtr created)
	{
		return Fn<ActivateObjectDlg>(activate, 33)(activate, ref iid, out created);
	}

	private void BindTransform()
	{
		_getInputStatus = Fn<GetInputStatusDlg>(_transform, 19);
		_getOutputStreamInfo = Fn<GetOutputStreamInfoDlg>(_transform, 7);
		_getOutputCurrentType = Fn<GetOutputCurrentTypeDlg>(_transform, 18);
		_processMessage = Fn<ProcessMessageDlg>(_transform, 23);
		_processInput = Fn<ProcessInputDlg>(_transform, 24);
		_processOutput = Fn<ProcessOutputDlg>(_transform, 25);
	}

	private static int BufferLock(IntPtr buffer, out IntPtr data, out int maxLength, out int currentLength)
	{
		return Fn<LockDlg>(buffer, 3)(buffer, out data, out maxLength, out currentLength);
	}

	private static int BufferSetCurrentLength(IntPtr buffer, int length)
	{
		return Fn<SetCurrentLengthDlg>(buffer, 6)(buffer, length);
	}

	private static int BufferUnlock(IntPtr buffer)
	{
		return Fn<UnlockDlg>(buffer, 4)(buffer);
	}

	private static byte ClampToByte(int value)
	{
		if (value < 0)
		{
			return 0;
		}

		if (value > 255)
		{
			return 255;
		}

		return (byte) value;
	}

	[DllImport("ole32.dll", ExactSpelling = true, PreserveSig = true)]
	private static extern int CoCreateInstance(ref Guid clsid, IntPtr pUnkOuter, uint dwClsContext, ref Guid riid, out IntPtr ppv);

	[DllImport("ole32.dll", ExactSpelling = true, PreserveSig = true)]
	private static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

	private void ConfigureLowLatency()
	{
		if (TransformGetAttributes(_transform, out var attributes) < 0)
		{
			return;
		}

		try
		{
			SetUInt32(attributes, MfLowLatency, 1);
		}
		finally
		{
			Release(attributes);
		}
	}

	private bool CopySampleToBgra(IntPtr sample)
	{
		if (SampleConvertToContiguousBuffer(sample, out var contiguous) < 0)
		{
			return false;
		}

		try
		{
			if (BufferLock(contiguous, out var data, out _, out var currentLength) < 0)
			{
				return false;
			}

			try
			{
				ReadNv12Layout(currentLength, 0, out var codedHeight, out var stride, out var cropX, out var cropY, out var width, out var height);
				if ((width <= 0) || (height <= 0) || (codedHeight <= 0) || (currentLength <= 0))
				{
					return false;
				}

				Nv12ToBgra(data, stride, codedHeight, cropX, cropY, width, height);
				DecodedFrames++;
				return true;
			}
			finally
			{
				BufferUnlock(contiguous);
			}
		}
		finally
		{
			Release(contiguous);
		}
	}

	private static int CropToPicture(int coded, int picture)
	{
		if ((picture > 0) && (picture <= coded) && ((coded - picture) <= 64))
		{
			return picture;
		}

		return coded;
	}

	private bool DrainOutput()
	{
		var gotFrame = false;
		for (var n = 0; n < 8; n++)
		{
			TransformGetOutputStreamInfo(_transform, 0, out var info);
			var outSample = IntPtr.Zero;
			var outBuffer = IntPtr.Zero;
			if ((info.dwFlags & MftOutputStreamProvidesSamples) == 0)
			{
				if (MfCreateSample(out outSample) < 0)
				{
					return gotFrame;
				}

				var size = info.cbSize > 0 ? info.cbSize : PictureWidth * PictureHeight * 2;
				if (MfCreateMemoryBuffer(size, out outBuffer) < 0)
				{
					Release(outSample);
					return gotFrame;
				}

				SampleAddBuffer(outSample, outBuffer);
			}

			var output = new MftOutputDataBuffer
			{
				dwStreamID = 0,
				pSample = outSample,
				dwStatus = 0,
				pEvents = IntPtr.Zero
			};

			var hr = TransformProcessOutput(_transform, ref output);
			if (hr == MfETransformNeedMoreInput)
			{
				Release(outBuffer);
				Release(outSample);
				return gotFrame;
			}

			if (hr == MfETransformStreamChange)
			{
				Release(outBuffer);
				Release(outSample);
				_layoutCached = false;
				if (!TrySetNv12Output())
				{
					return gotFrame;
				}

				continue;
			}

			if (hr < 0)
			{
				LastError = "ProcessOutput 0x" + hr.ToString("X8");
				Release(outBuffer);
				Release(outSample);
				return gotFrame;
			}

			var frameSample = output.pSample != IntPtr.Zero ? output.pSample : outSample;
			if (frameSample != IntPtr.Zero)
			{
				gotFrame |= CopySampleToBgra(frameSample);
			}

			if ((output.pSample != IntPtr.Zero) && (output.pSample != outSample))
			{
				Release(output.pSample);
			}

			Release(outBuffer);
			Release(outSample);
		}

		return gotFrame;
	}

	private static T Fn<T>(IntPtr com, int slot) where T : Delegate
	{
		return Marshal.GetDelegateForFunctionPointer<T>(Vtable(com, slot));
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern IntPtr LoadLibrary(string lpFileName);

	[DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = true, EntryPoint = "MFCreateMediaType")]
	private static extern int MfCreateMediaType(out IntPtr ppMFType);

	[DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = true, EntryPoint = "MFCreateMemoryBuffer")]
	private static extern int MfCreateMemoryBuffer(int cbMaxLength, out IntPtr ppBuffer);

	[DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = true, EntryPoint = "MFCreateSample")]
	private static extern int MfCreateSample(out IntPtr ppIMFSample);

	[DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = true, EntryPoint = "MFStartup")]
	private static extern int MfStartup(int version, int dwFlags);

	[DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = true, EntryPoint = "MFTEnumEx")]
	private static extern int MftEnumEx(
		[MarshalAs(UnmanagedType.LPStruct)] Guid guidCategory,
		uint flags,
		ref MftRegisterTypeInfo pInputType,
		IntPtr pOutputType,
		out IntPtr pppMftActivate,
		out uint pnumMftActivate);

	private unsafe void Nv12ToBgra(IntPtr nv12, int stride, int codedHeight, int cropX, int cropY, int width, int height)
	{
		if ((width <= 0) || (height <= 0) || (width > 8192) || (height > 8192) || (stride < (cropX + width)) || (codedHeight < (cropY + height)))
		{
			return;
		}

		var bgraStride = width * 4;
		var needed = bgraStride * height;
		if ((_bgra == null) || (_bgra.Length < needed))
		{
			_bgra = new byte[needed];
		}

		var src = (byte*) nv12;
		var uvStart = stride * codedHeight;
		fixed (byte* dst = _bgra)
		{
			for (var y = 0; y < height; y++)
			{
				var yRow = src + ((cropY + y) * stride) + cropX;
				var uvRow = src + uvStart + (((cropY + y) / 2) * stride) + cropX;
				var dRow = dst + (y * bgraStride);
				var x = 0;
				for (; (x + 1) < width; x += 2)
				{
					var d = uvRow[x] - 128;
					var e = uvRow[x + 1] - 128;
					WriteBgra(dRow + (x * 4), yRow[x], d, e);
					WriteBgra(dRow + ((x + 1) * 4), yRow[x + 1], d, e);
				}

				if (x < width)
				{
					var d = uvRow[x & ~1] - 128;
					var e = uvRow[(x & ~1) + 1] - 128;
					WriteBgra(dRow + (x * 4), yRow[x], d, e);
				}
			}
		}
	}

	private static bool PictureChanged(int previousWidth, int previousHeight, int width, int height)
	{
		if ((previousWidth <= 0) || (previousHeight <= 0))
		{
			return false;
		}

		if ((previousHeight > previousWidth) != (height > width))
		{
			return true;
		}

		return (Math.Abs(width - previousWidth) > 64) || (Math.Abs(height - previousHeight) > 64);
	}

	private bool PictureChanged(int width, int height)
	{
		return PictureChanged(PictureWidth, PictureHeight, width, height);
	}

	private void ReadNv12Layout(int bufferLength, int pitch, out int codedHeight, out int stride, out int cropX, out int cropY, out int width, out int height)
	{
		if (_layoutCached && (pitch <= 0))
		{
			codedHeight = _cachedCodedHeight;
			stride = _cachedStride;
			cropX = _cachedCropX;
			cropY = _cachedCropY;
			width = _cachedWidth;
			height = _cachedHeight;
			return;
		}

		width = PictureWidth;
		height = PictureHeight;
		stride = PictureWidth;
		cropX = 0;
		cropY = 0;
		var codedWidth = width;
		codedHeight = height;
		var hasAperture = false;
		if (TransformGetOutputCurrentType(_transform, 0, out var type) >= 0)
		{
			try
			{
				if (TryGetUInt64(type, MfMtFrameSize, out var packed))
				{
					codedWidth = (int) (packed >> 32);
					codedHeight = (int) (packed & 0xFFFFFFFF);
					width = codedWidth;
					height = codedHeight;
				}

				if (TryGetUInt32(type, MfMtDefaultStride, out var s) && (s != 0))
				{
					stride = Math.Abs(s);
				}

				hasAperture = TryGetVideoArea(type, MfMtMinimumDisplayAperture, out cropX, out cropY, out width, out height)
					|| TryGetVideoArea(type, MfMtGeometricAperture, out cropX, out cropY, out width, out height);
			}
			finally
			{
				Release(type);
			}
		}

		if (pitch > 0)
		{
			stride = pitch;
		}
		else if ((codedHeight > 0) && (bufferLength >= (codedWidth * codedHeight)))
		{
			var fromSize = (int) ((bufferLength * 2L) / (codedHeight * 3L));
			if (fromSize >= codedWidth)
			{
				stride = fromSize;
			}
		}

		if (stride < codedWidth)
		{
			stride = codedWidth;
		}

		if ((codedWidth > 8192) || (codedHeight > 8192) || (codedWidth <= 0) || (codedHeight <= 0))
		{
			codedWidth = PictureWidth;
			codedHeight = PictureHeight;
			stride = Math.Max(stride, codedWidth);
			if (!hasAperture)
			{
				width = codedWidth;
				height = codedHeight;
			}
		}

		if (!hasAperture || ((width >= codedWidth) && (height >= codedHeight)))
		{
			width = CropToPicture(codedWidth, PictureWidth);
			height = CropToPicture(codedHeight, PictureHeight);
			cropX = 0;
			cropY = 0;
		}

		cropX &= ~1;
		cropY &= ~1;
		width &= ~1;
		height &= ~1;
		if (cropX < 0)
		{
			cropX = 0;
		}

		if (cropY < 0)
		{
			cropY = 0;
		}

		if ((cropX + width) > codedWidth)
		{
			width = Math.Max(0, codedWidth - cropX) & ~1;
		}

		if ((cropY + height) > codedHeight)
		{
			height = Math.Max(0, codedHeight - cropY) & ~1;
		}

		if ((width <= 0) || (height <= 0))
		{
			width = codedWidth & ~1;
			height = codedHeight & ~1;
			cropX = 0;
			cropY = 0;
		}

		_decodeWidth = width;
		_decodeHeight = height;
		_cachedCodedHeight = codedHeight;
		_cachedStride = stride;
		_cachedCropX = cropX;
		_cachedCropY = cropY;
		_cachedWidth = width;
		_cachedHeight = height;
		_layoutCached = true;
	}

	private static void Release(IntPtr p)
	{
		if (p != IntPtr.Zero)
		{
			Marshal.Release(p);
		}
	}

	private void ReleaseTransform()
	{
		Release(_transform);
		_transform = IntPtr.Zero;
		_started = false;
		_layoutCached = false;
		_getInputStatus = null;
		_getOutputStreamInfo = null;
		_getOutputCurrentType = null;
		_processMessage = null;
		_processInput = null;
		_processOutput = null;
	}

	private static int SampleAddBuffer(IntPtr sample, IntPtr buffer)
	{
		return Fn<AddBufferDlg>(sample, 42)(sample, buffer);
	}

	private static int SampleConvertToContiguousBuffer(IntPtr sample, out IntPtr buffer)
	{
		return Fn<ConvertToContiguousBufferDlg>(sample, 41)(sample, out buffer);
	}

	private static int SampleSetTime(IntPtr sample, long time)
	{
		return Fn<SetSampleTimeDlg>(sample, 36)(sample, time);
	}

	private static int SetGuid(IntPtr attributes, Guid key, Guid value)
	{
		return Fn<SetGuidDlg>(attributes, 24)(attributes, ref key, ref value);
	}

	private static int SetUInt32(IntPtr attributes, Guid key, int value)
	{
		return Fn<SetUInt32Dlg>(attributes, 21)(attributes, ref key, value);
	}

	private static int SetUInt64(IntPtr attributes, Guid key, ulong value)
	{
		return Fn<SetUInt64Dlg>(attributes, 22)(attributes, ref key, value);
	}

	private static int TransformGetAttributes(IntPtr transform, out IntPtr attributes)
	{
		return Fn<GetAttributesDlg>(transform, 8)(transform, out attributes);
	}

	private int TransformGetInputStatus(IntPtr transform, int stream, out int flags)
	{
		return _getInputStatus(transform, stream, out flags);
	}

	private static int TransformGetOutputAvailableType(IntPtr transform, int stream, int index, out IntPtr type)
	{
		return Fn<GetOutputAvailableTypeDlg>(transform, 14)(transform, stream, index, out type);
	}

	private int TransformGetOutputCurrentType(IntPtr transform, int stream, out IntPtr type)
	{
		return _getOutputCurrentType(transform, stream, out type);
	}

	private int TransformGetOutputStreamInfo(IntPtr transform, int stream, out MftOutputStreamInfo info)
	{
		return _getOutputStreamInfo(transform, stream, out info);
	}

	private int TransformProcessInput(IntPtr transform, int stream, IntPtr sample, int flags)
	{
		return _processInput(transform, stream, sample, flags);
	}

	private int TransformProcessMessage(IntPtr transform, int message, IntPtr param)
	{
		return _processMessage(transform, message, param);
	}

	private int TransformProcessOutput(IntPtr transform, ref MftOutputDataBuffer buffer)
	{
		var status = 0;
		return _processOutput(transform, 0, 1, ref buffer, out status);
	}

	private static int TransformSetInputType(IntPtr transform, int stream, IntPtr type, int flags)
	{
		return Fn<SetMediaTypeDlg>(transform, 15)(transform, stream, type, flags);
	}

	private static int TransformSetOutputType(IntPtr transform, int stream, IntPtr type, int flags)
	{
		return Fn<SetMediaTypeDlg>(transform, 16)(transform, stream, type, flags);
	}

	private bool TryCreateTransform(out int hr)
	{
		_transform = IntPtr.Zero;
		if (TryCreateTransformFromClsid(out hr) || TryCreateTransformFromEnum(out hr))
		{
			return _transform != IntPtr.Zero;
		}

		return false;
	}

	private bool TryCreateTransformFromClsid(out int hr)
	{
		hr = 0;
		try
		{
			LoadLibrary("mfplat.dll");
			LoadLibrary("mf.dll");
			LoadLibrary("msmpeg2vdec.dll");

			const uint clsContextInproc = 1;
			var clsid = ClsidCmsH264DecoderMft;
			var iidUnknown = IidIUnknown;

			// msmpeg2vdec on this OS returns E_NOINTERFACE for IID_IMFTransform.
			// The IUnknown identity vtable is the transform (GetStreamLimits lives at slot 3).
			hr = CoCreateInstance(ref clsid, IntPtr.Zero, clsContextInproc, ref iidUnknown, out _transform);
			return (hr >= 0) && (_transform != IntPtr.Zero);
		}
		catch (Exception ex)
		{
			LastError = ex.Message;
			hr = unchecked((int) 0x80004005);
			return false;
		}
	}

	private bool TryCreateTransformFromEnum(out int hr)
	{
		hr = 0;
		var input = new MftRegisterTypeInfo
		{
			guidMajorType = MfMediaTypeVideo,
			guidSubtype = MfVideoFormatH264
		};

		try
		{
			// MFT_ENUM_FLAG_SYNCMFT | LOCALMFT | SORTANDFILTER (software H.264 decoder)
			const uint enumFlags = 0x51;
			hr = MftEnumEx(MftCategoryVideoDecoder, enumFlags, ref input, IntPtr.Zero, out var activates, out var count);
			if ((hr < 0) || (count == 0) || (activates == IntPtr.Zero))
			{
				if (hr >= 0)
				{
					hr = unchecked((int) 0xC00D5212);
				}

				return false;
			}

			try
			{
				for (var i = 0; i < (int) count; i++)
				{
					var activate = Marshal.ReadIntPtr(activates, i * IntPtr.Size);
					if (activate == IntPtr.Zero)
					{
						continue;
					}

					var iid = IidIUnknown;
					hr = ActivateObject(activate, ref iid, out var transform);
					if ((hr >= 0) && (transform != IntPtr.Zero))
					{
						_transform = transform;
						return true;
					}
				}
			}
			finally
			{
				for (var i = 0; i < (int) count; i++)
				{
					var p = Marshal.ReadIntPtr(activates, i * IntPtr.Size);
					Release(p);
				}

				Marshal.FreeCoTaskMem(activates);
			}
		}
		catch (Exception ex)
		{
			LastError = ex.Message;
			hr = unchecked((int) 0x80004005);
			return false;
		}

		return false;
	}

	private bool TryDecode(byte[] annexB, int length)
	{
		var hr = MfCreateSample(out var sample);
		if (hr < 0)
		{
			LastError = "MFCreateSample 0x" + hr.ToString("X8");
			return false;
		}

		hr = MfCreateMemoryBuffer(length, out var buffer);
		if (hr < 0)
		{
			Release(sample);
			LastError = "MFCreateMemoryBuffer 0x" + hr.ToString("X8");
			return false;
		}

		try
		{
			hr = BufferLock(buffer, out var data, out _, out _);
			if (hr < 0)
			{
				LastError = "Lock 0x" + hr.ToString("X8");
				return false;
			}

			Marshal.Copy(annexB, 0, data, length);
			BufferUnlock(buffer);
			BufferSetCurrentLength(buffer, length);
			SampleAddBuffer(sample, buffer);
			SampleSetTime(sample, _sampleTime);
			_sampleTime += 333333;

			if ((TransformGetInputStatus(_transform, 0, out var status) >= 0)
				&& ((status & MftInputStatusAcceptData) == 0))
			{
				DrainOutput();
			}

			hr = TransformProcessInput(_transform, 0, sample, 0);
			if (hr == MfENotAccepting)
			{
				DrainOutput();
				hr = TransformProcessInput(_transform, 0, sample, 0);
			}

			if (hr == MfENotAccepting)
			{
				TransformProcessMessage(_transform, MftMessageCommandFlush, IntPtr.Zero);
				TransformProcessMessage(_transform, MftMessageNotifyBeginStreaming, IntPtr.Zero);
				TransformProcessMessage(_transform, MftMessageNotifyStartOfStream, IntPtr.Zero);
				hr = TransformProcessInput(_transform, 0, sample, 0);
			}

			if (hr < 0)
			{
				LastError = "ProcessInput 0x" + hr.ToString("X8");
				return false;
			}

			return DrainOutput();
		}
		finally
		{
			Release(buffer);
			Release(sample);
		}
	}

	private static bool TryGetGuid(IntPtr attributes, Guid key, out Guid value)
	{
		var hr = Fn<GetGuidDlg>(attributes, 10)(attributes, ref key, out value);
		return hr >= 0;
	}

	private static bool TryGetUInt32(IntPtr attributes, Guid key, out int value)
	{
		var hr = Fn<GetUInt32Dlg>(attributes, 7)(attributes, ref key, out value);
		return hr >= 0;
	}

	private static bool TryGetUInt64(IntPtr attributes, Guid key, out ulong value)
	{
		var hr = Fn<GetUInt64Dlg>(attributes, 8)(attributes, ref key, out value);
		return hr >= 0;
	}

	private static bool TryGetVideoArea(IntPtr attributes, Guid key, out int offsetX, out int offsetY, out int width, out int height)
	{
		offsetX = 0;
		offsetY = 0;
		width = 0;
		height = 0;
		var buffer = Marshal.AllocHGlobal(16);
		try
		{
			var hr = Fn<GetBlobDlg>(attributes, 15)(attributes, ref key, buffer, 16, out var size);
			if ((hr < 0) || (size < 16))
			{
				return false;
			}

			offsetX = Marshal.ReadInt16(buffer, 2);
			offsetY = Marshal.ReadInt16(buffer, 6);
			width = Marshal.ReadInt32(buffer, 8);
			height = Marshal.ReadInt32(buffer, 12);
			return (width > 0) && (height > 0);
		}
		finally
		{
			Marshal.FreeHGlobal(buffer);
		}
	}

	private bool TrySetH264Input()
	{
		if (TrySetH264Input(MfVideoFormatH264) || TrySetH264Input(MfVideoFormatH264Es))
		{
			return true;
		}

		return false;
	}

	private bool TrySetH264Input(Guid subtype)
	{
		var hr = MfCreateMediaType(out var inputType);
		if (hr < 0)
		{
			LastError = "MFCreateMediaType 0x" + hr.ToString("X8");
			return false;
		}

		try
		{
			SetGuid(inputType, MfMtMajorType, MfMediaTypeVideo);
			SetGuid(inputType, MfMtSubtype, subtype);
			hr = TransformSetInputType(_transform, 0, inputType, 0);
		}
		finally
		{
			Release(inputType);
		}

		if (hr < 0)
		{
			LastError = "SetInputType 0x" + hr.ToString("X8");
			return false;
		}

		return true;
	}

	private bool TrySetNv12Output()
	{
		var lastHr = unchecked((int) 0xC00D36B9);
		for (var i = 0; i < 32; i++)
		{
			var hr = TransformGetOutputAvailableType(_transform, 0, i, out var type);
			if (hr < 0)
			{
				lastHr = hr;
				break;
			}

			try
			{
				if (!TryGetGuid(type, MfMtSubtype, out var subtype) || (subtype != MfVideoFormatNv12))
				{
					continue;
				}

				// Use the type the decoder offered. Forcing session width/height here
				// returns MF_E_INVALIDMEDIATYPE after SPS (coded size != AirPlay size).
				hr = TransformSetOutputType(_transform, 0, type, 0);
				if (hr >= 0)
				{
					LastError = null;
					return true;
				}

				lastHr = hr;
			}
			finally
			{
				Release(type);
			}
		}

		LastError = "SetOutputType 0x" + lastHr.ToString("X8");
		return false;
	}

	private bool TryStart()
	{
		try
		{
			CoInitializeEx(IntPtr.Zero, 0);
			var hr = MfStartup(MfVersion, 0);
			if (hr < 0)
			{
				LastError = "MFStartup 0x" + hr.ToString("X8");
				return false;
			}

			if (_transform == IntPtr.Zero)
			{
				if (!TryCreateTransform(out hr))
				{
					LastError = "Create H.264 MFT 0x" + hr.ToString("X8");
					return false;
				}

				BindTransform();
				ConfigureLowLatency();
			}

			if (!TrySetH264Input())
			{
				return false;
			}

			if (!TrySetNv12Output())
			{
				return false;
			}

			TransformProcessMessage(_transform, MftMessageNotifyBeginStreaming, IntPtr.Zero);
			TransformProcessMessage(_transform, MftMessageNotifyStartOfStream, IntPtr.Zero);
			_sampleTime = 0;
			_started = true;
			LastError = null;
			return true;
		}
		catch (Exception ex)
		{
			LastError = ex.Message;
			ReleaseTransform();
			return false;
		}
	}

	private static IntPtr Vtable(IntPtr com, int slot)
	{
		var table = Marshal.ReadIntPtr(com);
		return Marshal.ReadIntPtr(table, slot * IntPtr.Size);
	}

	private static unsafe void WriteBgra(byte* o, int y, int d, int e)
	{
		var r = y + (((403 * e) + 128) >> 8);
		var g = y - (((48 * d) + (120 * e) + 128) >> 8);
		var b = y + (((475 * d) + 128) >> 8);
		o[0] = ClampToByte(b);
		o[1] = ClampToByte(g);
		o[2] = ClampToByte(r);
		o[3] = 255;
	}

	#endregion

	#region Structures

	[StructLayout(LayoutKind.Sequential)]
	private struct MftOutputStreamInfo
	{
		public int dwFlags;
		public int cbSize;
		public int cbAlignment;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct MftOutputDataBuffer
	{
		public int dwStreamID;
		public IntPtr pSample;
		public int dwStatus;
		public IntPtr pEvents;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct MftRegisterTypeInfo
	{
		public Guid guidMajorType;
		public Guid guidSubtype;
	}

	#endregion

	#region Delegates

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int ActivateObjectDlg(IntPtr self, ref Guid iid, out IntPtr created);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int AddBufferDlg(IntPtr self, IntPtr buffer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int ConvertToContiguousBufferDlg(IntPtr self, out IntPtr buffer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int GetAttributesDlg(IntPtr self, out IntPtr attributes);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int GetBlobDlg(IntPtr self, ref Guid key, IntPtr buffer, int bufferSize, out int blobSize);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int GetGuidDlg(IntPtr self, ref Guid key, out Guid value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int GetInputStatusDlg(IntPtr self, int stream, out int flags);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int GetOutputAvailableTypeDlg(IntPtr self, int stream, int index, out IntPtr type);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int GetOutputCurrentTypeDlg(IntPtr self, int stream, out IntPtr type);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int GetOutputStreamInfoDlg(IntPtr self, int stream, out MftOutputStreamInfo info);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int GetUInt32Dlg(IntPtr self, ref Guid key, out int value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int GetUInt64Dlg(IntPtr self, ref Guid key, out ulong value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int LockDlg(IntPtr self, out IntPtr data, out int maxLength, out int currentLength);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int ProcessInputDlg(IntPtr self, int stream, IntPtr sample, int flags);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int ProcessMessageDlg(IntPtr self, int message, IntPtr param);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int ProcessOutputDlg(IntPtr self, int flags, int count, ref MftOutputDataBuffer buffers, out int status);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int SetCurrentLengthDlg(IntPtr self, int length);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int SetGuidDlg(IntPtr self, ref Guid key, ref Guid value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int SetMediaTypeDlg(IntPtr self, int stream, IntPtr type, int flags);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int SetSampleTimeDlg(IntPtr self, long time);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int SetUInt32Dlg(IntPtr self, ref Guid key, int value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int SetUInt64Dlg(IntPtr self, ref Guid key, ulong value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int UnlockDlg(IntPtr self);

	#endregion
}
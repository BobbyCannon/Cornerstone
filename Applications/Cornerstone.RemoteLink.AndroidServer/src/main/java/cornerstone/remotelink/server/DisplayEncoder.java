package cornerstone.remotelink.server;

import java.io.OutputStream;
import java.lang.reflect.Field;
import java.lang.reflect.Method;
import java.nio.ByteBuffer;

/**
 * H.264 of the default display. Reflection so the jar builds on a desktop JDK.
 * Capture order matches scrcpy: DisplayManager.createVirtualDisplay(mirror id 0),
 * then SurfaceControl.createDisplay.
 */
public final class DisplayEncoder {
	private static final int COLOR_FORMAT_SURFACE = 0x7F000789;
	private static final int CONFIGURE_FLAG_ENCODE = 1;
	private static final int BUFFER_FLAG_CODEC_CONFIG = 2;
	private static final int INFO_TRY_AGAIN_LATER = -1;
	private static final long DEQUEUE_TIMEOUT_US = 0L;

	private final OutputStream out;
	private final int width;
	private final int height;
	private final int deviceWidth;
	private final int deviceHeight;
	private final int densityDpi;
	private final int layerStack;
	private volatile boolean running = true;

	private Object codec;
	private Object displayToken;
	private Object virtualDisplay;
	private Object surface;
	private Class<?> mediaCodecClass;

	public DisplayEncoder(OutputStream out, int width, int height) {
		DisplayMetrics metrics = DisplayMetrics.query();
		this.deviceWidth = metrics.width > 0 ? metrics.width : Math.max(width, 16);
		this.deviceHeight = metrics.height > 0 ? metrics.height : Math.max(height, 16);
		this.densityDpi = metrics.densityDpi > 0 ? metrics.densityDpi : 160;
		this.layerStack = metrics.layerStack;
		int encodeWidth = this.deviceWidth;
		int encodeHeight = this.deviceHeight;
		this.width = Math.max(16, encodeWidth & ~15);
		this.height = Math.max(16, encodeHeight & ~15);
		this.out = out;
	}

	public int getWidth() {
		return width;
	}

	public int getHeight() {
		return height;
	}

	public void stop() {
		running = false;
		release();
	}

	public void run() throws Exception {
		prepareLooper();
		mediaCodecClass = Class.forName("android.media.MediaCodec");
		Class<?> mediaFormatClass = Class.forName("android.media.MediaFormat");
		Class<?> bufferInfoClass = Class.forName("android.media.MediaCodec$BufferInfo");

		Object format = mediaFormatClass.getMethod("createVideoFormat", String.class, int.class, int.class)
			.invoke(null, "video/avc", Integer.valueOf(width), Integer.valueOf(height));
		Method setInt = mediaFormatClass.getMethod("setInteger", String.class, int.class);
		setInt.invoke(format, "bitrate", Integer.valueOf(bitrateFor(width, height)));
		setInt.invoke(format, "frame-rate", Integer.valueOf(30));
		setInt.invoke(format, "i-frame-interval", Integer.valueOf(1));
		setInt.invoke(format, "color-format", Integer.valueOf(COLOR_FORMAT_SURFACE));
		try {
			setInt.invoke(format, "latency", Integer.valueOf(0));
		} catch (Throwable ignored) {
		}
		try {
			setInt.invoke(format, "priority", Integer.valueOf(0));
		} catch (Throwable ignored) {
		}
		try {
			setInt.invoke(format, "max-bframes", Integer.valueOf(0));
		} catch (Throwable ignored) {
		}
		try {
			mediaFormatClass.getMethod("setLong", String.class, long.class)
				.invoke(format, "repeat-previous-frame-after", Long.valueOf(33_333L));
		} catch (Throwable ignored) {
		}

		codec = mediaCodecClass.getMethod("createEncoderByType", String.class).invoke(null, "video/avc");
		findConfigure(mediaCodecClass).invoke(codec, format, null, null, Integer.valueOf(CONFIGURE_FLAG_ENCODE));
		surface = mediaCodecClass.getMethod("createInputSurface").invoke(codec);

		if (!mirrorToSurface(surface)) {
			throw new IllegalStateException("Could not mirror the display onto the encoder surface.");
		}

		mediaCodecClass.getMethod("start").invoke(codec);
		System.out.println("DisplayEncoder: codec started " + width + "x" + height
			+ " device " + deviceWidth + "x" + deviceHeight + " dpi " + densityDpi + " layerStack " + layerStack);

		Object info = bufferInfoClass.getConstructor().newInstance();
		Method dequeue = mediaCodecClass.getMethod("dequeueOutputBuffer", bufferInfoClass, long.class);
		Method releaseBuffer = mediaCodecClass.getMethod("releaseOutputBuffer", int.class, boolean.class);
		Method getOutputBuffer = mediaCodecClass.getMethod("getOutputBuffer", int.class);
		Field sizeField = bufferInfoClass.getField("size");
		Field offsetField = bufferInfoClass.getField("offset");
		Field flagsField = bufferInfoClass.getField("flags");

		while (running) {
			int index = ((Integer) dequeue.invoke(codec, info, Long.valueOf(DEQUEUE_TIMEOUT_US))).intValue();
			if (index < 0) {
				continue;
			}
			try {
				int size = sizeField.getInt(info);
				int offset = offsetField.getInt(info);
				int flags = flagsField.getInt(info);
				if (size > 0) {
					ByteBuffer buffer = (ByteBuffer) getOutputBuffer.invoke(codec, Integer.valueOf(index));
					byte[] nal = new byte[size];
					int position = buffer.position();
					buffer.position(offset);
					buffer.get(nal);
					buffer.position(position);
					nal = toAnnexB(nal, flags);
					if (nal != null && nal.length >= 4) {
						Protocol.writeVideoPacket(out, nal);
					}
				}
			} finally {
				releaseBuffer.invoke(codec, Integer.valueOf(index), Boolean.FALSE);
			}
		}
	}

	private boolean mirrorToSurface(Object surface) {
		if (tryDisplayManagerMirror(surface)) {
			System.out.println("DisplayEncoder: DisplayManager mirror display 0");
			return true;
		}
		if (trySurfaceControl(surface)) {
			System.out.println("DisplayEncoder: SurfaceControl createDisplay");
			return true;
		}
		return false;
	}

	private boolean tryDisplayManagerMirror(Object surface) {
		try {
			Class<?> displayManager = Class.forName("android.hardware.display.DisplayManager");
			Class<?> surfaceClass = Class.forName("android.view.Surface");
			Method create = displayManager.getMethod("createVirtualDisplay",
				String.class, int.class, int.class, int.class, surfaceClass);
			virtualDisplay = create.invoke(null, "remotelink", Integer.valueOf(width), Integer.valueOf(height),
				Integer.valueOf(densityDpi), surface);
			return virtualDisplay != null;
		} catch (Throwable ex) {
			System.err.println("DisplayManager mirror failed: " + ex);
			virtualDisplay = null;
			return false;
		}
	}

	private boolean trySurfaceControl(Object surface) {
		try {
			Class<?> surfaceControl = Class.forName("android.view.SurfaceControl");
			Class<?> binderClass = Class.forName("android.os.IBinder");
			Class<?> rectClass = Class.forName("android.graphics.Rect");
			Class<?> surfaceClass = Class.forName("android.view.Surface");
			boolean secure = sdkInt() < 31;
			try {
				displayToken = surfaceControl.getMethod("createDisplay", String.class, boolean.class)
					.invoke(null, "remotelink", Boolean.valueOf(secure));
			} catch (Throwable first) {
				displayToken = surfaceControl.getMethod("createDisplay", String.class, boolean.class)
					.invoke(null, "remotelink", Boolean.valueOf(!secure));
			}
			Object deviceRect = rectClass.getConstructor(int.class, int.class, int.class, int.class)
				.newInstance(Integer.valueOf(0), Integer.valueOf(0), Integer.valueOf(deviceWidth), Integer.valueOf(deviceHeight));
			Object videoRect = rectClass.getConstructor(int.class, int.class, int.class, int.class)
				.newInstance(Integer.valueOf(0), Integer.valueOf(0), Integer.valueOf(width), Integer.valueOf(height));
			surfaceControl.getMethod("openTransaction").invoke(null);
			try {
				surfaceControl.getMethod("setDisplaySurface", binderClass, surfaceClass)
					.invoke(null, displayToken, surface);
				surfaceControl.getMethod("setDisplayProjection", binderClass, int.class, rectClass, rectClass)
					.invoke(null, displayToken, Integer.valueOf(0), deviceRect, videoRect);
				surfaceControl.getMethod("setDisplayLayerStack", binderClass, int.class)
					.invoke(null, displayToken, Integer.valueOf(layerStack));
			} finally {
				surfaceControl.getMethod("closeTransaction").invoke(null);
			}
			return true;
		} catch (Throwable ex) {
			System.err.println("SurfaceControl mirror failed: " + ex);
			displayToken = null;
			return false;
		}
	}

	private void release() {
		try {
			if (codec != null && mediaCodecClass != null) {
				try {
					mediaCodecClass.getMethod("stop").invoke(codec);
				} catch (Throwable ignored) {
				}
				try {
					mediaCodecClass.getMethod("release").invoke(codec);
				} catch (Throwable ignored) {
				}
			}
		} finally {
			codec = null;
		}
		if (displayToken != null) {
			try {
				Class.forName("android.view.SurfaceControl")
					.getMethod("destroyDisplay", Class.forName("android.os.IBinder"))
					.invoke(null, displayToken);
			} catch (Throwable ignored) {
			}
			displayToken = null;
		}
		if (virtualDisplay != null) {
			try {
				virtualDisplay.getClass().getMethod("release").invoke(virtualDisplay);
			} catch (Throwable ignored) {
			}
			virtualDisplay = null;
		}
		if (surface != null) {
			try {
				surface.getClass().getMethod("release").invoke(surface);
			} catch (Throwable ignored) {
			}
			surface = null;
		}
	}

	private static void prepareLooper() {
		try {
			Class<?> looper = Class.forName("android.os.Looper");
			if (looper.getMethod("myLooper").invoke(null) == null) {
				looper.getMethod("prepare").invoke(null);
			}
		} catch (Throwable ignored) {
		}
	}

	private static Method findConfigure(Class<?> mediaCodecClass) throws Exception {
		Class<?> format = Class.forName("android.media.MediaFormat");
		Class<?> surface = Class.forName("android.view.Surface");
		Class<?> crypto = Class.forName("android.media.MediaCrypto");
		return mediaCodecClass.getMethod("configure", format, surface, crypto, int.class);
	}

	private static int sdkInt() {
		try {
			return Class.forName("android.os.Build$VERSION").getField("SDK_INT").getInt(null);
		} catch (Throwable ex) {
			return 0;
		}
	}

	private static int bitrateFor(int w, int h) {
		int pixels = w * h;
		if (pixels >= 1920 * 1080) {
			return 10_000_000;
		}
		if (pixels >= 1280 * 720) {
			return 6_000_000;
		}
		return 2_500_000;
	}

	private static byte[] toAnnexB(byte[] data, int flags) {
		if (data == null || data.length < 4) {
			return data;
		}
		if (data[0] == 0 && data[1] == 0 && (data[2] == 1 || (data[2] == 0 && data[3] == 1))) {
			return data;
		}
		java.io.ByteArrayOutputStream annex = new java.io.ByteArrayOutputStream(data.length + 8);
		int offset = 0;
		while (offset + 4 <= data.length) {
			int nalLength = ((data[offset] & 0xFF) << 24)
				| ((data[offset + 1] & 0xFF) << 16)
				| ((data[offset + 2] & 0xFF) << 8)
				| (data[offset + 3] & 0xFF);
			offset += 4;
			if (nalLength <= 0 || offset + nalLength > data.length) {
				return prefixStartCode(data);
			}
			annex.write(0);
			annex.write(0);
			annex.write(0);
			annex.write(1);
			annex.write(data, offset, nalLength);
			offset += nalLength;
		}
		return annex.toByteArray();
	}

	private static byte[] prefixStartCode(byte[] data) {
		byte[] out = new byte[data.length + 4];
		out[0] = 0;
		out[1] = 0;
		out[2] = 0;
		out[3] = 1;
		System.arraycopy(data, 0, out, 4, data.length);
		return out;
	}

	private static final class DisplayMetrics {
		final int width;
		final int height;
		final int densityDpi;
		final int layerStack;

		DisplayMetrics(int width, int height, int densityDpi, int layerStack) {
			this.width = width;
			this.height = height;
			this.densityDpi = densityDpi;
			this.layerStack = layerStack;
		}

		static DisplayMetrics query() {
			try {
				Class<?> global = Class.forName("android.hardware.display.DisplayManagerGlobal");
				Object manager = global.getMethod("getInstance").invoke(null);
				Object info = manager.getClass().getMethod("getDisplayInfo", int.class).invoke(manager, Integer.valueOf(0));
				if (info == null) {
					return new DisplayMetrics(0, 0, 0, 0);
				}
				Class<?> cls = info.getClass();
				Field widthField = cls.getDeclaredField("logicalWidth");
				Field heightField = cls.getDeclaredField("logicalHeight");
				Field stackField = cls.getDeclaredField("layerStack");
				widthField.setAccessible(true);
				heightField.setAccessible(true);
				stackField.setAccessible(true);
				int width = widthField.getInt(info);
				int height = heightField.getInt(info);
				int layerStack = stackField.getInt(info);
				int dpi = 160;
				try {
					Field dpiField = cls.getDeclaredField("logicalDensityDpi");
					dpiField.setAccessible(true);
					dpi = dpiField.getInt(info);
				} catch (Throwable ignored) {
				}
				return new DisplayMetrics(width, height, dpi, layerStack);
			} catch (Throwable ex) {
				System.err.println("DisplayInfo query failed: " + ex);
				return new DisplayMetrics(0, 0, 0, 0);
			}
		}
	}
}

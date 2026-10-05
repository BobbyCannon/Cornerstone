package cornerstone.remotelink.server;

import java.io.DataInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.util.Arrays;

/**
 * Framing shared with Cornerstone.RemoteLink C# (RemoteLinkProtocol / RemoteLinkFraming).
 */
public final class Protocol {
	public static final byte[] MAGIC = new byte[] { 0x52, 0x4C, 0x4E, 0x4B };
	public static final int VERSION = 1;
	public static final int VIDEO_PORT = 27182;
	public static final int CONTROL_PORT = 27183;
	public static final int HEADER_LENGTH = 11;

	public static final byte MESSAGE_HELLO = 1;
	public static final byte MESSAGE_VIDEO_HEADER = 2;
	public static final byte MESSAGE_VIDEO_PACKET = 3;
	public static final byte MESSAGE_CONTROL = 4;

	public static final byte CONTROL_TOUCH_DOWN = 1;
	public static final byte CONTROL_TOUCH_MOVE = 2;
	public static final byte CONTROL_TOUCH_UP = 3;
	public static final byte CONTROL_KEY = 4;
	public static final byte CONTROL_BACK = 5;
	public static final byte CONTROL_HOME = 6;
	public static final byte CONTROL_APP_SWITCH = 7;
	public static final byte CONTROL_TEXT = 8;

	public static final int KEY_BACK = 4;
	public static final int KEY_HOME = 3;
	public static final int KEY_APP_SWITCH = 187;

	public static final int FLAG_VIDEO = 1;

	private Protocol() {
	}

	public static void writeMessage(OutputStream raw, byte type, byte[] payload) throws IOException {
		writeMessage(raw, type, payload, true);
	}

	public static void writeMessage(OutputStream raw, byte type, byte[] payload, boolean flush) throws IOException {
		if (payload == null) {
			payload = new byte[0];
		}
		synchronized (raw) {
			raw.write(MAGIC);
			writeShort(raw, VERSION);
			raw.write(type);
			writeInt(raw, payload.length);
			if (payload.length > 0) {
				raw.write(payload);
			}
			if (flush) {
				raw.flush();
			}
		}
	}

	public static void writeHello(OutputStream raw, int width, int height, int flags) throws IOException {
		byte[] payload = new byte[6];
		payload[0] = (byte) ((width >> 8) & 0xFF);
		payload[1] = (byte) (width & 0xFF);
		payload[2] = (byte) ((height >> 8) & 0xFF);
		payload[3] = (byte) (height & 0xFF);
		payload[4] = (byte) ((flags >> 8) & 0xFF);
		payload[5] = (byte) (flags & 0xFF);
		writeMessage(raw, MESSAGE_HELLO, payload);
	}

	public static void writeVideoPacket(OutputStream raw, byte[] nal) throws IOException {
		writeMessage(raw, MESSAGE_VIDEO_PACKET, nal, false);
	}

	public static Frame readMessage(InputStream raw) throws IOException {
		DataInputStream in = new DataInputStream(raw);
		byte[] magic = new byte[MAGIC.length];
		in.readFully(magic);
		if (!Arrays.equals(MAGIC, magic)) {
			throw new IOException("Not a RemoteLink frame.");
		}
		int version = in.readUnsignedShort();
		if (version != VERSION) {
			throw new IOException("RemoteLink version " + version + " is not supported.");
		}
		int type = in.readUnsignedByte();
		int length = in.readInt();
		if (length < 0 || length > 8 * 1024 * 1024) {
			throw new IOException("RemoteLink payload is too large.");
		}
		byte[] payload = new byte[length];
		if (length > 0) {
			in.readFully(payload);
		}
		return new Frame((byte) type, payload);
	}

	private static void writeShort(OutputStream raw, int value) throws IOException {
		raw.write((value >> 8) & 0xFF);
		raw.write(value & 0xFF);
	}

	private static void writeInt(OutputStream raw, int value) throws IOException {
		raw.write((value >> 24) & 0xFF);
		raw.write((value >> 16) & 0xFF);
		raw.write((value >> 8) & 0xFF);
		raw.write(value & 0xFF);
	}

	public static final class Frame {
		public final byte type;
		public final byte[] payload;

		public Frame(byte type, byte[] payload) {
			this.type = type;
			this.payload = payload;
		}
	}
}

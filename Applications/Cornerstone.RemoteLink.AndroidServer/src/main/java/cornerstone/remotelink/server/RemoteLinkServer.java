package cornerstone.remotelink.server;

import java.io.InputStream;
import java.io.OutputStream;
import java.net.InetAddress;
import java.net.Socket;

/**
 * Connects to the PC over adb reverse, sends Hello, encodes H.264, reads control.
 *
 * java -jar remotelink-server.jar --version
 * app_process ... RemoteLinkServer &lt;width&gt; &lt;height&gt;
 */
public final class RemoteLinkServer {
	public static void main(String[] args) {
		if (args != null) {
			for (int i = 0; i < args.length; i++) {
				if ("--version".equals(args[i])) {
					System.out.println("remotelink-server " + Protocol.VERSION);
					System.out.println("video " + Protocol.VIDEO_PORT);
					System.out.println("control " + Protocol.CONTROL_PORT);
					return;
				}
			}
		}

		int width = parseArg(args, 0, 0);
		int height = parseArg(args, 1, 0);
		String host = "127.0.0.1";
		int port = Protocol.VIDEO_PORT;
		ControlInjector injector = new ControlInjector();
		DisplayEncoder encoder = null;
		try (Socket socket = new Socket(InetAddress.getByName(host), port)) {
			socket.setTcpNoDelay(true);
			OutputStream out = socket.getOutputStream();
			InputStream in = socket.getInputStream();
			int flags = 0;
			if (width > 0 && height > 0) {
				try {
					encoder = new DisplayEncoder(out, width, height);
					flags = Protocol.FLAG_VIDEO;
				} catch (Throwable ex) {
					System.err.println("DisplayEncoder setup: " + ex);
					encoder = null;
				}
			}
			Protocol.writeHello(out, encoder != null ? encoder.getWidth() : width, encoder != null ? encoder.getHeight() : height, flags);
			System.out.println("Hello " + width + "x" + height + " flags=" + flags);
			if (encoder != null) {
				final DisplayEncoder running = encoder;
				Thread video = new Thread(new Runnable() {
					@Override
					public void run() {
						try {
							running.run();
						} catch (Throwable ex) {
							System.err.println("DisplayEncoder: " + ex);
						}
					}
				}, "remotelink-video");
				video.setDaemon(true);
				video.start();
			}
			while (true) {
				Protocol.Frame frame = Protocol.readMessage(in);
				if (frame.type != Protocol.MESSAGE_CONTROL || frame.payload.length < 1) {
					continue;
				}
				injector.handle(frame.payload[0], frame.payload);
			}
		} catch (Exception ex) {
			System.err.println("remotelink-server: " + ex.getMessage());
			if (encoder != null) {
				encoder.stop();
			}
			System.exit(2);
		}
	}

	private static int parseArg(String[] args, int index, int fallback) {
		if (args == null || index >= args.length) {
			return fallback;
		}
		try {
			return Integer.parseInt(args[index]);
		} catch (NumberFormatException ex) {
			return fallback;
		}
	}
}

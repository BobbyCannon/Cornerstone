package cornerstone.remotelink.server;

import java.io.IOException;
import java.lang.reflect.Method;
import java.nio.charset.StandardCharsets;

/**
 * Injects keys and taps. Prefers InputManager (no process spawn); falls back to `input`.
 */
public final class ControlInjector {
	private static final int ACTION_DOWN = 0;
	private static final int ACTION_UP = 1;
	private static final int ACTION_MOVE = 2;
	private static final int INJECT_ASYNC = 0;

	private long downTime;
	private Method injectEvent;
	private Object inputManager;

	public void handle(byte type, byte[] payload) throws IOException, InterruptedException {
		int x = 0;
		int y = 0;
		if (payload != null && payload.length >= 5) {
			x = ((payload[1] & 0xFF) << 8) | (payload[2] & 0xFF);
			y = ((payload[3] & 0xFF) << 8) | (payload[4] & 0xFF);
		}
		switch (type) {
			case Protocol.CONTROL_BACK:
				key(Protocol.KEY_BACK);
				break;
			case Protocol.CONTROL_HOME:
				key(Protocol.KEY_HOME);
				break;
			case Protocol.CONTROL_APP_SWITCH:
				key(Protocol.KEY_APP_SWITCH);
				break;
			case Protocol.CONTROL_KEY:
				if (x > 0) {
					key(x);
				}
				break;
			case Protocol.CONTROL_TEXT:
				if (payload != null && payload.length > 1) {
					text(new String(payload, 1, payload.length - 1, StandardCharsets.UTF_8));
				}
				break;
			case Protocol.CONTROL_TOUCH_DOWN:
				touch(ACTION_DOWN, x, y);
				break;
			case Protocol.CONTROL_TOUCH_MOVE:
				touch(ACTION_MOVE, x, y);
				break;
			case Protocol.CONTROL_TOUCH_UP:
				touch(ACTION_UP, x, y);
				break;
			default:
				break;
		}
	}

	private void touch(int action, int x, int y) throws IOException {
		if (tryInjectMotion(action, x, y)) {
			return;
		}
		if (action == ACTION_UP) {
			new ProcessBuilder("input", "tap", Integer.toString(x), Integer.toString(y)).start();
		}
	}

	private boolean tryInjectMotion(int action, int x, int y) {
		try {
			if (!ensureInputManager()) {
				return false;
			}
			Class<?> motionEvent = Class.forName("android.view.MotionEvent");
			Class<?> systemClock = Class.forName("android.os.SystemClock");
			long now = ((Long) systemClock.getMethod("uptimeMillis").invoke(null)).longValue();
			if (action == ACTION_DOWN) {
				downTime = now;
			}
			if (downTime == 0L) {
				downTime = now;
			}
			Object event = motionEvent.getMethod("obtain", long.class, long.class, int.class, float.class, float.class, int.class)
				.invoke(null, Long.valueOf(downTime), Long.valueOf(now), Integer.valueOf(action),
					Float.valueOf(x), Float.valueOf(y), Integer.valueOf(0));
			try {
				injectEvent.invoke(inputManager, event, Integer.valueOf(INJECT_ASYNC));
			} finally {
				motionEvent.getMethod("recycle").invoke(event);
			}
			if (action == ACTION_UP) {
				downTime = 0L;
			}
			return true;
		} catch (Throwable ex) {
			injectEvent = null;
			inputManager = null;
			return false;
		}
	}

	private boolean ensureInputManager() {
		if (injectEvent != null && inputManager != null) {
			return true;
		}
		try {
			Class<?> inputManagerClass = Class.forName("android.hardware.input.InputManager");
			inputManager = inputManagerClass.getMethod("getInstance").invoke(null);
			Class<?> inputEvent = Class.forName("android.view.InputEvent");
			injectEvent = inputManagerClass.getMethod("injectInputEvent", inputEvent, int.class);
			return inputManager != null && injectEvent != null;
		} catch (Throwable ex) {
			return false;
		}
	}

	private static void key(int code) throws IOException {
		new ProcessBuilder("input", "keyevent", Integer.toString(code)).start();
	}

	private static void text(String value) throws IOException, InterruptedException {
		if (value == null || value.length() == 0) {
			return;
		}
		int start = 0;
		for (int i = 0; i <= value.length(); i++) {
			char c = i < value.length() ? value.charAt(i) : '\n';
			if (c != '\n' && c != '\r') {
				continue;
			}
			if (i > start) {
				new ProcessBuilder("input", "text", escapeInputText(value.substring(start, i))).start().waitFor();
			}
			if (i < value.length()) {
				key(66);
			}
			start = i + 1;
			if (c == '\r' && start < value.length() && value.charAt(start) == '\n') {
				start++;
				i++;
			}
		}
	}

	private static String escapeInputText(String value) {
		StringBuilder builder = new StringBuilder(value.length());
		for (int i = 0; i < value.length(); i++) {
			char c = value.charAt(i);
			if (c == ' ') {
				builder.append("%s");
			} else if (c == '%' || c == '"' || c == '\'' || c == '\\' || c == '&' || c == '<' || c == '>' || c == ';' || c == '(' || c == ')' || c == '|') {
				builder.append('\\').append(c);
			} else {
				builder.append(c);
			}
		}
		return builder.toString();
	}
}

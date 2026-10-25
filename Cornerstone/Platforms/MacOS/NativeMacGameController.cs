#region References

using System;
using System.Runtime.InteropServices;

#endregion

namespace Cornerstone.Platforms.MacOS;

/// <summary>
/// GameController.framework through the Objective-C runtime. Polling copies values out before the autorelease pool drains.
/// </summary>
internal static class NativeMacGameController
{
	#region Fields

	private static readonly object Gate;
	private static IntPtr _alloc;
	private static IntPtr _buttonA;
	private static IntPtr _buttonB;
	private static IntPtr _buttonMenu;
	private static IntPtr _buttonOptions;
	private static IntPtr _buttonX;
	private static IntPtr _buttonY;
	private static IntPtr _controllers;
	private static IntPtr _count;
	private static IntPtr _down;
	private static IntPtr _dpad;
	private static IntPtr _drain;
	private static bool _failed;
	private static IntPtr _gamepad;
	private static GetClassDelegate _getClass;
	private static IntPtr _init;
	private static IntPtr _isPressed;
	private static IntPtr _left;
	private static IntPtr _leftShoulder;
	private static IntPtr _leftThumbstick;
	private static IntPtr _leftThumbstickButton;
	private static IntPtr _leftTrigger;
	private static bool _loaded;
	private static IntPtr _objectAtIndex;
	private static IntPtr _right;
	private static IntPtr _rightShoulder;
	private static IntPtr _rightThumbstick;
	private static IntPtr _rightThumbstickButton;
	private static IntPtr _rightTrigger;
	private static SelRegisterNameDelegate _selRegisterName;
	private static MsgSendByteDelegate _sendByte;
	private static MsgSendDoubleDelegate _sendDouble;
	private static MsgSendFloatDelegate _sendFloat;
	private static MsgSendNuintDelegate _sendNuint;
	private static MsgSendPtrDelegate _sendPtr;
	private static MsgSendPtrNuintDelegate _sendPtrNuint;
	private static IntPtr _up;
	private static bool _useFpret;
	private static IntPtr _value;
	private static IntPtr _xAxis;
	private static IntPtr _yAxis;
	private static IntPtr _extendedGamepad;

	#endregion

	#region Constructors

	static NativeMacGameController()
	{
		Gate = new object();
		RtldDefault = new IntPtr(-1);
	}

	#endregion

	#region Methods

	internal static bool TryRead(int index, out ControllerSample sample)
	{
		sample = default;
		if ((index < 0) || !EnsureLoaded())
		{
			return false;
		}

		var pool = PushPool();
		try
		{
			return TryReadInside(index, out sample);
		}
		finally
		{
			PopPool(pool);
		}
	}

	private static bool EnsureLoaded()
	{
		if (_loaded)
		{
			return true;
		}

		if (_failed)
		{
			return false;
		}

		lock (Gate)
		{
			if (_loaded)
			{
				return true;
			}

			if (_failed)
			{
				return false;
			}

			try
			{
				dlerror();
				if (dlopen("/usr/lib/libobjc.A.dylib", RtldLazy) == IntPtr.Zero)
				{
					Fail("GameController could not load libobjc. " + LastDlError());
					return false;
				}

				if (dlopen("/System/Library/Frameworks/GameController.framework/GameController", RtldLazy) == IntPtr.Zero)
				{
					Fail("GameController.framework did not load. " + LastDlError());
					return false;
				}

				var send = dlsym(RtldDefault, "objc_msgSend");
				var getClass = dlsym(RtldDefault, "objc_getClass");
				var registerName = dlsym(RtldDefault, "sel_registerName");
				if ((send == IntPtr.Zero) || (getClass == IntPtr.Zero) || (registerName == IntPtr.Zero))
				{
					Fail("GameController could not resolve the Objective-C runtime. " + LastDlError());
					return false;
				}

				_useFpret = RuntimeInformation.ProcessArchitecture == Architecture.X64
					|| RuntimeInformation.ProcessArchitecture == Architecture.X86;
				_sendPtr = Marshal.GetDelegateForFunctionPointer<MsgSendPtrDelegate>(send);
				_sendPtrNuint = Marshal.GetDelegateForFunctionPointer<MsgSendPtrNuintDelegate>(send);
				_sendNuint = Marshal.GetDelegateForFunctionPointer<MsgSendNuintDelegate>(send);
				_sendByte = Marshal.GetDelegateForFunctionPointer<MsgSendByteDelegate>(send);
				if (_useFpret)
				{
					var sendFpret = dlsym(RtldDefault, "objc_msgSend_fpret");
					if (sendFpret == IntPtr.Zero)
					{
						Fail("GameController could not resolve objc_msgSend_fpret. " + LastDlError());
						return false;
					}

					_sendDouble = Marshal.GetDelegateForFunctionPointer<MsgSendDoubleDelegate>(sendFpret);
				}
				else
				{
					_sendFloat = Marshal.GetDelegateForFunctionPointer<MsgSendFloatDelegate>(send);
				}

				_getClass = Marshal.GetDelegateForFunctionPointer<GetClassDelegate>(getClass);
				_selRegisterName = Marshal.GetDelegateForFunctionPointer<SelRegisterNameDelegate>(registerName);
				_alloc = Selector("alloc");
				_init = Selector("init");
				_drain = Selector("drain");
				_controllers = Selector("controllers");
				_count = Selector("count");
				_objectAtIndex = Selector("objectAtIndex:");
				_extendedGamepad = Selector("extendedGamepad");
				_gamepad = Selector("gamepad");
				_buttonA = Selector("buttonA");
				_buttonB = Selector("buttonB");
				_buttonX = Selector("buttonX");
				_buttonY = Selector("buttonY");
				_leftShoulder = Selector("leftShoulder");
				_rightShoulder = Selector("rightShoulder");
				_buttonMenu = Selector("buttonMenu");
				_buttonOptions = Selector("buttonOptions");
				_leftThumbstickButton = Selector("leftThumbstickButton");
				_rightThumbstickButton = Selector("rightThumbstickButton");
				_leftTrigger = Selector("leftTrigger");
				_rightTrigger = Selector("rightTrigger");
				_leftThumbstick = Selector("leftThumbstick");
				_rightThumbstick = Selector("rightThumbstick");
				_dpad = Selector("dpad");
				_up = Selector("up");
				_down = Selector("down");
				_left = Selector("left");
				_right = Selector("right");
				_xAxis = Selector("xAxis");
				_yAxis = Selector("yAxis");
				_isPressed = Selector("isPressed");
				_value = Selector("value");
				_loaded = true;
				return true;
			}
			catch (Exception ex)
			{
				Fail(ex.Message);
				return false;
			}
		}
	}

	private static void Fail(string message)
	{
		_failed = true;
		Console.WriteLine(message);
	}

	private static string LastDlError()
	{
		var pointer = dlerror();
		if (pointer == IntPtr.Zero)
		{
			return string.Empty;
		}

		return Marshal.PtrToStringUTF8(pointer) ?? string.Empty;
	}

	private static void PopPool(IntPtr pool)
	{
		if (pool != IntPtr.Zero)
		{
			_sendPtr(pool, _drain);
		}
	}

	private static bool Pressed(IntPtr owner, IntPtr childSelector)
	{
		if (owner == IntPtr.Zero)
		{
			return false;
		}

		var input = _sendPtr(owner, childSelector);
		if (input == IntPtr.Zero)
		{
			return false;
		}

		return _sendByte(input, _isPressed) != 0;
	}

	private static IntPtr PushPool()
	{
		var poolClass = _getClass("NSAutoreleasePool");
		var allocated = _sendPtr(poolClass, _alloc);
		return _sendPtr(allocated, _init);
	}

	private static ControllerSample ReadProfile(IntPtr profile, bool extended)
	{
		var sample = new ControllerSample();
		sample.Found = true;
		sample.A = Pressed(profile, _buttonA);
		sample.B = Pressed(profile, _buttonB);
		sample.X = Pressed(profile, _buttonX);
		sample.Y = Pressed(profile, _buttonY);
		sample.LeftBumper = Pressed(profile, _leftShoulder);
		sample.RightBumper = Pressed(profile, _rightShoulder);
		var dpad = _sendPtr(profile, _dpad);
		sample.Up = Pressed(dpad, _up);
		sample.Down = Pressed(dpad, _down);
		sample.Left = Pressed(dpad, _left);
		sample.Right = Pressed(dpad, _right);
		if (!extended)
		{
			return sample;
		}

		sample.Back = Pressed(profile, _buttonOptions);
		sample.Start = Pressed(profile, _buttonMenu);
		sample.LeftStick = Pressed(profile, _leftThumbstickButton);
		sample.RightStick = Pressed(profile, _rightThumbstickButton);
		var leftStick = _sendPtr(profile, _leftThumbstick);
		var rightStick = _sendPtr(profile, _rightThumbstick);
		sample.LeftX = Value(_sendPtr(leftStick, _xAxis));
		sample.LeftY = Value(_sendPtr(leftStick, _yAxis));
		sample.RightX = Value(_sendPtr(rightStick, _xAxis));
		sample.RightY = Value(_sendPtr(rightStick, _yAxis));
		sample.LeftTrigger = Value(_sendPtr(profile, _leftTrigger));
		sample.RightTrigger = Value(_sendPtr(profile, _rightTrigger));
		return sample;
	}

	private static IntPtr Selector(string name)
	{
		var selector = _selRegisterName(name);
		if (selector == IntPtr.Zero)
		{
			throw new InvalidOperationException("Could not register selector " + name + ".");
		}

		return selector;
	}

	private static float SendFloat(IntPtr receiver, IntPtr selector)
	{
		if (_useFpret)
		{
			return (float) _sendDouble(receiver, selector);
		}

		return _sendFloat(receiver, selector);
	}

	private static bool TryReadInside(int index, out ControllerSample sample)
	{
		sample = default;
		var list = _sendPtr(_getClass("GCController"), _controllers);
		var count = _sendNuint(list, _count);
		var seen = 0;
		for (nuint i = 0; i < count; i++)
		{
			var controller = _sendPtrNuint(list, _objectAtIndex, i);
			if (controller == IntPtr.Zero)
			{
				continue;
			}

			var extended = _sendPtr(controller, _extendedGamepad);
			var basic = extended == IntPtr.Zero ? _sendPtr(controller, _gamepad) : IntPtr.Zero;
			if ((extended == IntPtr.Zero) && (basic == IntPtr.Zero))
			{
				continue;
			}

			if (seen != index)
			{
				seen++;
				continue;
			}

			sample = ReadProfile(extended != IntPtr.Zero ? extended : basic, extended != IntPtr.Zero);
			return true;
		}

		return false;
	}

	private static float Value(IntPtr input)
	{
		if (input == IntPtr.Zero)
		{
			return 0;
		}

		return SendFloat(input, _value);
	}

	[DllImport("/usr/lib/libSystem.B.dylib", EntryPoint = "dlerror")]
	private static extern IntPtr dlerror();

	[DllImport("/usr/lib/libSystem.B.dylib", EntryPoint = "dlopen")]
	private static extern IntPtr dlopen([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int mode);

	[DllImport("/usr/lib/libSystem.B.dylib", EntryPoint = "dlsym")]
	private static extern IntPtr dlsym(IntPtr handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string symbol);

	#endregion

	#region Structures

	internal struct ControllerSample
	{
		public bool A;
		public bool B;
		public bool Back;
		public bool Down;
		public bool Found;
		public bool Left;
		public bool LeftBumper;
		public bool LeftStick;
		public float LeftTrigger;
		public float LeftX;
		public float LeftY;
		public bool Right;
		public bool RightBumper;
		public bool RightStick;
		public float RightTrigger;
		public float RightX;
		public float RightY;
		public bool Start;
		public bool Up;
		public bool X;
		public bool Y;
	}

	#endregion

	#region Delegates

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate IntPtr GetClassDelegate([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate byte MsgSendByteDelegate(IntPtr receiver, IntPtr selector);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate double MsgSendDoubleDelegate(IntPtr receiver, IntPtr selector);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate float MsgSendFloatDelegate(IntPtr receiver, IntPtr selector);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate nuint MsgSendNuintDelegate(IntPtr receiver, IntPtr selector);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate IntPtr MsgSendPtrDelegate(IntPtr receiver, IntPtr selector);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate IntPtr MsgSendPtrNuintDelegate(IntPtr receiver, IntPtr selector, nuint arg);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate IntPtr SelRegisterNameDelegate([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

	#endregion

	#region Constants

	private const int RtldLazy = 1;
	private static readonly IntPtr RtldDefault;

	#endregion
}

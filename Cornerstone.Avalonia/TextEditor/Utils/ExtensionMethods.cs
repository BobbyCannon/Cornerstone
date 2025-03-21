﻿#region References

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Drawing;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Reactive;
using Avalonia.VisualTree;
using Point = Avalonia.Point;
using Size = Avalonia.Size;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Utils;

public static class ExtensionMethods
{
	#region Constants

	/// <summary>
	/// Epsilon used for <c> IsClose() </c> implementations.
	/// We can use up quite a few digits in front of the decimal point (due to visual positions being relative to document origin),
	/// and there's no need to be too accurate (we're dealing with pixels here),
	/// so we will use the value 0.01.
	/// Previosly we used 1e-8 but that was causing issues:
	/// https://community.sharpdevelop.net/forums/t/16048.aspx
	/// </summary>
	public const double Epsilon = 0.01;

	#endregion

	#region Methods

	public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> elements)
	{
		foreach (var e in elements)
		{
			collection.Add(e);
		}
	}
	
	public static IEnumerable<char> AsEnumerable(this string s)
	{
		// ReSharper disable once ForCanBeConvertedToForeach
		for (var i = 0; i < s.Length; i++)
		{
			yield return s[i];
		}
	}

	public static bool CapturePointer(this IInputElement element, IPointer device)
	{
		device.Capture(element);
		return device.Captured == element;
	}

	[Conditional("DEBUG")]
	public static void CheckIsFrozen(object o)
	{
		if (o is IFreezable f && !f.IsFrozen)
		{
			Debug.WriteLine("Performance warning: Not frozen: " + f);
		}
	}

	/// <summary>
	/// Forces the value to stay between minimum and maximum.
	/// </summary>
	/// <returns>
	/// minimum, if value is less than minimum.
	/// Maximum, if value is greater than maximum.
	/// Otherwise, value.
	/// </returns>
	public static double CoerceValue(this double value, double minimum, double maximum)
	{
		return Math.Max(Math.Min(value, maximum), minimum);
	}

	/// <summary>
	/// Forces the value to stay between minimum and maximum.
	/// </summary>
	/// <returns>
	/// minimum, if value is less than minimum.
	/// Maximum, if value is greater than maximum.
	/// Otherwise, value.
	/// </returns>
	public static int CoerceValue(this int value, int minimum, int maximum)
	{
		return Math.Max(Math.Min(value, maximum), minimum);
	}

	/// <summary>
	/// Creates typeface from the framework element.
	/// </summary>
	public static Typeface CreateTypeface(this Control fe)
	{
		return new Typeface(
			fe.GetValue(TextElement.FontFamilyProperty),
			fe.GetValue(TextElement.FontStyleProperty),
			fe.GetValue(TextElement.FontWeightProperty),
			fe.GetValue(TextElement.FontStretchProperty)
		);
	}
	///// <summary>
	///// Gets the value of the attribute, or null if the attribute does not exist.
	///// </summary>
	//public static string GetAttributeOrNull(this XmlElement element, string attributeName)
	//{
	//    XmlAttribute attr = element.GetAttributeNode(attributeName);
	//    return attr != null ? attr.Value : null;
	//}

	///// <summary>
	///// Gets the value of the attribute as boolean, or null if the attribute does not exist.
	///// </summary>
	//public static bool? GetBoolAttribute(this XmlElement element, string attributeName)
	//{
	//    XmlAttribute attr = element.GetAttributeNode(attributeName);
	//    return attr != null ? (bool?)XmlConvert.ToBoolean(attr.Value) : null;
	//}

	/// <summary>
	/// Gets the value of the attribute as boolean, or null if the attribute does not exist.
	/// </summary>
	public static bool? GetBoolAttribute(this XmlReader reader, string attributeName)
	{
		var attributeValue = reader.GetAttribute(attributeName);
		if (attributeValue == null)
		{
			return null;
		}
		return XmlConvert.ToBoolean(attributeValue);
	}

	public static IList<T> GetRange<T>(this IList<T> collection, int start, int count)
	{
		var response = new List<T>();
		var end = start + count;
		for (var i = start; i < end; i++)
		{
			response.Add(collection[i]);
		}
		return response;
	}

	/// <summary>
	/// Returns true if the doubles are close (difference smaller than 0.01).
	/// </summary>
	public static bool IsClose(this double d1, double d2)
	{
		// ReSharper disable once CompareOfFloatsByEqualityOperator
		if (d1 == d2) // required for infinities
		{
			return true;
		}
		return Math.Abs(d1 - d2) < Epsilon;
	}

	/// <summary>
	/// Returns true if the doubles are close (difference smaller than 0.01).
	/// </summary>
	public static bool IsClose(this Size d1, Size d2)
	{
		return IsClose(d1.Width, d2.Width) && IsClose(d1.Height, d2.Height);
	}

	/// <summary>
	/// Returns true if the doubles are close (difference smaller than 0.01).
	/// </summary>
	public static bool IsClose(this Vector d1, Vector d2)
	{
		return IsClose(d1.X, d2.X) && IsClose(d1.Y, d2.Y);
	}

	[Conditional("DEBUG")]
	public static void Log(bool condition, string format, params object[] args)
	{
		if (condition)
		{
			var output = DateTime.Now.ToString("hh:MM:ss") + ": " + string.Format(format, args); //+ Environment.NewLine + Environment.StackTrace;
			//Console.WriteLine(output);
			Debug.WriteLine(output);
		}
	}

	public static T PeekOrDefault<T>(this ImmutableStack<T> stack)
	{
		return stack.IsEmpty ? default : stack.Peek();
	}

	public static void ReleasePointerCapture(this IInputElement element, IPointer device)
	{
		if (element == device.Captured)
		{
			device.Capture(null);
		}
	}

	/// <summary>
	/// Creates an IEnumerable with a single value.
	/// </summary>
	public static IEnumerable<T> Sequence<T>(T value)
	{
		yield return value;
	}

	public static Point SnapToDevicePixels(this Point p, Visual targetVisual)
	{
		var root = targetVisual.GetVisualRoot();

		// Get the root control and its scaling
		var scaling = new Vector(root.RenderScaling, root.RenderScaling);

		// Create a matrix to translate from control coordinates to device coordinates.
		var m = targetVisual.TransformToVisual((Control) root) * Matrix.CreateScale(scaling);

		if (m == null)
		{
			return p;
		}

		// Translate the point to device coordinates.
		var devicePoint = p.Transform(m.Value);

		// Snap the coordinate to the midpoint between device pixels.
		devicePoint = new Point((int) devicePoint.X + 0.5, (int) devicePoint.Y + 0.5);

		// Translate the point back to control coordinates.
		var inv = m.Value.Invert();
		var result = devicePoint.Transform(inv);
		return result;
	}

	public static IDisposable Subscribe<T>(this IObservable<T> observable, Action<T> action)
	{
		return observable.Subscribe(new AnonymousObserver<T>(action));
	}

	public static Point ToAvalonia(this System.Drawing.Point p)
	{
		return new Point(p.X, p.Y);
	}

	public static Size ToAvalonia(this System.Drawing.Size s)
	{
		return new Size(s.Width, s.Height);
	}

	public static Rect ToAvalonia(this Rectangle rect)
	{
		return new Rect(rect.Location.ToAvalonia(), rect.Size.ToAvalonia());
	}

	public static System.Drawing.Point ToSystemDrawing(this Point p)
	{
		return new System.Drawing.Point((int) p.X, (int) p.Y);
	}

	public static IEnumerable<AvaloniaObject> VisualAncestorsAndSelf(this AvaloniaObject obj)
	{
		while (obj != null)
		{
			yield return obj;
			if (obj is Visual v)
			{
				obj = v.GetVisualParent();
			}
			else
			{
				break;
			}
		}
	}

	#endregion

	//public static Rect TransformToDevice(this Rect rect, Visual visual)
	//{
	//	Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice;
	//	return Rect.Transform(rect, matrix);
	//}

	//public static Rect TransformFromDevice(this Rect rect, Visual visual)
	//{
	//	Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformFromDevice;
	//	return Rect.Transform(rect, matrix);
	//}

	//public static Size TransformToDevice(this Size size, Visual visual)
	//{
	//	Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice;
	//	return new Size(size.Width * matrix.M11, size.Height * matrix.M22);
	//}

	//public static Size TransformFromDevice(this Size size, Visual visual)
	//{
	//	Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformFromDevice;
	//	return new Size(size.Width * matrix.M11, size.Height * matrix.M22);
	//}

	//public static Point TransformToDevice(this Point point, Visual visual)
	//{
	//	Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice;
	//	return new Point(point.X * matrix.M11, point.Y * matrix.M22);
	//}

	//public static Point TransformFromDevice(this Point point, Visual visual)
	//{
	//	Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformFromDevice;
	//	return new Point(point.X * matrix.M11, point.Y * matrix.M22);
	//}
}
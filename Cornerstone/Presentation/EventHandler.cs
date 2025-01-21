#region References

using System;

#endregion

namespace Cornerstone.Presentation;

/// <summary> Represents the method that will handle an event when the event provides data. </summary>
/// <param name="sender"> The source of the event. </param>
/// <param name="args"> An object that contains the event data. </param>
public delegate void EventHandler<T1, T2>(object sender, (T1, T2) args);

/// <summary> Represents the method that will handle an event when the event provides data. </summary>
/// <param name="sender"> The source of the event. </param>
/// <param name="args"> An object that contains the event data. </param>
public delegate void EventHandler<T1, T2, T3>(object sender, Tuple<T1, T2, T3> args);

/// <summary> Represents the method that will handle an event when the event provides data. </summary>
/// <param name="sender"> The source of the event. </param>
/// <param name="args"> An object that contains the event data. </param>
public delegate void EventHandler<T1, T2, T3, T4>(object sender, Tuple<T1, T2, T3, T4> args);
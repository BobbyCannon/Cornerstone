﻿#region References

using System;
using System.Collections.Generic;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Exceptions;

/// <summary>
/// Represents a sync issue exception.
/// </summary>
[Serializable]
public class SyncIssueException : SyncException
{
	#region Constructors

	/// <summary>
	/// Initializes an instance of the exception.
	/// </summary>
	public SyncIssueException()
		: this(SyncIssueType.Unknown, string.Empty)
	{
	}

	/// <summary>
	/// Initializes an instance of the exception.
	/// </summary>
	public SyncIssueException(SyncIssueType type, string message, params SyncIssue[] issues)
		: this(type, message, null, issues)
	{
	}

	/// <summary>
	/// Initializes an instance of the exception.
	/// </summary>
	public SyncIssueException(SyncIssueType type, string message, Exception inner, params SyncIssue[] issues)
		: base(message, inner)
	{
		IssueType = type;
		Issues = issues ?? [];
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the child sync issues for this exception.
	/// </summary>
	public IEnumerable<SyncIssue> Issues { get; set; }

	/// <summary>
	/// Gets the type of the issue.
	/// </summary>
	public SyncIssueType IssueType { get; set; }

	#endregion
}
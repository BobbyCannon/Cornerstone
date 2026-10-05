#region References

using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.PropertyStore;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.PropertyStore;

#pragma warning disable format

[TestClass]
public class FramePriorityExtensionsTests
{
	#region Methods

	[PresentationTestMethod]
	[DataRow(BindingPriority.Animation, FrameType.Style, FramePriority.Animation)]
	[DataRow(BindingPriority.Animation, FrameType.TemplatedParentTheme, FramePriority.AnimationTemplatedParentTheme)]
	[DataRow(BindingPriority.Animation, FrameType.Theme, FramePriority.AnimationTheme)]
	[DataRow(BindingPriority.StyleTrigger, FrameType.Style, FramePriority.StyleTrigger)]
	[DataRow(BindingPriority.StyleTrigger, FrameType.TemplatedParentTheme, FramePriority.StyleTriggerTemplatedParentTheme)]
	[DataRow(BindingPriority.StyleTrigger, FrameType.Theme, FramePriority.StyleTriggerTheme)]
	[DataRow(BindingPriority.Template, FrameType.Style, FramePriority.Template)]
	[DataRow(BindingPriority.Template, FrameType.TemplatedParentTheme, FramePriority.TemplateTemplatedParentTheme)]
	[DataRow(BindingPriority.Template, FrameType.Theme, FramePriority.TemplateTheme)]
	[DataRow(BindingPriority.Style, FrameType.Style, FramePriority.Style)]
	[DataRow(BindingPriority.Style, FrameType.TemplatedParentTheme, FramePriority.StyleTemplatedParentTheme)]
	[DataRow(BindingPriority.Style, FrameType.Theme, FramePriority.StyleTheme)]
	internal void BindingPriorityToFramePriority(BindingPriority priority, FrameType type, FramePriority expected)
	{
		CornerstoneTest.AreEqual(expected, priority.ToFramePriority(type));
	}

	[PresentationTestMethod]
	[DataRow(FramePriority.Animation, FrameType.Style, true)]
	[DataRow(FramePriority.StyleTrigger, FrameType.Style, true)]
	[DataRow(FramePriority.Template, FrameType.Style, true)]
	[DataRow(FramePriority.Style, FrameType.Style, true)]
	[DataRow(FramePriority.AnimationTemplatedParentTheme, FrameType.TemplatedParentTheme, true)]
	[DataRow(FramePriority.StyleTriggerTemplatedParentTheme, FrameType.TemplatedParentTheme, true)]
	[DataRow(FramePriority.TemplateTemplatedParentTheme, FrameType.TemplatedParentTheme, true)]
	[DataRow(FramePriority.StyleTemplatedParentTheme, FrameType.TemplatedParentTheme, true)]
	[DataRow(FramePriority.AnimationTheme, FrameType.Theme, true)]
	[DataRow(FramePriority.StyleTriggerTheme, FrameType.Theme, true)]
	[DataRow(FramePriority.TemplateTheme, FrameType.Theme, true)]
	[DataRow(FramePriority.StyleTheme, FrameType.Theme, true)]

	//
	[DataRow(FramePriority.Style, FrameType.TemplatedParentTheme, false)]
	[DataRow(FramePriority.Style, FrameType.Theme, false)]
	[DataRow(FramePriority.StyleTheme, FrameType.TemplatedParentTheme, false)]
	[DataRow(FramePriority.StyleTheme, FrameType.Style, false)]
	internal void FramePriorityIsFrameType(FramePriority priority, FrameType type, bool expected)
	{
		CornerstoneTest.AreEqual(expected, priority.IsType(type));
	}

	#endregion
}
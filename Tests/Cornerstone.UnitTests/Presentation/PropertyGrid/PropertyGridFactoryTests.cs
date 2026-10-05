#region References

using System;
using System.ComponentModel;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Controls.PropertyGrid;
using Cornerstone.Presentation.Controls.PropertyGrid.Factories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cornerstone.Presentation.Controls.Input;

#endregion

namespace Cornerstone.UnitTests.Presentation.PropertyGrid;

[TestClass]
public class PropertyGridFactoryTests : CornerstoneCornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void BooleanFactoryCreatesCheckBox()
	{
		RunOnUi(() =>
		{
			var source = new SampleOptions { Flag = true };
			var property = TypeDescriptor.GetProperties(source)[nameof(SampleOptions.Flag)];
			var factory = new BooleanPropertyCellFactory();
			var context = new PropertyCellContext(source, property);
			var control = factory.HandleNewProperty(context);
			IsTrue(control is CheckBox);
			context.EditorControl = control;
			context.Factory = factory;
			IsTrue(factory.HandlePropertyChanged(context));
			AreEqual(true, ((CheckBox) control).IsChecked);
		});
	}

	[TestMethod]
	public void CharFactoryCreatesComboBox()
	{
		RunOnUi(() =>
		{
			var source = new SampleOptions { Indent = '\t' };
			var property = TypeDescriptor.GetProperties(source)[nameof(SampleOptions.Indent)];
			var factory = new CharPropertyCellFactory();
			var context = new PropertyCellContext(source, property);
			var control = factory.HandleNewProperty(context);
			IsTrue(control is ComboBox);
			context.EditorControl = control;
			context.Factory = factory;
			IsTrue(factory.HandlePropertyChanged(context));
			IsTrue(((ComboBox) control).SelectedItem is CharPropertyCellFactory.CharChoice choice && (choice.Value == '\t'));
		});
	}

	[TestMethod]
	public void ColorFactoryCreatesColorEditor()
	{
		RunOnUi(() =>
		{
			var source = new SampleOptions { Fill = Colors.Red };
			var property = TypeDescriptor.GetProperties(source)[nameof(SampleOptions.Fill)];
			var factory = new ColorPropertyCellFactory();
			var context = new PropertyCellContext(source, property);
			var control = factory.HandleNewProperty(context);
			IsTrue(control is ColorPropertyCellFactory.ColorValueEditor);
			context.EditorControl = control;
			context.Factory = factory;
			IsTrue(factory.HandlePropertyChanged(context));
		});
	}

	[TestMethod]
	public void DateTimeFactoryCreatesDatePicker()
	{
		RunOnUi(() =>
		{
			var source = new SampleOptions { When = new DateTime(2024, 6, 1, 8, 30, 0) };
			var property = TypeDescriptor.GetProperties(source)[nameof(SampleOptions.When)];
			var factory = new DateTimePropertyCellFactory();
			var context = new PropertyCellContext(source, property);
			var control = factory.HandleNewProperty(context);
			IsTrue(control is DatePicker);
			context.EditorControl = control;
			context.Factory = factory;
			IsTrue(factory.HandlePropertyChanged(context));
			AreEqual(new DateTime(2024, 6, 1), ((DatePicker) control).SelectedDate?.Date);
		});
	}

	[TestMethod]
	public void DescriptorSkipsNonBrowsable()
	{
		var source = new SampleOptions();
		var names = new PropertyDescriptorBuilder(source).GetProperties();
		IsNotNull(names[nameof(SampleOptions.Flag)]);
		IsFalse(names[nameof(SampleOptions.Hidden)].IsBrowsable);
	}

	[TestMethod]
	public void GuidFactoryCreatesTextBox()
	{
		RunOnUi(() =>
		{
			var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
			var source = new SampleOptions { Id = id };
			var property = TypeDescriptor.GetProperties(source)[nameof(SampleOptions.Id)];
			var factory = new GuidPropertyCellFactory();
			var context = new PropertyCellContext(source, property);
			var control = factory.HandleNewProperty(context);
			IsTrue(control is TextBox);
			context.EditorControl = control;
			context.Factory = factory;
			IsTrue(factory.HandlePropertyChanged(context));
			AreEqual(id.ToString(), ((TextBox) control).Text);
		});
	}

	[TestMethod]
	public void NumericFactoryCreatesNumericUpDown()
	{
		RunOnUi(() =>
		{
			var source = new SampleOptions { Count = 3 };
			var property = TypeDescriptor.GetProperties(source)[nameof(SampleOptions.Count)];
			var factory = new NumericPropertyCellFactory();
			var context = new PropertyCellContext(source, property);
			var control = factory.HandleNewProperty(context);
			IsTrue(control is NumericUpDown);
			context.EditorControl = control;
			context.Factory = factory;
			IsTrue(factory.HandlePropertyChanged(context));
			AreEqual(3m, ((NumericUpDown) control).Value);
		});
	}

	[TestMethod]
	public void ShortcutFactoryCreatesShortcutBox()
	{
		RunOnUi(() =>
		{
			var source = new SampleOptions { Shortcut = new ShortcutBinding() };
			var property = TypeDescriptor.GetProperties(source)[nameof(SampleOptions.Shortcut)];
			var factory = new ShortcutBindingPropertyCellFactory();
			var context = new PropertyCellContext(source, property);
			var control = factory.HandleNewProperty(context);
			IsTrue(control is ShortcutBox);
			context.EditorControl = control;
			context.Factory = factory;
			IsTrue(factory.HandlePropertyChanged(context));
		});
	}

	[TestMethod]
	public void TextBoxFactoryCreatesTextBoxForString()
	{
		RunOnUi(() =>
		{
			var source = new SampleOptions { Label = "hi" };
			var property = TypeDescriptor.GetProperties(source)[nameof(SampleOptions.Label)];
			var factory = new TextBoxPropertyCellFactory();
			var context = new PropertyCellContext(source, property);
			var control = factory.HandleNewProperty(context);
			IsTrue(control is TextBox);
			context.EditorControl = control;
			context.Factory = factory;
			IsTrue(factory.HandlePropertyChanged(context));
			AreEqual("hi", ((TextBox) control).Text);
		});
	}

	[TestMethod]
	public void TimeSpanFactoryCreatesDurationEditor()
	{
		RunOnUi(() =>
		{
			var source = new SampleOptions { Delay = new TimeSpan(1, 2, 3, 4) };
			var property = TypeDescriptor.GetProperties(source)[nameof(SampleOptions.Delay)];
			var factory = new TimeSpanPropertyCellFactory();
			var context = new PropertyCellContext(source, property);
			var control = factory.HandleNewProperty(context);
			var editor = control as TimeSpanPropertyCellFactory.TimeSpanDurationEditor;
			IsNotNull(editor);
			context.EditorControl = control;
			context.Factory = factory;
			IsTrue(factory.HandlePropertyChanged(context));
			AreEqual(1, editor.Days.Value);
			AreEqual(2, editor.Hours.Value);
			AreEqual(3, editor.Minutes.Value);
			AreEqual(4, editor.Seconds.Value);
		});
	}

	#endregion

	#region Classes

	public class SampleOptions
	{
		#region Properties

		public int Count { get; set; }

		public TimeSpan Delay { get; set; }

		public Color Fill { get; set; }

		public bool Flag { get; set; }

		[Browsable(false)]
		public string Hidden { get; set; }

		public Guid Id { get; set; }

		public char Indent { get; set; }

		public string Label { get; set; }

		public ShortcutBinding Shortcut { get; set; }

		public DateTime When { get; set; }

		#endregion
	}

	#endregion
}
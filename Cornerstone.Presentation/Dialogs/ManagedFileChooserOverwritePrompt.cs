using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Primitives;

namespace Cornerstone.Presentation.Dialogs;

public class ManagedFileChooserOverwritePrompt : TemplatedControl
{
    internal event Action<bool>? Result;

    private string _fileName = "";

    public static readonly DirectProperty<ManagedFileChooserOverwritePrompt, string> FileNameProperty = PresentationProperty.RegisterDirect<ManagedFileChooserOverwritePrompt, string>(
        "FileName", o => o.FileName, (o, v) => o.FileName = v);

    public string FileName
    {
        get => _fileName;
        set => SetAndRaise(FileNameProperty, ref _fileName, value);
    }

    public void Confirm()
    {
        Result?.Invoke(true);
    }

    public void Cancel()
    {
        Result?.Invoke(false);
    }
}

using System;
using System.Linq;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Controls.ApplicationLifetimes;

public sealed class ProtocolActivatedEventArgs : ActivatedEventArgs
{
    public ProtocolActivatedEventArgs(Uri uri) : base(ActivationKind.OpenUri)
    {
        Uri = uri;
    }

    public Uri Uri { get; }
}

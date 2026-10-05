using System;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Controls.Templates;

public interface ITypedDataTemplate : IDataTemplate
{
    [DataType]
    Type? DataType { get; }
}

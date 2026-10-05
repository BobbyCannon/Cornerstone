using System.Collections.Generic;

namespace Cornerstone.Generators.Presentation.Common.Domain;

internal interface ICodeGenerator
{
    string GenerateCode(string className, string nameSpace, IEnumerable<ResolvedName> names);
}

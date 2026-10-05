using System;

namespace Cornerstone.Generators.Presentation.Common.Domain;

internal interface IGlobPattern : IEquatable<IGlobPattern>
{
    bool Matches(string str);
}

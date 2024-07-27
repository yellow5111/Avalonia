using System.Collections.Generic;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Avalonia.Diagnostics;

[PrivateApi]
public sealed class ValueStoreDiagnostic(IReadOnlyList<IValueFrameDiagnostic> appliedFrames)
{
    /// <summary>
    /// Currently applied frames.
    /// </summary>
    public IReadOnlyList<IValueFrameDiagnostic> AppliedFrames { get; } = appliedFrames;
}

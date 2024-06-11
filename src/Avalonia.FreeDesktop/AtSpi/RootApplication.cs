using System;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace Avalonia.FreeDesktop.AtSpi;

internal class RootApplication : OrgA11yAtspiApplication
{
    public RootApplication(Connection connection)
    {
        Connection = connection;
        AtspiVersion = AVersion;
        ToolkitName = "Avalonia";
        Id = 0;
        Version = typeof(RootApplication).Assembly.GetName().Version?.ToString();
    }

    private const string AVersion = "2.1";

    public override Connection Connection { get; }

    protected override ValueTask<string> OnGetLocaleAsync(uint lctype)
    {
        return ValueTask.FromResult(Environment.GetEnvironmentVariable("LANG") ?? string.Empty);
    }
}

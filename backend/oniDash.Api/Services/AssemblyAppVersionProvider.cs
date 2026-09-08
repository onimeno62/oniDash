using System.Reflection;
using oniDash.Application.Abstractions;

namespace oniDash.Api.Services;

/// <summary>
/// Reports the version of the API assembly (the running product shell).
/// </summary>
public sealed class AssemblyAppVersionProvider : IAppVersionProvider
{
    public string Version { get; } =
        typeof(AssemblyAppVersionProvider).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "unknown";
}

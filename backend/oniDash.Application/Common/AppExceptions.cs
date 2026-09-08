using System;
using System.Collections.Generic;

namespace oniDash.Application.Common;

/// <summary>The referenced resource does not exist. Mapped to HTTP 404.</summary>
public sealed class NotFoundException(string resource, object key)
    : Exception($"{resource} '{key}' was not found.");

/// <summary>The request payload is invalid. Mapped to HTTP 400 validation problem.</summary>
public sealed class ValidationException : Exception
{
    private const string DefaultKey = "request";

    public ValidationException(string message, IDictionary<string, string[]>? errors = null)
        : base(message)
    {
        Errors = errors ?? new Dictionary<string, string[]> { [DefaultKey] = [message] };
    }

    public IDictionary<string, string[]> Errors { get; }
}

/// <summary>The request conflicts with existing state. Mapped to HTTP 409.</summary>
public sealed class ConflictException(string message) : Exception(message);

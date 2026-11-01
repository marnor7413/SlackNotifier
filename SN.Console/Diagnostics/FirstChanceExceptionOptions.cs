using System;
using System.Collections.Generic;
using System.Text;

namespace SN.ConsoleApp.Diagnostics;

/// <summary>
/// Settings for <see cref="FirstChanceExceptionLogger"/>, bound from the
/// configuration section "Diagnostics:FirstChanceExceptions".
/// </summary>
internal sealed class FirstChanceExceptionOptions
{
    /// <summary>
    /// Turns the logging on or off. Read once at startup.
    /// In Kubernetes: Diagnostics__FirstChanceExceptions__Enabled=true
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Full type names to log, matched exactly (subclasses do not match).
    /// An empty list logs every exception type.
    /// </summary>
    public string[] ExceptionTypes { get; set; } = [];
}
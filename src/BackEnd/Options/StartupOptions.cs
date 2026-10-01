// Copyright (C) 2024 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved

namespace DotNetNuxt.StarterKit.BackEnd.Options;

/// <summary>
/// Configuration options for program startup
/// </summary>
public record StartupOptions
{
    public static readonly string Section = "Startup";

    /// <summary>
    /// Accept connections on HTTP (not forcing HTTPS)
    /// </summary>
    public bool AllowHttpInsecure { get; init; }

    /// <summary>
    /// Show the swagger UI
    /// </summary>
    public bool EnableSwaggerUi { get; init; }

    /// <summary>
    /// Allowed CORS origins
    /// </summary>
    public string[] AllowedCorsOrigins { get; set; } = [];
}

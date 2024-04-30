﻿using System.Diagnostics.CodeAnalysis;

namespace Preservation.API.Models;

public class UriGenerator(IHttpContextAccessor httpContextAccessor)
{
    [return: NotNullIfNotNull(nameof(storageUri))]
    public Uri? GetRepositoryPath(Uri? storageUri, string? version = null)
    {
        if (storageUri == null) return null;
        
        // Only append ?version if provided as arg, so use AbsolutePath only be default
        var request = httpContextAccessor.HttpContext.Request;
        var uriBuilder = new UriBuilder(request.Scheme, request.Host.Host)
        {
            Port = request.Host.Port ?? 80,
            Path = GetPreservationPath(storageUri)
        };

        if (!string.IsNullOrEmpty(version))
        {
            uriBuilder.Query = $"version={version}";
        }

        return uriBuilder.Uri;
    }

    private static string GetPreservationPath(Uri storageUri)
    {
        var storageUriAbsolutePath = storageUri.AbsolutePath;
        const string storageApiPrefix = "/api";
        return Uri.UnescapeDataString(storageUriAbsolutePath[..4] == storageApiPrefix
            ? storageUriAbsolutePath[4..]
            : storageUriAbsolutePath);
    }
}
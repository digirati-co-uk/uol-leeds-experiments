﻿using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;

namespace Preservation.API.Models;

public class UriGenerator
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly PreservationSettings preservationSettings;

    public UriGenerator(IHttpContextAccessor httpContextAccessor, IOptions<PreservationSettings> options)
    {
        this.httpContextAccessor = httpContextAccessor;
        preservationSettings = options.Value;
    }
    

    [return: NotNullIfNotNull(nameof(storageUri))]
    public Uri? GetRepositoryPath(Uri? storageUri, string? version = null)
    {
        if (storageUri == null) return null;
        
        // Only append ?version if provided as arg, so use AbsolutePath only be default
        var uriBuilder = GetUriBuilderForCurrentHost(GetPreservationPath(storageUri));
        if (!string.IsNullOrEmpty(version))
        {
            uriBuilder.Query = $"version={version}";
        }

        return uriBuilder.Uri;
    }

    [return: NotNullIfNotNull(nameof(storageUri))]
    public Uri? GetRepositoryPath(string? storageUri, string? version = null)
        => GetRepositoryPath(string.IsNullOrEmpty(storageUri) ? null : new Uri(storageUri, UriKind.RelativeOrAbsolute));

    public Uri GetImportJobResultUri(string depositId, string importJobId)
    {
        var path = $"deposits/{depositId}/importJob/{importJobId}";
        return GetUriBuilderForCurrentHost(path).Uri;
    } 
    
    public Uri GetDepositPath(string depositId)
    {
        var uriBuilder = GetUriBuilderForCurrentHost($"deposits/{depositId}");
        return uriBuilder.Uri;
    }
    
    private UriBuilder GetUriBuilderForCurrentHost(string path)
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request != null)
        {
            var uriBuilderFromContext = new UriBuilder(request.Scheme, request.Host.Host)
            {
                Port = request.Host.Port ?? 80,
                Path = path
            };
            return uriBuilderFromContext;
        }
        else
        {
            var baseAddress = preservationSettings.PreservationApiBaseAddress;
            var uriBuilderFromConfig = new UriBuilder(baseAddress.Scheme, baseAddress.Host)
            {
                Port = baseAddress.Port,
            };

            return uriBuilderFromConfig;
        }
    }

    private static string GetPreservationPath(Uri storageUri)
    {
        var storageUriAbsolutePath = storageUri.IsAbsoluteUri ? storageUri.AbsolutePath : storageUri.OriginalString;
        const string storageApiPrefix = "/api";
        return Uri.UnescapeDataString(storageUriAbsolutePath[..4] == storageApiPrefix
            ? storageUriAbsolutePath[4..]
            : storageUriAbsolutePath);
    }
}
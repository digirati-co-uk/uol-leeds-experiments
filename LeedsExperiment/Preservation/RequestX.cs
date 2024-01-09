﻿using System;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Xml.Linq;
using Fedora.ApiModel;
using Fedora.Vocab;

namespace Preservation
{
    public static class RequestX
    {
        public static HttpRequestMessage ForJsonLd(this HttpRequestMessage requestMessage)
        {
            requestMessage.Headers.Accept.Clear();
            var contentTypeHeader = new MediaTypeWithQualityHeaderValue("application/ld+json");
            contentTypeHeader.Parameters.Add(new NameValueHeaderValue("profile", JsonLdModes.Compacted));
            requestMessage.Headers.Accept.Add(contentTypeHeader);
            return requestMessage;
        }

        public static HttpRequestMessage WithContainedDescriptions(this HttpRequestMessage requestMessage)
        {
            requestMessage.Headers.Add("Prefer", $"return=representation; include=\"{Prefer.PreferContainedDescriptions}\"");
            return requestMessage;
        }

        public static HttpRequestMessage InTransaction(this HttpRequestMessage requestMessage, Transaction? transaction)
        {
            if(transaction != null)
            {
                requestMessage.Headers.Add(Transaction.HeaderName, transaction.Location.ToString());
            }
            return requestMessage;
        }

        public static HttpRequestMessage WithName(this HttpRequestMessage requestMessage, string? name)
        {
            if(requestMessage.Content == null && !string.IsNullOrWhiteSpace(name)) 
            {
                var turtle = MediaTypeHeaderValue.Parse("text/turtle");
                requestMessage.Content = new StringContent($"PREFIX dc: <http://purl.org/dc/elements/1.1/>  <> dc:title \"{name}\"", turtle);
            }
            return requestMessage;
        }

        public static HttpContent WithContentDisposition(this HttpContent httpContent, string? contentDisposition)
        {
            if (!string.IsNullOrWhiteSpace(contentDisposition))
            {
                httpContent.Headers.Add("Content-Disposition", $"attachment; filename=\"{contentDisposition}\""); 
            }
            return httpContent;
        }

        public static HttpContent WithContentType(this HttpContent httpContent, string? contentType)
        {
            if (!string.IsNullOrWhiteSpace(contentType))
            {
                httpContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            }
            return httpContent;
        }

        public static HttpRequestMessage WithDigest(this HttpRequestMessage requestMessage, string? digest, string algorithm)
        {
            if (!string.IsNullOrWhiteSpace(digest))
            {
                requestMessage.Headers.Add("digest", $"{algorithm}={digest}");
            }
            return requestMessage;
        }


        public static HttpRequestMessage WithAcceptDate(this HttpRequestMessage requestMessage, string? mementoTimestamp)
        {
            if (!string.IsNullOrWhiteSpace(mementoTimestamp))
            {
                requestMessage.Headers.Add("Accept-Datetime", mementoTimestamp.ToRFC1123());
            }
            return requestMessage;
        }

        public static HttpRequestMessage WithAcceptDate(this HttpRequestMessage requestMessage, DateTime dt)
        {
            requestMessage.Headers.Add("Accept-Datetime", dt.ToRFC1123());
            return requestMessage;
        }

        public static HttpRequestMessage WithSlug(this HttpRequestMessage requestMessage, string slug) 
        {
            requestMessage.Headers.Add("slug", slug);
            return requestMessage;
        }

        public static HttpRequestMessage AsArchivalGroup(this HttpRequestMessage requestMessage)
        {
            // This tells Fedora it's an archival group:
            requestMessage.Headers.Add("Link", $"<{RepositoryTypes.ArchivalGroup}>;rel=\"type\"");

            // But we want to assign an additional type that will be returned in contained resources
            //var stringContent = requestMessage.Content;
            //var turtle = MediaTypeHeaderValue.Parse("text/turtle");
            //var sparql = "  <> rdf:type <http://purl.org/dc/dcmitype/Collection> ;";
            //if (stringContent == null)
            //{
            //    stringContent = new StringContent(sparql, turtle);
            //}
            //else
            //{
            //    string oldContent = await stringContent.ReadAsStringAsync();
            //    stringContent = new StringContent($"{oldContent}\n{sparql}", turtle);
            //}
            //requestMessage.Content = stringContent;

            return requestMessage;
        }

        public static HttpRequestMessage OverwriteTombstone(this HttpRequestMessage requestMessage)
        {
            requestMessage.Headers.Add("Overwrite-Tombstone", "true");
            return requestMessage;
        }


        public static HttpRequestMessage AsInsertTitlePatch(this HttpRequestMessage requestMessage, string title)
        {
            var sparql = $$"""
                           PREFIX dc: <http://purl.org/dc/elements/1.1/>
                           INSERT {   
                               <> dc:title "{{title}}" .
                           }
                           WHERE { }
                           """;

            requestMessage.Content = new StringContent(sparql)
                .WithContentType("application/sparql-update");
            return requestMessage;
        }


        public static HttpRequestMessage AsInsertTypePatch(this HttpRequestMessage requestMessage, string type)
        {
            var sparql = $$"""
                           INSERT {   
                               <> a {{type}} .
                           }
                           WHERE { }
                           """;

            requestMessage.Content = new StringContent(sparql)
                .WithContentType("application/sparql-update");
            return requestMessage;
        }

        /// <summary>
        /// This should really obtain the rel=describedBy link via a HEAD request
        /// But in the interests of efficienct, we'll be a little less RESTful and
        /// assume a Fedora convention.
        /// </summary>
        /// <param name="resourceUri"></param>
        /// <returns></returns>
        public static Uri MetadataUri(this Uri resourceUri)
        {
            // I think it's actually impossible to construct the Uri in .NET without dropping back to strings
            // because resourceUri does not have a trailing slash, and the relative Uri would have to be "./fcr:metadata"
            // if you construct that Uri it ends up as https://domain.com/fcr:metadata - the path is stripped.
            return new Uri($"{resourceUri}/fcr:metadata");
        }

        /// <summary>
        /// Same comments as above
        /// </summary>
        /// <param name="resourceUri"></param>
        /// <returns></returns>
        public static Uri VersionsUri(this Uri resourceUri)
        {
            return new Uri($"{resourceUri}/fcr:versions");
        }
    }
}

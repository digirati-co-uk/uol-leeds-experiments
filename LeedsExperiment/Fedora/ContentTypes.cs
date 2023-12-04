namespace Fedora;

public static class ContentTypes
{
    public const string JsonLd = "application/ld+json";
    public const string NTriples = "application/n-triples";
    public const string RdfXml = "application/rdf+xml";
    public const string TextN3 = "text/n3";
    public const string TextPlain = "text/plain";
    public const string TextTurtle = "text/turtle";

    public static string FormatAcceptsHeader(string contentType, string jsonLdMode = JsonLdModes.Expanded)
    {
        if (jsonLdMode == JsonLdModes.Expanded || contentType != JsonLd)
        {
            // expanded is the default
            return contentType;
        }

        return $"{JsonLd}; profile=\"{jsonLdMode}\"";
    }
}

public static class JsonLdModes
{
    /// <summary>
    /// The default Fedora JSON-LD representation
    /// </summary>
    public const string Expanded = "\"http://www.w3.org/ns/json-ld#expanded\"";


    /// <summary>
    /// Compacted JSON-LD (not the default)
    /// </summary>
    public const string Compacted = "\"http://www.w3.org/ns/json-ld#compacted\"";


    /// <summary>
    /// Flattened JSON-LD (not the default)
    /// </summary>
    public const string Flattened = "\"http://www.w3.org/ns/json-ld#flattened\"";
}

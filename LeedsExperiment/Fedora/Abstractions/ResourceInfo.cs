namespace Fedora.Abstractions;

public class ResourceInfo
{
    public string? Type {  get; set; }
    public bool Exists { get; set; }
    public int StatusCode { get; set; }
}

using Fedora;
using Fedora.Storage.Ocfl;
using Microsoft.AspNetCore.Mvc;

namespace Preservation.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OcflController(IStorageMapper storageMapper, IFedora fedora) : Controller
{
    /// <summary>
    /// Fetch OCFL file for item at specified path.
    /// </summary>
    /// <param name="path">Path to item in Fedora (e.g. path/to/item)</param>
    /// <returns>OCFL resource representing item at path</returns>
    [HttpGet("{*path}", Name = "Inventory")]
    [Produces<Inventory>]
    [Produces("application/json")]
    public async Task<Inventory?> Index(string path)
    {
        var uri = fedora.GetUri(path);
        var inventory = await storageMapper.GetInventory(uri);
        return inventory;
    }
}
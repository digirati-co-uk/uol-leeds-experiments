using Fedora;
using Fedora.Storage.Ocfl;
using Microsoft.AspNetCore.Mvc;

namespace Preservation.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OcflController : Controller
    {
        private readonly IStorageMapper storageMapper;
        private readonly IFedora fedora;

        public OcflController(IStorageMapper storageMapper, IFedora fedora)
        {
            this.storageMapper = storageMapper;
            this.fedora = fedora;
        }

        [HttpGet("{*path}", Name = "Inventory")]
        public async Task<Inventory?> Index(string path)
        {
            var uri = fedora.GetUri(path);
            var inventory = await storageMapper.GetInventory(uri);
            return inventory;
        }
    }
}

﻿using Fedora;
using Fedora.Vocab;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Preservation.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RepositoryController : Controller
    {
        private IFedora fedora;

        public RepositoryController(IFedora fedora)
        {
            this.fedora = fedora;
        }

        [HttpGet(Name = "Browse")]
        [Route("{*path}")]
        public async Task<Resource?> Index(string path)
        {
            var resource = await fedora.GetObject(path);
            return resource;
        }
    }
}

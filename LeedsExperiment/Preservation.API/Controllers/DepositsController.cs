using Microsoft.AspNetCore.Mvc;
using Preservation.API.Models;

namespace Preservation.API.Controllers;

[Route("[controller]")]
[ApiController]
public class DepositsController : Controller
{
    /// <summary>
    /// Create a new Deposit object. This will create a working location in s3 that will contain working set of files
    /// that will become a <see cref="DigitalObject"/>. 
    /// </summary>
    /// <param name="deposit">(Optional) Partial deposit object containing Preservation URI or submission text</param>
    /// <returns></returns>
    /// <remarks>
    /// Sample request:
    ///
    ///  Empty body, creates new Deposit
    ///     POST /deposits/
    ///     { }
    ///
    ///  Partial body, creates new Deposit specifying Preservation URI with optional submissionText
    ///     POST /deposits/
    ///     {
    ///       "digitalObject": "https://preservation.dlip.leeds.ac.uk/repository/example-objects/DigitalObject2",
    ///       "submissionText": "Just leaving this here"
    ///     }
    /// </remarks>
    [HttpPost]
    [Produces("application/json")]
    [Produces<Deposit>]
    public IActionResult Create(Deposit? deposit = null)
    {
        // NOTE: In reality it would be the Id service that generates this 
        var id = Identifiable.Generate();
        /*
         * empty body = create new Deposit + assign a new URI (from ID service)
         * partial - has PreservationURI and/or submissionText
         */
        
        /*
         * Create key in S3 - need to store what was (maybe) provided.
         * but this could also be provided from export. Would be in DB but just store in json file?
         */
        throw new NotImplementedException();
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    [HttpGet("{id}", Name = "GetDeposit")]
    [Produces("application/json")]
    [Produces<Deposit>]
    public IActionResult Get([FromRoute] string id)
    {
        /*
         * empty body = create new Deposit + assign a new URI (from ID service)
         */
        throw new NotImplementedException();
    }
}
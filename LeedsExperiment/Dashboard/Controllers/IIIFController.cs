using Dashboard.Helpers;
using Dashboard.Models;
using Dlcs;
using Dlcs.Hydra;
using Fedora.Abstractions;
using IIIF;
using IIIF.ImageApi.V2;
using IIIF.Presentation.V3;
using IIIF.Presentation.V3.Annotation;
using IIIF.Serialisation;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Preservation;
using Utils;

namespace Dashboard.Controllers
{
    public class IIIFController : Controller
    {
        private readonly ILogger<IIIFController> logger;
        private readonly IPreservation preservation;
        private readonly IDlcs dlcs;

        public IIIFController(
            IPreservation preservation,
            IDlcs dlcs,
            ILogger<IIIFController> logger)
        {
            this.preservation = preservation;
            this.logger = logger;
            this.dlcs = dlcs;
        }

        private string GetDlcsIdentifier(string path)
        {
            return path.Replace("/", "__");
        }

        [HttpGet]
        [ActionName("IIIFSyncView")]
        [Route("iiif/{*path}")]
        public async Task<IActionResult> IndexAsync([FromRoute] string path)
        {
            ViewBag.Path = path;
            var model = new IIIFSyncModel { Path = path };
            var getAg = preservation.GetArchivalGroup(path, null);
            var getDlcsImages = dlcs.GetImagesFromQuery(new ImageQuery { String1 = path });

            await Task.WhenAll(getAg, getDlcsImages);
            var ag = getAg.Result;
            if (ag == null)
            {
                return NotFound();
            }
            if (ag.Type != "ArchivalGroup")
            {
                return BadRequest("Not an Archival Group");
            }
            model.ArchivalGroup = ag;
            var dlcsImages = getDlcsImages.Result.ToList();
            var preservedImages = GetFlattenedImageAssets(ag);
            foreach (var binary in preservedImages)
            {
                string string2 = GetString2(ag, binary);
                var dlcsImage = dlcsImages.SingleOrDefault(di => di.String2 == string2);
                model.ImageMap[string2] = dlcsImage;
            }
            return View("SyncStatus", model);
        }

        [HttpPost]
        [ActionName("IIIFSyncExecute")]
        [Route("iiif/{*path}")]
        public async Task<IActionResult> SyncAsync([FromRoute] string path)
        {
            ViewBag.Path = path;
            var getAg = preservation.GetArchivalGroup(path, null);
            var getDlcsImages = dlcs.GetImagesFromQuery(new ImageQuery { String1 = path });
            await Task.WhenAll(getAg, getDlcsImages);
            ArchivalGroup ag = getAg.Result!;
            var dlcsImages = getDlcsImages.Result!.ToList();
            var preservedImages = GetFlattenedImageAssets(ag);
            List<Image> imagesToRegister = new List<Image>();
            int sequenceIndex = 1;
            foreach (var binary in preservedImages)
            {
                string string2 = GetString2(ag, binary);
                var dlcsImage = dlcsImages.SingleOrDefault(di => di.String2 == string2);
                if(dlcsImage == null)
                {
                    imagesToRegister.Add(new Image()
                    {
                        ModelId = GetDlcsIdentifier(string2),
                        Space = 2,
                        String1 = path,
                        String2 = string2,
                        Number1 = sequenceIndex,
                        MediaType = binary.ContentType,
                        Family = 'I',
                        Origin = ToHttpFormat(binary.Origin!)
                    });
                }
                sequenceIndex++;
            }
            var hydraCollection = new HydraImageCollection() { Members = imagesToRegister.ToArray() };
            var batch = await dlcs.RegisterImages(hydraCollection);

            var model = new IIIFSyncModel { Path = path, ArchivalGroup = ag, Batch = batch };
            return View("Batch", model);
        }


        [HttpGet]
        [ActionName("IIIFMAnifest")]
        [Route("manifest/{*path}")]
        [EnableCors(PolicyName = "IIIF")]
        public async Task<IActionResult> Manifest([FromRoute] string path)
        {
            var getAg = preservation.GetArchivalGroup(path, null);
            var getDlcsImages = dlcs.GetImagesFromQuery(new ImageQuery { String1 = path });
            await Task.WhenAll(getAg, getDlcsImages);

            var ag = getAg.Result;
            if (ag == null)
            {
                return NotFound();
            }
            if (ag.Type != "ArchivalGroup")
            {
                return BadRequest("Not an Archival Group");
            }

            var dlcsImages = getDlcsImages.Result!.ToList().OrderBy(image => image.Number1);

            var baseUrl = Request.GetDisplayUrl();
            var manifest = new Manifest
            {
                Id = baseUrl,
                Label = new IIIF.Presentation.V3.Strings.LanguageMap("en", ag.GetDisplayName()),
                Items = []
            };
            foreach(var image in dlcsImages)
            {
                var canvasId = $"{baseUrl}/canvas/{image.Number1}";
                manifest.Items.Add(new Canvas
                {
                    Id = canvasId,
                    Label = new IIIF.Presentation.V3.Strings.LanguageMap("en", $"Canvas {image.Number1}"),
                    Width = image.Width,
                    Height = image.Height,
                    Items =
                    [
                        new AnnotationPage
                        {
                            Id = $"{baseUrl}/annopage/{image.Number1}",
                            Items =
                            [
                                new PaintingAnnotation
                                {
                                    Id = $"{baseUrl}/painting/{image.Number1}",
                                    Body = new IIIF.Presentation.V3.Content.Image
                                    {
                                        Id = $"{image.ImageService}/full/max/0/default.jpg",
                                        Width = image.Width,
                                        Height = image.Height,
                                        Format = "image/jpeg",
                                        Service =
                                        [
                                            new ImageService2
                                            {
                                                Id = image.ImageService,
                                                Width = image.Width!.Value,
                                                Height = image.Height!.Value,
                                                Profile = ImageService2.Level1Profile
                                            }
                                        ]
                                    },
                                    Target = new Canvas {Id = canvasId}
                                }
                            ]
                        }
                    ],
                    Thumbnail =
                    [
                        new IIIF.Presentation.V3.Content.Image
                        {
                            Id = $"{image.ImageService}/full/!200,200/0/default.jpg",
                            Format = "image/jpeg",
                            Service =
                            [
                                new ImageService2
                                {
                                    Id = image.ThumbnailImageService,
                                    Profile = ImageService2.Level0Profile
                                }
                            ]
                        }
                    ]
                });
            }
            return Content(manifest.AsJson(), "application/json");
        }

        private string ToHttpFormat(string origin)
        {
            var uri = new Uri(origin);
            return $"https://s3-eu-west-1.amazonaws.com/{uri.Host}{uri.AbsolutePath}";
        }

        private string GetString2(ArchivalGroup ag, Binary binary)
        {
            return binary.ObjectPath.RemoveStart(ag.ObjectPath!)!.TrimStart('/');
        }

        private List<Binary> GetFlattenedImageAssets(ArchivalGroup ag)
        {
            var images = new List<Binary>();
            AddImagesFromContainer(images, ag);
            return images;
        }

        private static void AddImagesFromContainer(List<Binary> images, Container container) 
        {
            images.AddRange(container.Binaries
                .Where(b => b.ContentType != null && b.ContentType.StartsWith("image/"))
                .OrderBy(b => b.GetSlug()));
            foreach(Container c in container.Containers.OrderBy(c => c.GetSlug()))
            {
                AddImagesFromContainer(images, c);
            }
        }
    }
}

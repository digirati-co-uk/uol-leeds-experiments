using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Fedora.Abstractions.Transfer;
using Fedora.Storage;
using System.Xml.Linq;
using System.Xml.Schema;

namespace MetsParser
{
    public class Parser
    {
        private IAmazonS3? s3Client;

        public Parser(IAmazonS3? s3Client) 
        {
            this.s3Client = s3Client;            
        }

        public void Populate(MetsFile mets, XDocument xMets)
        {
            var modsTitle = xMets.Descendants(XNames.mods + "title").FirstOrDefault()?.Value;
            if(!string.IsNullOrWhiteSpace(modsTitle))
            {
                mets.Name = modsTitle;
            }
            // TODO - where else to look for title?

            // There may be more than one, and they may or may not be qualified as physical or logical
            XElement? physicalStructMap = null;
            foreach(var sm in xMets.Descendants(XNames.MetsStructMap))
            {
                var typeAttr = sm.Attribute("TYPE");
                if(typeAttr?.Value != null)
                {
                    if(typeAttr.Value.ToLowerInvariant() == "physical")
                    {
                        physicalStructMap = sm;
                        break;
                    }
                    if (typeAttr.Value.ToLowerInvariant() == "logical")
                    {
                        continue;
                    }
                }
                if(physicalStructMap == null)
                {
                    // This may get overwritten if we find a better one in the loop
                    // EPRints METS files structMap don't have type
                    physicalStructMap = sm;
                }
            }

            if(physicalStructMap == null)
            {
                throw new NotSupportedException("METS file muct have a physical structmap");
            }

            // Now walk down the structmap
            // Each div either contains 1 (or sometimes more) mets:fptr, or it contains child DIVs.
            // If a DIV containing a mets:fptr has a LABEL (not ORDERLABEL) then that is the name of the file
            // If those DIVs have TYPE="Directory" and a LABEL, that gives us the name of the directory.
            // We need to see the path of the file, too.

            // A DIV TYPE="Directory" should never directly contain a file

            // GOOBI METS at Wellcome contain images and ALTO in the same DIV; the ADM_ID is for the Image not the ALTO.
            // Not sure how to be formal about that.

            var parent = physicalStructMap;
            var fileSec = xMets.Descendants(XNames.MetsFileSec).Single();

            foreach(var div in parent.Elements(XNames.MetsDiv))
            {
                var type = div.Attribute("TYPE")?.Value?.ToLowerInvariant();

                // type may be Directory, we need to match them up to file paths
                // but there might not be any directories in the structmap, just implied by flocats.

                // build all the files first on one pass then re=parse to make directories?

                foreach(var fptr in div.Elements(XNames.MetsFptr))
                {
                    var fileId = fptr.Attribute("FILEID")!.Value;
                    var fileEl = fileSec.Descendants(XNames.MetsFile).Single(f => f.Attribute("ID")!.Value == fileId);
                    var mimeAttr = fileEl.Attribute("MIMETYPE")?.Value;  // Archivematica does not have this, have to get it from PRONOM, even reverse lookup
                    var flocat = fileEl.Elements(XNames.MetsFLocat).Single().Attribute(XNames.XLinkHref)!.Value;
                    var admid = fileEl.Attribute("ADMID")!.Value;
                    var techMd = xMets.Descendants(XNames.MetsTechMD).Single(t => t.Attribute("ID")!.Value == admid);
                    var fixity = techMd.Descendants(XNames.PremisFixity).SingleOrDefault();
                    string? digest = null;
                    if(fixity != null)
                    {
                        var algorithm = fixity.Element(XNames.PremisMessageDigestAlgorithm)?.Value?.ToLowerInvariant().Replace("-", "");
                        if(algorithm == "sha256")
                        {
                            digest = fixity.Element(XNames.PremisMessageDigest)?.Value;
                        }
                    }

                    // use flocat to create entries in our dictionary of directories, which we can match up ro 
                    var binaryFile = new BinaryFile()
                    {
                        
                    }
                }
            }









        }

        public async Task<MetsFile> ResolveAndParseAsync(Uri root)
        {
            // might be a file path or an S3 URI
            var mets = new MetsFile
            {
                Root = root,
                Self = await FindMetsFileAsync(root)
            };
            if(mets.Self != null)
            {
                var xMets = await LoadXml(mets.Self);
                Populate(mets, xMets);
            }
            return mets;
        }


        private async Task<BinaryFile?> FindMetsFileAsync(Uri root)
        {
            // This "find the METS file" logic is VERY basic and doesn't even look at the file.
            // But this is just for Proof of Concept.

            switch (root.Scheme)
            {
                case "file":
                    var dir = new DirectoryInfo(root.AbsolutePath);
                    var firstXmlFile = dir.EnumerateFiles().FirstOrDefault(f => f.Name.ToLowerInvariant().EndsWith(".xml"));
                    if (firstXmlFile != null)
                    {
                        return new BinaryFile()
                        {
                            ContentType = "application/xml",
                            Parent = root,
                            ExternalLocation = firstXmlFile.FullName,
                            Name = firstXmlFile.Name,
                            Path = firstXmlFile.Name,
                            StorageType = StorageTypes.FileSystem,
                            Digest = Storage.Checksum.Sha256FromFile(firstXmlFile)
                        };
                    }
                    break;
                case "s3":
                    var s3Uri = new AmazonS3Uri(root);
                    var prefix = $"{s3Uri.Key.TrimEnd('/')}/";
                    var listObjectsReq = new ListObjectsV2Request()
                    {
                        BucketName = s3Uri.Bucket,
                        Prefix = prefix,
                        Delimiter = "/" // first "children" only                        
                    };
                    var resp = await s3Client!.ListObjectsV2Async(listObjectsReq);
                    var firstXmlKey = resp.S3Objects.FirstOrDefault(s => s.Key.ToLowerInvariant().EndsWith(".xml"));
                    if(firstXmlKey != null)
                    {
                        var s3Stream = await s3Client!.GetObjectStreamAsync(firstXmlKey.BucketName, firstXmlKey.Key, null);
                        var digest = Storage.Checksum.Sha256FromStream(s3Stream);
                        var name = firstXmlKey.Key.Replace(prefix, string.Empty);
                        return new BinaryFile()
                        {
                            ContentType = "application/xml",
                            Parent = root,
                            ExternalLocation = $"s3://{firstXmlKey.BucketName}/{firstXmlKey.Key}",
                            Name = name,
                            Path = name,
                            StorageType = StorageTypes.S3,
                            Digest = digest
                        };
                    }
                    // 
                    break;
                default:
                    throw new NotSupportedException(root.Scheme + " not supported");
            }

            return null;
        }

        private async Task<XDocument> LoadXml(BinaryFile self)
        {
            switch(self.StorageType)
            {
                case StorageTypes.FileSystem:
                    return XDocument.Load(self.ExternalLocation);

                case StorageTypes.S3:
                    var s3Uri = new AmazonS3Uri(self.ExternalLocation);
                    var s3Stream = await s3Client!.GetObjectStreamAsync(s3Uri.Bucket, s3Uri.Key, null);
                    var xMets = await XDocument.LoadAsync(s3Stream, LoadOptions.None, CancellationToken.None);
                    return xMets;

                default:
                    throw new NotSupportedException(self.StorageType + " not supported");
            }
        }
    }
}

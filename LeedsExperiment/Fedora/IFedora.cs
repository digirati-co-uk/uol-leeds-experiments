using Fedora.Abstractions;
using Fedora.Abstractions.Transfer;
using Fedora.ApiModel;

namespace Fedora
{
    public interface IFedora
    {
        Uri GetUri(string path);

        Task<string> Proxy(string contentType, string path, string? jsonLdMode = null, bool preferContained = false, string? acceptDate = null, bool head = false);

        Task<Resource?> GetObject(Uri uri, Transaction? transaction = null);
        Task<Resource?> GetObject(string path, Transaction? transaction = null);

        Task<T?> GetObject<T>(Uri uri, Transaction? transaction = null) where T: Resource;
        Task<T?> GetObject<T>(string path, Transaction? transaction = null) where T : Resource;

        Task<ArchivalGroup?> GetPopulatedArchivalGroup(Uri uri, string? version = null, Transaction? transaction = null);
        Task<ArchivalGroup?> GetPopulatedArchivalGroup(string path, string? version = null, Transaction? transaction = null);

        Task<ArchivalGroup?> CreateArchivalGroup(Uri parent, string slug, string name, Transaction? transaction = null);
        Task<ArchivalGroup?> CreateArchivalGroup(string parentPath, string slug, string name, Transaction? transaction = null);

        Task<Container?> CreateContainer(ContainerDirectory containerDirectory, Transaction? transaction = null);

        /// <summary>
        /// DISALLOW a POST for binaries, for now
        /// Always require a PUT, so we can detect tombstone issues and not allow Fedora to give alternative slugs
        /// </summary>
        /// <param name="location">Uri of parent container resource</param>
        /// <param name="localFileInfo">(for now) a FileInfo - will we have an S3 impl, a filesystem impl, etc? We'll take the slug from this file (?)</param>
        /// <param name="originalName">The original name of the file, provided from external metadata, stored as dc:title</param>
        /// <param name="transaction">A transaction if running</param>
        /// <param name="checksum">An initial checksum, e.g., calculated in browser on upload. This method will still calculate a checksum and compare with what it gets back from Fedora.</param>
        /// <returns></returns>
        // Task<Binary> AddBinary(Uri parent, FileInfo localFileInfo, string originalName, string contentType, Transaction? transaction = null, string? checksum = null);
        // Task<Binary> PutBinary(Uri location, FileInfo localFileInfo, string originalName, string contentType, Transaction? transaction = null, string? checksum = null);
        Task<Binary> PutBinary(BinaryFile binaryFile, Transaction? transaction = null);

        // Transactions
        Task<Transaction> BeginTransaction();
        Task CheckTransaction(Transaction tx);
        Task KeepTransactionAlive(Transaction tx);
        Task CommitTransaction(Transaction tx);
        Task RollbackTransaction(Transaction tx);

        Task Delete(Uri uri, Transaction? transaction = null);
        Task<Resource?> GetRepositoryRoot(Transaction? transaction = null);
    }
}

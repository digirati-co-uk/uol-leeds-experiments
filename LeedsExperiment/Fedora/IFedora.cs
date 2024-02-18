using Fedora.Abstractions;
using Fedora.Abstractions.Transfer;
using Fedora.ApiModel;

namespace Fedora
{
    public interface IFedora
    {
        Uri GetUri(string path);

        Task<Resource?> GetRepositoryRoot(Transaction? transaction = null);

        Task<string> Proxy(string contentType, string path, string? jsonLdMode = null, bool preferContained = false, string? acceptDate = null, bool head = false);
        
        Task<ResourceInfo> GetResourceInfo(Uri uri, Transaction? transaction = null);
        
        Task<Resource?> GetObject(Uri uri, Transaction? transaction = null);        
        Task<Resource?> GetObject(string path, Transaction? transaction = null);

        Task<T?> GetObject<T>(Uri uri, Transaction? transaction = null) where T: Resource;
        Task<T?> GetObject<T>(string path, Transaction? transaction = null) where T : Resource;

        Task<ArchivalGroup?> GetPopulatedArchivalGroup(Uri uri, string? version = null, Transaction? transaction = null);
        Task<ArchivalGroup?> GetPopulatedArchivalGroup(string path, string? version = null, Transaction? transaction = null);

        Task<ArchivalGroup?> CreateArchivalGroup(Uri parent, string slug, string name, Transaction? transaction = null);
        Task<ArchivalGroup?> CreateArchivalGroup(string parentPath, string slug, string name, Transaction? transaction = null);

        Task<Container?> CreateContainer(ContainerDirectory containerDirectory, Transaction? transaction = null);
             
        Task<Binary> PutBinary(BinaryFile binaryFile, Transaction? transaction = null);

        // Transactions
        Task<Transaction> BeginTransaction();
        Task CheckTransaction(Transaction tx);
        Task KeepTransactionAlive(Transaction tx);
        Task CommitTransaction(Transaction tx);
        Task RollbackTransaction(Transaction tx);

        Task Delete(Uri uri, Transaction? transaction = null);
    }
}

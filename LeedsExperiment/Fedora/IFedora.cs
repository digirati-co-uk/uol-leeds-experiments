using Fedora.ApiModel;

namespace Fedora
{
    public interface IFedora
    {
        Task<string> Proxy(string contentType, string path, string? jsonLdMode = null, bool preferContained = false);
        Task<ArchivalGroup?> CreateArchivalGroup(string parentPath, string slug, string name, Transaction? transaction = null);


        // Transactions
        Task<Transaction> BeginTransaction();
        Task CheckTransaction(Transaction tx);
        Task KeepTransactionAlive(Transaction tx);
        Task CommitTransaction(Transaction tx);
        Task RollbackTransaction(Transaction tx);
    }
}

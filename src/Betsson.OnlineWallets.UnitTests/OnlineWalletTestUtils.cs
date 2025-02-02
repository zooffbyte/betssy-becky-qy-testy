using Betsson.OnlineWallets.Data.Repositories;
using Betsson.OnlineWallets.Data.Models;
using Betsson.OnlineWallets.Services;
using Microsoft.EntityFrameworkCore;

namespace Betsson.OnlineWallets.UnitTests.TestUtils
{
    internal class OnlineWalletContextMock : DbContext
    {
        public DbSet<OnlineWalletEntry> Transactions { get; set; }

        public OnlineWalletContextMock(DbContextOptions<OnlineWalletContextMock> options) : base(options) { }
    }

    internal class OnlineWalletRepositoryMock : IOnlineWalletRepository
    {
        private readonly OnlineWalletContextMock _onlineWalletContext;

        public OnlineWalletRepositoryMock(OnlineWalletContextMock onlineWalletContext)
        {
            _onlineWalletContext = onlineWalletContext;
        }

        public async Task<OnlineWalletEntry?> GetLastOnlineWalletEntryAsync()
        {
            return await _onlineWalletContext
                .Transactions
                .OrderByDescending(onlineWalletEntry => onlineWalletEntry.EventTime)
                .FirstOrDefaultAsync();
        }

        public Task InsertOnlineWalletEntryAsync(OnlineWalletEntry onlineWalletEntry)
        {
            _onlineWalletContext.Transactions.Add(onlineWalletEntry);
            _onlineWalletContext.SaveChanges();
            return Task.CompletedTask;
        }
    }
}

using Betsson.OnlineWallets.Data.Repositories;
using Betsson.OnlineWallets.Data.Models;
using Betsson.OnlineWallets.Models;
using Betsson.OnlineWallets.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Threading.Tasks;
using Xunit;

namespace Betsson.OnlineWallets.UnitTests
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

    public class OnlineWalletServiceTests
    {
        private readonly IOnlineWalletService _wallet;

        public OnlineWalletServiceTests()
        {
            var options = new DbContextOptionsBuilder<OnlineWalletContextMock>()
                .UseInMemoryDatabase(databaseName: "TestDbContext")
                .Options;
            var context = new OnlineWalletContextMock(options);
            var repository = new OnlineWalletRepositoryMock(context);

            _wallet = new OnlineWalletService(repository);
        }

        [Fact]
        public async Task CustomerOpenBalance_ShouldBeZero()
        {
            Balance currentBalance = await _wallet.GetBalanceAsync();

            Assert.Equal(0, currentBalance.Amount);
        }
    }
}

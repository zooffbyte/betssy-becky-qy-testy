using Betsson.OnlineWallets.Data.Repositories;
using Betsson.OnlineWallets.Data.Models;
using Betsson.OnlineWallets.Models;
using Betsson.OnlineWallets.Services;
using Betsson.OnlineWallets.Exceptions;
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
        // private readonly IOnlineWalletService _wallet;

        // public OnlineWalletServiceTests()
        // {
        //     var options = new DbContextOptionsBuilder<OnlineWalletContextMock>()
        //         .UseInMemoryDatabase(databaseName: "TestDbContext")
        //         .Options;
        //     var context = new OnlineWalletContextMock(options);
        //     var repository = new OnlineWalletRepositoryMock(context);

        //     this._wallet = new OnlineWalletService(repository);
        // }

        public IOnlineWalletService WalletFixture()
        {
            var options = new DbContextOptionsBuilder<OnlineWalletContextMock>()
                .UseInMemoryDatabase(databaseName: "TestDbContext_" + System.Guid.NewGuid())
                .Options;
            var context = new OnlineWalletContextMock(options);
            var repository = new OnlineWalletRepositoryMock(context);

            return new OnlineWalletService(repository);
        }

        [Fact]
        public async Task CustomerOpenBalance_ShouldBeZero()
        {
            // Arrange
            IOnlineWalletService wallet = WalletFixture();

            // Act
            Balance currentBalance = await wallet.GetBalanceAsync();

            // Assert
            Assert.Equal(0, currentBalance.Amount);
        }

        [Fact]
        public async Task CustomerDepositAmount_ShouldBeVisibleOnAccount()
        {
            // Arrange
            IOnlineWalletService wallet = WalletFixture();
            Deposit deposit = new Deposit();
            deposit.Amount = 3;

            // Act
            Balance newBalance = await wallet.DepositFundsAsync(deposit);

            // Assert
            Assert.Equal(deposit.Amount, newBalance.Amount);
        }

        [Fact]
        public async Task CustomerDepositMultipleAmount_ShouldBeVisibleOnAccount()
        {
            // Arrange
            IOnlineWalletService wallet = WalletFixture();
            int[] amounts = {3, 4, 7};
            int expAmount = 0;
            Deposit deposit = new Deposit();

            for (int i = 0; i < amounts.Length; i++)
            {
                deposit.Amount = amounts[i];
                expAmount += amounts[i];

                // Act
                Balance newBalance = await wallet.DepositFundsAsync(deposit);

                // Assert
                Assert.Equal(expAmount, newBalance.Amount);
            }
        }

        [Fact]
        public async Task CustomerWithdrawAmount_ShouldBeVisibleOnAccount()
        {
            // Arrange
            IOnlineWalletService wallet = WalletFixture();
            Deposit deposit = new Deposit();
            deposit.Amount = 30;
            Withdrawal withdrawal = new Withdrawal();
            withdrawal.Amount = 7;

            _ = await wallet.DepositFundsAsync(deposit);

            // Act
            Balance newBalance = await wallet.WithdrawFundsAsync(withdrawal);

            // Assert
            Assert.Equal(deposit.Amount - withdrawal.Amount, newBalance.Amount);
        }

        [Fact]
        public async Task CustomerWithdrawMultipleAmount_ShouldBeVisibleOnAccount()
        {
            // Arrange
            IOnlineWalletService wallet = WalletFixture();
            int initialAmount = 40;
            int[] amounts = {3, 4, 7};
            int expAmount = initialAmount;
            Deposit deposit = new Deposit();
            deposit.Amount = initialAmount;
            Withdrawal withdrawal = new Withdrawal();
            withdrawal.Amount = 7;

            _ = await wallet.DepositFundsAsync(deposit);

            for (int i = 0; i < amounts.Length; i++)
            {
                withdrawal.Amount = amounts[i];
                expAmount -= amounts[i];

                // Act
                Balance newBalance = await wallet.WithdrawFundsAsync(withdrawal);

                // Assert
                Assert.Equal(expAmount, newBalance.Amount);
            }
        }

        [Fact]
        public async Task CustomerReadBalanceAfterMultipleAmount_ShouldShowProperAmountOnAccount()
        {
            // Arrange
            IOnlineWalletService wallet = WalletFixture();
            Deposit deposit = new Deposit();
            deposit.Amount = 60;
            Withdrawal withdrawal = new Withdrawal();
            withdrawal.Amount = 33;

            // Act
            _ = await wallet.DepositFundsAsync(deposit);
            Balance currentBalanceAfterFirstDeposit = await wallet.GetBalanceAsync();

            // Assert
            Assert.Equal(deposit.Amount, currentBalanceAfterFirstDeposit.Amount);

            // Act
            _ = await wallet.WithdrawFundsAsync(withdrawal);
            Balance currentBalanceAfterFirstWithdrawal = await wallet.GetBalanceAsync();

            // Assert
            Assert.Equal(deposit.Amount - withdrawal.Amount, currentBalanceAfterFirstWithdrawal.Amount);
        }

        [Fact]
        public async Task BankingSystemDepositNegativeAmount_ShouldBePresentAsNegativeAndBeVisible()
        {
            // Arrange
            IOnlineWalletService wallet = WalletFixture();
            Deposit deposit = new Deposit();
            deposit.Amount = -10;

            // Act
            Balance newBalance = await wallet.DepositFundsAsync(deposit);

            // Assert
            Assert.Equal(deposit.Amount, newBalance.Amount);
        }

        [Fact]
        public async Task BankingSystemWithdrawNegativeAmount_ShouldBeAddedToFundAndBeVisible()
        {
            // Arrange
            IOnlineWalletService wallet = WalletFixture();
            Deposit deposit = new Deposit();
            deposit.Amount = 10;
            Withdrawal withdrawal = new Withdrawal();
            withdrawal.Amount = -3;

            _ = await wallet.DepositFundsAsync(deposit);

            // Act
            Balance newBalance = await wallet.WithdrawFundsAsync(withdrawal);

            // Assert
            Assert.Equal(deposit.Amount - withdrawal.Amount, newBalance.Amount);
        }

        [Fact]
        public async Task CustomerTryToWithdrawMoreThanAvailableAmount_ShouldResultInError()
        {
            // Arrange
            IOnlineWalletService wallet = WalletFixture();
            Deposit deposit = new Deposit();
            deposit.Amount = 10;
            Withdrawal withdrawal = new Withdrawal();
            withdrawal.Amount = 33;

            _ = await wallet.DepositFundsAsync(deposit);

            // Act
            Func<Task> action = async () => await wallet.WithdrawFundsAsync(withdrawal);
            Balance currentBalance = await wallet.GetBalanceAsync();

            // Assert
            var ex = await Assert.ThrowsAsync<InsufficientBalanceException>(action);
            Assert.Contains("Invalid withdrawal amount. There are insufficient funds.", ex.Message);
            Assert.Equal(deposit.Amount, currentBalance.Amount);
        }
    }
}

using Betsson.OnlineWallets.Services;
using Betsson.OnlineWallets.Web.Controllers;
using Betsson.OnlineWallets.Web.Models;
using Betsson.OnlineWallets.Web.Mappers;
using Betsson.OnlineWallets.Exceptions;
using Betsson.OnlineWallets.UnitTests.TestUtils;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Xunit;

namespace Betsson.OnlineWallets.UnitTests.APITests
{
    public class OnlineWalletAPITests
    {
        public static IOnlineWalletService WalletFixture()
        {
            var options = new DbContextOptionsBuilder<OnlineWalletContextMock>()
                .UseInMemoryDatabase(databaseName: "TestDbContext_" + System.Guid.NewGuid())
                .Options;

            var context = new OnlineWalletContextMock(options);

            var repository = new OnlineWalletRepositoryMock(context);

            return new OnlineWalletService(repository);
        }

        public OnlineWalletController ControllerFixture()
        {
            // ILogger<OnlineWalletController> logger = null;

            var mappingConfig = new MapperConfiguration(mc =>
            {
                mc.AddProfile(new OnlineWalletMappingProfile());
            });
            IMapper mapper = mappingConfig.CreateMapper();

            IOnlineWalletService onlineWalletService = WalletFixture();

            return new OnlineWalletController(null, mapper, onlineWalletService);
        }

        [Fact]
        public async Task CustomerOpenBalance_ShouldBeZero()
        {
            // Arrange
            OnlineWalletController controller = ControllerFixture();

            // Act
            var actionResult = await controller.Balance() as ActionResult<BalanceResponse>;

            // Assert
            Assert.IsType<ActionResult<BalanceResponse>>(actionResult);

            var okObjectResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.NotNull(okObjectResult);
            Assert.Equal(200, okObjectResult.StatusCode);

            var balanceResponse = okObjectResult.Value as BalanceResponse;
            Assert.NotNull(balanceResponse);
            Assert.Equal(0, balanceResponse.Amount);
        }

        [Fact]
        public async Task CustomerDepositAmount_ShouldBeVisibleOnAccount()
        {
            // Arrange
            OnlineWalletController controller = ControllerFixture();
            DepositRequest depositRequest = new DepositRequest();
            depositRequest.Amount = 3;

            // Act
            var actionResult = await controller.Deposit(depositRequest) as ActionResult<BalanceResponse>;

            // Assert
            Assert.IsType<ActionResult<BalanceResponse>>(actionResult);

            var okObjectResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.NotNull(okObjectResult);
            Assert.Equal(200, okObjectResult.StatusCode);

            var balanceResponse = okObjectResult.Value as BalanceResponse;
            Assert.NotNull(balanceResponse);
            Assert.Equal(depositRequest.Amount, balanceResponse.Amount);
        }

        [Fact]
        public async Task CustomerDepositMultipleAmount_ShouldBeVisibleOnAccount()
        {
            // Arrange
            OnlineWalletController controller = ControllerFixture();
            int[] amounts = {3, 4, 7};
            int expAmount = 0;
            DepositRequest depositRequest = new DepositRequest();

            for (int i = 0; i < amounts.Length; i++)
            {
                depositRequest.Amount = amounts[i];
                expAmount += amounts[i];

                // Act
                var actionResult = await controller.Deposit(depositRequest) as ActionResult<BalanceResponse>;

                // Assert
                Assert.IsType<ActionResult<BalanceResponse>>(actionResult);

                var okObjectResult = Assert.IsType<OkObjectResult>(actionResult.Result);
                Assert.NotNull(okObjectResult);
                Assert.Equal(200, okObjectResult.StatusCode);

                var balanceResponse = okObjectResult.Value as BalanceResponse;
                Assert.NotNull(balanceResponse);
                Assert.Equal(expAmount, balanceResponse.Amount);
            }
        }

        [Fact]
        public async Task CustomerWithdrawAmount_ShouldBeVisibleOnAccount()
        {
            // Arrange
            OnlineWalletController controller = ControllerFixture();
            DepositRequest depositRequest = new DepositRequest();
            depositRequest.Amount = 30;
            WithdrawalRequest withdrawalRequest = new WithdrawalRequest();
            withdrawalRequest.Amount = 7;

            _ = await controller.Deposit(depositRequest) as ActionResult<BalanceResponse>;

            // Act
            var actionResult = await controller.Withdraw(withdrawalRequest) as ActionResult<BalanceResponse>;

            // Assert
            Assert.IsType<ActionResult<BalanceResponse>>(actionResult);

            var okObjectResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.NotNull(okObjectResult);
            Assert.Equal(200, okObjectResult.StatusCode);

            var balanceResponse = okObjectResult.Value as BalanceResponse;
            Assert.NotNull(balanceResponse);
            Assert.Equal(depositRequest.Amount - withdrawalRequest.Amount, balanceResponse.Amount);
        }

        [Fact]
        public async Task CustomerWithdrawMultipleAmount_ShouldBeVisibleOnAccount()
        {
            // Arrange
            OnlineWalletController controller = ControllerFixture();
            int initialAmount = 40;
            int[] amounts = {3, 4, 7};
            int expAmount = initialAmount;
            DepositRequest depositRequest = new DepositRequest();
            depositRequest.Amount = initialAmount;
            WithdrawalRequest withdrawalRequest = new WithdrawalRequest();
            withdrawalRequest.Amount = 7;

            _ = await controller.Deposit(depositRequest) as ActionResult<BalanceResponse>;

            for (int i = 0; i < amounts.Length; i++)
            {
                withdrawalRequest.Amount = amounts[i];
                expAmount -= amounts[i];

                // Act
                var actionResult = await controller.Withdraw(withdrawalRequest) as ActionResult<BalanceResponse>;

                // Assert
                Assert.IsType<ActionResult<BalanceResponse>>(actionResult);

                var okObjectResult = Assert.IsType<OkObjectResult>(actionResult.Result);
                Assert.NotNull(okObjectResult);
                Assert.Equal(200, okObjectResult.StatusCode);

                var balanceResponse = okObjectResult.Value as BalanceResponse;
                Assert.NotNull(balanceResponse);
                Assert.Equal(expAmount, balanceResponse.Amount);
            }
        }

        [Fact]
        public async Task CustomerReadBalanceAfterMultipleAmount_ShouldShowProperAmountOnAccount()
        {
            // Arrange
            OnlineWalletController controller = ControllerFixture();
            DepositRequest depositRequest = new DepositRequest();
            depositRequest.Amount = 60;
            WithdrawalRequest withdrawalRequest = new WithdrawalRequest();
            withdrawalRequest.Amount = 33;

            // Act
            _ = await controller.Deposit(depositRequest) as ActionResult<BalanceResponse>;
            var actionResult = await controller.Balance() as ActionResult<BalanceResponse>;

            // Assert
            Assert.IsType<ActionResult<BalanceResponse>>(actionResult);

            var okObjectResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.NotNull(okObjectResult);
            Assert.Equal(200, okObjectResult.StatusCode);

            var balanceResponse = okObjectResult.Value as BalanceResponse;
            Assert.NotNull(balanceResponse);
            Assert.Equal(depositRequest.Amount, balanceResponse.Amount);

            // Act
            _ = await controller.Withdraw(withdrawalRequest) as ActionResult<BalanceResponse>;
            var actionResult_2 = await controller.Balance() as ActionResult<BalanceResponse>;

            // Assert
            Assert.IsType<ActionResult<BalanceResponse>>(actionResult_2);

            var okObjectResult_2 = Assert.IsType<OkObjectResult>(actionResult_2.Result);
            Assert.NotNull(okObjectResult_2);
            Assert.Equal(200, okObjectResult_2.StatusCode);

            var balanceResponse_2 = okObjectResult_2.Value as BalanceResponse;
            Assert.NotNull(balanceResponse_2);
            Assert.Equal(depositRequest.Amount - withdrawalRequest.Amount, balanceResponse_2.Amount);
        }

        [Fact]
        public async Task BankingSystemDepositNegativeAmount_ShouldBePresentAsNegativeAndBeVisible()
        {
            // Arrange
            OnlineWalletController controller = ControllerFixture();
            DepositRequest depositRequest = new DepositRequest();
            depositRequest.Amount = -10;

            // Act
            var actionResult = await controller.Deposit(depositRequest) as ActionResult<BalanceResponse>;

            // Assert
            Assert.IsType<ActionResult<BalanceResponse>>(actionResult);

            var okObjectResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.NotNull(okObjectResult);
            Assert.Equal(200, okObjectResult.StatusCode);

            var balanceResponse = okObjectResult.Value as BalanceResponse;
            Assert.NotNull(balanceResponse);
            Assert.Equal(depositRequest.Amount, balanceResponse.Amount);
        }

        [Fact]
        public async Task BankingSystemWithdrawNegativeAmount_ShouldBeAddedToFundAndBeVisible()
        {
            // Arrange
            OnlineWalletController controller = ControllerFixture();
            DepositRequest depositRequest = new DepositRequest();
            depositRequest.Amount = 10;
            WithdrawalRequest withdrawalRequest = new WithdrawalRequest();
            withdrawalRequest.Amount = -3;

            _ = await controller.Deposit(depositRequest) as ActionResult<BalanceResponse>;

            // Act
            var actionResult = await controller.Withdraw(withdrawalRequest) as ActionResult<BalanceResponse>;

            // Assert
            Assert.IsType<ActionResult<BalanceResponse>>(actionResult);

            var okObjectResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.NotNull(okObjectResult);
            Assert.Equal(200, okObjectResult.StatusCode);

            var balanceResponse = okObjectResult.Value as BalanceResponse;
            Assert.NotNull(balanceResponse);
            Assert.Equal(depositRequest.Amount - withdrawalRequest.Amount, balanceResponse.Amount);
        }

        [Fact]
        public async Task CustomerTryToWithdrawMoreThanAvailableAmount_ShouldResultInError()
        {
            // Arrange
            OnlineWalletController controller = ControllerFixture();
            DepositRequest depositRequest = new DepositRequest();
            depositRequest.Amount = 10;
            WithdrawalRequest withdrawalRequest = new WithdrawalRequest();
            withdrawalRequest.Amount = 33;

            _ = await controller.Deposit(depositRequest) as ActionResult<BalanceResponse>;

            // Act
            Func<Task> action = async () => await controller.Withdraw(withdrawalRequest);
            var actionResult_2 = await controller.Balance() as ActionResult<BalanceResponse>;

            // Assert
            var ex = await Assert.ThrowsAsync<InsufficientBalanceException>(action);
            Assert.Contains("Invalid withdrawal amount. There are insufficient funds.", ex.Message);

            Assert.IsType<ActionResult<BalanceResponse>>(actionResult_2);

            var okObjectResult_2 = Assert.IsType<OkObjectResult>(actionResult_2.Result);
            Assert.NotNull(okObjectResult_2);
            Assert.Equal(200, okObjectResult_2.StatusCode);

            var balanceResponse_2 = okObjectResult_2.Value as BalanceResponse;
            Assert.NotNull(balanceResponse_2);
            Assert.Equal(depositRequest.Amount, balanceResponse_2.Amount);
        }
    }
}

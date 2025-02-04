using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using System.Text.Json;

namespace Betsson.OnlineWallets.E2EAPITests.E2EAPITests
{
    [TestClass]
    public class OnlineWalletE2EAPITests : PlaywrightTest
    {
        private IAPIRequestContext Request = null!;

        [TestInitialize]
        public async Task SetUpAPITesting()
        {
            await CreateAPIRequestContext();
        }

        private async Task CreateAPIRequestContext()
        {
            var headers = new Dictionary<string, string>();
            headers.Add("Accept", "application/json");
            Request = await this.Playwright.APIRequest.NewContextAsync(new() {
                BaseURL = "http://localhost:8080",
                ExtraHTTPHeaders = headers,
            });
        }

        [TestCleanup]
        public async Task TearDownAPITesting()
        {
            await Request.DisposeAsync();
        }

        [TestMethod]
        public async Task CustomerOpenBalance_ShouldBeZero()
        {
            var balanceResponse = await Request.GetAsync("/onlinewallet/balance");
            await Expect(balanceResponse).ToBeOKAsync();

            var balanceJsonResponse = await balanceResponse.JsonAsync();
            Assert.IsNotNull(balanceJsonResponse);
            Assert.AreEqual(0, balanceJsonResponse?.GetProperty("amount").GetDecimal());
        }

        [TestMethod]
        public async Task CustomerDepositAmount_ShouldBeVisibleOnAccount()
        {
            var depositData = new Dictionary<string, int>
            {
                { "amount", 3 }
            };

            var balanceResponse = await Request.PostAsync("/onlinewallet/deposit", new() { DataObject = depositData });
            await Expect(balanceResponse).ToBeOKAsync();

            var balanceJsonResponse = await balanceResponse.JsonAsync();
            Assert.IsNotNull(balanceJsonResponse);
            Assert.AreEqual(3, balanceJsonResponse?.GetProperty("amount").GetDecimal());
        }

        [TestMethod]
        public async Task CustomerDepositMultipleAmount_ShouldBeVisibleOnAccount()
        {
            int[] amounts = {3, 4, 7};
            int expAmount = 0;

            for (int i = 0; i < amounts.Length; i++)
            {
                var depositData = new Dictionary<string, int>
                {
                    { "amount", amounts[i] }
                };
                expAmount += amounts[i];

                var balanceResponse = await Request.PostAsync("/onlinewallet/deposit", new() { DataObject = depositData });
                await Expect(balanceResponse).ToBeOKAsync();

                var balanceJsonResponse = await balanceResponse.JsonAsync();
                Assert.IsNotNull(balanceJsonResponse);
                Assert.AreEqual(expAmount, balanceJsonResponse?.GetProperty("amount").GetDecimal());
            }
        }

        [TestMethod]
        public async Task CustomerWithdrawAmount_ShouldBeVisibleOnAccount()
        {
            var depositData = new Dictionary<string, int>
            {
                { "amount", 30 }
            };
            var withdrawalData = new Dictionary<string, int>
            {
                { "amount", 7 }
            };

            var balanceDepositResponse = await Request.PostAsync("/onlinewallet/deposit", new() { DataObject = depositData });
            await Expect(balanceDepositResponse).ToBeOKAsync();

            var balanceWithdrawalResponse = await Request.PostAsync("/onlinewallet/withdraw", new() { DataObject = withdrawalData });
            await Expect(balanceWithdrawalResponse).ToBeOKAsync();

            var balanceWithdrawalJsonResponse = await balanceWithdrawalResponse.JsonAsync();
            Assert.IsNotNull(balanceWithdrawalJsonResponse);
            Assert.AreEqual(23, balanceWithdrawalJsonResponse?.GetProperty("amount").GetDecimal());
        }

        [TestMethod]
        public async Task CustomerWithdrawMultipleAmount_ShouldBeVisibleOnAccount()
        {
            int initialAmount = 40;
            int[] amounts = {3, 4, 7};
            int expAmount = initialAmount;
            var depositData = new Dictionary<string, int>
            {
                { "amount", initialAmount }
            };

            var balanceDepositResponse = await Request.PostAsync("/onlinewallet/deposit", new() { DataObject = depositData });
            await Expect(balanceDepositResponse).ToBeOKAsync();

            for (int i = 0; i < amounts.Length; i++)
            {
                var withdrawalData = new Dictionary<string, int>
                {
                    { "amount", amounts[i] }
                };
                expAmount -= amounts[i];

                var balanceWithdrawalResponse = await Request.PostAsync("/onlinewallet/withdraw", new() { DataObject = withdrawalData });
                await Expect(balanceWithdrawalResponse).ToBeOKAsync();

                var balanceWithdrawalJsonResponse = await balanceWithdrawalResponse.JsonAsync();
                Assert.IsNotNull(balanceWithdrawalJsonResponse);
                Assert.AreEqual(expAmount, balanceWithdrawalJsonResponse?.GetProperty("amount").GetDecimal());
            }
        }

        [TestMethod]
        public async Task CustomerReadBalanceAfterMultipleAmount_ShouldShowProperAmountOnAccount()
        {
            var depositData = new Dictionary<string, int>
            {
                { "amount", 60 }
            };
            var withdrawalData = new Dictionary<string, int>
            {
                { "amount", 33 }
            };

            // Deposit
            var balanceDepositResponse = await Request.PostAsync("/onlinewallet/deposit", new() { DataObject = depositData });
            await Expect(balanceDepositResponse).ToBeOKAsync();

            var balanceResponse = await Request.GetAsync("/onlinewallet/balance");
            await Expect(balanceResponse).ToBeOKAsync();

            var balanceJsonResponse = await balanceResponse.JsonAsync();
            Assert.IsNotNull(balanceJsonResponse);
            Assert.AreEqual(60, balanceJsonResponse?.GetProperty("amount").GetDecimal());

            // Withdraw
            var balanceWithdrawalResponse = await Request.PostAsync("/onlinewallet/withdraw", new() { DataObject = withdrawalData });
            await Expect(balanceWithdrawalResponse).ToBeOKAsync();

            var balanceResponse_2 = await Request.GetAsync("/onlinewallet/balance");
            await Expect(balanceResponse_2).ToBeOKAsync();

            var balanceJsonResponse_2 = await balanceResponse_2.JsonAsync();
            Assert.IsNotNull(balanceJsonResponse_2);
            Assert.AreEqual(27, balanceJsonResponse_2?.GetProperty("amount").GetDecimal());
        }

        [TestMethod]
        public async Task BankingSystemDepositNegativeAmount_ShouldResultInError()
        {
            var depositData = new Dictionary<string, int>
            {
                { "amount", -10 }
            };

            var balanceResponse = await Request.PostAsync("/onlinewallet/deposit", new() { DataObject = depositData });
            await Expect(balanceResponse).Not.ToBeOKAsync();

            var balanceJsonResponse = await balanceResponse.JsonAsync();
            Assert.IsNotNull(balanceJsonResponse);
            Assert.AreEqual(400, balanceJsonResponse?.GetProperty("status").GetDecimal());
            Assert.AreEqual("One or more validation errors occurred.", balanceJsonResponse?.GetProperty("title").GetString());
            Assert.AreEqual("'Amount' must be greater than or equal to '0'.", balanceJsonResponse?.GetProperty("errors").GetProperty("Amount")[0].GetString());
        }

        [TestMethod]
        public async Task BankingSystemWithdrawNegativeAmount_ShouldResultInError()
        {
            var depositData = new Dictionary<string, int>
            {
                { "amount", 10 }
            };
            var withdrawalData = new Dictionary<string, int>
            {
                { "amount", -3 }
            };

            var balanceDepositResponse = await Request.PostAsync("/onlinewallet/deposit", new() { DataObject = depositData });
            await Expect(balanceDepositResponse).ToBeOKAsync();

            var balanceWithdrawalResponse = await Request.PostAsync("/onlinewallet/withdraw", new() { DataObject = withdrawalData });
            await Expect(balanceWithdrawalResponse).Not.ToBeOKAsync();

            var balanceWithdrawalJsonResponse = await balanceWithdrawalResponse.JsonAsync();
            Assert.IsNotNull(balanceWithdrawalJsonResponse);
            Assert.AreEqual(400, balanceWithdrawalJsonResponse?.GetProperty("status").GetDecimal());
            Assert.AreEqual("One or more validation errors occurred.", balanceWithdrawalJsonResponse?.GetProperty("title").GetString());
            Assert.AreEqual("'Amount' must be greater than or equal to '0'.", balanceWithdrawalJsonResponse?.GetProperty("errors").GetProperty("Amount")[0].GetString());
        }

        [TestMethod]
        public async Task CustomerTryToWithdrawMoreThanAvailableAmount_ShouldResultInError()
        {
            var depositData = new Dictionary<string, int>
            {
                { "amount", 10 }
            };
            var withdrawalData = new Dictionary<string, int>
            {
                { "amount", 33 }
            };

            var balanceDepositResponse = await Request.PostAsync("/onlinewallet/deposit", new() { DataObject = depositData });
            await Expect(balanceDepositResponse).ToBeOKAsync();

            var balanceWithdrawalResponse = await Request.PostAsync("/onlinewallet/withdraw", new() { DataObject = withdrawalData });
            await Expect(balanceWithdrawalResponse).Not.ToBeOKAsync();

            var balanceWithdrawalJsonResponse = await balanceWithdrawalResponse.JsonAsync();
            Assert.IsNotNull(balanceWithdrawalJsonResponse);
            Assert.AreEqual(400, balanceWithdrawalJsonResponse?.GetProperty("status").GetDecimal());
            Assert.AreEqual("Invalid withdrawal amount. There are insufficient funds.", balanceWithdrawalJsonResponse?.GetProperty("title").GetString());
        }

        [TestMethod]
        public async Task CustomerDepositHugeAmount_ShouldResultInError()
        {
            var depositData = new Dictionary<string, string>
            {
                { "amount", "79228162514264337593543950336" }
            };

            var balanceResponse = await Request.PostAsync("/onlinewallet/deposit", new() { DataObject = depositData });
            await Expect(balanceResponse).Not.ToBeOKAsync();

            var balanceJsonResponse = await balanceResponse.JsonAsync();
            Assert.IsNotNull(balanceJsonResponse);
            Assert.AreEqual(400, balanceJsonResponse?.GetProperty("status").GetDecimal());
            Assert.AreEqual("One or more validation errors occurred.", balanceJsonResponse?.GetProperty("title").GetString());
            Assert.AreEqual("The depositRequest field is required.", balanceJsonResponse?.GetProperty("errors").GetProperty("depositRequest")[0].GetString());
            Assert.AreEqual("The JSON value could not be converted to System.Decimal. Path: $.amount | LineNumber: 0 | BytePositionInLine: 41.", balanceJsonResponse?.GetProperty("errors").GetProperty("$.amount")[0].GetString());
        }
    }
}

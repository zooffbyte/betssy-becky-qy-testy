#!/bin/bash

set -e

declare -a TestCaseArray=(
    CustomerOpenBalance_ShouldBeZero
    CustomerDepositAmount_ShouldBeVisibleOnAccount
    CustomerDepositMultipleAmount_ShouldBeVisibleOnAccount
    CustomerWithdrawAmount_ShouldBeVisibleOnAccount
    CustomerWithdrawMultipleAmount_ShouldBeVisibleOnAccount
    CustomerReadBalanceAfterMultipleAmount_ShouldShowProperAmountOnAccount
    BankingSystemDepositNegativeAmount_ShouldResultInError
    BankingSystemWithdrawNegativeAmount_ShouldResultInError
    CustomerTryToWithdrawMoreThanAvailableAmount_ShouldResultInError
    CustomerDepositHugeAmount_ShouldResultInError
)

for tc in ${TestCaseArray[@]}
do
    docker run -dit --rm --name betsson-online-wallet-e2etests -p 8080:8080 betsson-online-wallet
    dotnet test --filter ${tc} || :
    docker stop betsson-online-wallet-e2etests
done

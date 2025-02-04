@echo off

set TestCaseArray[0]=CustomerOpenBalance_ShouldBeZero
set TestCaseArray[1]=CustomerDepositAmount_ShouldBeVisibleOnAccount
set TestCaseArray[2]=CustomerDepositMultipleAmount_ShouldBeVisibleOnAccount
set TestCaseArray[3]=CustomerWithdrawAmount_ShouldBeVisibleOnAccount
set TestCaseArray[4]=CustomerWithdrawMultipleAmount_ShouldBeVisibleOnAccount
set TestCaseArray[5]=CustomerReadBalanceAfterMultipleAmount_ShouldShowProperAmountOnAccount
set TestCaseArray[6]=BankingSystemDepositNegativeAmount_ShouldResultInError
set TestCaseArray[7]=BankingSystemWithdrawNegativeAmount_ShouldResultInError
set TestCaseArray[8]=CustomerTryToWithdrawMoreThanAvailableAmount_ShouldResultInError
set TestCaseArray[9]=CustomerDepositHugeAmount_ShouldResultInError

set "x=0"

:SymLoop
if not defined TestCaseArray[%x%] goto :endLoop
call docker run -dit --rm --name betsson-online-wallet-e2etests -p 8080:8080 betsson-online-wallet
call dotnet test --filter %%TestCaseArray[%x%]%%
call docker stop betsson-online-wallet-e2etests
SET /a "x+=1"
GOTO :SymLoop

:endLoop

# E2E Automated Testing of Angor 

Automated testing of Angor with C#, .NET 8, Playwright, and xUnit.

# Setup

### 1. Clone the repo
```bash
git clone https://github.com/mistic0xb/e2e.git
cd e2e
```

### 2. Install dependencies
```bash
dotnet restore
```
```bash
# You should have node 20+ install
npx playwright install --with-deps chromium
```
### 3. Run tests (various options)
- **For testing in console: (Option 1)**
```bash
dotnet test
```

- **For testing with step-by-step UI-mode: (Option 2)**
```bash
PWDEBUG=1 dotnet test
```

- **For testing with automated UI-mode: (Option 3)**
```bash
HEADED=1 dotnet test
```

## Run specific tests
```bash
# Run specific test suite
dotnet test --filter "FullyQualifiedName~TEST_1"
```

# What does this test cover ?

## TEST_1: Investor and Founder Workflow

- The `TEST_1` test suite, located in the `e2e.Tests.TEST_1` namespace, automates an end-to-end workflow for testing wallet creation, investment, and founder approval processes on the Angor test platform (`https://test.angor.io`). It uses Playwright for browser automation and XUnit for test execution. Below is a summary of the test scenarios.

### Test Scenarios

#### 1.1 Create Investor 1 Wallet and Invest
- **Objective**: Simulate an investor creating a wallet, obtaining test Bitcoin (TBTC), and investing in a predefined project.
- **Steps**:
  1. Navigate to the wallet page and create a new wallet with a generated recovery phrase.
  2. Set a default password (`123`) for the wallet.
  3. Retrieve and store the wallet's seed phrase.
  4. Obtain TBTC by clicking "Get Test Coins" and wait for a positive balance (with retries).
  5. Search for the predefined project (`auto_test1`, ID: `angor1qe9cpnllqu5tjws3pawy7k2ek5ue4phw5e4g6lh`).
  6. Invest 1.0 TBTC in the project using the "Priority" investment option, confirming with the wallet password.
  7. Verify the investment by navigating to the portfolio page.
  8. Store the investor's wallet information (name: `investor1`, seed phrase) for later use.
- **Assertions**: Confirms that the investor’s wallet is created and stored successfully.
- **Status**: Active (`Test1_CreateInvestor1AndInvest`).

#### 1.2 Founder Approves Investments and Manages Funds
- **Objective**: Simulate a founder importing their wallet, approving investments, and managing funds for the project.
- **Steps**:
  1. Launch a new browser context for the founder.
  2. Import the founder’s wallet using a predefined seed phrase (`frown skill mail ... town`).
  3. Navigate to the "Founder" section and scan for projects.
  4. Select the `auto_test1` project and approve all pending investments with the wallet password.
  5. For each investor (currently only `investor1`):
     - Import the investor’s wallet using the stored seed phrase.
     - Navigate to the portfolio, refresh, and complete the investment process with the wallet password.
     - Wipe the investor’s wallet data via the "Settings" page to reset the state.
  6. Manage project funds by selecting and claiming TBTC for the project.
- **Assertions**: Ensures the founder’s approval and fund management complete successfully.
- **Status**: Active (`Test2_FounderAcceptsInvestments`).

#### 1.3 and 1.4 Create Additional Investor Wallets and Invest (Optional)
- **Objective**: Simulate additional investors (`investor2` and `investor3`) performing the same workflow as `investor1`.
- **Details**: Identical to step 1.1, creating wallets and investing 1.0 TBTC in the `auto_test1` project.
- **Status**: Disabled (commented out in `InvestorFounderTests.cs`). Can be enabled by uncommenting `Test1_CreateInvestor2AndInvest` and `Test1_CreateInvestor3AndInvest`.

#### 1.5–1.7 Founder Stages, Penalty, and Recovery (Not Implemented)
- **Objective**: Test additional scenarios where the founder progresses project stages, an investor enters a penalty state, and recovers coins.
- **Details**:
  - 1.5: Founder advances the `auto_test1` project to stage 1.
  - 1.6: Investor 1 enters a penalty state (e.g., due to missed actions or project conditions).
  - 1.7: Investor 1 recovers their coins from the penalty state.
- **Status**: Not implemented in the current codebase. These steps are planned but require additional test methods.

### Notes
- **Dependencies**: The tests rely on the `WalletTestHelper` class for reusable wallet and investment operations, Playwright for browser automation, and a custom logger (`TestLogger`) for logging.
- **Test Environment**: The tests interact with the Angor test platform at `https://test.angor.io`. Ensure the platform is accessible and the project `auto_test1` exists before running tests.
- **Future Enhancements**:
  - Enable tests for `investor2` and `investor3` by uncommenting the respective test methods.
  - Implement tests for steps 1.5–1.7 to cover project stage progression, penalties, and coin recovery.
- **Running Tests**: Use `dotnet test` or an IDE’s test runner (e.g., Visual Studio Test Explorer) to execute the tests.

This test suite validates critical user flows for investors and founders, ensuring the wallet and investment functionalities work as expected on the Angor platform.
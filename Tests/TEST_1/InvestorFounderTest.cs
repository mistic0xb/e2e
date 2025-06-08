using e2e.Tests.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace e2e.Tests.TEST_1;

[Collection("InvestorFounderTests")]
[TestCaseOrderer("e2e.Tests.Shared.AlphabeticalTestOrderer", "e2e")]
public class InvestorFounderTests : PageTest
{
    private const double INVESTMENT_AMOUNT = 1.0;
    private const string FOUNDER_WALLET_PHRASE = "frown skill mail speak clever hour fury bonus profit doll pioneer town";

    // Static storage for wallet info across test runs
    private static readonly Dictionary<string, WalletInfo> StoredWallets = new();

    // Initialize Logger
    private readonly ILogger<InvestorFounderTests> _logger = TestLogger.Create<InvestorFounderTests>();

    public class WalletInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = WalletTestHelper.DEFAULT_PASSWORD;
        public string WalletSeed { get; set; } = string.Empty;
    }

    [Fact]
    // Investor 1
    public async Task Test1_CreateInvestor1AndInvest()
    {
        _logger.LogInformation("=== TEST_1[1.1]: Creating Investor 1 ===");
        await CreateInvestorAndInvest("investor1");
        Assert.True(StoredWallets.ContainsKey("investor1"));
        _logger.LogInformation("investor1 wallet stored for future tests");
    }

    // [Fact]
    // // Investor 2
    // public async Task Test1_CreateInvestor2AndInvest()
    // {
        // _logger.LogInformation("=== TEST_1[1.1]: Creating Investor 2 ===");
    //     await CreateInvestorAndInvest("investor2");
    //     Assert.True(StoredWallets.ContainsKey("investor2"));
        // _logger.LogInformation("investor2 wallet stored for future tests");
    // }

    // [Fact]
    // // Investor 3
    // public async Task Test1_CreateInvestor3AndInvest()
    // {
    //     Console.WriteLine("=== TEST_1[1.1]: Creating Investor 3 ===");
    //     await CreateInvestorAndInvest("investor3");
    //     Assert.True(StoredWallets.ContainsKey("investor3"));
    //     Console.WriteLine("investor3 wallet stored for future tests");
    // }

    [Fact]
    public async Task Test2_FounderAcceptsInvestments()
    {
        _logger.LogInformation("=== TEST_1 [1.2]: Founder Accepts Investments ===");

        // Create new browser context for founder
        var browser = await Playwright.Chromium.LaunchAsync();
        var context = await browser.NewContextAsync();
        var founderPage = await context.NewPageAsync();

        try
        {
            await FounderApprovalFlow(founderPage);

            // After founder approves, handle all investors
            await HandleAllInvestorsFlow(founderPage);

            _logger.LogInformation("Founder approval and investor flows completed!");
        }
        finally
        {
            await context.CloseAsync();
            await browser.CloseAsync();
        }
    }

    private async Task CreateInvestorAndInvest(string investorName)
    {
        try
        {
            _logger.LogInformation($"Starting wallet creation for {investorName}...");

            // Step 1: Create Wallet
            var walletSeed = await WalletTestHelper.CreateWalletFlow(Page);
            _logger.LogInformation($"Wallet seed obtained for {investorName}: {walletSeed}");

            // Step 2: Get TBTC
            var balance = await WalletTestHelper.GetTestBTCAndWaitForBalance(Page);

            // Step 3: Invest in project
            await WalletTestHelper.InvestInProject(Page, INVESTMENT_AMOUNT);
            _logger.LogInformation($"Investment completed: {INVESTMENT_AMOUNT} BTC");

            // Store wallet info
            StoredWallets[investorName] = new WalletInfo
            {
                Name = investorName,
                WalletSeed = walletSeed,
            };

            _logger.LogInformation($"{investorName} process completed successfully!");
        }
        catch (Exception e)
        {
            _logger.LogError($"Error creating {investorName}: {e.Message}");
            throw;
        }
    }

    private async Task FounderApprovalFlow(IPage founderPage)
    {
        _logger.LogInformation("️Starting Founder approval flow...");

        // Steps 1-9: Import founder wallet
        await ImportFounderWallet(founderPage);

        // Steps 8-17: Founder operations
        _logger.LogInformation("Handling founder operations...");

        // Step 8: Click link "Founder"
        await founderPage.GetByRole(AriaRole.Link, new() { Name = "Founder" }).ClickAsync();

        // Step 9: Click button "Scan for Founder Projects"
        await founderPage.GetByRole(AriaRole.Button, new() { Name = "Scan for Founder Projects" }).ClickAsync();

        // Step 10: Click link "auto_test1"
        await founderPage.GetByRole(AriaRole.Link, new() { Name = "auto_test1" }).ClickAsync();

        // Step 11: Click button "Approve Investments"
        await founderPage.GetByRole(AriaRole.Link, new() { Name = "Approve Investments" }).ClickAsync();

        // Step 12: Dialog box operations
        _logger.LogInformation("Handling approval dialog...");
        await founderPage.WaitForSelectorAsync("input[type='password']", new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        var passwordInput = founderPage.Locator("input[type='password']");
        await passwordInput.FillAsync("123");
        await passwordInput.PressAsync("Enter");

        // Step x: Click button "Refresh"
        await founderPage.GetByRole(AriaRole.Button, new() { Name = "Refresh" }).ClickAsync();

        // Step 13:
        await founderPage.GetByRole(AriaRole.Button, new() { Name = "Approve All" }).ClickAsync();

        // Step 14: Click link "Founder"
        await founderPage.GetByRole(AriaRole.Link, new() { Name = "Founder" }).ClickAsync();

        // Step 15: Click link "auto_test1"
        await founderPage.GetByRole(AriaRole.Link, new() { Name = "auto_test1" }).ClickAsync();

        _logger.LogInformation("Founder approval flow completed!");
    }

    private async Task ImportFounderWallet(IPage founderPage)
    {
        _logger.LogInformation("Importing founder wallet...");

        // Step 1: Goto wallet page
        await founderPage.GotoAsync(WalletTestHelper.WALLET_URL);

        // Step 2: Click "Import wallet"
        await founderPage.GetByRole(AriaRole.Button, new() { Name = "Import Wallet" }).ClickAsync();

        // Step 3: Select textarea and enter wallet phrase
        var textArea = founderPage.Locator(".info-card textarea.form-control");
        await textArea.ClickAsync();
        await textArea.FillAsync(FOUNDER_WALLET_PHRASE);

        // Step 5: Click "Next"
        await founderPage.GetByRole(AriaRole.Button, new() { Name = "Next" }).ClickAsync();

        // Step 6: Click "Next"
        await founderPage.GetByRole(AriaRole.Button, new() { Name = "Next" }).ClickAsync();

        // Step 7: Enter password
        var passwordInput = founderPage.GetByRole(AriaRole.Textbox, new() { Name = "Enter password" });
        await passwordInput.FillAsync("123");
        await passwordInput.PressAsync("Enter");

        // Step 9: Click "Recover Wallet"
        await founderPage.GetByRole(AriaRole.Button, new() { Name = "Recover Wallet" }).ClickAsync();

        _logger.LogInformation("Founder wallet imported successfully!");
    }

    private async Task HandleAllInvestorsFlow(IPage founderPage)
    {
        _logger.LogInformation("Processing all investors...");

        // Process each investor
        foreach (var investor in StoredWallets.Values)
        {
            _logger.LogInformation($"Processing investor: {investor.Name}");
            await ProcessInvestorFlow(investor);
        }

        // Continue with founder steps 18-22
        await CompleteFounderManageFunds(founderPage);
    }

    private async Task CompleteFounderManageFunds(IPage founderPage)
    {
        _logger.LogInformation("Managing founder funds...");

        // Step 18: Click button "Manage funds"
        await founderPage.GetByRole(AriaRole.Button, new() { Name = "Manage funds" }).ClickAsync();

        // Step 19: Click button "Refresh"
        await founderPage.GetByRole(AriaRole.Button, new() { Name = "Refresh" }).ClickAsync();

        // Step 20: Click first button with name "Expand"
        await founderPage.GetByRole(AriaRole.Button, new() { Name = "Expand" }).First.ClickAsync();

        // Step 21: Click first checkbox with label "TBTC"
        await founderPage.GetByLabel("TBTC").First.ClickAsync();

        // Step 22: Click button "Claim Selected Coins"
        await founderPage.GetByRole(AriaRole.Button, new() { Name = "Claim Selected Coins" }).ClickAsync();

        _logger.LogInformation("Founder funds management completed!");
    }

    private async Task ProcessInvestorFlow(WalletInfo investor)
    {
        _logger.LogInformation($"Processing investor flow for: {investor.Name}");

        // Create new browser context for this investor
        var browser = await Playwright.Chromium.LaunchAsync();
        var context = await browser.NewContextAsync();
        var investorPage = await context.NewPageAsync();

        try
        {
            // Steps 1-9: Import investor wallet
            await ImportInvestorWallet(investorPage, investor);

            // Steps 10-15: Investor operations
            await CompleteInvestorOperations(investorPage);

            // Step 16: Wipe wallet data
            await WipeWalletData(investorPage);

            _logger.LogInformation($"Investor {investor.Name} flow completed!");
        }
        finally
        {
            await context.CloseAsync();
            await browser.CloseAsync();
        }
    }

    private async Task ImportInvestorWallet(IPage investorPage, WalletInfo investor)
    {
        _logger.LogInformation($"Importing wallet for {investor.Name}...");

        // Step 1: Goto wallet page
        await investorPage.GotoAsync(WalletTestHelper.WALLET_URL);

        // Step 2: Click "Import wallet"
        await investorPage.GetByRole(AriaRole.Button, new() { Name = "Import wallet" }).ClickAsync();

        // Step 3: Select textarea and enter wallet seed
        var textArea = investorPage.Locator(".info-card textarea.form-control");
        await textArea.ClickAsync();
        await textArea.FillAsync(investor.WalletSeed);
        _logger.LogInformation($"imported walletSeed: {investor.WalletSeed}");


        // Step 5: Click "Next"
        await investorPage.GetByRole(AriaRole.Button, new() { Name = "Next" }).ClickAsync();

        // Step 6: Click "Next"
        await investorPage.GetByRole(AriaRole.Button, new() { Name = "Next" }).ClickAsync();

        // Step 7: Enter password
        var passwordInput = investorPage.GetByRole(AriaRole.Textbox, new() { Name = "Enter password" });
        await passwordInput.FillAsync("123");
        await passwordInput.PressAsync("Enter");

        // Step 9: Click "Recover Wallet"
        await investorPage.GetByRole(AriaRole.Button, new() { Name = "Recover Wallet" }).ClickAsync();

        _logger.LogWarning($"Wallet imported for {investor.Name}!");
    }

    private async Task CompleteInvestorOperations(IPage investorPage)
    {
        // Step 10: Click link "Portfolio"
        await investorPage.GetByRole(AriaRole.Link, new() { Name = "Portfolio" }).ClickAsync();

        // Step 10: Click button: "Refresh"
        await investorPage.GetByRole(AriaRole.Button, new() { Name = "Refresh" }).ClickAsync();

        // Step 11: Enter Pass
        var passwordInput = investorPage.GetByRole(AriaRole.Textbox, new() { Name = "Enter password" });
        await passwordInput.FillAsync("123");
        await passwordInput.PressAsync("Enter");

        // Step 12: Click button: "Refresh"
        await investorPage.GetByRole(AriaRole.Button, new() { Name = "Refresh" }).ClickAsync();

        // Step 11: Click link "Complete Investments"
        await investorPage.GetByRole(AriaRole.Link, new() { Name = "Complete Investments" }).ClickAsync();

        // Step 12: Dialog box operations
        _logger.LogInformation("Handling investment completion dialog...");
        await investorPage.WaitForSelectorAsync("input[type='password']", new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        passwordInput = investorPage.Locator("input[type='password']");
        await passwordInput.FillAsync("123");
        await investorPage.GetByRole(AriaRole.Button, new() { Name = "Submit" }).ClickAsync();

        // Step 13: Wait 1 second
        await Task.Delay(1000);

        // Step 14: Click button "Invest"
        await investorPage.GetByRole(AriaRole.Button, new() { Name = "Invest" }).ClickAsync();

        // Step 15: Wait 1 second
        await Task.Delay(1000);

        _logger.LogInformation("Investor operations completed!");
    }

    private async Task WipeWalletData(IPage investorPage)
    {
        _logger.LogInformation(" Wiping wallet data...");

        // Step 16: Click link "Settings"
        await investorPage.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();

        // Step 2: Click "Wipe Data"
        await investorPage.GetByRole(AriaRole.Button, new() { Name = "Wipe Data" }).ClickAsync();

        // Step 3: Check "Confirm"
        await investorPage.GetByText("Confirm?").Last.ClickAsync(new() { Button = MouseButton.Left });

        // Step 4: Click "Wipe Storage"
        await investorPage.GetByRole(AriaRole.Button, new() { Name = "Wipe Storage" }).ClickAsync();

        _logger.LogInformation("Wallet data wiped!");
    }
}
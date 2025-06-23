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
    private const string FOUNDER_WALLET_PHRASE = "luggage mail figure puzzle wrap bike torch theory offer marine labor decline"; // 29 investors:"frown skill mail speak clever hour fury bonus profit doll pioneer town";

    // Static storage for wallet info across test runs
    private static readonly Dictionary<string, WalletInfo> StoredWallets = new();
    // Static storage for page-context info across test runs
    private static readonly Dictionary<string, IPage> InvestorPages = new();

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
    public async Task Test1_CompleteInvestorFounderFlow()
    {
        _logger.LogInformation("=== TEST_1: Starting Complete Investor-Founder Flow ===");

        // Step 1: Create Investor and Invest
        _logger.LogInformation("=== Phase 1: Creating Investor 1 ===");
        await CreateInvestorAndInvest("investor1");
        Assert.True(StoredWallets.ContainsKey("investor1"));
        _logger.LogInformation("investor1 wallet stored and investment complete");


        // Step 2: Founder Accepts Investments
        _logger.LogInformation("=== Phase 2: Founder Accepts Investments ===");
        // Create new browser context for founder
        var browser = await Playwright.Chromium.LaunchAsync();
        var context = await browser.NewContextAsync();
        var founderPage = await context.NewPageAsync();

        try
        {
            await FounderApprovalFlow(founderPage);

            // Step 3: Process investor using existing page context
            _logger.LogInformation("=== Phase 3: Processing Investor with Same Context ===");
            await HandleAllInvestorsFlow(founderPage);
            _logger.LogInformation("Complete investor-founder flow completed successfully!");
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

            // Store page-context info
            InvestorPages[investorName] = Page;

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

        // Steps 1-7: Import founder wallet
        await ImportFounderWallet(founderPage);

        _logger.LogInformation("Handling founder operations...");
        // Step 8: Click link "Founder"
        await founderPage.GetByRole(AriaRole.Link, new() { Name = "Founder" }).ClickAsync();

        // Step 9: Click button "Scan for Founder Projects"
        await founderPage.GetByRole(AriaRole.Button, new() { Name = "Scan for Founder Projects" }).ClickAsync();

        // Step 10: Click link : PROJECT_NAME
        await founderPage.GetByRole(AriaRole.Link, new() { Name = WalletTestHelper.FOUNDER_TEST_PROJECT_NAME }).ClickAsync();

        // Step 11: Click button "Approve Investments"
        await founderPage.GetByRole(AriaRole.Link, new() { Name = "Approve Investments" }).ClickAsync();

        // Step 12: Dialog box operations
        _logger.LogInformation("Handling approval dialog...");
        await founderPage.WaitForSelectorAsync("input[type='password']", new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        var passwordInput = founderPage.Locator("input[type='password']");
        await passwordInput.FillAsync("123");
        await founderPage.Locator("label.form-check-label[for='cacheActive']").ClickAsync(); // check the box: remain password active
        await passwordInput.PressAsync("Enter");

        // Step x: Click button "Refresh"
        await founderPage.GetByRole(AriaRole.Button, new() { Name = "Refresh" }).ClickAsync();

        // Step 13:
        await founderPage.GetByRole(AriaRole.Button, new() { Name = "Approve All" }).ClickAsync();

        // Step 14: Click link "Founder"
        await founderPage.GetByRole(AriaRole.Link, new() { Name = "Founder" }).ClickAsync();

        // Step 15: Click link: <PROJECT_NAME>
        await founderPage.GetByRole(AriaRole.Link, new() { Name = WalletTestHelper.FOUNDER_TEST_PROJECT_NAME }).ClickAsync();

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
            if (InvestorPages.TryGetValue(investor.Name, out var investorPage))
            {
                // Use existing page context, no investor wallet import
                await ProcessInvestorFlow(investorPage, investor);
            }
            else
            {
                _logger.LogError($"No page context found for investor: {investor.Name}");
            }
        }

        // Continue with founder steps 18-22
        await CompleteFounderManageFunds(founderPage);

        foreach (var investor in StoredWallets.Values)
        {
            _logger.LogInformation($"Processing investor refund and penalty: {investor.Name}");
            if (InvestorPages.TryGetValue(investor.Name, out var investorPage))
            {
                // Func InvestorRefundOperations
                await CompleteInvestorRefundAndPenaltyOperations(investorPage);
            }
            else
            {
                _logger.LogError($"No page context found for investor: {investor.Name}");
            }
        }
    }

    private async Task CompleteFounderManageFunds(IPage founderPage)
    {
        _logger.LogInformation("Managing founder funds...");

        // Step 18: Click button "Manage funds"
        await founderPage.GetByRole(AriaRole.Link, new() { Name = "Manage funds" }).ClickAsync();

        // Step 19: Click button "Refresh" : Taking too much time to refresh
        await founderPage.GetByRole(AriaRole.Button, new() { Name = "Refresh" }).ClickAsync();

        // Step 20: Click first button with name "Expand"
        await founderPage.GetByRole(AriaRole.Button, new() { Name = "Expand" }).First.ClickAsync();

        // Step 21: Click button "Select All Available" 
        await founderPage.GetByRole(AriaRole.Button, new() { Name = "Select All Available" }).ClickAsync();

        // Step 22: Click button "Claim Selected Coins"
        await founderPage.GetByRole(AriaRole.Button, new() { Name = "Claim Selected Coins" }).ClickAsync();

        // // Step 23: Enter pass
        // var passwordInput = founderPage.Locator("input[type='password']");
        // await passwordInput.FillAsync("123");
        // await passwordInput.PressAsync("Enter");

        // Step 24: Select "priority" investment
        await founderPage.GetByText("Priority").ClickAsync();

        // Step 25: Click btn "Confirm Transaction"
        await founderPage.GetByRole(AriaRole.Button, new() { Name = "Confirm Transaction" }).ClickAsync();
        await Task.Delay(2000); // wait a bit for confirmation

        _logger.LogInformation("Founder funds management completed!");
    }

    private async Task ProcessInvestorFlow(IPage investorPage, WalletInfo investor)
    {
        _logger.LogInformation($"Processing investor flow for: {investor.Name}");

        try
        {
            // Steps 10-15: Investor operations (using existing wallet)
            await CompleteInvestorOperations(investorPage);

            _logger.LogInformation($"Investor {investor.Name} flow completed!");
        }
        catch (Exception e)
        {
            _logger.LogError($"Error processing investor {investor.Name}: {e.Message}");
            throw;
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

    private async Task CompleteInvestorRefundAndPenaltyOperations(IPage investorPage)
    {
        // Step 16: Click link: "Manage Investment"
        await investorPage.GetByRole(AriaRole.Button, new() { Name = "Manage Investment" }).ClickAsync();
        await Task.Delay(5000);

        // Step 17: Check the table
        /**
        1st stage -> Spent by founder
        2nd stage -> Not Spent
        3nd stage -> Not Spent
        **/

        // Define expected data
        var expectedStage1Status = "Spent by founder";
        var expectedStage2Status = "Not Spent";
        var expectedStage3Status = "Not Spent";

        // Locate the table body
        var tableBody = Page.Locator("table.table.align-items-center.mb-0 tbody");

        // Get the rows in the table
        var rows = await tableBody.Locator("tr").AllAsync();

        // Assertions for Stage 1
        var stage1Row = rows[0];
        var stage1StatusElement = await stage1Row.Locator("td:nth-child(3) span").ElementHandleAsync(); // Find the span in the 3rd column
        var stage1StatusText = await stage1StatusElement!.TextContentAsync();
        Assert.Contains(expectedStage1Status, stage1StatusText!);

        // Assertions for Stage 2
        var stage2Row = rows[1];
        var stage2StatusElement = await stage2Row.Locator("td:nth-child(3) span").ElementHandleAsync(); // Find the span in the 3rd column
        var stage2StatusText = await stage2StatusElement!.TextContentAsync();
        Assert.Contains(expectedStage2Status, stage2StatusText!);

        // Assertions for Stage 3
        var stage3Row = rows[2];
        var stage3StatusElement = await stage3Row.Locator("td:nth-child(3) span").ElementHandleAsync(); // Find the span in the 3rd column
        var stage3StatusText = await stage3StatusElement!.TextContentAsync();
        Assert.Contains(expectedStage3Status, stage3StatusText!);
        _logger.LogInformation("Table verification successful!");

        // Step 18: Click "Recover Funds"
        await investorPage.GetByRole(AriaRole.Button, new() { Name = "Recover Funds" }).ClickAsync();

        // // Step 19: Enter pass with condition
        // var dialogLocator = investorPage.Locator("css=[role='dialog'],[aria-modal='true'],.modal");
        // bool isDialogPresent = await dialogLocator.IsVisibleAsync();
        // var passwordInput = investorPage.Locator("input[type='password']");
        // if (isDialogPresent)
        // {
        //     await passwordInput.FillAsync("123");
        //     await passwordInput.PressAsync("Enter");
        //     await Task.Delay(1000); // wait for confirmation
        // }

        // Step 24: Select "priority" investment
        await investorPage.GetByText("Priority").ClickAsync();

        // Step 25: Click btn "Confirm"
        await investorPage.GetByRole(AriaRole.Button, new() { Name = "Confirm" }).ClickAsync();

        // Step 26: Click link "Portfolio"
        await investorPage.GetByRole(AriaRole.Link, new() { Name = "Portfolio" }).ClickAsync();

        // Step 27: Click link "Penalties"
        await investorPage.GetByRole(AriaRole.Button, new() { Name = "Penalties" }).ClickAsync();
        await Task.Delay(1000); // wait for confirmation

        // Step 28: Click btn: "Claim Penalty:
        await investorPage.GetByRole(AriaRole.Button, new() { Name = "Claim Penalty" }).ClickAsync();

        // Step 29: Check the table
        /**
            1st stage -> Spent by founder
            2nd stage -> Penalty can be released 
            3nd stage -> Penalty can be released
        **/

        // Define expected data
        expectedStage1Status = "Spent by founder";
        expectedStage2Status = "Penalty can be released";
        expectedStage3Status = "Penalty can be released";

        // Locate the table body
        tableBody = Page.Locator("table.table.align-items-center.mb-0 tbody");

        // Get the rows in the table
        rows = await tableBody.Locator("tr").AllAsync();

        // Assertions for Stage 1
        stage1Row = rows[0];
        stage1StatusElement = await stage1Row.Locator("td:nth-child(3) span").ElementHandleAsync(); // Find the span in the 3rd column
        stage1StatusText = await stage1StatusElement!.TextContentAsync();
        Assert.Contains(expectedStage1Status, stage1StatusText!);

        // Assertions for Stage 2
        stage2Row = rows[1];
        stage2StatusElement = await stage2Row.Locator("td:nth-child(3) span").ElementHandleAsync(); // Find the span in the 3rd column
        stage2StatusText = await stage2StatusElement!.TextContentAsync();
        Assert.Contains(expectedStage2Status, stage2StatusText!);

        // Assertions for Stage 3
        stage3Row = rows[2];
        stage3StatusElement = await stage3Row.Locator("td:nth-child(3) span").ElementHandleAsync(); // Find the span in the 3rd column
        stage3StatusText = await stage3StatusElement!.TextContentAsync();
        Assert.Contains(expectedStage3Status, stage3StatusText!);
        _logger.LogInformation("Table verification successful!");


        //-----
        // Step 30: Click btn "Release Funds"
        await investorPage.GetByRole(AriaRole.Button, new() { Name = "Release Funds" }).ClickAsync();

        // Step 32: Select checkbox "Custom Fee Rate" - Wait for modal to be fully loaded
        await investorPage.WaitForSelectorAsync(".custom-fee-toggle");
        await investorPage.GetByText("Custom Fee Rate").ClickAsync();

        // Step 33: Wait for the fee input field to appear after checking custom fee toggle
        await investorPage.WaitForSelectorAsync("input[type='number']", new() { State = WaitForSelectorState.Visible });

        // Fill the custom fee input
        var customInput = investorPage.Locator("input[type='number']");
        await customInput.FillAsync("10000");
        await customInput.PressAsync("Enter");
        // Wait for the fee to be processed/validated
        await Task.Delay(1500);

        // Step 34: Wait for Confirm button to be enabled and click
        var confirmButton = investorPage.GetByRole(AriaRole.Button, new() { Name = "Confirm" });
        await confirmButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        await confirmButton.ClickAsync();
        await Task.Delay(3000); // Increased wait time for transaction processing

        // // Step 30: Click btn "Release Funds"
        // await investorPage.GetByRole(AriaRole.Button, new() { Name = "Release Funds" }).ClickAsync();

        // // // Step 31: Enter pass
        // // passwordInput = investorPage.Locator("input[type='password']");
        // // await passwordInput.FillAsync("123");
        // // await passwordInput.PressAsync("Enter");

        // // Step 32: Select checkbox "Custom Fee Rate"
        // await investorPage.GetByText("Custom Fee Rate").ClickAsync();

        // // Step 33: Enter custom fee rate: 10_000 
        // var feeRateInput = investorPage.Locator("input");
        // await feeRateInput.Last.FillAsync("10000");
        // await feeRateInput.Last.PressAsync("Enter");
        // await Task.Delay(1000); // Additional delay for UI/backend processing

        // // Step 34: Click btn "Confirm"
        // await investorPage.GetByRole(AriaRole.Button, new() { Name = "Confirm" }).ClickAsync();
        // await Task.Delay(2000); // wait to confirm

        // Step 1: Goto wallet page
        await investorPage.GotoAsync(WalletTestHelper.WALLET_URL);

        _logger.LogInformation("==== TESTING SUCCESSFUL ====");
        // check in the wallet(calculate the recovered value)
        // asset the result
    }

    private async Task CompleteInvestorOperations(IPage investorPage)
    {
        _logger.LogInformation("Starting investor operations...");
        // Step 10: Click link "Portfolio"
        await investorPage.GetByRole(AriaRole.Link, new() { Name = "Portfolio" }).ClickAsync();

        // Step 13: Click link "Completed Investment"
        await investorPage.GetByRole(AriaRole.Link, new() { Name = "Completed Investment" }).ClickAsync();

        // // Step 12: Dialog box operations
        // _logger.LogInformation("Handling investment completion dialog...");
        // await investorPage.WaitForSelectorAsync("input[type='password']", new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        // var passwordInput = investorPage.Locator("input[type='password']");
        // await passwordInput.FillAsync("123");
        // await investorPage.GetByRole(AriaRole.Button, new() { Name = "Submit" }).ClickAsync();
        // // Step 13: Wait 1 second
        // await Task.Delay(1000);

        // Step 14: Click button "Invest"
        await investorPage.GetByRole(AriaRole.Button, new() { Name = "Invest" }).ClickAsync();

        // Step 15: Wait 1 second
        await Task.Delay(1000);

        _logger.LogInformation("Investor operations completed!");
    }
}
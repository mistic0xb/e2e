using e2e.Tests.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace e2e.Tests.TEST_1;

public static class WalletTestHelper
{
    public const string WALLET_URL = "https://test.angor.io/wallet"; //"http://localhost:5062/wallet";  
    public const string DEFAULT_PASSWORD = "123";
    public const string FOUNDER_TEST_PROJECT_ID = "angor1q9wxdx2p0jgpvcrym3f0djr4eehpa2vtt6mhk7h"; // 29-investors: "angor1qe9cpnllqu5tjws3pawy7k2ek5ue4phw5e4g6lh";
    public const string FOUNDER_TEST_PROJECT_NAME = "k2";
    public const string FOUNDER_WALLET_PHRASE = "meadow tackle soon color wave lounge evidence favorite cloud prosper siren powder"; // 29 investors:"frown skill mail speak clever hour fury bonus profit doll pioneer town";
    private static readonly ILogger _logger = TestLogger.Create("WalletTestHelper");


    public static async Task<string> CreateWalletFlow(IPage page)
    {
        // Navigate to wallet creation
        await page.GotoAsync(WALLET_URL);

        // Click "New wallet"
        await page.GetByRole(AriaRole.Button, new() { Name = "New wallet" }).ClickAsync();

        // Generate recovery phrase
        await page.GetByRole(AriaRole.Button, new() { Name = "Generate recovery phrase" }).ClickAsync();

        // Confirm backup
        await page.GetByText("I have safely backed up my recovery phrase").ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Next" }).ClickAsync();

        // Skip verification
        await page.GetByRole(AriaRole.Button, new() { Name = "Skip verification" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Next" }).ClickAsync();

        // Set password
        var passwordInput = page.GetByRole(AriaRole.Textbox, new() { Name = "Enter password" });
        await passwordInput.FillAsync(DEFAULT_PASSWORD);
        await passwordInput.PressAsync("Enter");

        // Create wallet
        await page.GetByRole(AriaRole.Button, new() { Name = "Create wallet" }).ClickAsync();

        // Get the seed phrase
        var viewSeedButton = page.GetByRole(AriaRole.Button, new() { Name = "View Seed" }).First;
        await viewSeedButton.ClickAsync();

        passwordInput = page.GetByRole(AriaRole.Textbox).First;
        await passwordInput.FillAsync(DEFAULT_PASSWORD);
        await passwordInput.PressAsync("Enter");
        var seedPhraseElement = await page.WaitForSelectorAsync("[data-cy='alert-wallet-words'] p") ?? throw new Exception("Seed phrase element not found.");
        var seedPhrase = await seedPhraseElement.TextContentAsync() ?? throw new Exception("Seed phrase is empty or null.");

        // close wallet_phrase dialogue box
        var closeButton = page.Locator(".btn-close-custom").Last;
        await closeButton!.ClickAsync();

        return seedPhrase;
    }

    public static async Task<double> GetTestBTCAndWaitForBalance(IPage page)
    {
        // Navigate to wallet
        await page.GetByRole(AriaRole.Link, new() { Name = "wallet" }).ClickAsync();

        // Get test coins
        await page.GetByRole(AriaRole.Button, new() { Name = "Get Test Coins" }).ClickAsync();

        // Initial wait
        await Task.Delay(10000);

        // Refresh and wait
        await page.GetByRole(AriaRole.Button, new() { Name = "refresh" }).ClickAsync();

        // Wait for positive balance
        return await WaitForPositiveBalance(page);
    }

    public static async Task<double> WaitForPositiveBalance(IPage page, int maxRetries = 30)
    {
        var retries = maxRetries;

        while (retries > 0)
        {
            var btcValueElement = page.Locator(".btc-value");
            var btcValueText = await btcValueElement.TextContentAsync();

            if (!string.IsNullOrWhiteSpace(btcValueText) && double.TryParse(btcValueText, out double btcValue) && btcValue > 0)
            {
                _logger.LogInformation($"TBTC Balance obtained: {btcValue}");
                return btcValue;
            }

            _logger.LogInformation($"Waiting for TBTC balance... Attempt {maxRetries - retries + 1}/{maxRetries}");

            await page.GetByRole(AriaRole.Button, new() { Name = "refresh" }).ClickAsync();
            await Task.Delay(5000);
            retries--;
        }

        throw new Exception($"Failed to obtain positive BTC balance after {maxRetries} attempts");
    }

    public static async Task InvestInProject(IPage page, double amount, string password = DEFAULT_PASSWORD)
    {
        // Go to browse page
        await page.GetByRole(AriaRole.Link, new() { Name = "browse" }).ClickAsync();

        // Search for project with projectID
        var searchInput = await page.WaitForSelectorAsync("#searchQuery");
        await searchInput!.FillAsync(FOUNDER_TEST_PROJECT_ID);
        await searchInput.PressAsync("Enter");

        // Step 17: Click founder's project link
        await page.GetByRole(AriaRole.Link, new() { Name = FOUNDER_TEST_PROJECT_NAME }).ClickAsync();

        // Click invest         
        await page.GetByRole(AriaRole.Button, new() { Name = "Invest Now" }).ClickAsync();

        // Enter investment amount
        var investmentInput = page.Locator("#investmentAmount");
        await investmentInput.ClickAsync();
        await investmentInput.FillAsync(amount.ToString());

        // Continue to confirmation
        await page.GetByRole(AriaRole.Button, new() { Name = "Continue To Confirmation" }).ClickAsync();

        // Enter password
        await page.WaitForSelectorAsync("#passwordInput", new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        var passwordInput = page.Locator("#passwordInput");
        await passwordInput.ClickAsync();
        await passwordInput.FillAsync(password);
        await page.Locator("label.form-check-label[for='cacheActive']").ClickAsync(); // check the box: remain password active
        await passwordInput.PressAsync("Enter");

        // Select "priority" investment
        await page.GetByText("Priority").ClickAsync();

        // Confirm investment
        await page.GetByRole(AriaRole.Button, new() { Name = "Send Request" }).ClickAsync();
        await Task.Delay(3000); // waiting to confirm

        // Go To Portfolio page to confirm
        await page.GetByRole(AriaRole.Link, new() { Name = "portfolio" }).ClickAsync();
        await Task.Delay(1000); // waiting to confirm

        _logger.LogInformation($"Investment of {amount} BTC completed");
    }
}
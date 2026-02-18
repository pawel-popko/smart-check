using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Runtime.Intrinsics.Arm;

namespace SmartCheck;


public class TestWebsiteForm : PlaywrightTest
{
    [Test]
    public async Task Step1_FillForm_And_Click_Wysylam()
    {
        // Start przeglądarki w trybie okienkowym + spowolnienie dla obserwacji
        await using var browser = await Playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions
            {
                Headless = false,
                SlowMo = 2000,
                Args = new[] { "--start-maximized" }
            });

        // Kontekst
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = null
        });

        // Nowa karta/strona
        var page = await context.NewPageAsync();

        // Wejście na krok 1 i czekanie aż strona się załaduje
        await page.GotoAsync(
            "https://nadaj.orlenpaczka.pl/nadaj-paczke-krok1",
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Akceptacja cookies (jeśli wyskoczą)
        await TryClickAsync(page.GetByRole(AriaRole.Button, new() { NameString = "Allow all" }));
        await TryClickAsync(page.GetByRole(AriaRole.Button, new() { NameString = "Zezwól na wszystkie" }));

        // Przewiń do sekcji "Dane nadawcy" (z uwzględnieniem sticky headera)
        await page.GetByText("Dane nadawcy")
            .EvaluateAsync(@"el => {
                const y = el.getBoundingClientRect().top + window.scrollY - 100;
                window.scrollTo({ top: y });
            }");

        // Wypełnij dane nadawcy
        await page.GetByText("Dane nadawcy").ScrollIntoViewIfNeededAsync();
        await page.Locator("#nadawca-imie").FillAsync("Anna");
        await page.Locator("#nadawca-nazwisko").FillAsync("Nowak");
        await page.Locator("#nadawca-email").FillAsync("anna.nowak@test.pl");
        await page.Locator("#nadawca-telefon").FillAsync("48501502503");

        // Wybór punktu nadania (lista -> pierwszy wynik)
        await page.Locator("#searchnadawca").ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Lista" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Wybierz" }).First.ClickAsync();

        // Wybór sposobu nadania
        await page.GetByRole(AriaRole.Heading, new() { NameString = "Sposób nadania" }).ScrollIntoViewIfNeededAsync();
        await page.GetByText("Drukuję etykietę", new() { Exact = false }).ClickAsync();

        // Przewiń do sekcji "Dane odbiorcy" (z uwzględnieniem sticky headera)
        await page.GetByText("Dane odbiorcy")
            .EvaluateAsync(@"el => {
                const y = el.getBoundingClientRect().top + window.scrollY - 100;
                window.scrollTo({ top: y });
            }");

        // Wypełnij dane odbiorcy
        await page.GetByText("Dane odbiorcy").ScrollIntoViewIfNeededAsync();
        await page.Locator("#odbiorca-imie").FillAsync("Jan");
        await page.Locator("#odbiorca-nazwisko").FillAsync("Kowalski");
        await page.Locator("#odbiorca-email").FillAsync("jan.kowalski@test.pl");
        await page.Locator("#odbiorca-telefon").FillAsync("48602603604");

        // Wybór punktu odbioru (lista -> ostatni wynik)
        await page.Locator("#searchodbiorca").ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Lista" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Wybierz" }).Last.ClickAsync();

        // Wybór rozmiaru paczki
        await page.GetByText("Rozmiar paczki").ScrollIntoViewIfNeededAsync();
        await TryClickAsync(page.GetByText("Paczka L", new() { Exact = false }));

        // Przewiń do zgód (z uwzględnieniem sticky headera)
        await page.GetByText("Zgody i oświadczenia")
            .EvaluateAsync(@"el => {
                const y = el.getBoundingClientRect().top + window.scrollY - 100;
                window.scrollTo({ top: y });
            }");

        // Zaznacz wszystkie zgody
        await page.GetByText("Zgody i oświadczenia").ScrollIntoViewIfNeededAsync();
        await page.Locator("#Krok8ZaznaczWszystkie").ClickAsync();

        // Przewiń do przycisku "Wysyłam" (z uwzględnieniem sticky headera)
        await page.GetByRole(AriaRole.Button, new() { NameString = "Wysyłam" })
            .EvaluateAsync(@"el => {
                const y = el.getBoundingClientRect().top + window.scrollY - 100;
                window.scrollTo({ top: y });
            }");

        // Klik "Wysyłam" i poczekaj na przejście do kroku 2 (zmiana URL)
        var wysylam = page.GetByRole(AriaRole.Button, new() { NameString = "Wysyłam" });
        var beforeUrl = page.Url;

        await Expect(wysylam).ToBeEnabledAsync();

        await Task.WhenAll(
            page.WaitForURLAsync(url => url != beforeUrl, new() { Timeout = 60_000 }),
            wysylam.ClickAsync()
        );

        // Loguj URL kroku 2
        Console.WriteLine("URL after Wysyłam: " + page.Url);

        // Krótka pauza, żeby zobaczyć krok 2
        await page.WaitForTimeoutAsync(3_000);

        // Klik "Opłać przesyłkę" i poczekaj na przejście dalej (zmiana URL)
        var oplac = page.GetByRole(AriaRole.Button, new() { NameString = "Opłać przesyłkę" });

        // Przewiń do przycisku (sticky header)
        await oplac.EvaluateAsync(@"el => {
            const y = el.getBoundingClientRect().top + window.scrollY - 100;
            window.scrollTo({ top: y });
        }");

        // Upewnij się, że przycisk jest widoczny i aktywny
        await Expect(oplac).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await Expect(oplac).ToBeEnabledAsync(new() { Timeout = 60_000 });

        var beforePayUrl = page.Url;

        await Task.WhenAll(
            page.WaitForURLAsync(url => url != beforePayUrl, new() { Timeout = 60_000 }),
            oplac.ClickAsync()
        );

        // Loguj URL kolejnego kroku
        Console.WriteLine("URL after Opłać przesyłkę: " + page.Url);

        // Poczekaj aż strona po płatności się uspokoi
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 60_000 });

        // Pauza do ręcznej obserwacji (tylko lokalnie)
        await page.WaitForTimeoutAsync(5_000);

        // Sprzątanie zasobów
        await context.CloseAsync();
        await browser.CloseAsync();
    }

    // Bezpieczny klik: nie wywala testu, jeśli element nie istnieje / nie jest widoczny
    private static async Task TryClickAsync(ILocator locator)
    {
        try
        {
            if (await locator.IsVisibleAsync())
                await locator.ClickAsync(new() { Timeout = 1000 });
        }
        catch { }
    }
}

/* Uruchomienie testu w PowerShell

    $env:HEADED="1"
    $env:PWDEBUG="1"
    dotnet build
    dotnet test

*/

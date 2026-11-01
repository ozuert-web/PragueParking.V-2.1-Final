using PragueParking.Core;
using PragueParking.Data;
using Spectre.Console;
using System.Text;

internal class Program
{
    static void Main()
    {
        var service = Setup();
        Run(service);
    }

    static ParkingService? Setup()
    {
        Console.OutputEncoding = Encoding.UTF8;

        string folder = Path.Combine(
            AppContext.BaseDirectory,
            "Data");

        try
        {
            return new ParkingService(
                new FileStore(folder),
                createTestData: true,
                now: DateTimeOffset.Now);
        }
        catch (Exception ex) when (
            ex is IOException
            or UnauthorizedAccessException
            or ArgumentException
            or System.Text.Json.JsonException)
        {
            Console.WriteLine(
                $"Programmet kunde inte starta: {ex.Message}");

            Console.WriteLine(
                $"Kontrollera filerna i: {folder}");

            Environment.ExitCode = 1;

            return null;
        }
    }

    static void Run(ParkingService? service)
    {
        if (service == null)
        {
            Console.WriteLine("Tryck Enter för att avsluta.");
            Console.ReadLine();
            return;
        }

        bool running = true;

        while (running)
        {
            AnsiConsole.Clear();

            ShowDashboard(service);

            string choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold cyan]VAD VILL DU GÖRA?[/]")
                    .HighlightStyle(new Style(Color.Cyan))
                    .PageSize(10)
                    .AddChoices(
                        "Checka in",
                        "Sök fordon",
                        "Flytta fordon",
                        "Checka ut",
                        "Visa alla fordon",
                        "Visa priser",
                        "Läs om priser",
                        "Avsluta"));

            try
            {
                switch (choice)
                {
                    case "Checka in":
                        CheckIn(service);
                        break;

                    case "Sök fordon":
                        ShowVehicle(
                            service.Garage.Find(
                                ReadRegistration()));
                        break;

                    case "Flytta fordon":
                        MoveVehicle(service);
                        break;

                    case "Checka ut":
                        CheckOut(service);
                        break;

                    case "Visa alla fordon":
                        ShowAllVehicles(service);
                        break;

                    case "Visa priser":
                        ShowPrices(service);
                        break;

                    case "Läs om priser":
                        service.ReloadPrices();

                        AnsiConsole.MarkupLine(
                            "[green]Prislistan är uppdaterad.[/]");
                        break;

                    case "Avsluta":
                        running = false;
                        break;
                }
            }
            catch (Exception ex) when (
                ex is ArgumentException
                or IOException
                or UnauthorizedAccessException
                or System.Text.Json.JsonException)
            {
                AnsiConsole.MarkupLine(
                    $"[red]{Markup.Escape(ex.Message)}[/]");
            }

            if (running)
            {
                AnsiConsole.Prompt(
                    new TextPrompt<string>(
                        "[grey]Tryck Enter för att fortsätta.[/]")
                        .AllowEmpty());
            }
        }
    }

    static void ShowDashboard(ParkingService service)
    {
        var banner = new Panel(
            new Markup(
                "[bold white on blue] P  PRAGUE PARKING [/]\n" +
                "[cyan]VERSION 2.0 • BIL OCH MC[/]"))
            .Border(BoxBorder.Double)
            .BorderColor(Color.Blue)
            .Expand();

        AnsiConsole.Write(banner);

        AnsiConsole.MarkupLine(
            "[white on green] INFART → [/]  " +
            "[white on red] ← UTFART [/]");

        var spots = service.Garage.GetSpots();

        Console.WriteLine(
            $"Platser: {spots.Count} | " +
            $"Fordon: {service.Garage.Vehicales.Count}");

        var map = new Table()
            .NoBorder()
            .HideHeaders();

        int columns = Math.Clamp(
            (AnsiConsole.Profile.Width - 6) / 9,
            1,
            10);

        for (int index = 0; index < columns; index++)
        {
            map.AddColumn("");
        }

        for (
            int offset = 0;
            offset < spots.Count;
            offset += columns)
        {
            var cells = new string[columns];

            for (int index = 0; index < columns; index++)
            {
                if (offset + index >= spots.Count)
                {
                    cells[index] = "";
                    continue;
                }

                IParkingSpot spot = spots[offset + index];

                string color;
                string symbol;

                if (spot.Used == 0)
                {
                    color = "green";
                    symbol = "L";
                }
                else if (spot.Free == 0)
                {
                    color = "red";
                    symbol = "F";
                }
                else
                {
                    color = "yellow";
                    symbol = "D";
                }

                cells[index] =
                    $"[{color}]{spot.Number:000} {symbol}[/]";
            }

            map.AddRow(cells);
        }

        AnsiConsole.Write(map);

        int empty = spots.Count(
            spot => spot.Used == 0);

        int partial = spots.Count(
            spot => spot.Used > 0 && spot.Free > 0);

        int full = spots.Count(
            spot => spot.Free == 0);

        AnsiConsole.MarkupLine(
            $"[green]L Lediga: {empty}[/]  " +
            $"[yellow]D Delvis: {partial}[/]  " +
            $"[red]F Fulla: {full}[/]");

        AnsiConsole.Write(
            new Rule().RuleStyle("grey"));

        AnsiConsole.Write(
            Align.Right(
                new Text(
                    "By Talha Özuer",
                    new Style(Color.Grey))));

        AnsiConsole.WriteLine();
    }

    static string ReadRegistration()
    {
        return AnsiConsole.Ask<string>(
            "[cyan]Registreringsnummer:[/]");
    }

    static void CheckIn(ParkingService service)
    {
        string type = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]Välj fordonstyp[/]")
                .AddChoices(
                    service.Garage.Config.VehicaleTypes.Keys));

        string registration = ReadRegistration();

        int spot = AnsiConsole.Prompt(
            new TextPrompt<int>(
                "Startplats (0 = automatisk placering):")
                .DefaultValue(0)
                .Validate(number =>
                    number >= 0
                        ? ValidationResult.Success()
                        : ValidationResult.Error(
                            "Ange noll eller ett positivt nummer.")));

        service.CheckIn(
            type,
            registration,
            DateTimeOffset.Now,
            spot == 0 ? null : spot);

        ShowVehicle(
            service.Garage.Find(registration));
    }

    static void MoveVehicle(ParkingService service)
    {
        string registration = ReadRegistration();

        int destination = AnsiConsole.Prompt(
            new TextPrompt<int>("Ny parkeringsplats:")
                .Validate(number =>
                    number > 0
                        ? ValidationResult.Success()
                        : ValidationResult.Error(
                            "Ange ett positivt nummer.")));

        service.Move(registration, destination);

        ShowVehicle(
            service.Garage.Find(registration));
    }

    static void CheckOut(ParkingService service)
    {
        Receipt receipt = service.CheckOut(
            ReadRegistration(),
            DateTimeOffset.Now);

        var content = new Markup(
            $"Fordon: {Markup.Escape(receipt.Registration)}\n" +
            $"Tid: {receipt.Duration.TotalMinutes:F1} minuter\n" +
            $"[bold green]Att betala: {receipt.Price:F2} CZK[/]");

        AnsiConsole.Write(
            new Panel(content)
                .Header(" KVITTO ")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Green));
    }

    static void ShowVehicle(ParkedVehicale parked)
    {
        var vehicle = parked.Vehicale;

        string spots = string.Join(
            ", ",
            parked.Allocations.Select(
                allocation => allocation.Spot));

        var content = new Markup(
            $"Registrering: {Markup.Escape(vehicle.Registration)}\n" +
            $"Typ: {Markup.Escape(vehicle.Type)}\n" +
            $"Platser: {spots}\n" +
            $"Ankomst: {vehicle.ArrivedAt.LocalDateTime:yyyy-MM-dd HH:mm}");

        AnsiConsole.Write(
            new Panel(content)
                .Header(" FORDON ")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Blue));
    }

    static void ShowAllVehicles(ParkingService service)
    {
        if (service.Garage.Vehicales.Count == 0)
        {
            Console.WriteLine("Parkeringshuset är tomt.");
            return;
        }

        foreach (var parked in service.Garage.Vehicales)
        {
            ShowVehicle(parked);
        }
    }

    static void ShowPrices(ParkingService service)
    {
        var table = new Table()
            .BorderColor(Color.Blue);

        table.AddColumn("Fordon");
        table.AddColumn("CZK per påbörjad timme");

        foreach (var price in service.Prices.Rates)
        {
            table.AddRow(
                Markup.Escape(price.Key),
                $"{price.Value:F2}");
        }

        AnsiConsole.Write(table);

        AnsiConsole.MarkupLine(
            "[grey]Högst tio minuter är gratis. " +
            "Därefter räknas tiden från ankomst.[/]");
    }
}
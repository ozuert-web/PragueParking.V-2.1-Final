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
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.White;

        string folder = Path.Combine(
            AppContext.BaseDirectory,
            "data");

        try
        {
            return new ParkingService(
                new FileStore(folder),
                true,
                DateTimeOffset.Now);
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
            Console.ReadLine();
            return;
        }

        bool running = true;

        while (running)
        {
            AnsiConsole.Clear();

            ShowDashboard(service);
            ShowCredit();

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
                        "Läs om konfiguration",
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

                    case "Läs om konfiguration":
                        service.ReloadConfig();

                        AnsiConsole.MarkupLine(
                            "[green]Konfigurationen är uppdaterad.[/]");
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

        AnsiConsole.Clear();

        AnsiConsole.Write(
            Align.Center(
                new Text(
                    "Tack för besöket på Prague Parking!",
                    new Style(Color.Cyan))));

        ShowCredit();

        Console.ResetColor();
    }

    static void ShowDashboard(ParkingService service)
    {
        var banner = new Panel(
            new Markup(
                "[bold white on blue] P  PRAGUE PARKING  [/]\n" +
                "[cyan]PARKERINGSHUS • PRAG • VERSION 2.1[/]"))
            .Border(BoxBorder.Double)
            .BorderColor(Color.Blue)
            .Expand();

        AnsiConsole.Write(banner);

        AnsiConsole.MarkupLine(
            "[bold white on green] INFART → [/]  " +
            "[bold white on red] ← UTFART [/]  " +
            "[white on blue] BUSSOMRÅDE: HÖGT TAK [/]");

        var spots = service.Garage.GetSpots();

        int empty = spots.Count(
            spot => spot.Used == 0);

        int partial = spots.Count(
            spot => spot.Used > 0 && spot.Free > 0);

        int full = spots.Count(
            spot => spot.Free == 0);

        int highCeilingSpots = Math.Min(
            service.Garage.Config.HighCelingSpots,
            spots.Count);

        Console.WriteLine(
            $"Platser: {spots.Count} | " +
            $"Fordon: {service.Garage.Vehicales.Count} | " +
            $"Högt tak: 1–{highCeilingSpots}");

        var map = new Table()
            .NoBorder()
            .HideHeaders();

        int columns = Math.Clamp(
            (AnsiConsole.Profile.Width - 6) / 9,
            1,
            10);

        for (int i = 0; i < columns; i++)
        {
            map.AddColumn("");
        }

        for (
            int offset = 0;
            offset < spots.Count;
            offset += columns)
        {
            var cells = new string[columns];

            for (int i = 0; i < columns; i++)
            {
                if (offset + i >= spots.Count)
                {
                    cells[i] = "";
                    continue;
                }

                IParkingSpot spot = spots[offset + i];

                string color;
                string state;

                if (spot.Used == 0)
                {
                    color = "green";
                    state = "L";
                }
                else if (spot.Free == 0)
                {
                    color = "red";
                    state = "F";
                }
                else
                {
                    color = "yellow";
                    state = "D";
                }

                cells[i] =
                    $"[{color}]{spot.Number:000} {state}[/]";
            }

            map.AddRow(cells);
        }

        AnsiConsole.Write(map);

        AnsiConsole.MarkupLine(
            $"[green]L Lediga: {empty}[/]  " +
            $"[yellow]D Delvis: {partial}[/]  " +
            $"[red]F Fulla/reserverade: {full}[/]");

        AnsiConsole.Write(
            new Rule().RuleStyle("grey"));
    }

    static void ShowCredit()
    {
        const string credit = "By Talha Özuer";

        if (Console.IsOutputRedirected)
        {
            return;
        }

        try
        {
            int left = Console.CursorLeft;
            int top = Console.CursorTop;

            int bottom =
                Console.WindowTop + Console.WindowHeight - 1;

            if (bottom - top < 15
                || Console.WindowWidth < credit.Length + 2)
            {
                AnsiConsole.Write(
                    Align.Right(
                        new Text(
                            credit,
                            new Style(Color.Grey))));
                AnsiConsole.WriteLine();

                return;
            }

            ConsoleColor previousColor =
                Console.ForegroundColor;

            Console.SetCursorPosition(
                Console.WindowWidth - credit.Length - 1,
                bottom);

            Console.ForegroundColor = ConsoleColor.Gray;

            Console.Write(credit);

            Console.ForegroundColor = previousColor;

            Console.SetCursorPosition(left, top);
        }
        catch (IOException)
        {
        }
        catch (ArgumentOutOfRangeException)
        {
        }
    }

    static string ReadRegistration()
    {
        return AnsiConsole.Ask<string>(
            "[cyan]Registreringsnummer eller ID:[/]");
    }

    static void CheckIn(ParkingService service)
    {
        string type = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]Välj fordonstyp[/]")
                .HighlightStyle(new Style(Color.Cyan))
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
            new TextPrompt<int>("Ny startplats:")
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
            $"Fordon: [bold]{Markup.Escape(receipt.Registration)}[/]\n" +
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
            $"ID: [bold]{Markup.Escape(vehicle.Registration)}[/]\n" +
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
            AnsiConsole.MarkupLine(
                "[yellow]Det finns inga parkerade fordon.[/]");

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

        foreach (var rate in service.Prices.Rates)
        {
            table.AddRow(
                Markup.Escape(rate.Key),
                $"{rate.Value:F2}");
        }

        AnsiConsole.Write(table);

        AnsiConsole.MarkupLine(
            "[grey]Högst 10 minuter är gratis. " +
            "Därefter räknas tiden från ankomst.[/]");
    }
}
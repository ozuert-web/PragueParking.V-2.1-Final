using System.Text.Json;
using PragueParking.Core;
using System.Globalization;


namespace PragueParking.Data;

public class FileStore
{
    private readonly string directory;

    private readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
    public FileStore(string directory)
    {
        this.directory = directory;
    }

    // Sökväg 

    private string GetPath(string fileName)
    {
        return Path.Combine(directory, fileName);
    }
    // läser igenom config.json
    public GarageConfig ReadConfig()
    {
        string json = File.ReadAllText(GetPath("config.json"));

        GarageConfig? config = JsonSerializer.Deserialize<GarageConfig>(json, jsonOptions);

        if (config == null)
        {
            throw new InvalidDataException("filen är tom");
        }

        config.GetCapacity(1);

        return config;
    }
    // Läsa igenom prislistan
    public PriceList ReadPrices()
    {
        var rates = new Dictionary<string, decimal>();

        int lineNumber = 0;

        foreach (string rawLine in File.ReadLines(
            GetPath("price.txt")))
        {
            lineNumber++;

            string line = rawLine.Split('#')[0].Trim();

            if (line.Length == 0)
            {
                continue;
            }

            string[] parts = line.Split('=');

            if (parts.Length != 2)
            {
                throw new InvalidDataException(
                    $"Felaktig prisrad: {lineNumber}.");
            }

            string type = parts[0].Trim();

            bool validPrice = decimal.TryParse(
                parts[1].Trim(),
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out decimal price);

            if (type.Length == 0
                || !validPrice
                || price < 0
                || price > 1000000
                || !rates.TryAdd(type, price))
            {
                throw new InvalidDataException(
                    $"Ogiltigt eller dubblerat pris på rad {lineNumber}.");
            }
        }

        return new PriceList(rates);
    }

    // kontroller av filer
    public bool DataExists()
    {
        return File.Exists(GetPath("vehicales.json"));
    }

    public List<VehicaleRecord> ReadVehicales()
    {
        string json = File.ReadAllText(GetPath("vehicales.json"));

        List<VehicaleRecord>? records = JsonSerializer.Deserialize<List<VehicaleRecord>>(json, jsonOptions);

        if (records == null)
        {
            throw new InvalidDataException("Detta innehåller inte fordonslista");
        }

        return records;
    }

    // spara fordon som är parkerad i Json format

    public void SaveVehicales(List<VehicaleRecord> records)
    {
        Directory.CreateDirectory(directory);

        string destination = GetPath("vehicales.json");
        string temporary = GetPath("vehicale.json.tmp");

        try
        {
            string json = JsonSerializer.Serialize(records, jsonOptions);

            File.WriteAllText(temporary, json);

            File.Move(temporary, destination, overwrite: true);
        }

        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }
}

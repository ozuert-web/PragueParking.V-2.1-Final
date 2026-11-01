using PragueParking.Core;

namespace PragueParking.Core;

public interface IVehical

{
    string Registration { get; }
    string Type { get; }
    DateTimeOffset ArrivedAt { get; }

}

public abstract class Vehicale : IVehical
{
    public string Registration { get; }
    public DateTimeOffset ArrivedAt { get; }
    public abstract string Type { get; }

    protected Vehicale(string registration, DateTimeOffset arrivedAt)
    {
        Registration = NormalizeRegistration(registration);
        ArrivedAt = arrivedAt;
    }

    public static string NormalizeRegistration(string registration)
    {
        string result = registration.Trim().ToUpperInvariant();

        if (result.Length < 1 || result.Length > 20)
        {
            throw new ArgumentException("Registrering eller ID måste innehålla tecken mellan 1 - 20.");
        }

        foreach (char character in result)
        {
            bool valid = char.IsLetterOrDigit(character) || character == '-';

            if (!valid)
            {
                throw new ArgumentException("Registrering eller ID får endast innehålla bokstäver, siffror eller bindestreck.");
            }
        }

        return result;
    }
}

public class Car : Vehicale
{
    public override string Type => "Car";

    public Car(string registration, DateTimeOffset arrivedAt) : base(registration, arrivedAt)
    {
    }
}

public class MC : Vehicale
{
    public override string Type => "MC";
    public MC(string registration, DateTimeOffset arrivedAt) : base(registration, arrivedAt)
    {
    }
}
public class Bicycle : Vehicale
{
    public override string Type => "Bicycle";
    public Bicycle(string registration, DateTimeOffset arrivedAt) : base(registration, arrivedAt)
    {
    }
}
public class Bus : Vehicale
{
    public override string Type => "Bus";
    public Bus(string registration, DateTimeOffset arrivedAt) : base(registration, arrivedAt)
    {
    }
}

// Beskriver vad en parkeringplats måste ha

public interface IParkingSpot
{
    int Number { get; }
    int Capacity { get; }
    int Used { get; }
    int Free { get; }
}

// en parkerplats med nummer och kapacitet

public class ParkingSpot : IParkingSpot
{
    public int Number { get; }
    public int Capacity { get; }
    public int Used { get; }
    public int Free => Capacity - Used;

    public ParkingSpot(int number, int capacity, int used)
    {
        Number = number;
        Capacity = capacity;
        Used = used;
    }
}

// inställningar för en fordontyp 

public class VehicaleTypeConfig
{
    public int Size { get; set; }
    public bool HighCeilingOnly { get; set; }

}

// inställningar för hela parkeringshuset 

public class GarageConfig
{
    public int SpotCount { get; set; } = 100;

    public int DefultSpotSize { get; set; } = 4;

    public int HighCelingSpots { get; set; } = 50;

    public List<int> SpotSizes { get; set; } = new();

    public Dictionary<string, VehicaleTypeConfig> VehicaleTypes { get; set; } = new();

    // Hämtar Kapacitet för parkeringsplats

    public int GetCapacity(int spotNumber)
    {
        if(SpotCount < 1 || SpotCount > 100)
        {
            throw new ArgumentException("Antal parkeringsplatser måste vara mellan 1 - 100.");
        }

        if(DefultSpotSize < 1 || DefultSpotSize > 10)
        {
            throw new ArgumentException("Standardstorlek för parkeringsplatser måste vara mellan 1 - 10.");
        }

        if(HighCelingSpots < 0 || HighCelingSpots > 50)
        {
            throw new ArgumentException("Antal höga parkeringsplatser måste vara mellan 0 - 50 antal parkeringsplatser.");
        }

        if (SpotSizes == null)
        {
            throw new ArgumentException("SpotSizes får inte vara null.");
        }

        if (SpotSizes.Count != 0)
        {
            if (SpotSizes.Count != SpotCount)
            {
                throw new ArgumentException("Antal parkeringsplatser måste vara lika med antal storlekar i SpotSizes.");
            }
            if (SpotSizes.Any(size => size < 1 || size > 100))
            {
                throw new ArgumentException("Alla storlekar i SpotSizes måste vara mellan 1 - 100.");
            }
        }

        if (VehicaleTypes == null || VehicaleTypes.Count == 0)
        {
            throw new ArgumentException(
                "Minst en fordonstyp måste finnas.");
        }

        foreach (var entry in VehicaleTypes)
        {
            bool validName =
                entry.Key.Length >= 1
                && entry.Key.Length <= 30
                && entry.Key.All(char.IsAsciiLetterOrDigit);

            if (!validName
                || entry.Value == null
                || entry.Value.Size < 1
                || entry.Value.Size > 100)
            {
                throw new ArgumentException(
                    "Ogiltigt namn eller storlek för en fordonstyp.");
            }
        }

        if (VehicaleTypes.TryGetValue("Bus", out var bus)
            && !bus.HighCeilingOnly)
        {
            throw new ArgumentException(
                "Bus måste kräva högt tak.");
        }

        if (spotNumber < 1 || spotNumber > SpotCount)
        {
            throw new ArgumentOutOfRangeException(nameof(spotNumber));
        }

        return SpotSizes.Count == 0 ? DefultSpotSize : SpotSizes[spotNumber - 1];
    }
}

// hur mycket utrymme en buss kommer använda

public class Allocation
{
    public int Spot { get; set; }

    public int Units { get; set; }
}

public class ParkedVehicale
{
    public IVehical Vehicale { get; }

    public IReadOnlyList<Allocation> Allocations { get; }

    public ParkedVehicale(IVehical vehicale, IReadOnlyList<Allocation> allocations)
    {
        Vehicale = vehicale;
        Allocations = allocations;
    }
}

// Incheckning information som sparas i vehical.json
public class VehicaleRecord
{
    public string Registration { get; set; } = "";

    public string Type { get; set; } = "";

    public DateTimeOffset ArrivedAt { get; set; }

    public List<Allocation> Allocations { get; set; } = new();
}

// Utcheckning

public class Receipt
{
    public string Registration { get; set; } = "";

    public TimeSpan Duration { get; set; }

    public decimal Price { get; set; }
}

// Prisberäkning för parkering

public class PriceList
{
    public IReadOnlyDictionary<string, decimal> Rates { get; }

    public PriceList(Dictionary<string, decimal> rates)
    {
        Rates = new System.Collections.ObjectModel.ReadOnlyDictionary<string, decimal>(new Dictionary<string, decimal>(rates));
    }

    // Kontroller för giltig pris för alla fordosntyp 

    public void Validate(GarageConfig config)

    {
        foreach (string type in config.VehicaleTypes.Keys)
        {
            if (!Rates.ContainsKey(type))
            {
                throw new ArgumentException($"Pris saknas för{type}.");
            }
        }

        foreach (var rate in Rates)
        {
            if (rate.Value < 0 || rate.Value > 1000000)
            {
                throw new ArgumentException($"Ogiltigt pris för{rate.Key}.");

            }
        }
    }
    public decimal Calculate(string type, TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentException("Ankomsttiden ligger längre fram.");
        }
        if (duration <= TimeSpan.FromMinutes(10))
        {
            return 0;
        }

        decimal startedHours = (decimal)Math.Ceiling(duration.TotalHours);

        return startedHours * Rates[type];
    }

}

public class ConfiguredVehicale : Vehicale

{
    public override string Type { get; }

    public ConfiguredVehicale(string registration, DateTimeOffset arrivedAt, string type) : base(registration, arrivedAt)
    {
        Type = type;
    }
}

// Skapar rätt case för rätt Fordonstyp

public static class VehicaleFactory
{
    public static IVehical Create(string type, string registration, DateTimeOffset arrivedAt)
    {
        switch (type)
        {
            case "Car":
                return new Car(registration, arrivedAt);

            case "MC":
                return new MC(registration, arrivedAt);

            case "Bicycle":
                return new Bicycle(registration, arrivedAt);

            case "Bus":
                return new Bus(registration, arrivedAt);

            default: return new ConfiguredVehicale(registration, arrivedAt, type);
        }
    }
}
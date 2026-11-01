using PragueParking.Core;
using System.ComponentModel;
using System.Text.Json;

namespace PragueParking.Core;

public class ParkingGarage
{
    private readonly List<ParkedVehicale> parkedVehicales = new();

    private GarageConfig config;

    public IReadOnlyList<ParkedVehicale> Vehicales => parkedVehicales.AsReadOnly();
    public GarageConfig Config => CopyConfig(config);

    // GÖR EN KOPIA

    public ParkingGarage(GarageConfig setting)
    {
        setting.GetCapacity(1);

        config = CopyConfig(setting);
    }

    private static GarageConfig CopyConfig(GarageConfig setting)
    {
        string json = JsonSerializer.Serialize(setting);
        return JsonSerializer.Deserialize<GarageConfig>(json)!;

    }

    // översikt över pakeringsplatserna

    public IReadOnlyList<IParkingSpot> GetSpots()
    {
        var result = new List<IParkingSpot>();

        for (int number = 1; number <= config.SpotCount; number++)
        {
            int used = 0;
            bool reserved = false;

            foreach (var parked in parkedVehicales)
            {
                foreach (var allocation in parked.Allocations)
                {
                    if (allocation.Spot == number)
                    {
                        used += allocation.Units;

                        if (parked.Allocations.Count > 1)
                        {
                            reserved = true;
                    }
}
                }
            }
            int capacity = config.GetCapacity(number);

            if (reserved)
            {
                used = capacity;
            }

            result.Add(new ParkingSpot(number, capacity, used));
        }

        return result;
    }

    // Hitta parkerad fordon
    public ParkedVehicale Find(string registration)
    {
        string normalized = Vehicale.NormalizeRegistration(registration);

        var result = parkedVehicales.FirstOrDefault(parked => parked.Vehicale.Registration == normalized);

        if (result == null)
        {
            throw new ArgumentException("Fordonet finns inte");
        }

        return result;
    }
    public void CheckIn(string type, string registration, DateTimeOffset arrivedAt, int? preferredSpot = null)
    {
        if (!config.VehicaleTypes.TryGetValue(type, out var specificartion))
        {
            throw new ArgumentException("Okänd fordonstyp");
        }

        IVehical vehicale = VehicaleFactory.Create(type, registration, arrivedAt);

        bool alredyExists = parkedVehicales.Any(parked => parked.Vehicale.Registration == vehicale.Registration);

        if (alredyExists)
        {
            throw new ArgumentException("Fordonet är redan registrerad i parkeringshuset");
        }

        List<Allocation> allocations = FindSpace(specificartion, preferredSpot, null);

        parkedVehicales.Add(new ParkedVehicale(vehicale, allocations));
    }

    private List<Allocation> FindSpace(
    VehicaleTypeConfig specification,
    int? preferredSpot,
    string? ignoredRegistration)
    {
        int firstStart = preferredSpot ?? 1;
        int lastStart = preferredSpot ?? config.SpotCount;

        if (firstStart < 1 || firstStart > config.SpotCount)
        {
            throw new ArgumentException("Platsnumret finns inte.");
        }

        var used = new Dictionary<int, int>();
        var reserved = new HashSet<int>();

        foreach (var parked in parkedVehicales)
        {
            // Vid flytt räknas inte fordonets egna platser som upptagna.
            if (parked.Vehicale.Registration == ignoredRegistration)
            {
                continue;
            }

            foreach (var allocation in parked.Allocations)
            {
                used[allocation.Spot] =
                    used.GetValueOrDefault(allocation.Spot)
                    + allocation.Units;

                if (parked.Allocations.Count > 1)
                {
                    reserved.Add(allocation.Spot);
                }
            }
        }

        for (int start = firstStart; start <= lastStart; start++)
        {
            int remaining = specification.Size;

            bool needsSeveralSpots =
                specification.Size > config.GetCapacity(start);

            var allocations = new List<Allocation>();

            for (
                int number = start;
                number <= config.SpotCount && remaining > 0;
                number++)
            {
                // Hela fordonet måste få plats där taket är tillräckligt högt.
                if (specification.HighCeilingOnly
                    && number > config.HighCelingSpots)
                {
                    break;
                }

                if (reserved.Contains(number))
                {
                    break;
                }

                int capacity = config.GetCapacity(number);
                int occupied = used.GetValueOrDefault(number);
                int free = capacity - occupied;

                // Stora fordon behöver sammanhängande, helt lediga rutor.
                if (needsSeveralSpots && occupied > 0)
                {
                    break;
                }

                if (free <= 0)
                {
                    break;
                }

                // Ett mindre fordon ska få plats helt på en ruta.
                if (!needsSeveralSpots && free < remaining)
                {
                    break;
                }

                int units = Math.Min(free, remaining);

                allocations.Add(new Allocation
                {
                    Spot = number,
                    Units = units
                });

                remaining -= units;
            }

            if (remaining == 0)
            {
                return allocations;
            }
        }

        throw new ArgumentException(
            "Det finns inte tillräckligt med tillåtet utrymme.");
    }
// Flytta en fordon utan att ändra ankomsttiden
public void Move(string registration, int destination)
    {
        ParkedVehicale current = Find(registration);

        VehicaleTypeConfig specification =
            config.VehicaleTypes[current.Vehicale.Type];

        // Fordonets egna platser får återanvändas vid flytten.
        List<Allocation> allocations = FindSpace(
            specification,
            destination,
            current.Vehicale.Registration);

        int index = parkedVehicales.IndexOf(current);

        // Byt placering först när en giltig ny placering har hittats.
        parkedVehicales[index] = new ParkedVehicale(
            current.Vehicale,
            allocations);
    }

    // Beräknar parkeringstid och pris utan att ta bort fordonet.
    public Receipt GetReceipt(
        string registration,
        DateTimeOffset now,
        PriceList prices)
    {
        IVehical vehicle = Find(registration).Vehicale;

        TimeSpan duration = now - vehicle.ArrivedAt;

        return new Receipt
        {
            Registration = vehicle.Registration,
            Duration = duration,
            Price = prices.Calculate(vehicle.Type, duration)
        };
    }

    // Tar bort fordonet och frigör dess parkeringsplatser.
    public void Remove(string registration)
    {
        parkedVehicales.Remove(Find(registration));
    }

    // Skapar en lista som filhanteringen kan spara som JSON.
    public List<VehicaleRecord> Export()
    {
        return parkedVehicales.Select(parked => new VehicaleRecord
        {
            Registration = parked.Vehicale.Registration,
            Type = parked.Vehicale.Type,
            ArrivedAt = parked.Vehicale.ArrivedAt,

            Allocations = parked.Allocations.Select(
                allocation => new Allocation
                {
                    Spot = allocation.Spot,
                    Units = allocation.Units
                }).ToList()

        }).ToList();
    }

    // Läser in fordonsdata efter att filhanteringen har läst JSON-filen.
    public void Load(
        List<VehicaleRecord> records,
        DateTimeOffset now)
    {
        var candidate = new ParkingGarage(config);

        foreach (var record in records)
        {
            if (record == null
                || string.IsNullOrWhiteSpace(record.Type)
                || record.Registration == null
                || record.Allocations == null
                || record.Allocations.Any(item => item == null)
                || record.ArrivedAt == default
                || record.ArrivedAt > now)
            {
                throw new ArgumentException(
                    "Datafilen innehåller en ogiltig post.");
            }

            IVehical vehicle = VehicaleFactory.Create(
                record.Type,
                record.Registration,
                record.ArrivedAt);

            bool duplicate = candidate.parkedVehicales.Any(
                parked =>
                    parked.Vehicale.Registration
                    == vehicle.Registration);

            if (duplicate)
            {
                throw new ArgumentException(
                    "Datafilen innehåller dubbla registreringar.");
            }

            candidate.parkedVehicales.Add(
                new ParkedVehicale(vehicle, record.Allocations));
        }

        candidate.ValidatePlacements();

        // Ersätt nuvarande data först när allt har godkänts.
        parkedVehicales.Clear();
        parkedVehicales.AddRange(candidate.parkedVehicales);
    }

    // Kontrollerar nya inställningar mot redan parkerade fordon.
    public void Reconfigure(
        GarageConfig replacement,
        PriceList prices)
    {
        replacement.GetCapacity(1);
        prices.Validate(replacement);

        var candidate = new ParkingGarage(replacement);

        candidate.parkedVehicales.AddRange(parkedVehicales);

        candidate.ValidatePlacements();

        // Behåll tidigare inställningar om någon kontroll misslyckas.
        config = CopyConfig(replacement);
    }

    // Kontrollerar att fordonen har giltiga placeringar.
    private void ValidatePlacements()
    {
        foreach (var parked in parkedVehicales)
        {
            if (!config.VehicaleTypes.TryGetValue(
                parked.Vehicale.Type,
                out var specification))
            {
                throw new ArgumentException(
                    "En parkerad fordonstyp saknas i konfigurationen.");
            }

            var allocations = parked.Allocations;

            if (allocations.Count == 0)
            {
                throw new ArgumentException(
                    "Ett fordon saknar parkeringsplats.");
            }

            int distinctSpots = allocations
                .Select(item => item.Spot)
                .Distinct()
                .Count();

            if (distinctSpots != allocations.Count)
            {
                throw new ArgumentException(
                    "Ett fordon har samma ruta flera gånger.");
            }

            long totalUnits = 0;

            foreach (var allocation in allocations)
            {
                if (allocation.Spot < 1
                    || allocation.Spot > config.SpotCount
                    || allocation.Units <= 0)
                {
                    throw new ArgumentException(
                        "Ogiltig placering i data eller konfiguration.");
                }

                if (specification.HighCeilingOnly
                    && allocation.Spot > config.HighCelingSpots)
                {
                    throw new ArgumentException(
                        "Ett fordon skulle stå där taket är för lågt.");
                }

                totalUnits += allocation.Units;
            }

            if (totalUnits != specification.Size)
            {
                throw new ArgumentException(
                    "Fordonsstorleken stämmer inte med placeringen.");
            }

            var sorted = allocations
                .OrderBy(item => item.Spot)
                .ToList();

            for (int index = 1; index < sorted.Count; index++)
            {
                if (sorted[index].Spot != sorted[index - 1].Spot + 1)
                {
                    throw new ArgumentException(
                        "Stora fordon måste stå på sammanhängande rutor.");
                }
            }
        }

        // Kontrollera kapacitet och delning för varje ruta.
        for (int number = 1; number <= config.SpotCount; number++)
        {
            long used = 0;
            int vehicleCount = 0;
            bool hasLargeVehicle = false;

            foreach (var parked in parkedVehicales)
            {
                foreach (var allocation in parked.Allocations)
                {
                    if (allocation.Spot == number)
                    {
                        used += allocation.Units;
                        vehicleCount++;

                        if (parked.Allocations.Count > 1)
                        {
                            hasLargeVehicle = true;
                        }
                    }
                }
            }

            if (used > config.GetCapacity(number))
            {
                throw new ArgumentException(
                    "En parkeringsplats har för liten kapacitet.");
            }

            if (hasLargeVehicle && vehicleCount > 1)
            {
                throw new ArgumentException(
                    "Stora fordons rutor får inte delas med andra fordon.");
            }
        }
    }
}

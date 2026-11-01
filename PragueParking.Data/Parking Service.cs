using PragueParking.Core;
using System.Data;

namespace PragueParking.Data;

public class ParkingService
{
    private readonly FileStore fileStore;
    private readonly GarageConfig config;
    public ParkingGarage Garage { get; }
    public PriceList Prices { get; private set; }

    public ParkingService(FileStore fileStore, bool createTestData, DateTimeOffset now)
    {
        this.fileStore = fileStore;

        config = fileStore.ReadConfig();
        Prices = fileStore.ReadPrices();
        Prices.Validate(config);

        Garage = new ParkingGarage(config);

        if (fileStore.DataExists())
        {
            Garage.Load(fileStore.ReadVehicales(), now);
        }
        else
        {
            if (createTestData)
            {
                CreateTestData(now);
            }

            fileStore.SaveVehicales(Garage.Export());
        }
    }

    private void CreateTestData(DateTimeOffset now)
    {
        var examples = new Dictionary<string, string>
        {
            ["Car"] = "DEMO-CAR",
            ["MC"] = "DEMO-MC",
            ["Bicycle"] = "DEMO-BIKE",
            ["Bus"] = "DEMO-BUS"
        };

        foreach (var example in examples)
        {
            if (!config.VehicaleTypes.ContainsKey(example.Key))
            {
                continue;
            }

            try
            {
                Garage.CheckIn(example.Key, example.Value, now.AddHours(-2));
            }

            catch (ArgumentException)
            {

            }
        }
    }

    private void ChangeAndSave(Action change)
    {
        List<VehicaleRecord> previousState = Garage.Export();

        try
        {
            change();

            fileStore.SaveVehicales(Garage.Export());
        }
        catch
        {
            Garage.Load(previousState, DateTimeOffset.MaxValue);

            throw;
        }
    }

    public void CheckIn(
        string type,
        string registration,
        DateTimeOffset now,
        int? preferredSpot = null)
    {
        ChangeAndSave(() =>
            Garage.CheckIn(
                type,
                registration,
                now,
                preferredSpot));
    }

    public void Move(string registration, int destination)
    {
        ChangeAndSave(() =>
            Garage.Move(registration, destination));
    }

    public Receipt CheckOut(
        string registration,
        DateTimeOffset now)
    {
        Receipt receipt = Garage.GetReceipt(
            registration,
            now,
            Prices);

        ChangeAndSave(() =>
            Garage.Remove(registration));

        return receipt;
    }

    public void ReloadPrices()
    {
        PriceList newPrices = fileStore.ReadPrices();

        newPrices.Validate(Garage.Config);

        Prices = newPrices;
    }

    public void ReloadConfig()
    {
        GarageConfig newConfig = fileStore.ReadConfig();

        PriceList newPrices = fileStore.ReadPrices();

        Garage.Reconfigure(newConfig, newPrices);

        Prices = newPrices;
    }
}

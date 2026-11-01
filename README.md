# Prague Parking 2.0 och 2.1

**Utvecklare: Talha Özuer**

Prague Parking är en konsolapplikation i C# för att hantera ett parkeringshus. Programmet registrerar fordon, fördelar parkeringsutrymme, sparar ankomsttid och beräknar avgiften vid utcheckning. Gränssnittet använder Spectre.Console för menyer, skyltar, tabeller och en färgmarkerad parkeringskarta.

Lösningen innehåller två körbara versioner och två gemensamma klassbibliotek. Versionerna startas var för sig och använder separata datafiler.

## Innehåll

- [Versioner](#versioner)
- [Krav för att köra](#krav-för-att-köra)
- [Starta i Visual Studio](#starta-i-visual-studio)
- [Starta från terminal](#starta-från-terminal)
- [Användning](#användning)
- [Parkeringsregler](#parkeringsregler)
- [Prissättning](#prissättning)
- [Datafiler och sparning](#datafiler-och-sparning)
- [Ändra priser under körning](#ändra-priser-under-körning)
- [Konfiguration](#konfiguration)
- [Automatisk testdata](#automatisk-testdata)
- [Kodstruktur](#kodstruktur)
- [Testning](#testning)
- [Felsökning](#felsökning)
- [Dokumentation och leveranskontroll](#dokumentation-och-leveranskontroll)

## Versioner

| Projekt i Visual Studio | Version | Innehåll |
|---|---|---|
| `PragueParking.V.2.0` | 2.0, G | Bil och MC, parkering, sökning, flytt, utcheckning, prislista, parkeringskarta och sparning. |
| `PragueParking.v21` | 2.1, VG | Grundfunktionerna samt cyklar, bussar och omläsning av konfiguration med kontroll mot parkerade fordon. |

Båda versionerna använder `PragueParking.Core` och `PragueParking.Data`. Ett byte av startprojekt tar inte bort eller ersätter den andra versionen.

## Krav för att köra

- .NET 10 SDK.
- En Visual Studio-version som stöder .NET 10 och lösningsformatet `.slnx`.
- Arbetsbelastningen **.NET desktop development** för utveckling i Visual Studio.
- NuGet-paketet **Spectre.Console 0.57.2**, refererat av respektive konsolprojekt.
- Internetanslutning för första paketåterställningen om paketet inte redan finns lokalt.
- Skrivbehörighet i programmets data-mapp.

Ingen databas eller databasanslutning behövs. Programmet är manuellt provkört på Windows. Maximera gärna konsolfönstret för att få plats med kartan och hela menyn.

## Starta i Visual Studio

1. Hämta hela repot och packa upp det om det hämtats som ZIP.
2. Öppna lösningsfilen **`PragueParking.V 2.1.slnx`** i Visual Studio.
3. Låt Visual Studio återställa NuGet-paketen.
4. Kontrollera att båda konsolprojekten och de två klassbiblioteken visas i Solution Explorer.
5. Välj startprojekt enligt instruktionerna nedan.
6. Bygg med **Ctrl + Shift + B**.
7. Starta med **Ctrl + F5**.

### Starta VG-versionen 2.1

Högerklicka på **PragueParking.v21** i Solution Explorer och välj **Set as Startup Project**. Tryck sedan **Ctrl + F5**.

Programskylten ska visa **VERSION 2.1**. Vid incheckning finns bil, MC, cykel och buss i standardkonfigurationen. Menyn innehåller **Läs om konfiguration**.

### Starta G-versionen 2.0

Högerklicka på **PragueParking.V.2.0** och välj **Set as Startup Project**. Tryck sedan **Ctrl + F5**.

Programskylten ska visa **VERSION 2.0**. Standardkonfigurationen innehåller endast bil och MC. Menyn innehåller omläsning av priser, men inte omläsning av konfiguration.

Om fel version startas, kontrollera vilket projekt som är valt som startprojekt. Att bara öppna en annan Program.cs-fil ändrar inte startprojektet.

## Starta från terminal

Öppna en terminal i mappen där lösningsfilen ligger:

```powershell
dotnet build "PragueParking.V 2.1.slnx"
```

Starta 2.1:

```powershell
dotnet run --project "Prague Parking 2.0 - OOP-VERSION/PragueParking.v21.csproj"
```

Starta 2.0:

```powershell
dotnet run --project "PragueParking.V.2.0/PragueParking.V.2.0.csproj"
```

2.1-projektets fysiska mapp behåller sitt äldre namn, `Prague Parking 2.0 - OOP-VERSION`. Projektnamnet i Visual Studio och programmets versionsskylt visar att det är 2.1.

## Användning

Navigera i menyn med piltangenterna och välj med Enter. Efter en åtgärd används Enter för att återgå till menyn.

| Menyval | Funktion |
|---|---|
| Checka in | Välj fordonstyp och ange registreringsnummer eller ett unikt cykel-ID. Ange startplats, eller 0 för automatisk placering. |
| Sök fordon | Visa fordonstyp, ankomsttid och platser för en registrering eller ett ID. |
| Flytta fordon | Ange registrering och ny startplats. Ankomsttiden behålls. |
| Checka ut | Ta bort fordonet, frigör platserna och visa parkeringstid och avgift. |
| Visa alla fordon | Visa parkerade fordon och deras placeringar. |
| Visa priser | Visa den inlästa prislistan. |
| Läs om priser | Läs om prisfilen medan programmet körs. |
| Läs om konfiguration | Endast 2.1: kontrollera och använd nya inställningar medan programmet körs. |
| Avsluta | Avsluta programmet. |

Registreringar och ID:n normaliseras till stora bokstäver och får innehålla 1–20 bokstäver, siffror eller bindestreck. Mellanslag inne i ett ID tillåts inte. Samma ID får inte användas av två samtidigt parkerade fordon.

## Parkeringsregler

Garaget har 100 platser som standard. En vanlig ruta har storleken 4.

| Fordon | Namn i konfigurationen | Storlek | Utrymme med standardinställningar |
|---|---|---:|---|
| Bil | Car | 4 | En ruta. |
| MC | MC | 2 | Två MC kan dela en ruta. |
| Cykel | Bicycle | 1 | Fyra cyklar kan dela en ruta. |
| Buss | Bus | 16 | Fyra sammanhängande rutor. |

Små fordon kan dela samma ruta så länge den sammanlagda storleken ryms. En MC och en cykel använder exempelvis 3 av rutans 4 enheter.

Stora fordon använder sammanhängande platser i nummerordning. Dessa rutor reserveras och delas inte med andra fordon. Bussar får endast använda platser med högt tak, normalt plats 1–50. En buss kan därför börja på plats 47 och använda 47–50, men inte börja på plats 48 om den då skulle behöva plats 51.

### Kartans markeringar

| Markering | Färg | Betydelse |
|---|---|---|
| L | Grön | Helt ledig ruta. |
| D | Gul | Delvis upptagen ruta med ledig kapacitet. |
| F | Röd | Full ruta eller ruta reserverad för ett större fordon. |

Antal parkerade fordon och antal upptagna rutor är olika mått. En buss räknas som ett fordon men använder fyra standardrutor.

## Prissättning

Grundtaxorna enligt uppgiften är:

| Fordon | CZK per påbörjad timme |
|---|---:|
| Bil | 20 |
| MC | 10 |
| Cykel | 5 |
| Buss | 80 |

Den faktiskt använda taxan läses från respektive versions `price.txt`. Kontrollera att grundpriserna är återställda efter tester av prisändringar.

### Gratisperiod och påbörjad timme

Parkering i **högst tio minuter**, inklusive exakt tio minuter, är gratis i denna implementation. Därefter debiteras påbörjade timmar från ankomsten. Gratisperioden dras alltså inte av från den totala parkeringstiden.

| Total parkeringstid | Bilpris vid 20 CZK per timme |
|---|---:|
| 9 minuter | 0 CZK |
| Exakt 10 minuter | 0 CZK |
| 10 minuter och 1 sekund | 20 CZK |
| Exakt 60 minuter | 20 CZK |
| 60 minuter och 1 sekund | 40 CZK |
| 2 timmar och 1 minut | 60 CZK |

Uppgiften använder både formuleringen ”högst tio minuter” och ”mindre än tio minuter”. Programmet följer den första formuleringen.

Kvittot visar parkeringstid avrundad för visning, men prisberäkningen använder den underliggande tiden. Den prislista som är inläst vid utcheckningen används även för fordon som checkats in före en prisändring.

## Datafiler och sparning

Varje konsolprojekt har egna startfiler för konfiguration och priser. Vid byggning kopieras de till programmets körningsmapp med inställningen **Copy if newer**.

| Fil | Användning |
|---|---|
| `config.json` | Antal platser, storlekar, fordonstyper och höjdbegränsning. |
| `price.txt` | Lättredigerade timpriser med stöd för kommentarer. |
| `vehicales.json` | Parkerade fordon, ankomsttider och placeringar. Skapas av programmet. |

Filnamnet **`vehicales.json`** följer stavningen i den nuvarande implementationen. Ändra inte filnamnet utan att även ändra filhanteringen.

### Projektfiler och körningsfiler

Det finns två uppsättningar konfigurations- och prisfiler:

1. **Projektets filer**, under Data-mappen i Solution Explorer. Dessa är startfilerna som ska finnas i repot.
2. **Körningskopiorna**, normalt under `bin/Debug/net10.0/Data` i respektive konsolprojekts mapp. Programmet läser och uppdaterar dessa under körning.

Mappnamnet Data kan visas med annan kombination av stora och små bokstäver i de två projekten. Under Windows motsvarar det samma sökväg.

Om Release används ligger körningskopiorna under `bin/Release/net10.0/Data`. Programmet utgår från sin egen körningsmapp, inte från en absolut sökväg på utvecklarens dator.

### När data sparas

Fordonsdata sparas efter varje lyckad incheckning, flytt och utcheckning. Vid omstart läses samma datafil in, inklusive ursprungliga ankomsttider.

Sparningen skriver först en tillfällig fil och ersätter sedan datafilen. ParkingService har kod för att återställa tillståndet i minnet om en förändring eller sparning misslyckas. Simulerat diskfel har inte verifierats manuellt.

De två konsolprojekten har separata körningsmappar och därmed separata fordonslistor. Använd en programinstans per data-mapp.

## Ändra priser under körning

1. Låt programmet vara igång på huvudmenyn.
2. Öppna den körande versionens `bin/Debug/net10.0/Data/price.txt`.
3. Ändra ett belopp, exempelvis `Car = 20` till `Car = 25`.
4. Spara filen.
5. Välj **Läs om priser** i programmet.
6. Välj **Visa priser** för att kontrollera ändringen.

Bygg inte om eller starta om programmet under detta test. Enbart en ändring av projektets startfil läses inte direkt av det redan körande programmet.

Exempel på filformat:

```text
# Priser i CZK per påbörjad timme.
# Allt efter # är en kommentar.
Car = 20
MC = 10
Bicycle = 5
Bus = 80
```

2.0-versionen behöver bara raderna för Car och MC. Decimaler skrivs med punkt, exempelvis `20.50`. När ändringen ska följa med en ny kopia av repot ändras även projektets startfil.

## Konfiguration

Exempel för 2.1:

```json
{
  "SpotCount": 100,
  "DefultSpotSize": 4,
  "HighCelingSpots": 50,
  "SpotSizes": [],
  "VehicaleTypes": {
    "Car": { "Size": 4, "HighCeilingOnly": false },
    "MC": { "Size": 2, "HighCeilingOnly": false },
    "Bicycle": { "Size": 1, "HighCeilingOnly": false },
    "Bus": { "Size": 16, "HighCeilingOnly": true }
  }
}
```

Nycklarnas stavning, exempelvis **DefultSpotSize**, **HighCelingSpots** och **VehicaleTypes**, matchar de befintliga C#-egenskaperna och måste behållas.

| Inställning | Betydelse | Gränser i nuvarande kod |
|---|---|---|
| SpotCount | Antal parkeringsplatser. | 1–100. |
| DefultSpotSize | Standardstorlek per ruta. | 1–10. |
| HighCelingSpots | Antal första platser med högt tak. | 0–50. |
| SpotSizes | Individuella rutstorlekar. | Tom lista, eller en storlek 1–100 för varje plats. |
| VehicaleTypes | Tillåtna fordonstyper. | Minst en typ. |
| Size | Fordonets storlek. | 1–100. |
| HighCeilingOnly | Om fordonet behöver högt tak. | Måste vara true för Bus. |

En tom SpotSizes-lista innebär att alla rutor använder DefultSpotSize. För 2.0 innehåller VehicaleTypes endast Car och MC. JSON tillåter inte kommentarer av typen `#` eller `//`.

### Läs om konfiguration i 2.1

1. Ändra den körande versionens `config.json` i körningsmappen.
2. Spara filen.
3. Välj **Läs om konfiguration**.
4. Kontrollera att kartan och platsantalet uppdateras.

Nya inställningar kontrolleras mot redan parkerade fordon innan de används. Exempelvis får en upptagen plats inte tas bort och kapaciteten får inte bli för liten för fordonen som redan står där. En parkerad fordonstyp får inte tas bort, och en buss får inte hamna utanför högtaksområdet. Vid ett avvisat konfigurationsbyte behålls den tidigare konfigurationen i programmet. Rätta även filen innan en senare omstart.

## Automatisk testdata

När `vehicales.json` saknas skapas en ny datafil med de testfordon som är tillåtna och får plats i konfigurationen:

| Version med standardinställningar | Testfordon |
|---|---|
| 2.0 | DEMO-CAR och DEMO-MC. |
| 2.1 | DEMO-CAR, DEMO-MC, DEMO-BIKE och DEMO-BUS. |

Testfordonen får ankomsttid två timmar före starten. Vid nästa start läses den befintliga filen; testfordonen läggs inte till igen. En befintlig fil som innehåller `[]` förblir tom.

För att prova skapandet: avsluta programmet, säkerhetskopiera eller byt namn på körningsmappens `vehicales.json` och starta igen. Ändra inte datafilen medan programmet körs. Ta inte bort en fil med fordonsdata som behöver behållas.

## Kodstruktur

```text
PragueParking.V 2.1.slnx
├── PragueParking.Core
│   ├── Models.cs
│   └── Parking Garage.cs
├── PragueParking.Data
│   ├── FileStore.cs
│   └── Parking Service.cs
├── Prague Parking 2.0 - OOP-VERSION
│   ├── PragueParking.v21.csproj
│   ├── Program.cs
│   └── Data
│       ├── config.json
│       └── price.txt
└── PragueParking.V.2.0
    ├── PragueParking.V.2.0.csproj
    ├── Program v2.0.cs
    └── DATA
        ├── config.json
        └── price.txt
```

- **Core** innehåller fordonsmodellen, arv, interface, parkeringsregler, konfiguration och prisberäkning. De två centrala gränssnitten heter `IVehical` och `IParkingSpot` i den nuvarande koden. Basklassen heter `Vehicale` och ärvs av Car, MC, Bicycle och Bus.
- **Data** är ett separat klassbibliotek. FileStore hanterar filåtkomsten. ParkingService samordnar uppstart, parkeringsändringar, sparning och omläsning av filer.
- **Konsolprojekten** innehåller uppstart, meny och presentation. Main har två anrop: förberedelser via Setup och programmets körning via Run.

Flera namn behåller den stavning som används i implementationen. Referenser, JSON-nycklar och filnamn behöver ändras tillsammans om de döps om.

## Testning

Följande funktioner har manuellt provats under utvecklingen. Listan beskriver genomförda kontroller, inte en automatiserad testsvit.

| Kontroll | Observerat resultat |
|---|---|
| Start av 2.0 och 2.1 | Respektive version startar och visar rätt meny och fordonstyper. |
| Incheckning och sökning | Fordonet hittas och kartan uppdateras. |
| Flytt | Ny plats används och tidigare plats frigörs. |
| Omstart | Fordon och placeringar finns kvar. |
| Utcheckning | Fordonet tas bort, platsen frigörs och kvitto visas. |
| Två MC på en ruta | Första ger delvis upptagen ruta, andra fyller rutan. En tredje nekas. |
| Fyra cyklar på en ruta | Fyra ryms, en femte nekas. |
| Buss med startplats 47 | Platserna 47–50 reserveras. |
| Bussflytt med startplats 48 | Nekas och den tidigare placeringen behålls. |
| Pris inom gratisperioden | 0 CZK. |
| Bil parkerad mer än tio minuter | 20 CZK vid timtaxan 20. |
| Påbörjad ny timme | Pris kontrollerat mot kvittots totala tid. |
| Prisomläsning | Ny taxa visas utan omstart. |
| Giltig konfigurationsändring | Platsantal och karta uppdateras. |
| Borttagning av upptagen plats | Nekas; befintliga platser och fordon behålls. |
| Saknad datafil | Testdata skapas. |
| Omstart efter skapad testdata | Antalet testfordon dubbleras inte. |
| Dubbelt registreringsnummer | Incheckningen nekas. |


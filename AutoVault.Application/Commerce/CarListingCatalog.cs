using System.ComponentModel.DataAnnotations;

namespace FinalProject.Commerce;

public sealed record CarFeatureDefinition(string Key, string Label);

public static class CarFeatureCatalog
{
    public const int MaxStoredLength = 800;

    public static readonly IReadOnlyList<CarFeatureDefinition> All =
    [
        new("ClimateControl", "Automatic Climate Control"),
        new("HeatedSeats", "Heated Seats"),
        new("LeatherSeats", "Leather Seats"),
        new("ParkingSensors", "Parking Sensors"),
        new("RearCamera", "Rear-view Camera"),
        new("Camera360", "360-degree Camera"),
        new("CruiseControl", "Cruise Control"),
        new("AdaptiveCruise", "Adaptive Cruise Control"),
        new("Abs", "ABS"),
        new("Esp", "ESP"),
        new("Airbags", "Airbags"),
        new("BlindSpot", "Blind-spot Monitoring"),
        new("LaneAssist", "Lane Assistance"),
        new("Navigation", "Navigation System"),
        new("Touchscreen", "Touchscreen Display"),
        new("Bluetooth", "Bluetooth"),
        new("CarPlay", "Apple CarPlay / Android Auto"),
        new("Sunroof", "Sunroof / Panoramic Roof"),
        new("LedHeadlights", "LED Headlights"),
        new("AutoHeadlights", "Automatic Headlights"),
        new("FogLights", "Fog Lights"),
        new("KeylessEntry", "Keyless Entry"),
        new("StartStop", "Start-Stop System"),
        new("PowerWindows", "Power Windows"),
        new("ElectricMirrors", "Electric Mirrors"),
        new("EngineType", "Engine Type"),
        new("Horsepower", "Horsepower")
    ];

    public static bool IsKnown(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var text = value.Trim();
        return All.Any(feature =>
            feature.Key.Equals(text, StringComparison.OrdinalIgnoreCase)
            || feature.Label.Equals(text, StringComparison.OrdinalIgnoreCase));
    }

    public static string Store(IEnumerable<string>? selected)
    {
        var chosen = new HashSet<string>(
            (selected ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim()),
            StringComparer.OrdinalIgnoreCase);

        return string.Join('|', All
            .Where(feature => chosen.Contains(feature.Key) || chosen.Contains(feature.Label))
            .Select(feature => feature.Key));
    }

    public static IReadOnlyList<string> ReadKeys(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return Array.Empty<string>();
        }

        var chosen = new HashSet<string>(
            stored.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);

        return All.Where(feature => chosen.Contains(feature.Key)).Select(feature => feature.Key).ToList();
    }

    public static IReadOnlyList<string> Labels(IEnumerable<string>? keys)
    {
        var chosen = new HashSet<string>(
            (keys ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim()),
            StringComparer.OrdinalIgnoreCase);

        return All
            .Where(feature => chosen.Contains(feature.Key) || chosen.Contains(feature.Label))
            .Select(feature => feature.Label)
            .ToList();
    }
}

public static class CarListingRules
{
    public static readonly string[] DriveTypes = ["FWD", "RWD", "AWD", "4WD"];

    public static readonly string[] Fuels = ["Petrol", "Diesel", "Hybrid", "Plug-in Hybrid", "Electric", "LPG", "CNG", "Other"];

    public static readonly string[] Transmissions = ["Automatic", "Manual", "CVT", "Semi-Automatic", "DCT", "Other"];

    public static readonly int[] SeatChoices = [2, 3, 4, 5, 6, 7, 8, 9];

    public static readonly int[] DoorChoices = [2, 3, 4, 5];

    public static readonly string[] MileageUnits = ["km", "mi"];

    public static readonly string[] SteeringPositions = ["LHD", "RHD"];

    public static readonly string[] AccidentChoices = ["None reported", "Minor damage", "Major damage"];

    public static readonly string[] CustomsChoices = ["Cleared", "Not cleared"];

    public static readonly string[] Conditions = ["New", "Used"];

    public static readonly int[] CylinderChoices = [2, 3, 4, 5, 6, 8, 10, 12, 16];

    public static readonly string[] EngineSizes =
    [
        "0.6L", "0.8L", "0.9L", "1.0L", "1.2L", "1.3L", "1.4L", "1.5L", "1.6L", "1.8L", "1.9L",
        "2.0L", "2.2L", "2.4L", "2.5L", "2.8L", "3.0L", "3.2L", "3.5L", "3.6L", "3.8L",
        "4.0L", "4.2L", "4.4L", "4.5L", "5.0L", "5.5L", "6.0L", "6.2L", "6.3L", "6.5L", "6.7L", "7.0L",
        "Dual-motor"
    ];

    public static readonly int[] HorsepowerChoices =
    [
        50, 60, 70, 75, 80, 90, 100, 110, 120, 130, 140, 150, 160, 170, 180, 190, 200,
        220, 240, 250, 270, 280, 300, 320, 350, 380, 400, 420, 450, 480, 500, 550, 600,
        650, 700, 750, 800, 850, 900, 1000
    ];

    public static readonly IReadOnlyList<(string Name, string Hex)> ExteriorColors =
    [
        ("Black", "#000000"),
        ("White", "#FFFFFF"),
        ("Silver", "#C0C0C0"),
        ("Gray", "#808080"),
        ("Blue", "#1E4E8C"),
        ("Red", "#C8102E"),
        ("Green", "#2E7D32"),
        ("Brown", "#6B4423"),
        ("Beige", "#D4C4A8"),
        ("Gold", "#C9A227"),
        ("Orange", "#F26522"),
        ("Yellow", "#F4C430"),
        ("Purple", "#5B2C6F"),
        ("Bronze", "#8C6239")
    ];

    public static readonly string[] MarketOrigins =
    [
        "Azerbaijan", "Germany", "United States", "Japan", "South Korea", "United Kingdom",
        "France", "Italy", "Spain", "Turkey", "United Arab Emirates", "China", "Sweden",
        "Czech Republic", "Poland", "Georgia", "Russia", "Canada", "Mexico", "Brazil",
        "India", "Thailand", "Australia", "Other"
    ];

    public static int MaxModelYear => Math.Max(2026, DateTime.UtcNow.Year);

    public static IEnumerable<ValidationResult> ValidateYear(int year)
    {
        if (year < 1900 || year > MaxModelYear)
        {
            yield return new ValidationResult(
                $"Enter a model year from 1900 through {MaxModelYear}.",
                ["Year"]);
        }
    }

    public static string? CanonicalDrive(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = value.Trim();
        return DriveTypes.FirstOrDefault(drive => drive.Equals(text, StringComparison.OrdinalIgnoreCase));
    }

    public static IEnumerable<ValidationResult> ValidateOptionalSpecs(
        string? driveType,
        IEnumerable<string>? features)
    {
        if (!string.IsNullOrWhiteSpace(driveType) && CanonicalDrive(driveType) == null)
        {
            yield return new ValidationResult(
                "Select FWD, RWD, AWD, or 4WD.",
                ["DriveType"]);
        }

        if (features == null)
        {
            yield break;
        }

        foreach (var feature in features)
        {
            if (!string.IsNullOrWhiteSpace(feature) && !CarFeatureCatalog.IsKnown(feature))
            {
                yield return new ValidationResult(
                    "Select a listed car feature.",
                    ["Features"]);
                yield break;
            }
        }
    }

    public static string? Canonical(string? value, IEnumerable<string> options)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = value.Trim();
        return options.FirstOrDefault(option => option.Equals(text, StringComparison.OrdinalIgnoreCase));
    }

    public static string? CanonicalFuel(string? value) => Canonical(value, Fuels);

    public static string? CanonicalTransmission(string? value) => Canonical(value, Transmissions);

    public static string? CanonicalMileageUnit(string? value)
    {
        if (string.Equals(value?.Trim(), "miles", StringComparison.OrdinalIgnoreCase))
        {
            return "mi";
        }

        if (string.Equals(value?.Trim(), "kilometers", StringComparison.OrdinalIgnoreCase))
        {
            return "km";
        }

        return Canonical(value, MileageUnits);
    }

    public static IEnumerable<ValidationResult> ValidateDetails(
        string? fuelType,
        string? transmission,
        string? mileageUnit,
        int? horsepower,
        int? doors,
        int? cylinders,
        string? steering,
        string? vin,
        string? accidentHistory,
        string? customsStatus,
        bool conditionSpecified,
        string? condition)
    {
        if (CanonicalFuel(fuelType) == null)
        {
            yield return new ValidationResult("Select a fuel type.", ["FuelType"]);
        }

        if (CanonicalTransmission(transmission) == null)
        {
            yield return new ValidationResult("Select a transmission.", ["Transmission"]);
        }

        if (!string.IsNullOrWhiteSpace(mileageUnit) && CanonicalMileageUnit(mileageUnit) == null)
        {
            yield return new ValidationResult("Select km or miles.", ["MileageUnit"]);
        }

        if (horsepower is < 1 or > 2000)
        {
            yield return new ValidationResult("Enter horsepower between 1 and 2000.", ["Horsepower"]);
        }

        if (doors is < 2 or > 5)
        {
            yield return new ValidationResult("Select 2 to 5 doors.", ["Doors"]);
        }

        if (cylinders is < 1 or > 16)
        {
            yield return new ValidationResult("Enter 1 to 16 cylinders.", ["Cylinders"]);
        }

        if (!string.IsNullOrWhiteSpace(steering) && Canonical(steering, SteeringPositions) == null)
        {
            yield return new ValidationResult("Select left-hand or right-hand drive.", ["Steering"]);
        }

        if (!string.IsNullOrWhiteSpace(vin))
        {
            var text = vin.Trim();
            if (text.Length is < 11 or > 17 || text.Any(character => !char.IsLetterOrDigit(character)))
            {
                yield return new ValidationResult(
                    "Enter a VIN or chassis number using 11 to 17 letters and numbers.",
                    ["Vin"]);
            }
        }

        if (!string.IsNullOrWhiteSpace(accidentHistory) && Canonical(accidentHistory, AccidentChoices) == null)
        {
            yield return new ValidationResult("Select an accident history.", ["AccidentHistory"]);
        }

        if (!string.IsNullOrWhiteSpace(customsStatus) && Canonical(customsStatus, CustomsChoices) == null)
        {
            yield return new ValidationResult("Select a customs status.", ["CustomsStatus"]);
        }

        if (conditionSpecified && Canonical(condition, Conditions) == null)
        {
            yield return new ValidationResult("Select New or Used.", ["Condition"]);
        }
    }

    public static IEnumerable<ValidationResult> ValidateColorAndEngine(string? color, string? engine, string? fuelType)
    {
        foreach (var result in VehicleColorCatalog.Validate(color))
        {
            yield return result;
        }

        foreach (var result in VehicleEngineCatalog.Validate(engine, fuelType))
        {
            yield return result;
        }
    }
}

public static class VehicleColorCatalog
{
    public static string DisplayName(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return string.Empty;
        }

        var parts = stored.Split('|', 2);
        return parts[0].Trim();
    }

    public static string? Hex(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return null;
        }

        var parts = stored.Split('|', 2);
        return parts.Length == 2 ? NormalizeHex(parts[1]) : null;
    }

    public static string Store(string? name, string? hex)
    {
        var color = string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();
        var code = NormalizeHex(hex);
        return string.IsNullOrWhiteSpace(code) ? color : $"{color}|{code}";
    }

    public static string FromPosted(string? posted)
    {
        if (string.IsNullOrWhiteSpace(posted))
        {
            return string.Empty;
        }

        var name = DisplayName(posted);
        var hex = Hex(posted);
        if (hex == null && posted.Contains('|', StringComparison.Ordinal) == false)
        {
            hex = NormalizeHex(posted);
            if (hex != null && string.Equals(name, posted.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                var known = CarListingRules.ExteriorColors.FirstOrDefault(item =>
                    item.Hex.Equals(hex, StringComparison.OrdinalIgnoreCase));
                name = string.IsNullOrEmpty(known.Name) ? name : known.Name;
            }
        }

        return Store(name, hex);
    }

    public static string? NormalizeHex(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = value.Trim();
        if (text.StartsWith('#'))
        {
            text = text[1..];
        }

        if (text.Length is not (3 or 6) || text.Any(character => !Uri.IsHexDigit(character)))
        {
            return null;
        }

        if (text.Length == 3)
        {
            text = string.Concat(text.Select(character => new string(character, 2)));
        }

        return "#" + text.ToUpperInvariant();
    }

    public static IEnumerable<ValidationResult> Validate(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            yield return new ValidationResult("Select or enter an exterior color.", ["Color"]);
            yield break;
        }

        var name = DisplayName(stored);
        var hex = Hex(stored);
        if (string.IsNullOrWhiteSpace(name) || name.Length > 40)
        {
            yield return new ValidationResult("Enter a color name of 40 characters or fewer.", ["Color"]);
        }

        if (stored.Contains('|', StringComparison.Ordinal) && hex == null)
        {
            yield return new ValidationResult("Enter a valid HEX color such as #000000.", ["Color"]);
        }
    }
}

public static class VehicleBodyPartCatalog
{
    public const string OtherKey = "Other";

    public static readonly string[] Parts =
    [
        "Hood",
        "Roof",
        "Front Bumper",
        "Rear Bumper",
        "Front Left Door",
        "Front Right Door",
        "Rear Left Door",
        "Rear Right Door",
        "Left Front Fender",
        "Right Front Fender",
        "Left Rear Fender",
        "Right Rear Fender",
        "Trunk/Boot",
        "Left Side Skirt",
        "Right Side Skirt"
    ];

    public static string? Store(IEnumerable<string>? selectedParts, string? otherText)
    {
        var chosen = new List<string>();
        foreach (var part in Parts)
        {
            if (selectedParts?.Any(item => part.Equals(item?.Trim(), StringComparison.OrdinalIgnoreCase)) == true)
            {
                chosen.Add(part);
            }
        }

        var other = string.IsNullOrWhiteSpace(otherText) ? null : otherText.Trim();
        if (selectedParts?.Any(item => OtherKey.Equals(item?.Trim(), StringComparison.OrdinalIgnoreCase)) == true
            || !string.IsNullOrWhiteSpace(other))
        {
            chosen.Add(string.IsNullOrWhiteSpace(other) ? OtherKey : $"{OtherKey}:{other}");
        }

        if (chosen.Count == 0)
        {
            return null;
        }

        var stored = string.Join('|', chosen);
        return stored.Length <= 400 ? stored : stored[..400];
    }

    public static string? Normalize(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return null;
        }

        var parsed = Read(stored);
        return Store(parsed.Parts.Concat(parsed.OtherSelected ? [OtherKey] : Array.Empty<string>()), parsed.OtherText);
    }

    public static (IReadOnlyList<string> Parts, bool OtherSelected, string? OtherText) Read(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return (Array.Empty<string>(), false, null);
        }

        var text = stored.Trim();
        var tokens = text.Contains('|', StringComparison.Ordinal)
            ? text.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            : text.Split([',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length == 1
            && !Parts.Contains(tokens[0], StringComparer.OrdinalIgnoreCase)
            && !tokens[0].StartsWith(OtherKey + ":", StringComparison.OrdinalIgnoreCase)
            && !OtherKey.Equals(tokens[0], StringComparison.OrdinalIgnoreCase))
        {
            return (Array.Empty<string>(), true, tokens[0]);
        }

        var selected = new List<string>();
        string? other = null;
        var otherSelected = false;
        foreach (var token in tokens)
        {
            if (OtherKey.Equals(token, StringComparison.OrdinalIgnoreCase))
            {
                otherSelected = true;
                continue;
            }

            if (token.StartsWith(OtherKey + ":", StringComparison.OrdinalIgnoreCase))
            {
                otherSelected = true;
                other = token[(OtherKey.Length + 1)..].Trim();
                continue;
            }

            var known = Parts.FirstOrDefault(part => part.Equals(token, StringComparison.OrdinalIgnoreCase));
            if (known != null)
            {
                selected.Add(known);
            }
            else
            {
                otherSelected = true;
                other = string.IsNullOrWhiteSpace(other) ? token : $"{other}, {token}";
            }
        }

        return (selected, otherSelected, other);
    }

    public static string Display(string? stored)
    {
        var parsed = Read(stored);
        var labels = parsed.Parts.ToList();
        if (parsed.OtherSelected)
        {
            labels.Add(string.IsNullOrWhiteSpace(parsed.OtherText) ? OtherKey : parsed.OtherText!);
        }

        return string.Join(", ", labels);
    }
}

public static class VehicleEngineCatalog
{
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = value.Trim();
        if (CarListingRules.EngineSizes.Contains(text, StringComparer.OrdinalIgnoreCase))
        {
            return CarListingRules.EngineSizes.First(item => item.Equals(text, StringComparison.OrdinalIgnoreCase));
        }

        if (decimal.TryParse(text.TrimEnd('L', 'l'), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var liters)
            && liters is >= 0.2m and <= 12m)
        {
            return liters.ToString(liters == decimal.Truncate(liters) ? "0.0" : "0.#", System.Globalization.CultureInfo.InvariantCulture) + "L";
        }

        return text.Length <= 80 ? text : null;
    }

    public static IEnumerable<ValidationResult> Validate(string? engine, string? fuelType)
    {
        if (string.IsNullOrWhiteSpace(engine))
        {
            yield break;
        }

        if (Normalize(engine) == null)
        {
            yield return new ValidationResult("Enter an engine size such as 2.0L, or Dual-motor for electric vehicles.", ["Engine"]);
        }
    }
}

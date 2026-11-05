using FinalProject.MVC.Mappings;
using FinalProject.MVC.DTOs;
using Xunit;

namespace FinalProject.Tests;

public class CarApiMapperTests
{
    private static CarApiDto Dto(string description, string driveType = "") => new()
    {
        Id = 1,
        Model = "370Z",
        Description = description,
        DriveType = driveType,
        Brand = new BrandApiDto { Name = "Nissan" }
    };

    [Fact]
    public void ToCar_splits_drive_type_and_engine_without_inventing_seats()
    {
        var car = CarApiMapper.ToCar(Dto("Nissan 370Z. 2 seats, rear-wheel drive, 3.7L V6."));

        Assert.Equal("Nissan 370Z", car.Name);
        Assert.Equal(0, car.Seats);
        Assert.Equal("RWD", car.DriveType);
        Assert.Equal("3.7L V6", car.Engine);
    }

    [Theory]
    [InlineData("BMW M340i. 5 seats, xDrive all-wheel drive.", "AWD")]
    [InlineData("Nissan Armada. 8 seats, four-wheel drive, 5.6L V8.", "4WD")]
    [InlineData("Nissan Altima. 5 seats, front-wheel drive, 2.5L.", "FWD")]
    public void ToCar_normalises_drive_type(string description, string expected)
    {
        Assert.Equal(expected, CarApiMapper.ToCar(Dto(description)).DriveType);
    }

    [Fact]
    public void ToCar_leaves_specs_empty_when_description_has_none()
    {
        var car = CarApiMapper.ToCar(Dto("Nissan 350Z"));

        Assert.Equal(0, car.Seats);
        Assert.Equal(string.Empty, car.DriveType);
        Assert.Equal(string.Empty, car.Engine);
    }

    [Fact]
    public void ToCar_does_not_treat_engine_text_as_drive_type()
    {
        var car = CarApiMapper.ToCar(Dto("BMW M3 Competition. 5 seats, rear-wheel drive."));

        Assert.Equal("RWD", car.DriveType);
        Assert.Equal(string.Empty, car.Engine);
    }

    [Fact]
    public void ToCar_capitalises_electric_motor_description()
    {
        var car = CarApiMapper.ToCar(Dto("Kia EV6. 5 seats, all-wheel drive, dual motor."));

        Assert.Equal("AWD", car.DriveType);
        Assert.Equal("Dual motor", car.Engine);
    }

    [Theory]
    [InlineData("Nissan 350Z")]
    [InlineData("Nissan 370Z. 2 seats, rear-wheel drive, 3.7L V6.")]
    [InlineData("BMW M340i. 5 seats, xDrive all-wheel drive.")]
    public void ToCar_marks_catalog_spec_text_as_summary(string description)
    {
        Assert.True(CarApiMapper.ToCar(Dto(description)).DescriptionIsSpecSummary);
    }

    [Fact]
    public void ToCar_does_not_parse_seller_prose_as_specs()
    {
        var car = CarApiMapper.ToCar(Dto("Great daily driver. Very clean, one owner, full service history."));

        Assert.False(car.DescriptionIsSpecSummary);
        Assert.Equal(0, car.Seats);
        Assert.Equal(string.Empty, car.DriveType);
        Assert.Equal(string.Empty, car.Engine);
    }

    [Fact]
    public void ToCar_prefers_explicit_api_drive_type()
    {
        var car = CarApiMapper.ToCar(Dto("Nissan 370Z. 2 seats, all-wheel drive, 3.7L V6.", "rear-wheel drive"));

        Assert.Equal("RWD", car.DriveType);
    }

    [Fact]
    public void ToCar_prefers_saved_specs_and_lists_only_selected_features()
    {
        var dto = Dto("Nissan 370Z. 2 seats, all-wheel drive, 3.7L V6.");
        dto.Seats = 7;
        dto.Engine = "2.0L Turbo";
        dto.Features = ["Touchscreen", "FogLights", "NotAFeature"];

        var car = CarApiMapper.ToCar(dto);

        Assert.Equal(7, car.Seats);
        Assert.Equal("2.0L Turbo", car.Engine);
        Assert.Equal(["Touchscreen Display", "Fog Lights"], car.Features);
    }

    [Fact]
    public void ToCar_does_not_read_seats_from_description_when_database_seats_are_missing()
    {
        var car = CarApiMapper.ToCar(
            Dto("Family SUV. 7 seats, all-wheel drive, 3.5L V6, leather trim, panoramic roof."));

        Assert.Equal(0, car.Seats);
        Assert.Equal(0, CarApiMapper.ToCar(Dto("Clean car. No seat count here, just prose.")).Seats);
    }

    [Fact]
    public void ToCar_ignores_out_of_range_database_seats_and_does_not_invent_counts()
    {
        var dto = Dto("Great daily driver. Very clean, one owner, full service history.");
        dto.Seats = 99;

        Assert.Equal(0, CarApiMapper.ToCar(dto).Seats);
    }

    [Fact]
    public void ToCar_uses_make_and_model_as_the_title_for_a_written_description()
    {
        var car = CarApiMapper.ToCar(Dto("Great daily driver. Very clean, one owner, full service history."));

        Assert.Equal("Nissan 370Z", car.Name);
        Assert.StartsWith("Great daily driver.", car.Description, StringComparison.Ordinal);
    }
}

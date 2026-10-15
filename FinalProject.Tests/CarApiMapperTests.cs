using FinalProject.MVC.Mappings;
using FinalProject.MVC.Models.Api;
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
    public void ToCar_splits_seats_drive_type_and_engine_from_description()
    {
        var car = CarApiMapper.ToCar(Dto("Nissan 370Z. 2 seats, rear-wheel drive, 3.7L V6."));

        Assert.Equal("Nissan 370Z", car.Name);
        Assert.Equal(2, car.Seats);
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
}

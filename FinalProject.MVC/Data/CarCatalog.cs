using FinalProject.MVC.Models;

namespace FinalProject.MVC.Data
{
    public static class CarCatalog
    {
        public static List<Car> TemplateCards()
        {
            return Create();
        }

        public static List<Car> Create()
        {
            return new List<Car>
            {
                Car(1, "Ferrari California", "Ferrari", "California", "Convertible",
                    "~/assets/images/portfolio/02.webp", "New", 2018, "Red", 4, 189000, 14500,
                    "Petrol", "Automatic", 2890, "Rear-Wheel Drive"),
                Car(2, "Mercedes-Benz SL-Class", "Mercedes-Benz", "SL-Class", "Convertible",
                    "~/assets/images/portfolio/05.webp", "Trending", 2019, "Black", 2, 92000, 18200,
                    "Petrol", "Automatic", 1480, "Rear-Wheel Drive"),
                Car(3, "Mercedes-Benz SL-Class", "Mercedes-Benz", "SL-Class", "Convertible",
                    "~/assets/images/about/07.webp", "Stocks", 2016, "White", 2, 78000, 27400,
                    "Petrol", "Automatic", 1260, "Rear-Wheel Drive"),

                Car(4, "Nissan 350Z", "Nissan", "350Z", "Sports Car",
                    "~/assets/images/portfolio/08.webp", "Stocks", 2008, "Black", 2, 18500, 46200,
                    "Petrol", "Manual", 310, "Rear-Wheel Drive"),
                Car(5, "Lamborghini Diablo", "Lamborghini", "Diablo", "Sports Car",
                    "~/assets/images/portfolio/09.webp", "Trending", 1998, "Red", 2, 265000, 16800,
                    "Petrol", "Manual", 3990, "Rear-Wheel Drive"),
                Car(6, "Porsche 911 GT3 RS", "Porsche", "911 GT3 RS", "Sports Car",
                    "~/assets/images/portfolio/24.webp", "New", 2019, "Red", 2, 219000, 6400,
                    "Petrol", "Automatic", 3350, "Rear-Wheel Drive"),
                Car(7, "Porsche 911", "Porsche", "911", "Sports Car",
                    "~/assets/images/banner/banner-slider-bg-09.webp", "Trending", 2011, "Gray", 2, 128000, 24100,
                    "Petrol", "Manual", 1980, "Rear-Wheel Drive"),
                Car(8, "Mercedes-AMG GT", "Mercedes-Benz", "AMG GT", "Sports Car",
                    "~/assets/images/portfolio/34.webp", "New", 2018, "Black", 2, 118000, 15300,
                    "Petrol", "Automatic", 1850, "Rear-Wheel Drive"),
                Car(9, "Maserati GranTurismo", "Maserati", "GranTurismo", "Sports Car",
                    "~/assets/images/banner/banner-slider-bg-08.webp", "Stocks", 2017, "White", 4, 74000, 22800,
                    "Petrol", "Automatic", 1190, "Rear-Wheel Drive"),

                Car(10, "Nissan Leaf", "Nissan", "Leaf", "Hatchback",
                    "~/assets/images/about/02.webp", "Stocks", 2021, "Red", 5, 21900, 18600,
                    "Electric", "Automatic", 360, "Front-Wheel Drive"),

                Car(11, "Rolls-Royce Phantom", "Rolls-Royce", "Phantom", "Luxury Cars",
                    "~/assets/images/portfolio/06.webp", "New", 2022, "Black", 5, 455000, 3200,
                    "Petrol", "Automatic", 6900, "Rear-Wheel Drive"),
                Car(12, "Rolls-Royce Wraith", "Rolls-Royce", "Wraith", "Luxury Cars",
                    "~/assets/images/about/06.webp", "New", 2020, "White", 4, 318000, 8100,
                    "Petrol", "Automatic", 4850, "Rear-Wheel Drive"),

                Car(13, "Tesla Model 3", "Tesla", "Model 3", "Sedan",
                    "~/assets/images/portfolio/29.webp", "New", 2023, "Red", 5, 42900, 11200,
                    "Electric", "Automatic", 690, "Rear-Wheel Drive"),

                Car(14, "Toyota Highlander", "Toyota", "Highlander", "SUV",
                    "~/assets/images/blog/11.webp", "New", 2024, "Red", 7, 46500, 4800,
                    "Petrol", "Automatic", 760, "All-Wheel Drive")
            };
        }

        public static int CountInCategory(string categoryName)
        {
            var category = CarCategories.Resolve(categoryName);
            if (category == null)
            {
                return 0;
            }

            return Create().Count(car => car.Category.Equals(category.Value, StringComparison.OrdinalIgnoreCase));
        }

        public static string CountLabel(string categoryName)
        {
            var count = CountInCategory(categoryName);
            return count == 1 ? "1 Car" : $"{count} Cars";
        }

        private static Car Car(
            int id,
            string name,
            string make,
            string model,
            string category,
            string imagePath,
            string badge,
            int year,
            string color,
            int seats,
            decimal price,
            int mileage,
            string fuel,
            string transmission,
            decimal monthlyPayment,
            string driveType)
        {
            return new Car
            {
                Id = id,
                Name = name,
                Make = make,
                Model = model,
                Category = category,
                ImagePath = imagePath,
                Badge = badge,
                ImageCount = 1,
                Year = year,
                Color = color,
                Seats = seats,
                Price = price,
                Mileage = mileage,
                Fuel = fuel,
                Transmission = transmission,
                MonthlyPayment = monthlyPayment,
                DriveType = driveType,
                Description = name,
                InStock = true
            };
        }
    }
}

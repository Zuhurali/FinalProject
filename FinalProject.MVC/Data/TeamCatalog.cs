using FinalProject.MVC.Models;

namespace FinalProject.MVC.Data
{
    public static class TeamCatalog
    {
        public static IReadOnlyList<TeamMember> Members { get; } = new List<TeamMember>
        {
            new()
            {
                Id = 1,
                Name = "Amelia Scot",
                Position = "Office Executive",
                ImagePath = "~/assets/images/team/05.webp",
                Biography = "Amelia welcomes buyers at the front desk and keeps showroom appointments on schedule. She coordinates test-drive bookings and makes sure every guest leaves with a clear next step.",
                Experience = "6 years in dealership guest services",
                Department = "Client Relations",
                Phone = "+994503546767",
                Email = "amelia.scot@autovault.com"
            },
            new()
            {
                Id = 2,
                Name = "Michael Roy",
                Position = "Manager",
                ImagePath = "~/assets/images/team/06.webp",
                Biography = "Michael leads the new-vehicle sales floor. He matches drivers with the right model, reviews offers with the finance desk, and coaches the sales team on transparent pricing.",
                Experience = "12 years in automotive sales",
                Department = "New Vehicle Sales",
                Phone = "+994503546767",
                Email = "michael.roy@autovault.com"
            },
            new()
            {
                Id = 3,
                Name = "Emma Elizabeth",
                Position = "Office Executive",
                ImagePath = "~/assets/images/team/07.webp",
                Biography = "Emma prepares purchase paperwork, trade-in documents, and payment schedules. She walks customers through each form so the handover stays clear and complete.",
                Experience = "4 years in dealership finance administration",
                Department = "Finance Office",
                Phone = "+994503546767",
                Email = "emma.elizabeth@autovault.com"
            },
            new()
            {
                Id = 4,
                Name = "Michael Roy",
                Position = "Manager",
                ImagePath = "~/assets/images/team/08.webp",
                Biography = "This Michael runs the service workshop. He sets repair priorities, reviews technician notes, and keeps maintenance visits moving without surprise delays.",
                Experience = "9 years managing service operations",
                Department = "Service Department",
                Phone = "+994503546767",
                Email = "michael.service@autovault.com"
            },
            new()
            {
                Id = 5,
                Name = "Amelia Scot",
                Position = "Office Executive",
                ImagePath = "~/assets/images/team/01.webp",
                Biography = "Amelia tracks incoming stock, vehicle locations, and delivery dates. She keeps the inventory list accurate so sales staff can promise only cars that are actually on the lot.",
                Experience = "8 years coordinating vehicle inventory",
                Department = "Inventory Coordination",
                Phone = "+994503546767",
                Email = "amelia.inventory@autovault.com"
            },
            new()
            {
                Id = 6,
                Name = "Michael Roy",
                Position = "Manager",
                ImagePath = "~/assets/images/team/02.webp",
                Biography = "Michael manages fleet and business accounts. He builds multi-vehicle quotes for companies and follows each order from selection through registration.",
                Experience = "15 years in fleet and commercial sales",
                Department = "Fleet Sales",
                Phone = "+994503546767",
                Email = "michael.fleet@autovault.com"
            },
            new()
            {
                Id = 7,
                Name = "Emma Elizabeth",
                Position = "Office Executive",
                ImagePath = "~/assets/images/team/03.webp",
                Biography = "Emma handles follow-up calls after delivery and service visits. She logs feedback, arranges courtesy cars, and makes sure open questions reach the right advisor.",
                Experience = "5 years in customer care",
                Department = "Customer Care",
                Phone = "+994503546767",
                Email = "emma.care@autovault.com"
            },
            new()
            {
                Id = 8,
                Name = "Michael Roy",
                Position = "Manager",
                ImagePath = "~/assets/images/team/04.webp",
                Biography = "Michael oversees warranties, parts, and aftersales packages. He helps owners choose maintenance plans that fit how they actually drive.",
                Experience = "11 years in aftersales management",
                Department = "Aftersales",
                Phone = "+994503546767",
                Email = "michael.aftersales@autovault.com"
            }
        };

        public static TeamMember? Find(int id)
        {
            return Members.FirstOrDefault(member => member.Id == id);
        }
    }
}

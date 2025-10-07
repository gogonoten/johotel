using System;
using System.Linq;

namespace DomainModels
{
    

    public static class Pricing
    {
        public static decimal? PriceForStay(RoomType? type, DateTimeOffset checkIn, DateTimeOffset checkOut)
        {
            if (type is null) return null;

            var nights = (checkOut.Date - checkIn.Date).Days;
            if (nights <= 0) return null;

            var basePrice = RoomPricing.GetPrice(type.Value);

            

            var weekendNights = Enumerable.Range(0, nights)
                .Select(i => checkIn.Date.AddDays(i))
                .Count(d => d.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday);

            var weekendBoost = weekendNights * (0.15m * basePrice);

            return nights * basePrice + weekendBoost;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainModels
{
    public static class RoomPricing
    {
        public static decimal GetPrice(RoomType type) => type switch
        {
            RoomType.Standard => 1000m,
            RoomType.Family => 1500m,
            RoomType.Suite => 2500m,
            _ => 1000m
        };

        public static void UpdatePrice(RoomType type, decimal newPrice)
        {
            _ = type; _ = newPrice;
        }
    }
}

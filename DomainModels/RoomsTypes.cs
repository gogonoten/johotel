using System.Globalization;

namespace DomainModels
{
    public static class RoomHelpers
    {
        public static RoomType ParseRoomType(string? s) =>
            (s ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "standard" => RoomType.Standard,
                "family" => RoomType.Family,
                "suite" => RoomType.Suite,
                _ => RoomType.Standard
            };

        public static string DisplayName(this RoomType type) => type switch
        {
            RoomType.Standard => "Standardværelse",
            RoomType.Family => "Familieværelse",
            RoomType.Suite => "Suite",
            _ => type.ToString()
        };

        public static decimal BasePrice(RoomType type) => RoomPricing.GetPrice(type);

        public static string ImageFor(RoomType type) => type switch
        {
            RoomType.Standard => "normalroom.png",
            RoomType.Family => "mediumroom.png",
            RoomType.Suite => "suite.png",
            _ => "normalroom.png"
        };

        public static string AsCurrency(this decimal amount, string culture = "da-DK") =>
            amount.ToString("C0", CultureInfo.GetCultureInfo(culture));
    }
}

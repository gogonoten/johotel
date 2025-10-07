namespace Blazor.Helpers
{
    public static class UiExtensions
    {
        public static string LocalDateString(this DateTimeOffset d) =>
            d.LocalDateTime.ToString("dd MMM yyyy");
    }
}

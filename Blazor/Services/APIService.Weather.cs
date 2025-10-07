//using System.Net.Http.Json;
//using DomainModels;

//namespace Blazor.Services;

//public partial class APIService
//{
//    public async Task<WeatherForecast[]> GetWeatherAsync(
//        int maxItems = 10,
//        CancellationToken cancellationToken = default
//    )
//    {
//        List<WeatherForecast>? forecasts = null;

//        await foreach (
//            var forecast in httpClient.GetFromJsonAsAsyncEnumerable<WeatherForecast>(
//                "/api/weatherforecast",
//                cancellationToken
//            )
//        )
//        {
//            if (forecasts?.Count >= maxItems)
//            {
//                break;
//            }
//            if (forecast is not null)
//            {
//                forecasts ??= [];
//                forecasts.Add(forecast);
//            }
//        }

//        return forecasts?.ToArray() ?? [];
//    }
//}

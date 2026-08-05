using System.Net.Http.Json;

namespace Vnta.Hrm.Web.Client.Services.DataProviders {
    public abstract class DataProvider {
        readonly HttpClient _httpClient;

        private protected DataProvider(HttpClient httpClient) {
            _httpClient = httpClient;
        }

        protected abstract string GetBasePath();

        protected async Task<T?> LoadDataAsync<T>(string[]? pathItems = null, CancellationToken cancellationToken = default) {
            var resultPath = GetBasePath();
            if(pathItems != null) {
                foreach(var pathItem in pathItems)
                    resultPath += $"/{pathItem.ToString()}";
            }

            try {
                return await _httpClient.GetFromJsonAsync<T>(resultPath, cancellationToken);
            }
            catch(OperationCanceledException) {
                return default;
            }
            catch(HttpRequestException) {
                return default;
            }
        }
    }
}


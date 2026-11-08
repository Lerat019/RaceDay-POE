namespace RaceDayMVC.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            
            _httpClient.BaseAddress = new Uri(configuration["ApiSettings:BaseUrl"]);
        }

        public async Task<HttpResponseMessage> GetAsync(string endpoint)
        {
            return await _httpClient.GetAsync(endpoint);
        }

        public async Task<HttpResponseMessage> PostAsync(string endpoint, HttpContent content)
        {
            return await _httpClient.PostAsync(endpoint, content);
        }

        public async Task<HttpResponseMessage> PutAsync(string endpoint, HttpContent content)
        {
            return await _httpClient.PutAsync(endpoint, content);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string endpoint)
        {
            return await _httpClient.DeleteAsync(endpoint);
        }
    }
}
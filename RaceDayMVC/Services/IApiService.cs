namespace RaceDayMVC.Services
{
    public interface IApiService
    {
        Task<HttpResponseMessage> GetAsync(string endpoint);
        Task<HttpResponseMessage> PostAsync(string endpoint, HttpContent content);
        Task<HttpResponseMessage> PutAsync(string endpoint, HttpContent content);
        Task<HttpResponseMessage> DeleteAsync(string endpoint);
    }
}
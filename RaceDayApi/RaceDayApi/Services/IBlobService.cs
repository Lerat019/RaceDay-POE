namespace RaceDayApi.Services
{
    public interface IBlobService
    {
        Task<string> UploadFileAsync(IFormFile file);
    }
}
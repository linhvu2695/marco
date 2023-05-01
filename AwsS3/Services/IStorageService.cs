using AwsS3.Models;

namespace AwsS3.Services
{
    public interface IStorageService
    {
        Task<S3ResponseDto> UploadFileAsync(S3Object obj, AwsCredentials credentials);
    }
}
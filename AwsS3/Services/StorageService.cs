using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;
using AwsS3.Models;

namespace AwsS3.Services
{
    public class StorageService : IStorageService
    {
        public async Task<S3ResponseDto> UploadFileAsync(S3Object obj, AwsCredentials awsCredentials)
        {
            var credentials = new BasicAWSCredentials(awsCredentials.AwsKey, awsCredentials.AwsSecretKey);

            var config = new AmazonS3Config()
            {
                RegionEndpoint = Amazon.RegionEndpoint.APSoutheast1
            };

            var response = new S3ResponseDto();

            try
            {
                var uploadRequest = new TransferUtilityUploadRequest()
                {
                    InputStream = obj.InputStream,
                    Key = obj.Name,
                    BucketName = obj.BucketName,
                    CannedACL = S3CannedACL.NoACL
                };
                
                using (var client = new AmazonS3Client(credentials, config))
                {
                    // Upload to S3
                    var transferUtility = new TransferUtility(client);
                    await transferUtility.UploadAsync(uploadRequest);

                    response.StatusCode = 200;
                    response.Message = $"{obj.Name} has been uploaded successfully";
                }
            }
            catch (AmazonS3Exception ex)
            {
                response.StatusCode = (int)ex.StatusCode;
                response.Message = ex.Message;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.Message = ex.Message;
            }

            return response;
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Amazon.S3;
using Amazon.S3.Model;

namespace WebApplication5.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly ILogger<FileController> _logger;

        public FileController(IAmazonS3 s3Client, IConfiguration config, ILogger<FileController> logger)
        {
            _s3Client = s3Client;
            _bucketName = Environment.GetEnvironmentVariable("S3_BUCKET_NAME") 
                          ?? config["S3:BucketName"] 
                          ?? "vet-clinic-b1gfvqa88jrcvav48j25";
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "File is empty" });

            try
            {
                var key = $"uploads/{DateTime.UtcNow:yyyyMMdd}/{Guid.NewGuid()}_{file.FileName}";
                
                var request = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                    InputStream = file.OpenReadStream(),
                    ContentType = file.ContentType
                };

                var response = await _s3Client.PutObjectAsync(request);
                
                _logger.LogInformation($"File uploaded: {key}");

                return Ok(new 
                { 
                    success = true,
                    fileName = file.FileName,
                    key = key,
                    url = $"https://storage.yandexcloud.net/{_bucketName}/{key}",
                    size = file.Length
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "S3 upload failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListFiles()
        {
            try
            {
                var request = new ListObjectsV2Request
                {
                    BucketName = _bucketName,
                    MaxKeys = 50
                };

                var response = await _s3Client.ListObjectsV2Async(request);

                var files = response.S3Objects.Select(obj => new
                {
                    key = obj.Key,
                    size = obj.Size,
                    lastModified = obj.LastModified,
                    url = $"https://storage.yandexcloud.net/{_bucketName}/{obj.Key}"
                });

                return Ok(new
                {
                    bucket = _bucketName,
                    count = files.Count(),
                    files = files
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "S3 list failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("s3-health")]
        public async Task<IActionResult> S3Health()
        {
            try
            {
                var request = new ListBucketsRequest();
                await _s3Client.ListBucketsAsync();
                return Ok(new { s3Status = "Connected", bucket = _bucketName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { s3Status = "Failed", error = ex.Message });
            }
        }
    }
}

using CommunityPlatform.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;

namespace CommunityPlatform.Infrastructure.Storage;

public class MinioMediaObjectStore : IMediaObjectStore
{
    private readonly IMinioClient _client;
    private readonly string _bucket;

    public MinioMediaObjectStore(IMinioClient client, IConfiguration config)
    {
        _client = client;
        _bucket = config["Minio:Bucket"] ?? "media";
    }

    public async Task EnsureBucketExistsAsync(CancellationToken ct = default)
    {
        var exists = await _client.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_bucket), ct);

        if (!exists)
        {
            await _client.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_bucket), ct);
        }
    }

    public async Task<long> PutAsync(string objectKey, Stream content, string contentType, CancellationToken ct = default)
    {
        var size = content.Length;

        await _client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucket)
            .WithObject(objectKey)
            .WithStreamData(content)
            .WithObjectSize(size)
            .WithContentType(contentType), ct);

        return size;
    }

    public async Task<Stream> GetAsync(string objectKey, CancellationToken ct = default)
    {
        var buffer = new MemoryStream();

        await _client.GetObjectAsync(new GetObjectArgs()
            .WithBucket(_bucket)
            .WithObject(objectKey)
            .WithCallbackStream(async (stream, innerCt) => await stream.CopyToAsync(buffer, innerCt)), ct);

        buffer.Position = 0;
        return buffer;
    }

    public Task DeleteAsync(string objectKey, CancellationToken ct = default) =>
        _client.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(_bucket)
            .WithObject(objectKey), ct);
}
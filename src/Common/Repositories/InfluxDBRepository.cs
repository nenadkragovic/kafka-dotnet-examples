using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using Microsoft.Extensions.Configuration;

namespace Common.Repositories
{
    public class InfluxDBRepository
    {
        private readonly string _token;
        private const string URL = "http://localhost:8086";

        public InfluxDBRepository(IConfiguration configuration)
        {
            _token = "VTN-eYiFp6h80O75SZlsEiqGfHgxNIxQWHR60QWOtBlcD5d3y2kf2SPh-gMlZy1dqmtrsiKJh_zrEwOTu3S5ag==";
        }

        public void Write(Action<WriteApi> action)
        {
            using var client = InfluxDBClientFactory.Create(URL, _token);
            using var write = client.GetWriteApi();
            action(write);
        }

        public async Task<T> QueryAsync<T>(Func<QueryApi, Task<T>> action)
        {
            using var client = InfluxDBClientFactory.Create(URL, _token);
            var query = client.GetQueryApi();
            return await action(query);
        }

        public QueryApi GetQueryApi()
        {
            var client = InfluxDBClientFactory.Create(URL, _token);
            return client.GetQueryApi();
        }

        public async void CreateOrganizationAndBucket(string organization, string bucketName)
        {
            using var client = InfluxDBClientFactory.Create(URL, _token);

            Organization org = null;

            try
            {
                org = (await client.GetOrganizationsApi().FindOrganizationsAsync(org: organization)).First(o =>
                    o.Name == organization);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            org ??= await client.GetOrganizationsApi().CreateOrganizationAsync(new Organization(null, organization, string.Empty, Organization.StatusEnum.Active)
            {
                Name = organization
            });

            var retention = new BucketRetentionRules(BucketRetentionRules.TypeEnum.Expire, 3600);

            Bucket bucket = null;

            try
            {
                bucket = await client.GetBucketsApi().FindBucketByNameAsync(bucketName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            if (bucket == null)
            {
                await client.GetBucketsApi().CreateBucketAsync(bucketName, retention, org.Id);

                //
                // Create access token to "iot_bucket"
                //
                var resource = new PermissionResource(PermissionResource.TypeBuckets, bucket.Id, null,
                    org.Id);

                // Read permission
                var read = new Permission(Permission.ActionEnum.Read, resource);

                // Write permission
                var write = new Permission(Permission.ActionEnum.Write, resource);

                var authorization = await client.GetAuthorizationsApi()
                    .CreateAuthorizationAsync(org.Id, new List<Permission> { read, write });
            }
        }
    }
}

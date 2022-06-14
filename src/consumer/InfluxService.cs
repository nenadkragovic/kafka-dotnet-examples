﻿using InfluxDB.Client;

namespace Consumer
{
    public class InfluxDBService
    {
        private readonly string _token;

        public InfluxDBService(IConfiguration configuration)
        {
            _token = "GA80ox_YjDW1zrjg6UgvRfo2BQzg5zLcVBQvtH_JBqahHOZFBVvM8ufb4B6qon39fERM7qJ_8lpx8m33DNDMoQgf";
        }

        public void Write(Action<WriteApi> action)
        {
            using var client = InfluxDBClientFactory.Create("http://localhost:8086", _token);
            using var write = client.GetWriteApi();
            action(write);
        }

        public async Task<T> QueryAsync<T>(Func<QueryApi, Task<T>> action)
        {
            using var client = InfluxDBClientFactory.Create("http://localhost:8086", _token);
            var query = client.GetQueryApi();
            return await action(query);
        }
    }
}

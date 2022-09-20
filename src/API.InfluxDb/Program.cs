using Common.Models;
using Common.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.Configure<InfluxDbConfig>(builder.Configuration.GetSection("InfluxDbConfig"));
builder.Services.Configure<KafkaConfig>(builder.Configuration.GetSection("KafkaConfig"));
builder.Services.AddScoped<InfluxDBRepository>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

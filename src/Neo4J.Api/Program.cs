using Neo4j.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddLogging(config =>
{
  config.AddConsole();
});

builder.Services.AddControllers();

builder.Services.AddScoped<IDriver>(provider =>
{
  var url = Environment.GetEnvironmentVariable("NEO4J_URL", EnvironmentVariableTarget.Process) ?? string.Empty;
  var driver = GraphDatabase.Driver(url);

  var verifyTask = driver.VerifyConnectivityAsync();
  verifyTask.Wait();

  return driver;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

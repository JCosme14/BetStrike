using BettingAPI.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Allow local dev UI to call APIs from different origin
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});
builder.Services.AddSingleton<DatabaseHelper>();
builder.Services.AddSingleton<PagamentosDatabaseHelper>();
builder.Services.AddHostedService<BettingAPI.Services.GameSyncService>();

var app = builder.Build();

// Swagger enabled in all environments so it works inside Docker containers too
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
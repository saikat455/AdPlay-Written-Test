using AdPlay.Api.Data;
using Microsoft.EntityFrameworkCore;
using AdPlay.Api.Services;
 
var builder = WebApplication.CreateBuilder(args);
 
var connString = builder.Configuration.GetConnectionString("Default")!;
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseMySql(connString, ServerVersion.AutoDetect(connString)));
 
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddSingleton<IPaymentProvider, FakePaymentProvider>();
builder.Services.AddScoped<QueryService>();

var app = builder.Build();
 
if (app.Environment.IsDevelopment())
    app.MapOpenApi();
 
app.UseHttpsRedirection();
app.MapControllers();
 
app.Run();
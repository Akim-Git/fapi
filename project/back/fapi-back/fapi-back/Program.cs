var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ICatalogService, CatalogService>();
builder.Services.AddSingleton<ICustomerStore, InMemoryCustomerStore>();
builder.Services.AddSingleton<IOrderStore, InMemoryOrderStore>();
builder.Services.AddHttpClient<IRateService, CnbRateService>(c => c.Timeout = TimeSpan.FromSeconds(8));

// CORS - povolit všem (pro dev/pohovorový úkol OK)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod()
    );
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS musí být pøed MapControllers
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }

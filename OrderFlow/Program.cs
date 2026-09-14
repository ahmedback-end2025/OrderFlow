
using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Infrastructure.Data;

namespace OrderFlow
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<OrderFlowDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddScoped<IApplicationDbContext>(provider =>
    provider.GetRequiredService<OrderFlowDbContext>());


            builder.Services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(typeof(OrderFlow.Application.Common.Interfaces.IApplicationDbContext).Assembly));

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}

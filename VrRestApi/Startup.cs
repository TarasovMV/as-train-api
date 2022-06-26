using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VrRestApi.Models.Context;
using VrRestApi.Services;

namespace VrRestApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            using (var db = new VrRestApiContext())
            {
                db.Database.EnsureCreated();
                db.Database.Migrate();
            }
            using (var db = new AdditionalContext())
            {
                db.Database.EnsureCreated();
                db.Database.Migrate();
            }
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddEntityFrameworkSqlite()
                .AddDbContext<VrRestApiContext>()
                .AddDbContext<AdditionalContext>();

            services.AddSignalR();

            services.AddControllers();

            services.AddTransient<ReportService>();
            services.AddTransient<TestingService>();
            services.AddTransient<AdditionalService>();
            services.AddSingleton<SocketHandler>();

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder => builder
                .AllowAnyHeader()
                //.SetIsOriginAllowed(s => true)
                .AllowAnyOrigin()
                //.AllowCredentials()
                .AllowAnyMethod());
            });

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors("AllowAll");

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<SocketHub>("/signalr");
            });
        }
    }
}

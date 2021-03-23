using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace ComputationModule
{
    public class Startup
    {
        public Startup()
        {
            var pinsConfigurationPath = Environment.GetEnvironmentVariable("SYS_PIN_CONFIG_FILE_PATH");
            var pinsArray = File.ReadAllText(pinsConfigurationPath);
            var pins = "{ \"Pins\":" + pinsArray + "}";
            var bytes = Encoding.ASCII.GetBytes(pins);
            var pinStream = new MemoryStream(bytes);
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonStream(pinStream);

            Configuration = configurationBuilder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddControllers().AddNewtonsoftJson(opt => opt.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}

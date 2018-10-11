using System;
using restAPI.DataContext.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

//Bret - Added to support angular client app
using Microsoft.AspNetCore.SpaServices.AngularCli;

namespace restAPI
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
      services.AddDbContext<DevicePositionContext>(options => options.UseSqlite($"Data Source={System.IO.Path.Combine(AppContext.BaseDirectory, "data.db")}"));

      //Bret - Added to support the MapApp angular app
      services.AddSpaStaticFiles(configuration =>
      {
        configuration.RootPath = "MapApp/dist";
      });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }
      else
      {
        app.UseHsts();
      }

      //BSS - modified to support angular client app
      app.UseHttpsRedirection();
      app.UseStaticFiles();
      app.UseSpaStaticFiles();
      app.UseHttpsRedirection();
      app.UseMvc();
      app.UseSpa(spa =>
      {
        spa.Options.SourcePath = "MapApp";
        if (env.IsDevelopment())
        {
          spa.UseAngularCliServer(npmScript: "start");
        }
      });

    }
  }
}

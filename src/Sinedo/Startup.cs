using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Sinedo.Components;
using Sinedo.Components.Logging;
using Sinedo.Components.Sharehoster;
using Sinedo.Background;
using Sinedo.Middleware;
using Sinedo.Singleton;
using System.Net;
using Microsoft.Extensions.Logging;

namespace Sinedo
{
    public class Startup
    {
        public Startup()
        {
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<WebSocketRouter>()
                    .AddSingleton<WebSocketConnections>()
                    .AddSingleton<WebSocketBroadcaster>()
                    .AddSingleton<DownloadRepository>()
                    .AddSingleton<DownloadScheduler>()
                    .AddSingleton<DiskSpaceHelper>()
                    .AddSingleton<Configuration>()
                    .AddSingleton<HyperlinkManager>()
                    .AddSingleton<Configuration>(Configuration.Current)
                    .AddSingleton<WebViewLoggerProvider>(e => WebViewLoggerProvider.Default);
                    
            services.AddHostedService<StorageService>()
                    .AddHostedService<AutoDiscovery>();
      
            AddLocalizationSupport(services);

            services.AddHealthChecks()
                    .AddCheck<HealthCheck>("application");
                    
            services.AddControllersWithViews()
                    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);

            AddCookieAuthenticationSupport(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else {
                app.UseExceptionHandler("/Error");
            }

            WebSocketOptions webSocketOptions = new()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120)
            };

            CookiePolicyOptions cookiePolicyOptions = new()
            {
                MinimumSameSitePolicy = SameSiteMode.Strict,
            };

            // app.UseHttpsRedirection();
            app.UseWebSockets(webSocketOptions);
            app.UseCookiePolicy(cookiePolicyOptions);

            app.UseStatusCodePages();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseRequestLocalization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health");
                endpoints.MapDefaultControllerRoute();
            });
            app.UseMiddleware<WebSocketRouting>();
        }

        private static void AddCookieAuthenticationSupport(IServiceCollection services) {
            services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(o => 
                {
                    o.Cookie.Name = "Sinedo";
                    o.LoginPath = "/login";
                });

            services.ConfigureApplicationCookie(options =>
            {
                // Cookie settings
                options.Cookie.Name = "Sinedo";
                options.ExpireTimeSpan = TimeSpan.FromDays(10);
                options.SlidingExpiration = true;
            });
        }

        private static void AddLocalizationSupport(IServiceCollection services) {
            services.Configure<RequestLocalizationOptions>(options => {
                List<CultureInfo> supportedCultures = new()
                {
                    new CultureInfo("de-DE"),
                    new CultureInfo("en-US")
                };  
                options.DefaultRequestCulture = new RequestCulture("en-US");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });
        }
    }
}

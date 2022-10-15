using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sinedo.Background;
using Sinedo.Components.Logging;
using Sinedo.Middleware;
using Sinedo.Singleton;

namespace Sinedo
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<WebSocketRouter>()
                    .AddSingleton<WebSocketConnections>()
                    .AddSingleton<BroadcastQueue>()
                    .AddSingleton<WebSocketPing>()
                    .AddSingleton<DownloadRepository>()
                    .AddSingleton<DownloadScheduler>()
                    .AddSingleton<SetupBuilder>()
                    .AddSingleton<HyperlinkManager>()
                    .AddSingleton<ServerControl>()
                    .AddSingleton<Singleton.IConfiguration>(Configuration.Current)
                    .AddSingleton<WebViewLoggerProvider>(WebViewLoggerProvider.Default);

            services.AddHostedService<StorageService>()
                    .AddHostedService<AutoDiscovery>()
                    .AddHostedService<Broadcaster>()
                    .AddHostedService<Downloader>();

            AddLocalizationSupport(services);

            services.AddHealthChecks()
                    .AddCheck<ServerHealthCheck>("application");

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
            else
            {
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

        private static void AddCookieAuthenticationSupport(IServiceCollection services)
        {
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

        private static void AddLocalizationSupport(IServiceCollection services)
        {
            services.Configure<RequestLocalizationOptions>(options =>
            {
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

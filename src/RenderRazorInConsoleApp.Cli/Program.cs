﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.PlatformAbstractions;
using RenderRazorInConsoleApp.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using static System.IO.Directory;
using static System.Environment;

namespace RenderRazorInConsoleApp.Cli
{
    public class Program
    {
        static void Main(string[] args)
        {            
#if DEBUG
            var serviceScopeFactory = InitializeServices($"{GetParent(CurrentDirectory)}\\RenderRazorInConsoleApp.Core");
#else
            var serviceScopeFactory = InitializeServices(); 
#endif

            var content = RenderViewAsync(serviceScopeFactory).Result;

            Console.WriteLine(content);
            Console.ReadLine();
        }

        public static IServiceScopeFactory InitializeServices(string customApplicationBasePath = null)
        {            
            var services = new ServiceCollection();
            ConfigureDefaultServices(services, customApplicationBasePath);
           
            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider.GetRequiredService<IServiceScopeFactory>();
        }

        public static Task<string> RenderViewAsync(IServiceScopeFactory scopeFactory)
        {
            using (var serviceScope = scopeFactory.CreateScope())
            {
                var helper = serviceScope.ServiceProvider.GetRequiredService<RazorViewToStringRenderer>();

                var model = new RenderRazorInConsoleApp.Core.Model
                {
                    ClassName = "Foo"
                };

                return helper.RenderViewToStringAsync(@"~/View.cshtml", model);
            }
        }

        private static void ConfigureDefaultServices(IServiceCollection services, string customApplicationBasePath)
        {
            string applicationName;
            IFileProvider fileProvider;
            if (!string.IsNullOrEmpty(customApplicationBasePath))
            {
                applicationName = Path.GetFileName(customApplicationBasePath);
                fileProvider = new PhysicalFileProvider(customApplicationBasePath);
            }
            else
            {
                applicationName = Assembly.GetEntryAssembly().GetName().Name;
                fileProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory());
            }

            services.AddSingleton<IHostingEnvironment>(new HostingEnvironment
            {
                ApplicationName = applicationName,
                WebRootFileProvider = fileProvider,
            });
            services.Configure<RazorViewEngineOptions>(options =>
            {
                options.FileProviders.Clear();
                options.FileProviders.Add(fileProvider);
            });
            var diagnosticSource = new DiagnosticListener("Microsoft.AspNetCore");
            services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
            services.AddSingleton<DiagnosticSource>(diagnosticSource);
            services.AddLogging();
            services.AddMvc();
            services.AddTransient<RazorViewToStringRenderer>();
        }
    }
}
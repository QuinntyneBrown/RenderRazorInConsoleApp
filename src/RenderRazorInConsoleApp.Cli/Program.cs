﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.PlatformAbstractions;
using RenderRazorInConsoleApp.Core;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace RenderRazorInConsoleApp.Cli
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var renderer = GetRenderer();
            var html = await renderer.RenderViewToStringAsync("/View.cshtml", new Model());
            System.Console.Write(html);
            System.Console.ReadKey();
        }

        private static RazorViewToStringRenderer GetRenderer()
        {
            var services = new ServiceCollection();
            var applicationEnvironment = PlatformServices.Default.Application;
            services.AddSingleton(applicationEnvironment);

            var appDirectory = Directory.GetCurrentDirectory();

            var environment = new HostingEnvironment
            {
                ApplicationName = Assembly.GetEntryAssembly().GetName().Name
            };
            services.AddSingleton<IHostingEnvironment>(environment);

            services.Configure<RazorViewEngineOptions>(options =>
            {
                options.FileProviders.Clear();
                options.FileProviders.Add(new PhysicalFileProvider(appDirectory));
            });

            services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();

            var diagnosticSource = new DiagnosticListener("Microsoft.AspNetCore");
            services.AddSingleton<DiagnosticSource>(diagnosticSource);

            services.AddLogging();
            services.AddMvc();
            services.AddSingleton<RazorViewToStringRenderer>();
            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<RazorViewToStringRenderer>();
        }
    }
}
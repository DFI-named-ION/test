using FMVideoManagerApp.Data;
using FMVideoManagerApp.Data.Repositories;
using FMVideoManagerApp.Data.Repositories.FileRepository;
using FMVideoManagerApp.Data.Repositories.UserPathRepository;
using FMVideoManagerApp.Services;
using FMVideoManagerApp.Services.Interfaces;
using FMVideoManagerApp.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace FMVideoManagerApp
{
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider = null!;
        private ApplicationService _applicationService = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ServiceCollection services = new ServiceCollection();

            services.AddDbContextFactory<LocalDbContext>(options =>
            {
                options.UseSqlite(LocalDatabase.GetConnectionString());
            });

            // repos
            services.AddSingleton<IUserRepository, UserRepository>();
            services.AddSingleton<IFileRepository, FileRepository>();
            services.AddSingleton<IUserPathRepository, UserPathRepository>();

            // services
            services.AddSingleton<MessageService>();
            services.AddSingleton<AuthService>();
            services.AddSingleton<FileIndexingService>();

            // viewmodels
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<LogInViewModel>();
            services.AddSingleton<FileListViewModel>();
            services.AddSingleton<SettingsViewModel>();

            // windows
            services.AddSingleton<MainWindow>();

            services.AddSingleton<ApplicationService>();

            _serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = true,
                ValidateOnBuild = true
            });

            LocalDatabase.Initialize(_serviceProvider);

            _applicationService = _serviceProvider.GetRequiredService<ApplicationService>();
            _applicationService.Initialize();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider.Dispose();

            base.OnExit(e);
        }
    }
}
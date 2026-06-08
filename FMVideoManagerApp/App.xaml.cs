using FMVideoManagerApp.Data;
using FMVideoManagerApp.Data.Repositories.LocalFileLocationRepository;
using FMVideoManagerApp.Data.Repositories.LocalIndexedPathRepository;
using FMVideoManagerApp.Services;
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

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ServiceCollection services = new ServiceCollection();

            services.AddDbContextFactory<LocalDbContext>(options =>
            {
                options.UseSqlite(LocalDatabase.GetConnectionString());
            });

            services.AddHttpClient<ApiClient>(client =>
            {
                client.BaseAddress = new Uri("https://localhost:7049/");
                client.Timeout = TimeSpan.FromMinutes(60); // 5
            });

            // repos
            services.AddSingleton<ILocalIndexedPathRepository, LocalIndexedPathRepository>();
            services.AddSingleton<ILocalFileLocationRepository, LocalFileLocationRepository>();

            // services
            services.AddSingleton<MessageService>();
            services.AddSingleton<LocalDeviceService>();
            services.AddSingleton<TokenStore>();
            services.AddSingleton<AuthService>();
            services.AddSingleton<LocalIndexedPathService>();
            services.AddSingleton<FileIndexingService>();
            services.AddSingleton<IndexingManagerService>();
            services.AddSingleton<FileLibraryService>();
            services.AddSingleton<HierarchyService>();
            services.AddSingleton<TagService>();

            // viewmodels
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<LogInViewModel>();
            services.AddSingleton<FileListViewModel>();
            services.AddSingleton<HierarchyRelationsViewModel>();
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
            await _applicationService.InitializeAsync();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider.Dispose();

            base.OnExit(e);
        }
    }
}
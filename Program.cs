using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AdminSERMAC.Core.Configuration;
using AdminSERMAC.Services;
using AdminSERMAC.Repositories;
using AdminSERMAC.Core.Interfaces;
using AdminSERMAC.Services.Database;
using System.Windows.Forms;
using System;
using AdminSERMAC.Forms;

namespace AdminSERMAC
{
    public class ProgramLogger { }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var services = new ServiceCollection();
            var connectionString = "Data Source=AdminSERMAC.db;Version=3;";

            ConfigureServices(services, connectionString);

            var serviceProvider = services.BuildServiceProvider();

            try
            {
                // Servicios existentes
                var clienteService = serviceProvider.GetRequiredService<IClienteService>();
                var mainLogger = serviceProvider.GetRequiredService<ILogger<MainForm>>();
                var sqliteLogger = serviceProvider.GetRequiredService<ILogger<SQLiteService>>();
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

                // Inicializar la base de datos
                var databaseService = serviceProvider.GetRequiredService<DatabaseService>();
                EnsureDatabaseInitialized(databaseService, sqliteLogger);

                Application.Run(new MainForm(
                    clienteService,
                    connectionString,
                    mainLogger,
                    sqliteLogger,
                    loggerFactory));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al iniciar la aplicación: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                var logger = serviceProvider.GetRequiredService<ILogger<ProgramLogger>>();
                logger.LogError(ex, "Error fatal al iniciar la aplicación");
            }
        }

        private static void ConfigureServices(IServiceCollection services, string connectionString)
        {
            // Configurar logging
            services.AddLogging(configure =>
            {
                configure.AddConsole();
                configure.AddDebug();
                configure.SetMinimumLevel(LogLevel.Information);
            });

            // Registrar servicios legacy (mantener durante la migración)
            services.AddSingleton<SQLiteService>();
            services.AddScoped<IClienteRepository>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<ClienteRepository>>();
                return new ClienteRepository(connectionString, logger);
            });
            services.AddScoped<IClienteService>(provider =>
            {
                var repository = provider.GetRequiredService<IClienteRepository>();
                var logger = provider.GetRequiredService<ILogger<ClienteService>>();
                return new ClienteService(repository, logger, connectionString);
            });

            // Registrar nuevos servicios de base de datos
            RegisterDatabaseServices(services, connectionString);

            // Registrar servicios adicionales existentes
            services.AddSingleton<NotificationService>();
            services.AddScoped<FileDataManager>();
            services.AddSingleton<ConfigurationService>();
            services.AddSingleton<DatabaseInitializer>();
        }

        private static void RegisterDatabaseServices(IServiceCollection services, string connectionString)
        {
            // Servicios base de datos
            services.AddSingleton<DatabaseService>();
            services.AddScoped<IClienteDatabaseService, ClienteDatabaseService>();
            services.AddScoped<IInventarioDatabaseService, InventarioDatabaseService>();
            services.AddScoped<IProductoDatabaseService, ProductoDatabaseService>();
            services.AddScoped<IVentaDatabaseService, VentaDatabaseService>();

            // Registrar el connectionString como servicio
            services.AddSingleton(_ => connectionString);
        }

        private static void EnsureDatabaseInitialized(DatabaseService databaseService, ILogger logger)
        {
            try
            {
                if (!databaseService.ValidateConnectionAsync().Result)
                {
                    throw new Exception("No se pudo establecer la conexión con la base de datos");
                }

                logger.LogInformation("Conexión a base de datos establecida correctamente");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al inicializar la base de datos");
                throw;
            }
        }
    }
}
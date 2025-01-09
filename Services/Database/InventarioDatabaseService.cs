using System.Data.SQLite;
using Microsoft.Extensions.Logging;
using AdminSERMAC.Models;

namespace AdminSERMAC.Services.Database
{
    public interface IInventarioDatabaseService
    {
        Task<bool> AddProductoAsync(string codigo, string producto, int unidades, double kilos, string fechaCompra, string fechaRegistro, string fechaVencimiento);
        Task<bool> ActualizarInventarioAsync(string codigo, int unidadesVendidas, double kilosVendidos);
        Task<IEnumerable<string>> GetCategoriasAsync();
        Task<IEnumerable<string>> GetSubCategoriasAsync(string categoria);
        Task<DataTable> GetInventarioAsync();
        Task<DataTable> GetInventarioPorCodigoAsync(string codigo);
        Task<bool> ActualizarFechasInventarioAsync(string codigo, DateTime fechaIngresada);
    }

    public class InventarioDatabaseService : BaseSQLiteService, IInventarioDatabaseService
    {
        private const string TableName = "Inventario";

        public InventarioDatabaseService(ILogger<InventarioDatabaseService> logger, string connectionString)
            : base(logger, connectionString)
        {
            EnsureTableExists();
        }

        private void EnsureTableExists()
        {
            const string createTableSql = @"
                CREATE TABLE IF NOT EXISTS Inventario (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Codigo TEXT NOT NULL,
                    Producto TEXT NOT NULL,
                    Unidades INTEGER NOT NULL,
                    Kilos REAL NOT NULL,
                    FechaMasAntigua TEXT NOT NULL,
                    FechaMasNueva TEXT NOT NULL,
                    FechaVencimiento TEXT,
                    Categoria TEXT,
                    SubCategoria TEXT
                )";

            ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                using var command = new SQLiteCommand(createTableSql, connection, transaction);
                await command.ExecuteNonQueryAsync();
            }).Wait();
        }

        public async Task<bool> AddProductoAsync(string codigo, string producto, int unidades, double kilos,
            string fechaCompra, string fechaRegistro, string fechaVencimiento)
        {
            return await ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                // Verificar si el producto existe
                using (var checkCommand = new SQLiteCommand(
                    "SELECT COUNT(*) FROM Inventario WHERE Codigo = @codigo",
                    connection, transaction))
                {
                    checkCommand.Parameters.AddWithValue("@codigo", codigo);
                    int exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

                    if (exists > 0)
                    {
                        // Actualizar inventario existente
                        using var updateCommand = new SQLiteCommand(@"
                            UPDATE Inventario 
                            SET Unidades = Unidades + @unidades,
                                Kilos = Kilos + @kilos,
                                FechaMasNueva = @fechaRegistro,
                                FechaVencimiento = @fechaVencimiento
                            WHERE Codigo = @codigo", connection, transaction);

                        updateCommand.Parameters.AddWithValue("@codigo", codigo);
                        updateCommand.Parameters.AddWithValue("@unidades", unidades);
                        updateCommand.Parameters.AddWithValue("@kilos", kilos);
                        updateCommand.Parameters.AddWithValue("@fechaRegistro", fechaRegistro);
                        updateCommand.Parameters.AddWithValue("@fechaVencimiento", fechaVencimiento);

                        return await updateCommand.ExecuteNonQueryAsync() > 0;
                    }
                    else
                    {
                        // Insertar nuevo registro
                        using var insertCommand = new SQLiteCommand(@"
                            INSERT INTO Inventario (
                                Codigo, 
                                Producto, 
                                Unidades, 
                                Kilos, 
                                FechaMasAntigua,
                                FechaMasNueva,
                                FechaVencimiento
                            ) VALUES (
                                @codigo,
                                @producto,
                                @unidades,
                                @kilos,
                                @fechaCompra,
                                @fechaRegistro,
                                @fechaVencimiento
                            )", connection, transaction);

                        insertCommand.Parameters.AddWithValue("@codigo", codigo);
                        insertCommand.Parameters.AddWithValue("@producto", producto);
                        insertCommand.Parameters.AddWithValue("@unidades", unidades);
                        insertCommand.Parameters.AddWithValue("@kilos", kilos);
                        insertCommand.Parameters.AddWithValue("@fechaCompra", fechaCompra);
                        insertCommand.Parameters.AddWithValue("@fechaRegistro", fechaRegistro);
                        insertCommand.Parameters.AddWithValue("@fechaVencimiento", fechaVencimiento);

                        return await insertCommand.ExecuteNonQueryAsync() > 0;
                    }
                }
            });
        }

        public async Task<bool> ActualizarInventarioAsync(string codigo, int unidadesVendidas, double kilosVendidos)
        {
            return await ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                using var command = new SQLiteCommand(@"
                    UPDATE Inventario
                    SET 
                        Unidades = Unidades - @unidadesVendidas,
                        Kilos = Kilos - @kilosVendidos
                    WHERE Codigo = @codigo", connection, transaction);

                command.Parameters.AddWithValue("@codigo", codigo);
                command.Parameters.AddWithValue("@unidadesVendidas", unidadesVendidas);
                command.Parameters.AddWithValue("@kilosVendidos", kilosVendidos);

                return await command.ExecuteNonQueryAsync() > 0;
            });
        }

        public async Task<IEnumerable<string>> GetCategoriasAsync()
        {
            return await ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                var categorias = new List<string>();
                using var command = new SQLiteCommand(
                    "SELECT DISTINCT Categoria FROM Inventario WHERE Categoria IS NOT NULL ORDER BY Categoria",
                    connection, transaction);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    categorias.Add(reader["Categoria"].ToString());
                }
                return categorias;
            });
        }

        public async Task<IEnumerable<string>> GetSubCategoriasAsync(string categoria)
        {
            return await ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                var subcategorias = new List<string>();
                using var command = new SQLiteCommand(
                    "SELECT DISTINCT SubCategoria FROM Inventario WHERE Categoria = @categoria AND SubCategoria IS NOT NULL ORDER BY SubCategoria",
                    connection, transaction);

                command.Parameters.AddWithValue("@categoria", categoria);
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    subcategorias.Add(reader["SubCategoria"].ToString());
                }
                return subcategorias;
            });
        }

        public async Task<DataTable> GetInventarioAsync()
        {
            return await ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                var dataTable = new DataTable();
                using var command = new SQLiteCommand("SELECT * FROM Inventario", connection, transaction);
                using var adapter = new SQLiteDataAdapter(command);
                adapter.Fill(dataTable);
                return dataTable;
            });
        }

        public async Task<DataTable> GetInventarioPorCodigoAsync(string codigo)
        {
            return await ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                var dataTable = new DataTable();
                using var command = new SQLiteCommand(
                    "SELECT * FROM Inventario WHERE Codigo = @codigo",
                    connection, transaction);

                command.Parameters.AddWithValue("@codigo", codigo);
                using var adapter = new SQLiteDataAdapter(command);
                adapter.Fill(dataTable);
                return dataTable;
            });
        }

        public async Task<bool> ActualizarFechasInventarioAsync(string codigo, DateTime fechaIngresada)
        {
            return await ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                using var command = new SQLiteCommand(@"
                    UPDATE Inventario
                    SET 
                        FechaMasAntigua = CASE WHEN FechaMasAntigua > @fecha THEN @fecha ELSE FechaMasAntigua END,
                        FechaMasNueva = CASE WHEN FechaMasNueva < @fecha THEN @fecha ELSE FechaMasNueva END
                    WHERE Codigo = @codigo", connection, transaction);

                command.Parameters.AddWithValue("@codigo", codigo);
                command.Parameters.AddWithValue("@fecha", fechaIngresada.ToString("yyyy-MM-dd"));

                return await command.ExecuteNonQueryAsync() > 0;
            });
        }
    }
}

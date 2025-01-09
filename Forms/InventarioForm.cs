using System;
using System.Data.SQLite;
using System.Windows.Forms;
using AdminSERMAC.Models;
using AdminSERMAC.Services;
using Microsoft.Extensions.Logging;


namespace AdminSERMAC.Forms
{
    public class InventarioForm : Form
    {
        private Label numeroCompraLabel;
        private TextBox numeroCompraTextBox;
        private Label fechaCompraLabel;
        private DateTimePicker fechaCompraPicker;
        private Label proveedorLabel;
        private ComboBox proveedorComboBox;
        private Label vendedorLabel;
        private ComboBox vendedorComboBox;

        private DataGridView inventarioDataGridView;
        private Button agregarButton;
        private Button visualizarInventarioButton;
        private Button crearProductoButton; // Botón para crear un producto
        private readonly ILogger<SQLiteService> _logger;
        private SQLiteService sqliteService;

        public InventarioForm(ILogger<SQLiteService> logger)
        {
            this.Text = "Gestión de Inventario";
            this.Width = 900;
            this.Height = 750;

            _logger = logger;
            sqliteService = new SQLiteService(_logger);

            // Número de Compra
            numeroCompraLabel = new Label() { Text = "Número de Compra", Top = 20, Left = 20, Width = 150 };
            numeroCompraTextBox = new TextBox() { Top = 20, Left = 180, Width = 200, ReadOnly = true };
            numeroCompraTextBox.Text = sqliteService.GetUltimoNumeroCompra().ToString();

            // Fecha de Compra
            fechaCompraLabel = new Label() { Text = "Fecha de Compra", Top = 50, Left = 20, Width = 150 };
            fechaCompraPicker = new DateTimePicker() { Top = 50, Left = 180, Width = 200 };

            // Proveedor
            proveedorLabel = new Label() { Text = "Proveedor", Top = 80, Left = 20, Width = 150 };
            proveedorComboBox = new ComboBox() { Top = 80, Left = 180, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };

            // Vendedor
            vendedorLabel = new Label() { Text = "Vendedor", Top = 110, Left = 20, Width = 150 };
            vendedorComboBox = new ComboBox() { Top = 110, Left = 180, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };

            // Tabla de Inventario
            inventarioDataGridView = new DataGridView()
            {
                Top = 150,
                Left = 20,
                Width = 850,
                Height = 400,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            inventarioDataGridView.Columns.Add("Codigo", "Código");
            inventarioDataGridView.Columns.Add("Descripcion", "Descripción");
            inventarioDataGridView.Columns.Add("Unidades", "Unidades");
            inventarioDataGridView.Columns.Add("Kilos", "Kilos");
            inventarioDataGridView.Columns.Add("FechaVencimiento", "Fecha de Vencimiento");

            // Agregar evento para manejar cambios en las celdas
            inventarioDataGridView.CellEndEdit += InventarioDataGridView_CellEndEdit;

            // Botones
            agregarButton = new Button() { Text = "Agregar Productos", Top = 570, Left = 20, Width = 200 };
            agregarButton.Click += AgregarButton_Click;

            visualizarInventarioButton = new Button() { Text = "Visualizar Inventario", Top = 570, Left = 240, Width = 200 };
            visualizarInventarioButton.Click += VisualizarInventarioButton_Click;

            crearProductoButton = new Button() { Text = "Crear Producto", Top = 570, Left = 460, Width = 200 }; // Nuevo botón
            crearProductoButton.Click += CrearProductoButton_Click;

            this.Controls.Add(numeroCompraLabel);
            this.Controls.Add(numeroCompraTextBox);
            this.Controls.Add(fechaCompraLabel);
            this.Controls.Add(fechaCompraPicker);
            this.Controls.Add(proveedorLabel);
            this.Controls.Add(proveedorComboBox);
            this.Controls.Add(vendedorLabel);
            this.Controls.Add(vendedorComboBox);
            this.Controls.Add(inventarioDataGridView);
            this.Controls.Add(agregarButton);
            this.Controls.Add(visualizarInventarioButton);
            this.Controls.Add(crearProductoButton);

            CargarProveedores();
            CargarVendedores();
        }

        private void CargarProveedores()
        {
            var proveedores = sqliteService.GetProveedores();
            if (proveedores.Count > 0)
            {
                proveedorComboBox.DataSource = proveedores;
            }
            else
            {
                proveedorComboBox.Items.Add("Sin Proveedores");
                MessageBox.Show("No se encontraron proveedores en la base de datos.");
            }
        }

        private void CargarVendedores()
        {
            var vendedores = sqliteService.GetVendedores();
            if (vendedores.Count > 0)
            {
                vendedorComboBox.DataSource = vendedores;
            }
            else
            {
                vendedorComboBox.Items.Add("Sin Vendedores");
                MessageBox.Show("No se encontraron vendedores en la base de datos.");
            }
        }

        private void InventarioDataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == inventarioDataGridView.Columns["Codigo"].Index)
            {
                string codigo = inventarioDataGridView.Rows[e.RowIndex].Cells["Codigo"].Value?.ToString();
                if (!string.IsNullOrEmpty(codigo))
                {
                    using (var connection = new SQLiteConnection(sqliteService.connectionString))
                    {
                        connection.Open();
                        var command = new SQLiteCommand(
                            "SELECT Codigo, Nombre FROM Productos WHERE Codigo = @codigo", connection);
                        command.Parameters.AddWithValue("@codigo", codigo);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                inventarioDataGridView.Rows[e.RowIndex].Cells["Descripcion"].Value = reader["Nombre"].ToString();
                            }
                            else
                            {
                                inventarioDataGridView.Rows[e.RowIndex].Cells["Codigo"].Value = null;
                                inventarioDataGridView.Rows[e.RowIndex].Cells["Descripcion"].Value = null;
                                MessageBox.Show("Código de producto no encontrado", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
        }

        private void AgregarButton_Click(object sender, EventArgs e)
        {
            string numeroCompra = numeroCompraTextBox.Text;
            string fechaCompra = fechaCompraPicker.Value.ToString("yyyy-MM-dd");

            foreach (DataGridViewRow row in inventarioDataGridView.Rows)
            {
                if (row.IsNewRow) continue;

                string codigo = row.Cells["Codigo"].Value?.ToString();
                string producto = row.Cells["Descripcion"].Value?.ToString();
                int unidades = int.TryParse(row.Cells["Unidades"].Value?.ToString(), out int u) ? u : 0;
                double kilos = double.TryParse(row.Cells["Kilos"].Value?.ToString(), out double k) ? k : 0.0;
                string fechaVencimiento = row.Cells["FechaVencimiento"].Value?.ToString() ?? fechaCompra;

                if (!string.IsNullOrEmpty(codigo) && !string.IsNullOrEmpty(producto))
                {
                    try
                    {
                        sqliteService.AddProducto(codigo, producto, unidades, kilos, fechaCompra, fechaCompra, fechaVencimiento);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al agregar el producto {codigo}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            // Limpiar el DataGridView después de agregar los productos
            inventarioDataGridView.Rows.Clear();

            sqliteService.IncrementarNumeroCompra();
            numeroCompraTextBox.Text = sqliteService.GetUltimoNumeroCompra().ToString();
            MessageBox.Show("Productos agregados al inventario.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void VisualizarInventarioButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Creamos una nueva instancia del logger específicamente para VisualizarInventarioForm
                var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder
                        .AddConsole()
                        .AddDebug();
                });

                var visualizarInventarioLogger = loggerFactory.CreateLogger<VisualizarInventarioForm>();
                var visualizarInventarioForm = new VisualizarInventarioForm(sqliteService, visualizarInventarioLogger);
                visualizarInventarioForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir el visualizador de inventario: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _logger.LogError(ex, "Error al abrir el visualizador de inventario");
            }
        }

        private void CrearProductoButton_Click(object sender, EventArgs e)
        {
            try
            {
                var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder
                        .AddConsole()
                        .AddDebug();
                });

                var crearProductoLogger = loggerFactory.CreateLogger<CrearProductoForm>();
                using (var crearProductoForm = new CrearProductoForm(crearProductoLogger))
                {
                    if (crearProductoForm.ShowDialog() == DialogResult.OK)
                    {
                        MessageBox.Show("Producto creado exitosamente.", "Éxito",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir el formulario de crear producto: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _logger.LogError(ex, "Error al abrir formulario de crear producto");
            }
        }
    }
}

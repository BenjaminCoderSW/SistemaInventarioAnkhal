using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Services;
using System.Web.UI.WebControls;
using GrupoAnkhalInventario.Helpers;

namespace GrupoAnkhalInventario
{
    public partial class Mermas : System.Web.UI.Page
    {
        private static readonly string _connStr =
            ConfigurationManager.ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

        private readonly JavaScriptSerializer _json = new JavaScriptSerializer();

        // ── ViewState keys ────────────────────────────────────────────────
        private int TotalMermas
        {
            get { return ViewState["TotalMermas"] is int v ? v : 0; }
            set { ViewState["TotalMermas"] = value; }
        }
        private int TotalHistorial
        {
            get { return ViewState["TotalHistorial"] is int v ? v : 0; }
            set { ViewState["TotalHistorial"] = value; }
        }

        // ── ViewModels ────────────────────────────────────────────────────
        private class MermaStockVM
        {
            public string  BaseNombre    { get; set; }
            public string  Codigo        { get; set; }
            public string  Descripcion   { get; set; }
            public string  Unidad        { get; set; }
            public decimal CantidadActual { get; set; }
        }

        private class HistorialVM
        {
            public int      ConversionID      { get; set; }
            public DateTime FechaConversion   { get; set; }
            public string   BaseNombre        { get; set; }
            public string   CodigoOrigen      { get; set; }
            public string   DescripcionOrigen { get; set; }
            public string   UnidadOrigen      { get; set; }
            public decimal  CantidadOrigen    { get; set; }
            public int      DetalleCount      { get; set; }
            public string   RegistradoPor     { get; set; }
            public string   Observaciones     { get; set; }
        }

        private class OutputItem
        {
            public int     matID { get; set; }
            public decimal cant  { get; set; }
        }

        // ── Page_Load ─────────────────────────────────────────────────────
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["ClaveID"] == null) { Response.Redirect("~/Login.aspx"); return; }
            if (!IsPostBack)
            {
                CargarFiltros();
                CargarDashboard();
                CargarGridMerma();
                CargarGridHistorial();
            }
            else
            {
                gvMermas.VirtualItemCount   = TotalMermas;
                gvHistorial.VirtualItemCount = TotalHistorial;
            }
        }

        // ── CargarFiltros ─────────────────────────────────────────────────
        private void CargarFiltros()
        {
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "SELECT BaseID, Nombre FROM dbo.Bases WHERE Activo=1 ORDER BY Nombre", conn))
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        var val  = rd.GetInt32(0).ToString();
                        var text = rd.GetString(1);
                        ddlFiltrBase.Items.Add(new ListItem(text, val));
                        ddlConvBase.Items.Add(new ListItem(text, val));
                    }
                }
            }
        }

        // ── CargarDashboard ───────────────────────────────────────────────
        private void CargarDashboard()
        {
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                const string sql = @"
                    SELECT
                        COUNT(*)                                         AS TotalEntradas,
                        ISNULL(SUM(sm.CantidadActual * m.PrecioUnitario), 0) AS ValorTotal
                    FROM dbo.StockMerma sm
                    JOIN dbo.Materiales m ON m.MaterialID = sm.MaterialID
                    WHERE sm.CantidadActual > 0";
                using (var cmd = new SqlCommand(sql, conn))
                using (var rd = cmd.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        lblTotalEntradas.Text = rd.GetInt32(0).ToString("N0");
                        lblValorMerma.Text    = rd.GetDecimal(1).ToString("C2");
                    }
                }
            }
        }

        // ── CargarGridMerma ───────────────────────────────────────────────
        private void CargarGridMerma()
        {
            int pageIdx  = gvMermas.PageIndex;
            int pageSz   = gvMermas.PageSize;
            string base_ = ddlFiltrBase.SelectedValue;
            string mat_  = txtFiltrMaterial.Text.Trim();

            string where = " WHERE sm.CantidadActual > 0 ";
            if (!string.IsNullOrEmpty(base_)) where += " AND sm.BaseID = @baseID ";
            if (!string.IsNullOrEmpty(mat_))  where += " AND (m.Codigo LIKE @mat OR m.Descripcion LIKE @mat) ";

            string countSql = "SELECT COUNT(*) FROM dbo.StockMerma sm JOIN dbo.Materiales m ON m.MaterialID=sm.MaterialID JOIN dbo.Bases b ON b.BaseID=sm.BaseID" + where;
            string dataSql  = @"
                SELECT b.Nombre AS BaseNombre, m.Codigo, m.Descripcion, m.Unidad, sm.CantidadActual
                FROM dbo.StockMerma sm
                JOIN dbo.Materiales m ON m.MaterialID = sm.MaterialID
                JOIN dbo.Bases      b ON b.BaseID     = sm.BaseID"
                + where +
                " ORDER BY b.Nombre, m.Codigo" +
                " OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY";

            var list = new List<MermaStockVM>();
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                int total = 0;
                using (var cmd = new SqlCommand(countSql, conn))
                {
                    AgregarFiltroParams(cmd, base_, mat_);
                    total = (int)cmd.ExecuteScalar();
                }
                TotalMermas = total;
                gvMermas.VirtualItemCount = total;
                lblResultados.Text = total == 0 ? "Sin resultados." : $"{total} material(es) en merma.";

                using (var cmd = new SqlCommand(dataSql, conn))
                {
                    AgregarFiltroParams(cmd, base_, mat_);
                    cmd.Parameters.AddWithValue("@skip", pageIdx * pageSz);
                    cmd.Parameters.AddWithValue("@take", pageSz);
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            list.Add(new MermaStockVM
                            {
                                BaseNombre    = rd["BaseNombre"].ToString(),
                                Codigo        = rd["Codigo"].ToString(),
                                Descripcion   = rd["Descripcion"].ToString(),
                                Unidad        = rd["Unidad"].ToString(),
                                CantidadActual = (decimal)rd["CantidadActual"]
                            });
                        }
                    }
                }
            }
            gvMermas.DataSource = list;
            gvMermas.DataBind();
        }

        private static void AgregarFiltroParams(SqlCommand cmd, string base_, string mat_)
        {
            if (!string.IsNullOrEmpty(base_)) cmd.Parameters.AddWithValue("@baseID", int.Parse(base_));
            if (!string.IsNullOrEmpty(mat_))  cmd.Parameters.AddWithValue("@mat", "%" + mat_ + "%");
        }

        // ── CargarGridHistorial ───────────────────────────────────────────
        private void CargarGridHistorial()
        {
            int pageIdx = gvHistorial.PageIndex;
            int pageSz  = gvHistorial.PageSize;

            const string countSql = "SELECT COUNT(*) FROM dbo.ConversionesMerma";
            const string headerSql = @"
                SELECT cm.ConversionID, cm.FechaConversion,
                       b.Nombre AS BaseNombre,
                       mo.Codigo AS CodigoOrigen, mo.Descripcion AS DescripcionOrigen, mo.Unidad AS UnidadOrigen,
                       cm.CantidadOrigen, cm.Observaciones,
                       u.Usuario AS RegistradoPor,
                       (SELECT COUNT(*) FROM dbo.DetalleConversionMerma d WHERE d.ConversionID = cm.ConversionID) AS DetalleCount
                FROM dbo.ConversionesMerma cm
                JOIN dbo.Bases      b  ON b.BaseID      = cm.BaseID
                JOIN dbo.Materiales mo ON mo.MaterialID = cm.MaterialOrigenID
                JOIN dbo.Usuario    u  ON u.ClaveID     = cm.RegistradoPorID
                ORDER BY cm.FechaRegistro DESC
                OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY";

            var list = new List<HistorialVM>();
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                int total = 0;
                using (var cmd = new SqlCommand(countSql, conn))
                    total = (int)cmd.ExecuteScalar();
                TotalHistorial = total;
                gvHistorial.VirtualItemCount = total;

                if (total == 0) { gvHistorial.DataSource = list; gvHistorial.DataBind(); return; }

                using (var cmd = new SqlCommand(headerSql, conn))
                {
                    cmd.Parameters.AddWithValue("@skip", pageIdx * pageSz);
                    cmd.Parameters.AddWithValue("@take", pageSz);
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            list.Add(new HistorialVM
                            {
                                ConversionID      = (int)rd["ConversionID"],
                                FechaConversion   = (DateTime)rd["FechaConversion"],
                                BaseNombre        = rd["BaseNombre"].ToString(),
                                CodigoOrigen      = rd["CodigoOrigen"].ToString(),
                                DescripcionOrigen = rd["DescripcionOrigen"].ToString(),
                                UnidadOrigen      = rd["UnidadOrigen"].ToString(),
                                CantidadOrigen    = (decimal)rd["CantidadOrigen"],
                                DetalleCount      = (int)rd["DetalleCount"],
                                Observaciones     = rd["Observaciones"] == DBNull.Value ? "" : rd["Observaciones"].ToString(),
                                RegistradoPor     = rd["RegistradoPor"].ToString()
                            });
                        }
                    }
                }
            }
            gvHistorial.DataSource = list;
            gvHistorial.DataBind();
        }

        // ── Eventos de paginación ─────────────────────────────────────────
        protected void gvMermas_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvMermas.PageIndex = e.NewPageIndex;
            CargarGridMerma();
        }

        protected void gvHistorial_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvHistorial.PageIndex = e.NewPageIndex;
            CargarGridHistorial();
        }

        // ── Filtros ───────────────────────────────────────────────────────
        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            gvMermas.PageIndex = 0;
            CargarGridMerma();
        }

        protected void btnLimpiarFiltros_Click(object sender, EventArgs e)
        {
            ddlFiltrBase.SelectedIndex    = 0;
            txtFiltrMaterial.Text         = "";
            gvMermas.PageIndex            = 0;
            CargarGridMerma();
        }

        // ── btnGuardarConversion_Click ────────────────────────────────────
        protected void btnGuardarConversion_Click(object sender, EventArgs e)
        {
            // Leer valores del formulario
            if (!int.TryParse(ddlConvBase.SelectedValue, out int baseID))
            { SetMsg("error", "Base inválida", "Seleccione una base."); return; }

            if (!int.TryParse(hdnConvMatOrigenID.Value, out int matOrigenID) || matOrigenID <= 0)
            { SetMsg("error", "Material inválido", "Seleccione el material en merma."); return; }

            string cantStr = txtConvCantOrigen.Text.Trim();
            if (!decimal.TryParse(cantStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal cantOrigen) || cantOrigen <= 0)
            { SetMsg("error", "Cantidad inválida", "Ingrese la cantidad a convertir."); return; }

            // Parsear outputs JSON
            string outputsJson = hdnOutputsJson.Value;
            if (string.IsNullOrWhiteSpace(outputsJson))
            { SetMsg("error", "Sin materiales destino", "Agregue al menos un material recuperado."); return; }

            List<OutputItem> outputs;
            try { outputs = _json.Deserialize<List<OutputItem>>(outputsJson); }
            catch { SetMsg("error", "Error de datos", "No se pudo procesar los materiales destino."); return; }

            if (outputs == null || outputs.Count == 0)
            { SetMsg("error", "Sin materiales destino", "Agregue al menos un material recuperado."); return; }

            int registradoPorID = (int)Session["ClaveID"];

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();

                // Verificar stock disponible
                decimal disponible = 0;
                using (var cmd = new SqlCommand(
                    "SELECT ISNULL(CantidadActual,0) FROM dbo.StockMerma WHERE BaseID=@b AND MaterialID=@m", conn))
                {
                    cmd.Parameters.AddWithValue("@b", baseID);
                    cmd.Parameters.AddWithValue("@m", matOrigenID);
                    var val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value) disponible = (decimal)val;
                }

                if (cantOrigen > disponible)
                {
                    SetMsg("warning", "Stock insuficiente",
                        $"Solo hay {disponible:N4} unidades en merma. No se puede convertir {cantOrigen:N4}.");
                    return;
                }

                using (var tx = conn.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        // 1. Restar del stock de merma
                        UpsertStockMerma(conn, tx, matOrigenID, baseID, -cantOrigen);

                        // 2. Sumar al stock regular por cada output
                        foreach (var o in outputs)
                            UpsertStockMaterialRegular(conn, tx, o.matID, baseID, o.cant);

                        // 3. Insertar cabecera de conversión
                        int convID;
                        using (var cmd = new SqlCommand(@"
                            INSERT INTO dbo.ConversionesMerma
                                (BaseID, FechaConversion, MaterialOrigenID, CantidadOrigen,
                                 Observaciones, RegistradoPorID, FechaRegistro)
                            OUTPUT INSERTED.ConversionID
                            VALUES (@baseID, @fecha, @matOrigen, @cantOrigen,
                                    @obs, @userID, SYSUTCDATETIME())", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@baseID",    baseID);
                            cmd.Parameters.AddWithValue("@fecha",     AppHelper.Ahora.Date);
                            cmd.Parameters.AddWithValue("@matOrigen", matOrigenID);
                            cmd.Parameters.AddWithValue("@cantOrigen",cantOrigen);
                            cmd.Parameters.AddWithValue("@obs",       (object)txtConvObs.Text.Trim() == "" ? DBNull.Value : (object)txtConvObs.Text.Trim());
                            cmd.Parameters.AddWithValue("@userID",    registradoPorID);
                            convID = (int)cmd.ExecuteScalar();
                        }

                        // 4. Insertar detalle por cada output
                        foreach (var o in outputs)
                        {
                            using (var cmd = new SqlCommand(@"
                                INSERT INTO dbo.DetalleConversionMerma (ConversionID, MaterialDestinoID, CantidadDestino)
                                VALUES (@convID, @matDest, @cant)", conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@convID",  convID);
                                cmd.Parameters.AddWithValue("@matDest", o.matID);
                                cmd.Parameters.AddWithValue("@cant",    o.cant);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }

            // Limpiar y recargar
            hdnConvMatOrigenID.Value = "";
            txtConvCantOrigen.Text   = "";
            txtConvObs.Text          = "";
            hdnOutputsJson.Value     = "";

            SetMsg("success", "Conversión registrada",
                $"Se convirtieron {cantOrigen:N4} unidades. El stock fue actualizado.");
            CargarDashboard();
            CargarGridMerma();
            CargarGridHistorial();
        }

        // ── Helpers de stock (raw SQL) ────────────────────────────────────
        private static void UpsertStockMerma(SqlConnection conn, SqlTransaction tx,
            int materialID, int baseID, decimal delta)
        {
            using (var cmd = new SqlCommand(@"
                UPDATE dbo.StockMerma
                   SET CantidadActual = CantidadActual + @delta, FechaUltimaModif = SYSUTCDATETIME()
                 WHERE BaseID = @baseID AND MaterialID = @materialID", conn, tx))
            {
                cmd.Parameters.AddWithValue("@delta",      delta);
                cmd.Parameters.AddWithValue("@baseID",     baseID);
                cmd.Parameters.AddWithValue("@materialID", materialID);
                int filas = cmd.ExecuteNonQuery();
                if (filas == 0 && delta > 0)
                {
                    cmd.Parameters.Clear();
                    cmd.CommandText = @"
                        INSERT INTO dbo.StockMerma (BaseID, MaterialID, CantidadActual, FechaUltimaModif)
                        VALUES (@baseID, @materialID, @delta, SYSUTCDATETIME())";
                    cmd.Parameters.AddWithValue("@baseID",     baseID);
                    cmd.Parameters.AddWithValue("@materialID", materialID);
                    cmd.Parameters.AddWithValue("@delta",      delta);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void UpsertStockMaterialRegular(SqlConnection conn, SqlTransaction tx,
            int materialID, int baseID, decimal delta)
        {
            using (var cmd = new SqlCommand(@"
                UPDATE dbo.StockMateriales
                   SET CantidadActual = CantidadActual + @delta, FechaUltimaModif = SYSUTCDATETIME()
                 WHERE BaseID = @baseID AND MaterialID = @materialID", conn, tx))
            {
                cmd.Parameters.AddWithValue("@delta",      delta);
                cmd.Parameters.AddWithValue("@baseID",     baseID);
                cmd.Parameters.AddWithValue("@materialID", materialID);
                int filas = cmd.ExecuteNonQuery();
                if (filas == 0 && delta > 0)
                {
                    cmd.Parameters.Clear();
                    cmd.CommandText = @"
                        INSERT INTO dbo.StockMateriales (BaseID, MaterialID, CantidadActual, FechaUltimaModif)
                        VALUES (@baseID, @materialID, @delta, SYSUTCDATETIME())";
                    cmd.Parameters.AddWithValue("@baseID",     baseID);
                    cmd.Parameters.AddWithValue("@materialID", materialID);
                    cmd.Parameters.AddWithValue("@delta",      delta);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ── SetMsg (SweetAlert) ───────────────────────────────────────────
        private void SetMsg(string icon, string title, string text)
        {
            hdnMensajePendiente.Value = _json.Serialize(new { icon, title, text });
        }

        // ── Métodos llamados desde ASPX (datos para JS) ───────────────────
        public string GetStockMermaJson()
        {
            var dict = new Dictionary<string, List<object>>();
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                const string sql = @"
                    SELECT sm.BaseID, sm.MaterialID, m.Codigo, m.Descripcion, m.Unidad, sm.CantidadActual
                    FROM dbo.StockMerma sm
                    JOIN dbo.Materiales m ON m.MaterialID = sm.MaterialID
                    WHERE sm.CantidadActual > 0
                    ORDER BY sm.BaseID, m.Codigo";
                using (var cmd = new SqlCommand(sql, conn))
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        string key = rd.GetInt32(0).ToString();
                        if (!dict.ContainsKey(key)) dict[key] = new List<object>();
                        dict[key].Add(new
                        {
                            matID        = rd.GetInt32(1),
                            codigo       = rd.GetString(2),
                            descripcion  = rd.GetString(3),
                            unidad       = rd.GetString(4),
                            cantDisponible = (double)(decimal)rd["CantidadActual"]
                        });
                    }
                }
            }
            return _json.Serialize(dict);
        }

        public string GetMaterialesActivosJson()
        {
            var list = new List<object>();
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                const string sql = "SELECT MaterialID, Codigo, Descripcion, Unidad FROM dbo.Materiales WHERE Activo=1 ORDER BY Codigo";
                using (var cmd = new SqlCommand(sql, conn))
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                        list.Add(new
                        {
                            matID       = rd.GetInt32(0),
                            codigo      = rd.GetString(1),
                            descripcion = rd.GetString(2),
                            unidad      = rd.GetString(3)
                        });
                }
            }
            return _json.Serialize(list);
        }

        // ── WebMethod: detalle de una conversión (llamado vía AJAX) ───────
        [WebMethod]
        public static object ObtenerDetalleConversion(int conversionID)
        {
            if (HttpContext.Current.Session["ClaveID"] == null)
                throw new Exception("No autorizado");

            string connStr = ConfigurationManager
                .ConnectionStrings["InventarioAnkhalDBConnectionString"].ConnectionString;

            var result = new List<object>();
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                const string sql = @"
                    SELECT m.Codigo, m.Descripcion, m.Unidad, d.CantidadDestino
                    FROM dbo.DetalleConversionMerma d
                    JOIN dbo.Materiales m ON m.MaterialID = d.MaterialDestinoID
                    WHERE d.ConversionID = @id
                    ORDER BY m.Codigo";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", conversionID);
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                            result.Add(new
                            {
                                Codigo       = rd.GetString(0),
                                Descripcion  = rd.GetString(1),
                                Unidad       = rd.GetString(2),
                                Cantidad     = (double)(decimal)rd["CantidadDestino"]
                            });
                    }
                }
            }
            return result;
        }
    }
}

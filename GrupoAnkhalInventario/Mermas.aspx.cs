using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
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
            public string  BaseNombre     { get; set; }
            public string  Codigo         { get; set; }
            public string  Descripcion    { get; set; }
            public string  Unidad         { get; set; }
            public decimal CantidadActual { get; set; }
        }

        private class HistorialVM
        {
            public int      ConversionID             { get; set; }
            public DateTime FechaConversion          { get; set; }
            public string   BaseNombre               { get; set; }
            public string   CodigoOrigen             { get; set; }
            public string   DescripcionOrigen        { get; set; }
            public string   UnidadOrigen             { get; set; }
            public decimal  CantidadOrigen           { get; set; }
            public decimal? CantidadCapturadaOrigen  { get; set; }
            public string   UnidadCapturaNombre      { get; set; }
            public bool     TuvoConversion           { get; set; }
            public int      DetalleCount             { get; set; }
            public string   RegistradoPor            { get; set; }
            public string   Observaciones            { get; set; }
        }

        private class OutputItem
        {
            public int     matID    { get; set; }
            public decimal cant     { get; set; }   // cantidad capturada (en unidad elegida)
            public int     unidadId { get; set; }   // ConversionID o 0 = base
            public decimal factor   { get; set; }   // 1 = base
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
                gvMermas.VirtualItemCount    = TotalMermas;
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
                        COUNT(*)                                             AS TotalEntradas,
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
                                BaseNombre     = rd["BaseNombre"].ToString(),
                                Codigo         = rd["Codigo"].ToString(),
                                Descripcion    = rd["Descripcion"].ToString(),
                                Unidad         = rd["Unidad"].ToString(),
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
                       cm.CantidadOrigen, cm.CantidadCapturada, uc.Nombre AS UnidadCapturaNombre,
                       cm.Observaciones,
                       u.Usuario AS RegistradoPor,
                       (SELECT COUNT(*) FROM dbo.DetalleConversionMerma d WHERE d.ConversionID = cm.ConversionID) AS DetalleCount
                FROM dbo.ConversionesMerma cm
                JOIN dbo.Bases      b  ON b.BaseID      = cm.BaseID
                JOIN dbo.Materiales mo ON mo.MaterialID = cm.MaterialOrigenID
                JOIN dbo.Usuario    u  ON u.ClaveID     = cm.RegistradoPorID
                LEFT JOIN dbo.UnidadesMedida uc ON uc.UnidadMedidaID = cm.UnidadCapturaID
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
                            decimal? cantCapt = rd["CantidadCapturada"] == DBNull.Value
                                ? (decimal?)null
                                : (decimal)rd["CantidadCapturada"];
                            string unidCapt = rd["UnidadCapturaNombre"] == DBNull.Value
                                ? null
                                : rd["UnidadCapturaNombre"].ToString();
                            bool tuvo = cantCapt.HasValue && cantCapt.Value != (decimal)rd["CantidadOrigen"];

                            list.Add(new HistorialVM
                            {
                                ConversionID            = (int)rd["ConversionID"],
                                FechaConversion         = (DateTime)rd["FechaConversion"],
                                BaseNombre              = rd["BaseNombre"].ToString(),
                                CodigoOrigen            = rd["CodigoOrigen"].ToString(),
                                DescripcionOrigen       = rd["DescripcionOrigen"].ToString(),
                                UnidadOrigen            = rd["UnidadOrigen"].ToString(),
                                CantidadOrigen          = (decimal)rd["CantidadOrigen"],
                                CantidadCapturadaOrigen = cantCapt,
                                UnidadCapturaNombre     = unidCapt,
                                TuvoConversion          = tuvo,
                                DetalleCount            = (int)rd["DetalleCount"],
                                Observaciones           = rd["Observaciones"] == DBNull.Value ? "" : rd["Observaciones"].ToString(),
                                RegistradoPor           = rd["RegistradoPor"].ToString()
                            });
                        }
                    }
                }
            }
            gvHistorial.DataSource = list;
            gvHistorial.DataBind();
        }

        // ── FormatCantidadHistorial (llamado desde ASPX) ──────────────────
        protected string FormatCantidadHistorial(object cantBase, object cantCapt,
            object unidCapt, object unidBase, object tuvo)
        {
            bool tuvoConv = tuvo is bool b && b;
            decimal cb = cantBase is decimal d ? d : 0m;
            string ub  = HttpUtility.HtmlEncode(unidBase?.ToString() ?? "");

            if (!tuvoConv || cantCapt == null || cantCapt == DBNull.Value)
                return $"<strong style='color:#922b21'>{FmtCant(cb)}</strong> <small class='text-muted'>{ub}</small>";

            decimal cc  = cantCapt is decimal dc ? dc : 0m;
            string  uc  = HttpUtility.HtmlEncode(unidCapt?.ToString() ?? "");
            return $"<strong style='color:#922b21'>{FmtCant(cc)}</strong> <small class='text-muted'>{uc}</small>" +
                   $"<br/><small class='text-muted'>= {FmtCant(cb)} {ub} (base)</small>";
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
            ddlFiltrBase.SelectedIndex = 0;
            txtFiltrMaterial.Text      = "";
            gvMermas.PageIndex         = 0;
            CargarGridMerma();
        }

        // ── btnGuardarConversion_Click ────────────────────────────────────
        protected void btnGuardarConversion_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(ddlConvBase.SelectedValue, out int baseID))
            { SetMsg("error", "Base inválida", "Seleccione una base."); return; }

            if (!int.TryParse(hdnConvMatOrigenID.Value, out int matOrigenID) || matOrigenID <= 0)
            { SetMsg("error", "Material inválido", "Seleccione el material en merma."); return; }

            string cantStr = txtConvCantOrigen.Text.Trim();
            if (!decimal.TryParse(cantStr, NumberStyles.Any, CultureInfo.InvariantCulture,
                out decimal cantOrigen) || cantOrigen <= 0)
            { SetMsg("error", "Cantidad inválida", "Ingrese la cantidad a convertir."); return; }

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

                // ── Resolver conversión del origen ────────────────────────
                decimal factorOrigen         = 1m;
                int?    unidadCapturaOrigenID = null;

                string origenUnidVal  = hdnOrigenUnidadVal.Value.Trim();
                string origenFactorSt = hdnOrigenFactor.Value.Trim();

                if (!string.IsNullOrEmpty(origenFactorSt) &&
                    decimal.TryParse(origenFactorSt, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal pf)
                    && pf > 0m && pf != 1m)
                {
                    // El JS indicó una conversión — verificar en servidor
                    if (int.TryParse(origenUnidVal, out int convIDOrigen))
                    {
                        using (var cmd = new SqlCommand(
                            "SELECT UnidadOrigenID, Factor FROM dbo.ConversionesMaterial " +
                            "WHERE ConversionID=@cid AND MaterialID=@mid AND Activo=1", conn))
                        {
                            cmd.Parameters.AddWithValue("@cid", convIDOrigen);
                            cmd.Parameters.AddWithValue("@mid", matOrigenID);
                            using (var rd = cmd.ExecuteReader())
                            {
                                if (!rd.Read())
                                { SetMsg("error", "Conversión inválida", "La unidad seleccionada ya no está activa."); return; }
                                unidadCapturaOrigenID = rd.GetInt32(0);
                                factorOrigen          = rd.GetDecimal(1);
                            }
                        }
                    }
                }
                else if (int.TryParse(origenUnidVal, out int baseUMID) && baseUMID > 0)
                {
                    // Es la unidad base del material
                    unidadCapturaOrigenID = baseUMID;
                }

                decimal cantOrigenBase = cantOrigen * factorOrigen;

                // ── Resolver conversiones de cada destino ─────────────────
                var outputsResueltos = new List<(OutputItem item, decimal cantBase, int? unidadCaptID)>();
                foreach (var o in outputs)
                {
                    decimal cantDestinoBase = o.cant;
                    int?    unidCapDest     = null;

                    if (o.factor > 0m && o.factor != 1m && o.unidadId > 0)
                    {
                        using (var cmd = new SqlCommand(
                            "SELECT UnidadOrigenID, Factor FROM dbo.ConversionesMaterial " +
                            "WHERE ConversionID=@cid AND MaterialID=@mid AND Activo=1", conn))
                        {
                            cmd.Parameters.AddWithValue("@cid", o.unidadId);
                            cmd.Parameters.AddWithValue("@mid", o.matID);
                            using (var rd = cmd.ExecuteReader())
                            {
                                if (rd.Read())
                                {
                                    unidCapDest     = rd.GetInt32(0);
                                    cantDestinoBase = o.cant * rd.GetDecimal(1);
                                }
                            }
                        }
                    }
                    else if (o.unidadId > 0)
                    {
                        unidCapDest = o.unidadId; // es un UnidadMedidaID (base)
                    }

                    outputsResueltos.Add((o, cantDestinoBase, unidCapDest));
                }

                // ── Verificar stock disponible ────────────────────────────
                decimal disponible = 0;
                using (var cmd = new SqlCommand(
                    "SELECT ISNULL(CantidadActual,0) FROM dbo.StockMerma WHERE BaseID=@b AND MaterialID=@m", conn))
                {
                    cmd.Parameters.AddWithValue("@b", baseID);
                    cmd.Parameters.AddWithValue("@m", matOrigenID);
                    var val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value) disponible = (decimal)val;
                }

                if (cantOrigenBase > disponible)
                {
                    string unidNombre = factorOrigen == 1m ? "" : $" ({FmtCant(cantOrigen)} en unidad capturada)";
                    SetMsg("warning", "Stock insuficiente",
                        $"Solo hay {FmtCant(disponible)} unidades base en merma. No se puede convertir {FmtCant(cantOrigenBase)}{unidNombre}.");
                    return;
                }

                using (var tx = conn.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        // 1. Restar del stock de merma (en unidades base)
                        UpsertStockMerma(conn, tx, matOrigenID, baseID, -cantOrigenBase);

                        // 2. Sumar al stock regular por cada output (en unidades base)
                        foreach (var (o, cantBase, _) in outputsResueltos)
                            UpsertStockMaterialRegular(conn, tx, o.matID, baseID, cantBase);

                        // 3. Insertar cabecera de conversión
                        int convID;
                        using (var cmd = new SqlCommand(@"
                            INSERT INTO dbo.ConversionesMerma
                                (BaseID, FechaConversion, MaterialOrigenID, CantidadOrigen,
                                 CantidadCapturada, UnidadCapturaID, FactorAplicado,
                                 Observaciones, RegistradoPorID, FechaRegistro)
                            OUTPUT INSERTED.ConversionID
                            VALUES (@baseID, @fecha, @matOrigen, @cantOrigenBase,
                                    @cantCapturada, @unidCaptID, @factorOrigen,
                                    @obs, @userID, SYSUTCDATETIME())", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@baseID",        baseID);
                            cmd.Parameters.AddWithValue("@fecha",         AppHelper.Ahora.Date);
                            cmd.Parameters.AddWithValue("@matOrigen",     matOrigenID);
                            cmd.Parameters.AddWithValue("@cantOrigenBase",cantOrigenBase);
                            cmd.Parameters.AddWithValue("@cantCapturada", factorOrigen == 1m ? (object)DBNull.Value : cantOrigen);
                            cmd.Parameters.AddWithValue("@unidCaptID",    unidadCapturaOrigenID.HasValue ? (object)unidadCapturaOrigenID.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@factorOrigen",  factorOrigen == 1m ? (object)DBNull.Value : factorOrigen);
                            cmd.Parameters.AddWithValue("@obs",           string.IsNullOrEmpty(txtConvObs.Text.Trim()) ? (object)DBNull.Value : txtConvObs.Text.Trim());
                            cmd.Parameters.AddWithValue("@userID",        registradoPorID);
                            convID = (int)cmd.ExecuteScalar();
                        }

                        // 4. Insertar detalle por cada output
                        foreach (var (o, cantBase, unidCapDest) in outputsResueltos)
                        {
                            using (var cmd = new SqlCommand(@"
                                INSERT INTO dbo.DetalleConversionMerma
                                    (ConversionID, MaterialDestinoID, CantidadDestino,
                                     CantidadCapturada, UnidadCapturaID, FactorAplicado)
                                VALUES (@convID, @matDest, @cantBase,
                                        @cantCapt, @unidCapt, @factor)", conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@convID",  convID);
                                cmd.Parameters.AddWithValue("@matDest", o.matID);
                                cmd.Parameters.AddWithValue("@cantBase",cantBase);
                                cmd.Parameters.AddWithValue("@cantCapt", o.factor == 1m ? (object)DBNull.Value : o.cant);
                                cmd.Parameters.AddWithValue("@unidCapt", unidCapDest.HasValue ? (object)unidCapDest.Value : DBNull.Value);
                                cmd.Parameters.AddWithValue("@factor",   o.factor == 1m ? (object)DBNull.Value : o.factor);
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
            hdnConvMatOrigenID.Value  = "";
            hdnOrigenUnidadVal.Value  = "";
            hdnOrigenFactor.Value     = "";
            txtConvCantOrigen.Text    = "";
            txtConvObs.Text           = "";
            hdnOutputsJson.Value      = "";

            SetMsg("success", "Conversión registrada",
                $"Se convirtieron {FmtCant(cantOrigen)} unidades. El stock fue actualizado.");
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

        // ── Formato inteligente de cantidades (sin ceros decimales innecesarios) ──
        private static string FmtCant(decimal v)
        {
            // Hasta 4 decimales, elimina los ceros finales
            string s = v.ToString("N4");
            if (s.Contains(".")) s = s.TrimEnd('0').TrimEnd('.');
            return s;
        }

        // ── SetMsg (SweetAlert) ───────────────────────────────────────────
        private void SetMsg(string icon, string title, string text)
        {
            hdnMensajePendiente.Value = _json.Serialize(new { icon, title, text });
        }

        // ── Datos para JS ─────────────────────────────────────────────────

        // Stock de merma por base (incluye UnidadMedidaID para lookup de conversiones)
        public string GetStockMermaJson()
        {
            var dict = new Dictionary<string, List<object>>();
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                const string sql = @"
                    SELECT sm.BaseID, sm.MaterialID, m.Codigo, m.Descripcion, m.Unidad,
                           m.UnidadMedidaID, sm.CantidadActual
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
                            matID          = rd.GetInt32(1),
                            codigo         = rd.GetString(2),
                            descripcion    = rd.GetString(3),
                            unidad         = rd.GetString(4),
                            unidadMedidaID = rd["UnidadMedidaID"] == DBNull.Value ? (int?)null : (int?)rd.GetInt32(5),
                            cantDisponible = (double)(decimal)rd["CantidadActual"]
                        });
                    }
                }
            }
            return _json.Serialize(dict);
        }

        // Materiales activos (incluye UnidadMedidaID para lookup de conversiones)
        public string GetMaterialesActivosJson()
        {
            var list = new List<object>();
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                const string sql = "SELECT MaterialID, Codigo, Descripcion, Unidad, UnidadMedidaID FROM dbo.Materiales WHERE Activo=1 ORDER BY Codigo";
                using (var cmd = new SqlCommand(sql, conn))
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                        list.Add(new
                        {
                            matID          = rd.GetInt32(0),
                            codigo         = rd.GetString(1),
                            descripcion    = rd.GetString(2),
                            unidad         = rd.GetString(3),
                            unidadMedidaID = rd["UnidadMedidaID"] == DBNull.Value ? (int?)null : (int?)rd.GetInt32(4)
                        });
                }
            }
            return _json.Serialize(list);
        }

        // Opciones de unidad de captura por materialID (misma estructura que Movimientos)
        public string GetConversionesMatJson()
        {
            // Cargar unidades base por material
            var matInfo = new Dictionary<int, (string unidad, int? umID)>();
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                const string sqlMats = @"
                    SELECT m.MaterialID, m.Unidad, m.UnidadMedidaID,
                           um.Nombre AS UMNombre, um.Clave AS UMClave
                    FROM dbo.Materiales m
                    LEFT JOIN dbo.UnidadesMedida um ON um.UnidadMedidaID = m.UnidadMedidaID
                    WHERE m.Activo = 1";
                using (var cmd = new SqlCommand(sqlMats, conn))
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        int    mid   = rd.GetInt32(0);
                        string unid  = rd.GetString(1);
                        int?   umID  = rd["UnidadMedidaID"] == DBNull.Value ? (int?)null : (int?)rd.GetInt32(2);
                        string umNom = rd["UMNombre"] == DBNull.Value ? null : rd["UMNombre"].ToString();
                        string umCla = rd["UMClave"]  == DBNull.Value ? null : rd["UMClave"].ToString();
                        if (umNom != null) unid = $"{umNom} ({umCla})";
                        matInfo[mid] = (unid, umID);
                    }
                }

                // Cargar conversiones activas
                var convsPorMat = new Dictionary<int, List<object>>();
                const string sqlConvs = @"
                    SELECT c.ConversionID, c.MaterialID, c.Factor, u.Nombre, u.Clave
                    FROM dbo.ConversionesMaterial c
                    JOIN dbo.UnidadesMedida u ON u.UnidadMedidaID = c.UnidadOrigenID
                    WHERE c.Activo = 1
                    ORDER BY c.MaterialID";
                using (var cmd = new SqlCommand(sqlConvs, conn))
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        int mid = rd.GetInt32(1);
                        if (!convsPorMat.ContainsKey(mid)) convsPorMat[mid] = new List<object>();
                        decimal factor = rd.GetDecimal(2);
                        convsPorMat[mid].Add(new
                        {
                            val    = rd.GetInt32(0).ToString(),
                            txt    = $"{rd.GetString(3)} ({rd.GetString(4)})  [×{factor:N6}]",
                            factor = (double)factor
                        });
                    }
                }

                // Construir diccionario final (solo materiales con conversiones o unidad base conocida)
                var result = new Dictionary<string, object>();
                foreach (var kv in matInfo)
                {
                    int  mid  = kv.Key;
                    var  info = kv.Value;
                    var  opts = new List<object>();

                    // Opción base
                    opts.Add(new
                    {
                        val    = info.umID.HasValue ? info.umID.Value.ToString() : "base",
                        txt    = info.unidad + " — base",
                        factor = 1.0
                    });

                    // Opciones adicionales
                    if (convsPorMat.ContainsKey(mid))
                        opts.AddRange(convsPorMat[mid]);

                    result[mid.ToString()] = opts;
                }

                return _json.Serialize(result);
            }
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
                    SELECT m.Codigo, m.Descripcion, m.Unidad,
                           d.CantidadDestino, d.CantidadCapturada, d.FactorAplicado,
                           uc.Nombre AS UnidadCapturaNombre
                    FROM dbo.DetalleConversionMerma d
                    JOIN dbo.Materiales m ON m.MaterialID = d.MaterialDestinoID
                    LEFT JOIN dbo.UnidadesMedida uc ON uc.UnidadMedidaID = d.UnidadCapturaID
                    WHERE d.ConversionID = @id
                    ORDER BY m.Codigo";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", conversionID);
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            decimal cantBase = (decimal)rd["CantidadDestino"];
                            bool    tuvo     = rd["CantidadCapturada"] != DBNull.Value
                                               && (decimal)rd["CantidadCapturada"] != cantBase;
                            result.Add(new
                            {
                                Codigo              = rd.GetString(0),
                                Descripcion         = rd.GetString(1),
                                Unidad              = rd.GetString(2),
                                Cantidad            = (double)cantBase,
                                CantidadCapturada   = rd["CantidadCapturada"] == DBNull.Value ? (double?)null : (double)(decimal)rd["CantidadCapturada"],
                                UnidadCapturaNombre = rd["UnidadCapturaNombre"] == DBNull.Value ? null : rd.GetString(6),
                                TuvoConversion      = tuvo
                            });
                        }
                    }
                }
            }
            return result;
        }
    }
}

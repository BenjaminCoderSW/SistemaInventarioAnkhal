using GrupoAnkhalInventario.Modelo;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace GrupoAnkhalInventario.Services
{
    // ── Parámetros de filtro compartidos por todos los métodos ────────────────

    public class FiltrosInventario
    {
        public List<int> BasesUsuario { get; set; }  // null = Administrador (sin restricción)
        public int? BaseFiltraID { get; set; }
        public string TipoFiltro { get; set; } = "";  // "", "MAT", "PROD"
        public string BuscarMat { get; set; }
        public string BuscarProd { get; set; }
        public bool SoloConExistencia { get; set; }
    }

    // ── View models ───────────────────────────────────────────────────────────

    public class MaterialInvVM
    {
        public int MaterialID { get; set; }
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public string TipoNombre { get; set; }
        public string Unidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal StockMinimo { get; set; }
        public decimal StockMaximo { get; set; }
        public decimal StockOptimo { get; set; }
        public decimal StockGlobal { get; set; }
        public decimal MermaGlobal { get; set; }
        public List<StockBaseInvVM> StockBases { get; set; }
    }

    public class StockBaseInvVM
    {
        public int BaseID { get; set; }
        public string BaseNombre { get; set; }
        public string BaseCodigo { get; set; }
        public decimal Cantidad { get; set; }
        public decimal MermaBase { get; set; }
    }

    public class ProductoInvVM
    {
        public int ProductoID { get; set; }
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public string TipoNombre { get; set; }
        public decimal PrecioVenta { get; set; }
        public int TotalBuenos { get; set; }
        public int TotalRechazo { get; set; }
        public List<StockProdBaseVM> StockBases { get; set; }
    }

    public class StockProdBaseVM
    {
        public int BaseID { get; set; }
        public string BaseNombre { get; set; }
        public string BaseCodigo { get; set; }
        public int Buenos { get; set; }
        public int Rechazo { get; set; }
    }

    public class ResumenBaseVM
    {
        public int BaseID { get; set; }
        public string BaseNombre { get; set; }
        public decimal ValorMateriales { get; set; }
        public decimal ValorBuenos { get; set; }
        public decimal ValorRechazo { get; set; }
        public decimal ValorMerma { get; set; }
    }

    public class KpisVM
    {
        public decimal ValorMateriales { get; set; }
        public decimal ValorBuenos { get; set; }
        public decimal ValorRechazo { get; set; }
        public decimal ValorMerma { get; set; }
        public decimal ValorTotal => ValorMateriales + ValorBuenos + ValorRechazo + ValorMerma;
    }

    // Filas planas para InventarioPorBase (una fila por combinación ítem×base)
    public class DetalleBaseMatVM
    {
        public int MaterialID { get; set; }
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public string TipoNombre { get; set; }
        public string BaseNombre { get; set; }
        public decimal Cantidad { get; set; }
        public string Unidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal ValorItem => Cantidad * PrecioUnitario;
    }

    public class DetalleBaseProdVM
    {
        public int ProductoID { get; set; }
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public string TipoNombre { get; set; }
        public string BaseNombre { get; set; }
        public int Buenos { get; set; }
        public int Rechazo { get; set; }
        public decimal PrecioVenta { get; set; }
        public decimal ValorBuenos => Buenos * PrecioVenta;
        public decimal ValorRechazo => Rechazo * (PrecioVenta * 0.5m);
    }

    // ── Servicio ──────────────────────────────────────────────────────────────

    public class InventarioService
    {
        private readonly string _connStr;

        public InventarioService(string connStr) { _connStr = connStr; }

        private InventarioAnkhalDBDataContext NuevoDb(bool tracking = true)
        {
            var ctx = new InventarioAnkhalDBDataContext(_connStr);
            ctx.ObjectTrackingEnabled = tracking;
            return ctx;
        }

        // ── Helpers ADO.NET para StockMerma (tabla no mapeada en DBML) ────────

        private void AgregarFiltroMerma(List<string> where, List<SqlParameter> parms,
            List<int> basesUsuario, int? baseFiltraID)
        {
            if (basesUsuario != null && basesUsuario.Count > 0)
            {
                var pNames = basesUsuario
                    .Select((id, i) => { parms.Add(new SqlParameter("@bu" + i, id)); return "@bu" + i; })
                    .ToList();
                where.Add("sm.BaseID IN (" + string.Join(",", pNames) + ")");
            }
            if (baseFiltraID.HasValue)
            {
                where.Add("sm.BaseID = @baseFiltraID");
                parms.Add(new SqlParameter("@baseFiltraID", baseFiltraID.Value));
            }
        }

        // materialID → { baseID → cantidadMerma }
        private Dictionary<int, Dictionary<int, decimal>> ObtenerMermaDict(
            List<int> basesUsuario, int? baseFiltraID)
        {
            var dict = new Dictionary<int, Dictionary<int, decimal>>();
            var where = new List<string> { "b.Activo = 1", "sm.CantidadActual > 0" };
            var parms = new List<SqlParameter>();
            AgregarFiltroMerma(where, parms, basesUsuario, baseFiltraID);

            string sql = "SELECT sm.MaterialID, sm.BaseID, sm.CantidadActual " +
                         "FROM dbo.StockMerma sm INNER JOIN dbo.Bases b ON b.BaseID = sm.BaseID " +
                         "WHERE " + string.Join(" AND ", where);

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddRange(parms.ToArray());
                    using (var rd = cmd.ExecuteReader())
                        while (rd.Read())
                        {
                            int mat = rd.GetInt32(0), bas = rd.GetInt32(1);
                            if (!dict.ContainsKey(mat)) dict[mat] = new Dictionary<int, decimal>();
                            dict[mat][bas] = rd.GetDecimal(2);
                        }
                }
            }
            return dict;
        }

        // baseID → valor merma (cantidad × precio)
        private Dictionary<int, decimal> ObtenerValorMermaPorBase(
            List<int> basesUsuario, int? baseFiltraID)
        {
            var dict = new Dictionary<int, decimal>();
            var where = new List<string> { "b.Activo = 1" };
            var parms = new List<SqlParameter>();
            AgregarFiltroMerma(where, parms, basesUsuario, baseFiltraID);

            string sql =
                "SELECT sm.BaseID, SUM(sm.CantidadActual * m.PrecioUnitario) " +
                "FROM dbo.StockMerma sm " +
                "INNER JOIN dbo.Materiales m ON m.MaterialID = sm.MaterialID " +
                "INNER JOIN dbo.Bases b ON b.BaseID = sm.BaseID " +
                "WHERE " + string.Join(" AND ", where) + " GROUP BY sm.BaseID";

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddRange(parms.ToArray());
                    using (var rd = cmd.ExecuteReader())
                        while (rd.Read()) dict[rd.GetInt32(0)] = rd.GetDecimal(1);
                }
            }
            return dict;
        }

        // Escalar: suma total de (cantidadMerma × precioUnitario) en el scope del filtro
        private decimal ObtenerValorMermaTotal(List<int> basesUsuario, int? baseFiltraID)
        {
            var where = new List<string> { "b.Activo = 1" };
            var parms = new List<SqlParameter>();
            AgregarFiltroMerma(where, parms, basesUsuario, baseFiltraID);

            string sql = "SELECT ISNULL(SUM(sm.CantidadActual * m.PrecioUnitario),0) " +
                         "FROM dbo.StockMerma sm " +
                         "INNER JOIN dbo.Materiales m ON m.MaterialID = sm.MaterialID " +
                         "INNER JOIN dbo.Bases b ON b.BaseID = sm.BaseID " +
                         "WHERE " + string.Join(" AND ", where);

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddRange(parms.ToArray());
                    return (decimal)cmd.ExecuteScalar();
                }
            }
        }

        // ── Métodos públicos ──────────────────────────────────────────────────

        /// <summary>
        /// Lista completa de materiales con su stock consolidado y por base.
        /// Los callers paginan con Skip/Take sobre la lista devuelta.
        /// Aplica filtro de texto (BuscarMat) en SQL antes del ToList().
        /// Aplica SoloConExistencia en memoria (necesita StockGlobal calculado primero).
        /// </summary>
        public List<MaterialInvVM> ObtenerMateriales(FiltrosInventario f)
        {
            using (var db = NuevoDb(tracking: false))
            {
                string buscar = (f.BuscarMat ?? "").Trim();

                var queryMat = from m in db.Materiales
                               join tp in db.TiposMaterial on m.TipoMaterialID equals tp.TipoMaterialID
                               where m.Activo
                               select new
                               {
                                   m.MaterialID, m.Codigo,
                                   Descripcion = m.Descripcion,
                                   TipoNombre = tp.Nombre,
                                   m.Unidad, m.PrecioUnitario,
                                   StockMinimo = m.StockMinimo,
                                   StockMaximo = m.StockMaximo,
                                   m.StockOptimo
                               };

                if (!string.IsNullOrEmpty(buscar))
                    queryMat = queryMat.Where(m => m.Codigo.Contains(buscar) || m.Descripcion.Contains(buscar));

                queryMat = queryMat.OrderBy(m => m.Codigo);
                var listaMat = queryMat.ToList();

                var stockQuery = from sm in db.StockMateriales
                                 join b in db.Bases on sm.BaseID equals b.BaseID
                                 where b.Activo
                                 select new { sm.MaterialID, b.BaseID, b.Nombre, b.Codigo, sm.CantidadActual };

                if (f.BasesUsuario != null)
                    stockQuery = stockQuery.Where(s => f.BasesUsuario.Contains(s.BaseID));
                if (f.BaseFiltraID.HasValue)
                    stockQuery = stockQuery.Where(s => s.BaseID == f.BaseFiltraID.Value);

                var stockTodos = stockQuery.ToList();
                var mermaDict = ObtenerMermaDict(f.BasesUsuario, f.BaseFiltraID);

                // Pre-cargar catálogo de bases activas para bases que solo tienen merma
                var basesInfo = db.Bases
                    .Where(b => b.Activo)
                    .Select(b => new { b.BaseID, b.Nombre, b.Codigo })
                    .ToList()
                    .ToDictionary(b => b.BaseID);

                var vms = new List<MaterialInvVM>();
                foreach (var m in listaMat)
                {
                    var basesStock = stockTodos
                        .Where(s => s.MaterialID == m.MaterialID)
                        .Select(s => new StockBaseInvVM
                        {
                            BaseID = s.BaseID,
                            BaseNombre = s.Nombre,
                            BaseCodigo = s.Codigo,
                            Cantidad = s.CantidadActual,
                            MermaBase = mermaDict.ContainsKey(m.MaterialID) && mermaDict[m.MaterialID].ContainsKey(s.BaseID)
                                        ? mermaDict[m.MaterialID][s.BaseID] : 0m
                        }).ToList();

                    // Agregar bases que solo tienen merma (sin fila en StockMateriales)
                    if (mermaDict.ContainsKey(m.MaterialID))
                    {
                        foreach (var kvp in mermaDict[m.MaterialID])
                        {
                            if (!basesStock.Any(b => b.BaseID == kvp.Key) && basesInfo.ContainsKey(kvp.Key))
                            {
                                var bi = basesInfo[kvp.Key];
                                basesStock.Add(new StockBaseInvVM
                                {
                                    BaseID = kvp.Key,
                                    BaseNombre = bi.Nombre,
                                    BaseCodigo = bi.Codigo,
                                    Cantidad = 0m,
                                    MermaBase = kvp.Value
                                });
                            }
                        }
                    }

                    decimal global = basesStock.Sum(s => s.Cantidad);
                    decimal mermaGlobal = mermaDict.ContainsKey(m.MaterialID)
                        ? mermaDict[m.MaterialID].Values.Sum() : 0m;

                    if (!f.BaseFiltraID.HasValue || global > 0 || basesStock.Any())
                    {
                        vms.Add(new MaterialInvVM
                        {
                            MaterialID = m.MaterialID,
                            Codigo = m.Codigo,
                            Descripcion = m.Descripcion,
                            TipoNombre = m.TipoNombre,
                            Unidad = m.Unidad,
                            PrecioUnitario = m.PrecioUnitario,
                            StockMinimo = m.StockMinimo,
                            StockMaximo = m.StockMaximo,
                            StockOptimo = m.StockOptimo,
                            StockGlobal = global,
                            MermaGlobal = mermaGlobal,
                            StockBases = basesStock
                        });
                    }
                }

                if (f.SoloConExistencia)
                    vms = vms.Where(m => m.StockGlobal > 0).ToList();

                return vms;
            }
        }

        /// <summary>
        /// Lista completa de productos con su stock consolidado y por base.
        /// Los callers paginan con Skip/Take sobre la lista devuelta.
        /// Aplica filtro de texto (BuscarProd) en SQL antes del ToList().
        /// </summary>
        public List<ProductoInvVM> ObtenerProductos(FiltrosInventario f)
        {
            using (var db = NuevoDb(tracking: false))
            {
                string buscar = (f.BuscarProd ?? "").Trim();

                var queryProd = from p in db.Productos
                                join tp in db.TiposProducto on p.TipoProductoID equals tp.TipoProductoID
                                where p.Activo
                                select new
                                {
                                    p.ProductoID, p.Codigo,
                                    Descripcion = p.Descripcion,
                                    TipoNombre = tp.Nombre,
                                    p.PrecioVenta
                                };

                if (!string.IsNullOrEmpty(buscar))
                    queryProd = queryProd.Where(p => p.Codigo.Contains(buscar) || p.Descripcion.Contains(buscar));

                queryProd = queryProd.OrderBy(p => p.Codigo);
                var listaProd = queryProd.ToList();

                var stockProdQuery = from sp in db.StockProductos
                                     join b in db.Bases on sp.BaseID equals b.BaseID
                                     where b.Activo
                                     select new { sp.ProductoID, b.BaseID, b.Nombre, b.Codigo, sp.CantidadBuenas, sp.CantidadRechazo };

                if (f.BasesUsuario != null)
                    stockProdQuery = stockProdQuery.Where(s => f.BasesUsuario.Contains(s.BaseID));
                if (f.BaseFiltraID.HasValue)
                    stockProdQuery = stockProdQuery.Where(s => s.BaseID == f.BaseFiltraID.Value);

                var stockProdTodos = stockProdQuery.ToList();

                var vms = new List<ProductoInvVM>();
                foreach (var p in listaProd)
                {
                    var basesStock = stockProdTodos
                        .Where(s => s.ProductoID == p.ProductoID)
                        .Select(s => new StockProdBaseVM
                        {
                            BaseID = s.BaseID,
                            BaseNombre = s.Nombre,
                            BaseCodigo = s.Codigo,
                            Buenos = s.CantidadBuenas,
                            Rechazo = s.CantidadRechazo
                        }).ToList();

                    int totalBuenos = basesStock.Sum(s => s.Buenos);
                    int totalRechazo = basesStock.Sum(s => s.Rechazo);

                    if (!f.BaseFiltraID.HasValue || basesStock.Any())
                    {
                        vms.Add(new ProductoInvVM
                        {
                            ProductoID = p.ProductoID,
                            Codigo = p.Codigo,
                            Descripcion = p.Descripcion,
                            TipoNombre = p.TipoNombre,
                            PrecioVenta = p.PrecioVenta,
                            TotalBuenos = totalBuenos,
                            TotalRechazo = totalRechazo,
                            StockBases = basesStock
                        });
                    }
                }

                if (f.SoloConExistencia)
                    vms = vms.Where(p => p.TotalBuenos > 0 || p.TotalRechazo > 0).ToList();

                return vms;
            }
        }

        /// <summary>
        /// Resumen de valor por base/planta (sin filtro de texto — siempre totales globales de la base).
        /// </summary>
        public List<ResumenBaseVM> ObtenerResumenPorBase(FiltrosInventario f)
        {
            using (var db = NuevoDb(tracking: false))
            {
                var basesQuery = db.Bases.Where(b => b.Activo).AsQueryable();
                if (f.BasesUsuario != null)
                    basesQuery = basesQuery.Where(b => f.BasesUsuario.Contains(b.BaseID));
                if (f.BaseFiltraID.HasValue)
                    basesQuery = basesQuery.Where(b => b.BaseID == f.BaseFiltraID.Value);
                var bases = basesQuery.OrderBy(b => b.Nombre).ToList();

                var stockMats = (from sm in db.StockMateriales
                                 join m in db.Materiales on sm.MaterialID equals m.MaterialID
                                 select new { sm.BaseID, Valor = sm.CantidadActual * m.PrecioUnitario }).ToList();

                var stockProds = (from sp in db.StockProductos
                                  join p in db.Productos on sp.ProductoID equals p.ProductoID
                                  select new
                                  {
                                      sp.BaseID,
                                      ValorBuenos = sp.CantidadBuenas * p.PrecioVenta,
                                      ValorRechazo = sp.CantidadRechazo * (p.PrecioVenta * 0.5m)
                                  }).ToList();

                var valorMermaBase = ObtenerValorMermaPorBase(f.BasesUsuario, f.BaseFiltraID);

                var resumen = new List<ResumenBaseVM>();
                foreach (var b in bases)
                {
                    resumen.Add(new ResumenBaseVM
                    {
                        BaseID = b.BaseID,
                        BaseNombre = b.Nombre,
                        ValorMateriales = stockMats.Where(s => s.BaseID == b.BaseID).Sum(s => s.Valor),
                        ValorBuenos = stockProds.Where(s => s.BaseID == b.BaseID).Sum(s => s.ValorBuenos),
                        ValorRechazo = stockProds.Where(s => s.BaseID == b.BaseID).Sum(s => s.ValorRechazo),
                        ValorMerma = valorMermaBase.ContainsKey(b.BaseID) ? valorMermaBase[b.BaseID] : 0m
                    });
                }
                return resumen;
            }
        }

        /// <summary>
        /// KPIs de valor total para las cards del dashboard.
        /// No aplica BuscarMat/BuscarProd/SoloConExistencia — siempre muestra totales de la base.
        /// </summary>
        public KpisVM ObtenerKpis(FiltrosInventario f)
        {
            using (var db = NuevoDb(tracking: false))
            {
                var smQuery = from sm in db.StockMateriales
                              join m in db.Materiales on sm.MaterialID equals m.MaterialID
                              join b in db.Bases on sm.BaseID equals b.BaseID
                              where b.Activo
                              select new { sm.BaseID, Valor = sm.CantidadActual * m.PrecioUnitario };

                if (f.BasesUsuario != null) smQuery = smQuery.Where(s => f.BasesUsuario.Contains(s.BaseID));
                if (f.BaseFiltraID.HasValue) smQuery = smQuery.Where(s => s.BaseID == f.BaseFiltraID.Value);

                decimal valMat = smQuery.Sum(s => (decimal?)s.Valor) ?? 0m;

                var spQuery = from sp in db.StockProductos
                              join p in db.Productos on sp.ProductoID equals p.ProductoID
                              join b in db.Bases on sp.BaseID equals b.BaseID
                              where b.Activo
                              select new
                              {
                                  sp.BaseID,
                                  ValBuenos = sp.CantidadBuenas * p.PrecioVenta,
                                  ValRechazo = sp.CantidadRechazo * (p.PrecioVenta * 0.5m)
                              };

                if (f.BasesUsuario != null) spQuery = spQuery.Where(s => f.BasesUsuario.Contains(s.BaseID));
                if (f.BaseFiltraID.HasValue) spQuery = spQuery.Where(s => s.BaseID == f.BaseFiltraID.Value);

                decimal valBuenos = spQuery.Sum(s => (decimal?)s.ValBuenos) ?? 0m;
                decimal valRechazo = spQuery.Sum(s => (decimal?)s.ValRechazo) ?? 0m;
                decimal valMerma = ObtenerValorMermaTotal(f.BasesUsuario, f.BaseFiltraID);

                return new KpisVM
                {
                    ValorMateriales = valMat,
                    ValorBuenos = valBuenos,
                    ValorRechazo = valRechazo,
                    ValorMerma = valMerma
                };
            }
        }

        /// <summary>
        /// Lista plana de (material × base) para InventarioPorBase.aspx.
        /// Aplica BuscarMat y SoloConExistencia en SQL (antes de ToList).
        /// Los callers paginan con Skip/Take.
        /// </summary>
        public List<DetalleBaseMatVM> ObtenerMaterialesPorBase(FiltrosInventario f)
        {
            using (var db = NuevoDb(tracking: false))
            {
                string buscar = (f.BuscarMat ?? "").Trim();

                var query = from sm in db.StockMateriales
                            join m in db.Materiales on sm.MaterialID equals m.MaterialID
                            join tp in db.TiposMaterial on m.TipoMaterialID equals tp.TipoMaterialID
                            join b in db.Bases on sm.BaseID equals b.BaseID
                            where m.Activo && b.Activo
                            select new
                            {
                                m.MaterialID, sm.BaseID, m.Codigo,
                                Descripcion = m.Descripcion,
                                TipoNombre = tp.Nombre,
                                BaseNombre = b.Nombre,
                                Cantidad = sm.CantidadActual,
                                m.Unidad, m.PrecioUnitario
                            };

                if (f.BasesUsuario != null)
                    query = query.Where(x => f.BasesUsuario.Contains(x.BaseID));
                if (f.BaseFiltraID.HasValue)
                    query = query.Where(x => x.BaseID == f.BaseFiltraID.Value);
                if (!string.IsNullOrEmpty(buscar))
                    query = query.Where(x => x.Codigo.Contains(buscar) || x.Descripcion.Contains(buscar));
                if (f.SoloConExistencia)
                    query = query.Where(x => x.Cantidad > 0);

                return query.OrderBy(x => x.Codigo).ThenBy(x => x.BaseNombre)
                    .Select(x => new DetalleBaseMatVM
                    {
                        MaterialID = x.MaterialID,
                        Codigo = x.Codigo,
                        Descripcion = x.Descripcion,
                        TipoNombre = x.TipoNombre,
                        BaseNombre = x.BaseNombre,
                        Cantidad = x.Cantidad,
                        Unidad = x.Unidad,
                        PrecioUnitario = x.PrecioUnitario
                    }).ToList();
            }
        }

        /// <summary>
        /// Lista plana de (producto × base) para InventarioPorBase.aspx.
        /// Aplica BuscarProd y SoloConExistencia en SQL (antes de ToList).
        /// Los callers paginan con Skip/Take.
        /// </summary>
        public List<DetalleBaseProdVM> ObtenerProductosPorBase(FiltrosInventario f)
        {
            using (var db = NuevoDb(tracking: false))
            {
                string buscar = (f.BuscarProd ?? "").Trim();

                var query = from sp in db.StockProductos
                            join p in db.Productos on sp.ProductoID equals p.ProductoID
                            join tp in db.TiposProducto on p.TipoProductoID equals tp.TipoProductoID
                            join b in db.Bases on sp.BaseID equals b.BaseID
                            where p.Activo && b.Activo
                            select new
                            {
                                p.ProductoID, sp.BaseID, p.Codigo,
                                Descripcion = p.Descripcion,
                                TipoNombre = tp.Nombre,
                                BaseNombre = b.Nombre,
                                Buenos = sp.CantidadBuenas,
                                Rechazo = sp.CantidadRechazo,
                                p.PrecioVenta
                            };

                if (f.BasesUsuario != null)
                    query = query.Where(x => f.BasesUsuario.Contains(x.BaseID));
                if (f.BaseFiltraID.HasValue)
                    query = query.Where(x => x.BaseID == f.BaseFiltraID.Value);
                if (!string.IsNullOrEmpty(buscar))
                    query = query.Where(x => x.Codigo.Contains(buscar) || x.Descripcion.Contains(buscar));
                if (f.SoloConExistencia)
                    query = query.Where(x => x.Buenos > 0 || x.Rechazo > 0);

                return query.OrderBy(x => x.Codigo).ThenBy(x => x.BaseNombre)
                    .Select(x => new DetalleBaseProdVM
                    {
                        ProductoID = x.ProductoID,
                        Codigo = x.Codigo,
                        Descripcion = x.Descripcion,
                        TipoNombre = x.TipoNombre,
                        BaseNombre = x.BaseNombre,
                        Buenos = x.Buenos,
                        Rechazo = x.Rechazo,
                        PrecioVenta = x.PrecioVenta
                    }).ToList();
            }
        }

        /// <summary>
        /// Devuelve el nombre de una base para encabezados de impresión/Excel.
        /// Retorna "Todas las bases" si baseID es null.
        /// </summary>
        public string ObtenerNombreBase(int? baseID)
        {
            if (!baseID.HasValue) return "Todas las bases";
            using (var db = NuevoDb(tracking: false))
            {
                var b = db.Bases.FirstOrDefault(x => x.BaseID == baseID.Value);
                return b != null ? b.Nombre : "Todas las bases";
            }
        }
    }
}

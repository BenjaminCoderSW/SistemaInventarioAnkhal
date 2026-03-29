//------------------------------------------------------------------------------
// <generado automáticamente>
//     Este código fue generado por una herramienta.
//
//     Los cambios en este archivo podrían causar un comportamiento incorrecto y se perderán si
//     se vuelve a generar el código.
// </generado automáticamente>
//------------------------------------------------------------------------------

namespace GrupoAnkhalInventario
{
    public partial class Default
    {
        protected global::System.Web.UI.WebControls.HiddenField hdnMensajePendiente;

        // Header
        protected global::System.Web.UI.WebControls.Label lblNombreUsuario;
        protected global::System.Web.UI.WebControls.Label lblRol;
        protected global::System.Web.UI.WebControls.Label lblFechaHora;

        // Filtros
        protected global::System.Web.UI.WebControls.DropDownList ddlBase;
        protected global::System.Web.UI.WebControls.DropDownList ddlPeriodo;
        protected global::System.Web.UI.WebControls.TextBox txtDesde;
        protected global::System.Web.UI.WebControls.TextBox txtHasta;
        protected global::System.Web.UI.WebControls.Button btnFiltrar;

        // KPI Row 1 — Inventario / Produccion / Entregas
        protected global::System.Web.UI.WebControls.Label lblValorInventario;
        protected global::System.Web.UI.WebControls.Label lblInvSub;
        protected global::System.Web.UI.WebControls.Label lblPeriodoA;

        protected global::System.Web.UI.WebControls.Label lblValorProducido;
        protected global::System.Web.UI.WebControls.Label lblProdSub;
        protected global::System.Web.UI.WebControls.Label lblPeriodoB;

        protected global::System.Web.UI.WebControls.Label lblValorEntregado;
        protected global::System.Web.UI.WebControls.Label lblEntSub;
        protected global::System.Web.UI.WebControls.Label lblPeriodoC;

        // KPI Row 2 — Costo / Margen / Alertas
        protected global::System.Web.UI.WebControls.Label lblCostoMaterial;
        protected global::System.Web.UI.WebControls.Label lblPeriodoD;

        protected global::System.Web.UI.WebControls.Panel pnlMargenCard;
        protected global::System.Web.UI.WebControls.Label lblMargenDia;
        protected global::System.Web.UI.WebControls.Label lblMargenPct;

        protected global::System.Web.UI.WebControls.Panel pnlAlertasCard;
        protected global::System.Web.UI.WebControls.Label lblCriticosCount;
        protected global::System.Web.UI.WebControls.Label lblCriticosSub;

        // Tabla Produccion
        protected global::System.Web.UI.WebControls.Label lblTituloProd;
        protected global::System.Web.UI.WebControls.Label lblTotalProdRows;
        protected global::System.Web.UI.WebControls.GridView gvProduccion;

        // Panel Criticos
        protected global::System.Web.UI.WebControls.Label lblCountCriticosPanel;
        protected global::System.Web.UI.WebControls.Literal litCriticos;

        // Ultimas Entregas
        protected global::System.Web.UI.WebControls.GridView gvUltimasEntregas;

        // Valor por Base
        protected global::System.Web.UI.WebControls.GridView gvValorPorBase;
    }
}

<%@ Page Title="Metas de Producción" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="MetasProduccion.aspx.cs" Inherits="GrupoAnkhalInventario.MetasProduccion" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="css/gridviewPantalla.css" rel="stylesheet" />
    <style>
        /* ── Dashboard cards ── */
        .stock-dashboard {
            display: flex;
            gap: 14px;
            margin-bottom: 18px;
            flex-wrap: wrap;
        }
        .stock-card {
            flex: 1;
            min-width: 200px;
            border-radius: 10px;
            padding: 16px 20px;
            color: #fff;
            display: flex;
            align-items: center;
            gap: 14px;
            box-shadow: 0 3px 10px rgba(0,0,0,0.15);
            transition: transform .15s, box-shadow .15s;
        }
        .stock-card:hover { transform: translateY(-3px); box-shadow: 0 6px 16px rgba(0,0,0,0.2); }
        .stock-card.meta  { background: linear-gradient(135deg,#6c3483,#8e44ad); }
        .stock-card.cumpl { background: linear-gradient(135deg,#1e8449,#27ae60); }
        .stock-card .icon      { font-size: 2.2rem; opacity: .9; }
        .stock-card .info .num { font-size: 2rem; font-weight: 700; line-height: 1; }
        .stock-card .info .lbl { font-size: .78rem; opacity: .9; text-transform: uppercase; letter-spacing: .5px; }

        /* ── Filtros ── */
        .filtros-bar {
            background: #f8f9fa; border: 1px solid #dee2e6;
            border-radius: 8px; padding: 14px 18px; margin-bottom: 14px;
        }
        .filtros-bar label { font-weight: 600; font-size: .84rem; color: #003366; margin-bottom: 2px; }
        .btn-filtro-rapido { border-radius: 20px; font-size: .82rem; padding: 4px 14px; margin-right: 4px; }
        .btn-filtro-rapido.active { background: #003366; color: #fff; }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
<div class="container-fluid">
<div class="row">
<div class="col-12">

    <!-- ══ DASHBOARD ══ -->
    <div class="stock-dashboard">
        <div class="stock-card meta">
            <div class="icon"><i class="fas fa-bullseye"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblMetaTotal" runat="server" Text="$0.00"></asp:Label></div>
                <div class="lbl"><asp:Label ID="lblCardMeta" runat="server" Text="META DEL PERÍODO — ANKHAL"></asp:Label></div>
            </div>
        </div>
        <div class="stock-card cumpl">
            <div class="icon"><i class="fas fa-chart-bar"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblCumplimiento" runat="server" Text="0%"></asp:Label></div>
                <div class="lbl">Cumplimiento General del Per&iacute;odo</div>
            </div>
        </div>
    </div>

    <!-- ══ FILTROS ══ -->
    <div class="filtros-bar">
        <div class="row align-items-end">
            <div class="col-auto">
                <label>Per&iacute;odo r&aacute;pido</label><br />
                <button type="button" class="btn btn-outline-secondary btn-sm btn-filtro-rapido" onclick="setFiltroRapido('hoy')">Hoy</button>
                <button type="button" class="btn btn-outline-secondary btn-sm btn-filtro-rapido" onclick="setFiltroRapido('semana')">Esta Semana</button>
                <button type="button" class="btn btn-outline-secondary btn-sm btn-filtro-rapido" onclick="setFiltroRapido('mes')">Este Mes</button>
            </div>
            <div class="col-auto">
                <label>Desde</label>
                <asp:TextBox ID="txtDesde" runat="server" CssClass="form-control form-control-sm" TextMode="Date"></asp:TextBox>
            </div>
            <div class="col-auto">
                <label>Hasta</label>
                <asp:TextBox ID="txtHasta" runat="server" CssClass="form-control form-control-sm" TextMode="Date"></asp:TextBox>
            </div>
            <div class="col-auto">
                <asp:Button ID="btnBuscar" runat="server" Text="Buscar" CssClass="btn btn-primary btn-sm" OnClick="btnBuscar_Click" />
                <asp:Button ID="btnLimpiar" runat="server" Text="Limpiar" CssClass="btn btn-outline-secondary btn-sm ml-1" OnClick="btnLimpiar_Click" />
            </div>
        </div>
    </div>

    <!-- ══ TABLA POR BASE ══ -->
    <div class="card">
        <div class="card-header">
            <h3 class="card-title">
                <i class="fas fa-flag-checkered mr-1"></i>
                Cumplimiento de metas por base
            </h3>
            <div class="card-tools">
                <small class="text-muted">
                    Per&iacute;odo:
                    <strong><asp:Label ID="lblPeriodo" runat="server"></asp:Label></strong>
                    &nbsp;&mdash;&nbsp;
                    <asp:Label ID="lblNumDias" runat="server"></asp:Label>
                </small>
            </div>
        </div>
        <div class="card-body p-0">
            <div class="table-responsive">
                <table class="table table-bordered table-striped custom-grid mb-0">
                    <thead>
                        <tr>
                            <th>Base</th>
                            <th>Tipo</th>
                            <th class="text-right">Meta diaria ($)</th>
                            <th class="text-right">Meta del per&iacute;odo ($)</th>
                            <th class="text-right">Producido ($)</th>
                            <th class="text-right" style="min-width:160px;">Cumplimiento</th>
                            <th class="text-center">Estatus</th>
                        </tr>
                    </thead>
                    <tbody>
                        <asp:Repeater ID="rptMetaBases" runat="server">
                            <ItemTemplate>
                                <tr>
                                    <td><strong><%# Eval("BaseNombre") %></strong></td>
                                    <td><span class="badge badge-secondary"><%# Eval("BaseTipo") %></span></td>
                                    <td class="text-right text-muted"><%# string.Format("{0:$#,##0.00}", Eval("MetaDiaria")) %></td>
                                    <td class="text-right"><%# string.Format("{0:$#,##0.00}", Eval("MetaPeriodo")) %></td>
                                    <td class="text-right"><%# string.Format("{0:$#,##0.00}", Eval("ValorPeriodo")) %></td>
                                    <td class="text-right">
                                        <div class="progress" style="height:20px;">
                                            <div class="progress-bar <%# (int)Eval("CumplPct") >= 100 ? "bg-success" : (int)Eval("CumplPct") >= 70 ? "bg-warning" : "bg-danger" %>"
                                                 style="width:<%# Math.Min((int)Eval("CumplPct"), 100) %>%">
                                                <%# Eval("CumplPct") %>%
                                            </div>
                                        </div>
                                    </td>
                                    <td class="text-center">
                                        <%# (bool)Eval("Cumplio")
                                            ? "<span class='badge badge-success'><i class='fas fa-check mr-1'></i>Cumpli&oacute;</span>"
                                            : "<span class='badge badge-danger'><i class='fas fa-times mr-1'></i>Pendiente</span>" %>
                                    </td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>
            </div>
        </div>
    </div>

</div>
</div>
</div>

<script>
    function setFiltroRapido(periodo) {
        var hoy   = new Date();
        var desde, hasta;

        if (periodo === 'hoy') {
            desde = hasta = formatDate(hoy);
        } else if (periodo === 'semana') {
            var lunes = new Date(hoy);
            lunes.setDate(hoy.getDate() - ((hoy.getDay() + 6) % 7));
            desde = formatDate(lunes);
            hasta = formatDate(hoy);
        } else if (periodo === 'mes') {
            desde = formatDate(new Date(hoy.getFullYear(), hoy.getMonth(), 1));
            hasta = formatDate(hoy);
        }

        document.getElementById('<%= txtDesde.ClientID %>').value = desde;
        document.getElementById('<%= txtHasta.ClientID %>').value = hasta;

        document.querySelectorAll('.btn-filtro-rapido').forEach(function(b) { b.classList.remove('active'); });
        event.target.classList.add('active');
    }

    function formatDate(d) {
        var mm = String(d.getMonth() + 1).padStart(2, '0');
        var dd = String(d.getDate()).padStart(2, '0');
        return d.getFullYear() + '-' + mm + '-' + dd;
    }
</script>
</asp:Content>

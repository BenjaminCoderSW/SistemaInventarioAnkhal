<%@ Page Title="Dashboard" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="GrupoAnkhalInventario.Default" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        /* ── Header bienvenida ──────────────────────────── */
        .dash-header {
            background: linear-gradient(135deg, #0d2d5e 0%, #1a5276 100%);
            color: #fff;
            border-radius: 10px;
            padding: 16px 24px;
            margin-bottom: 18px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            flex-wrap: wrap;
            gap: 10px;
            box-shadow: 0 3px 12px rgba(0,0,0,0.2);
        }
        .dash-header .welcome-info h4 { margin: 0; font-size: 1.15rem; font-weight: 700; }
        .dash-header .welcome-info p  { margin: 0; font-size: 0.82rem; opacity: 0.85; }
        .dash-header .filtros-inline { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
        .dash-header label { color: rgba(255,255,255,0.8); font-size: 0.78rem; margin-bottom: 2px; display: block; }

        /* ── KPI Cards ──────────────────────────────────── */
        .kpi-card {
            border-radius: 12px;
            padding: 18px 20px 14px 20px;
            color: #fff;
            box-shadow: 0 4px 14px rgba(0,0,0,0.18);
            position: relative;
            overflow: hidden;
            height: 100%;
            min-height: 120px;
        }
        .kpi-card .kpi-icon {
            position: absolute;
            right: 14px;
            top: 12px;
            font-size: 2.8rem;
            opacity: 0.18;
        }
        .kpi-card .kpi-label {
            font-size: 0.72rem;
            text-transform: uppercase;
            letter-spacing: 0.8px;
            opacity: 0.85;
            margin-bottom: 2px;
        }
        .kpi-card .kpi-value {
            font-size: 1.75rem;
            font-weight: 800;
            line-height: 1.1;
            margin-bottom: 4px;
        }
        .kpi-card .kpi-sub {
            font-size: 0.78rem;
            opacity: 0.8;
        }
        .kpi-card.inv     { background: linear-gradient(135deg, #0d2d5e, #1a5276); }
        .kpi-card.prod    { background: linear-gradient(135deg, #145a32, #1e8449); }
        .kpi-card.ventas  { background: linear-gradient(135deg, #154360, #1f618d); }
        .kpi-card.costo   { background: linear-gradient(135deg, #6e2f00, #d35400); }
        .kpi-card.margen  { background: linear-gradient(135deg, #0b6251, #17a589); }
        .kpi-card.margen-neg { background: linear-gradient(135deg, #7b241c, #c0392b); }
        .kpi-card.alertas { background: linear-gradient(135deg, #7b241c, #c0392b); }
        .kpi-card.alertas-ok { background: linear-gradient(135deg, #1d6a27, #27ae60); }

        /* ── Separadores de sección ─────────────────────── */
        .sec-title {
            background: #003366;
            color: #fff;
            padding: 7px 14px;
            border-radius: 6px 6px 0 0;
            font-size: 0.88rem;
            font-weight: 600;
            margin-top: 20px;
        }
        .sec-title i { margin-right: 6px; }
        .sec-card { border-radius: 0 0 6px 6px; border: 1px solid #dee2e6; border-top: none; }

        /* ── Tablas ─────────────────────────────────────── */
        .dash-table th { background: #003366 !important; color: #fff; font-size: 0.80rem; padding: 6px 8px; }
        .dash-table td { font-size: 0.82rem; padding: 5px 8px; vertical-align: middle; }
        .dash-table tfoot td { font-weight: 700; background: #e8ecf4; }

        /* ── Cumplimiento badge ──────────────────────────── */
        .cumpl-bar { background: #e0e0e0; border-radius: 20px; height: 8px; width: 80px; display:inline-block; vertical-align: middle; margin-right: 4px; }
        .cumpl-fill { height: 8px; border-radius: 20px; }
        .cumpl-100 { color: #1e8449; font-weight: 700; }
        .cumpl-ok  { color: #1f618d; }
        .cumpl-low { color: #d35400; }
        .cumpl-bad { color: #c0392b; font-weight: 700; }

        /* ── Estado badges ──────────────────────────────── */
        .badge-entregada  { background:#1e8449; color:#fff; padding:2px 7px; border-radius:10px; font-size:0.75rem; }
        .badge-programada { background:#1f618d; color:#fff; padding:2px 7px; border-radius:10px; font-size:0.75rem; }
        .badge-cancelada  { background:#7b241c; color:#fff; padding:2px 7px; border-radius:10px; font-size:0.75rem; }
        .badge-pendiente  { background:#7e5109; color:#fff; padding:2px 7px; border-radius:10px; font-size:0.75rem; }

        /* ── Criticos list ──────────────────────────────── */
        .critico-item {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 6px 10px;
            border-bottom: 1px solid #fde2e2;
            font-size: 0.82rem;
        }
        .critico-item:last-child { border-bottom: none; }
        .critico-item .mat-name { font-weight: 600; color: #922b21; }
        .critico-item .mat-stock { color: #666; }
        .critico-item .mat-deficit { color: #c0392b; font-weight: 700; }
        .critico-vacio { padding: 20px; text-align: center; color: #1e8449; font-size: 0.88rem; }

        /* ── Responsive ─────────────────────────────────── */
        @media (max-width: 768px) {
            .kpi-card .kpi-value { font-size: 1.3rem; }
            .dash-header { flex-direction: column; align-items: flex-start; }
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <asp:HiddenField ID="hdnMensajePendiente" runat="server" />

    <div class="container-fluid">

        <!-- ══ HEADER BIENVENIDA + FILTROS ═══════════════════════════════════ -->
        <div class="dash-header">
            <div class="welcome-info">
                <h4>
                    <i class="fas fa-tachometer-alt"></i>
                    Bienvenido, <asp:Label ID="lblNombreUsuario" runat="server" Text=""></asp:Label>
                </h4>
                <p>
                    <i class="fas fa-user-tag"></i> <asp:Label ID="lblRol" runat="server" Text=""></asp:Label>
                    &nbsp;&nbsp;
                    <i class="fas fa-calendar-alt"></i> <asp:Label ID="lblFechaHora" runat="server" Text=""></asp:Label>
                </p>
            </div>
            <div class="filtros-inline">
                <div>
                    <label>Base / Planta</label>
                    <asp:DropDownList ID="ddlBase" runat="server" CssClass="form-control form-control-sm" Style="min-width:130px;"></asp:DropDownList>
                </div>
                <div>
                    <label>Periodo</label>
                    <asp:DropDownList ID="ddlPeriodo" runat="server" CssClass="form-control form-control-sm">
                        <asp:ListItem Text="Hoy"          Value="hoy"          Selected="True" />
                        <asp:ListItem Text="Esta Semana"  Value="semana" />
                        <asp:ListItem Text="Este Mes"     Value="mes" />
                        <asp:ListItem Text="Personalizado" Value="personalizado" />
                    </asp:DropDownList>
                </div>
                <div>
                    <label>Desde</label>
                    <asp:TextBox ID="txtDesde" runat="server" TextMode="Date"
                        CssClass="form-control form-control-sm" Style="min-width:130px;" />
                </div>
                <div>
                    <label>Hasta</label>
                    <asp:TextBox ID="txtHasta" runat="server" TextMode="Date"
                        CssClass="form-control form-control-sm" Style="min-width:130px;" />
                </div>
                <div style="padding-top:17px;">
                    <asp:Button ID="btnFiltrar" runat="server" Text="Actualizar"
                        CssClass="btn btn-light btn-sm" OnClick="btnFiltrar_Click" />
                </div>
            </div>
        </div>

        <!-- ══ ROW 1 — KPI PRINCIPALES ═══════════════════════════════════════ -->
        <div class="row mb-3">
            <!-- Valor Inventario -->
            <div class="col-lg-4 col-md-6 mb-3">
                <div class="kpi-card inv h-100">
                    <i class="fas fa-warehouse kpi-icon"></i>
                    <div class="kpi-label">Valor Total en Inventario</div>
                    <div class="kpi-value">$<asp:Label ID="lblValorInventario" runat="server" Text="0.00"></asp:Label></div>
                    <div class="kpi-sub">
                        <asp:Label ID="lblInvSub" runat="server" Text=""></asp:Label>
                    </div>
                </div>
            </div>
            <!-- Valor Producido -->
            <div class="col-lg-4 col-md-6 mb-3">
                <div class="kpi-card prod h-100">
                    <i class="fas fa-industry kpi-icon"></i>
                    <div class="kpi-label">Valor Producido — <asp:Label ID="lblPeriodoA" runat="server" Text="Hoy"></asp:Label></div>
                    <div class="kpi-value">$<asp:Label ID="lblValorProducido" runat="server" Text="0.00"></asp:Label></div>
                    <div class="kpi-sub">
                        <asp:Label ID="lblProdSub" runat="server" Text=""></asp:Label>
                    </div>
                </div>
            </div>
            <!-- Valor Entregado -->
            <div class="col-lg-4 col-md-6 mb-3">
                <div class="kpi-card ventas h-100">
                    <i class="fas fa-truck kpi-icon"></i>
                    <div class="kpi-label">Valor Entregado / Vendido — <asp:Label ID="lblPeriodoB" runat="server" Text="Hoy"></asp:Label></div>
                    <div class="kpi-value">$<asp:Label ID="lblValorEntregado" runat="server" Text="0.00"></asp:Label></div>
                    <div class="kpi-sub">
                        <asp:Label ID="lblEntSub" runat="server" Text=""></asp:Label>
                    </div>
                </div>
            </div>
        </div>

        <!-- ══ ROW 2 — KPI FINANCIEROS ════════════════════════════════════════ -->
        <div class="row mb-3">
            <!-- Costo Material -->
            <div class="col-lg-4 col-md-6 mb-3">
                <div class="kpi-card costo h-100">
                    <i class="fas fa-box-open kpi-icon"></i>
                    <div class="kpi-label">Costo Materiales Consumidos — <asp:Label ID="lblPeriodoC" runat="server" Text="Hoy"></asp:Label></div>
                    <div class="kpi-value">$<asp:Label ID="lblCostoMaterial" runat="server" Text="0.00"></asp:Label></div>
                    <div class="kpi-sub">Materiales utilizados en produccion</div>
                </div>
            </div>
            <!-- Margen -->
            <div class="col-lg-4 col-md-6 mb-3">
                <asp:Panel ID="pnlMargenCard" runat="server" CssClass="kpi-card margen h-100">
                    <i class="fas fa-chart-line kpi-icon"></i>
                    <div class="kpi-label">Margen Bruto Estimado — <asp:Label ID="lblPeriodoD" runat="server" Text="Hoy"></asp:Label></div>
                    <div class="kpi-value">$<asp:Label ID="lblMargenDia" runat="server" Text="0.00"></asp:Label></div>
                    <div class="kpi-sub">
                        <asp:Label ID="lblMargenPct" runat="server" Text=""></asp:Label>
                        &nbsp; Ventas &minus; Costo Material
                    </div>
                </asp:Panel>
            </div>
            <!-- Alertas -->
            <div class="col-lg-4 col-md-6 mb-3">
                <asp:Panel ID="pnlAlertasCard" runat="server" CssClass="kpi-card alertas h-100">
                    <i class="fas fa-exclamation-triangle kpi-icon"></i>
                    <div class="kpi-label">Materiales Bajo Minimo</div>
                    <div class="kpi-value"><asp:Label ID="lblCriticosCount" runat="server" Text="0"></asp:Label></div>
                    <div class="kpi-sub">
                        <asp:Label ID="lblCriticosSub" runat="server" Text="Sin alertas activas"></asp:Label>
                    </div>
                </asp:Panel>
            </div>
        </div>

        <!-- ══ ROW 3 — PRODUCCIÓN + CRÍTICOS ═════════════════════════════════ -->
        <div class="row">
            <!-- Producción del período -->
            <div class="col-lg-8 mb-3">
                <div class="sec-title">
                    <i class="fas fa-industry"></i> Produccion por Base —
                    <asp:Label ID="lblTituloProd" runat="server" Text="Hoy"></asp:Label>
                    <span class="badge badge-light float-right" style="color:#003366;">
                        <asp:Label ID="lblTotalProdRows" runat="server" Text="0"></asp:Label> registros
                    </span>
                </div>
                <div class="sec-card">
                    <asp:GridView ID="gvProduccion" runat="server"
                        AutoGenerateColumns="False"
                        CssClass="table table-hover table-sm dash-table mb-0"
                        OnRowDataBound="gvProduccion_RowDataBound"
                        EmptyDataText="Sin produccion registrada para el periodo.">
                        <Columns>
                            <asp:BoundField DataField="Base"     HeaderText="Base"    ItemStyle-Width="110px" />
                            <asp:BoundField DataField="Producto" HeaderText="Producto" />
                            <asp:TemplateField HeaderText="Buenos" ItemStyle-Width="75px" ItemStyle-CssClass="text-right" HeaderStyle-CssClass="text-right">
                                <ItemTemplate><span class="badge badge-success"><%# Eval("Buenos") %></span></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Rechazo" ItemStyle-Width="75px" ItemStyle-CssClass="text-right" HeaderStyle-CssClass="text-right">
                                <ItemTemplate><span class="badge badge-warning"><%# Eval("Rechazo") %></span></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Valor Producido ($)" ItemStyle-Width="130px" ItemStyle-CssClass="text-right" HeaderStyle-CssClass="text-right">
                                <ItemTemplate><strong><%# ((decimal)Eval("ValorProducido")).ToString("C2") %></strong></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Costo Mat. ($)" ItemStyle-Width="120px" ItemStyle-CssClass="text-right" HeaderStyle-CssClass="text-right">
                                <ItemTemplate><span class="text-danger"><%# ((decimal)Eval("CostoMat")).ToString("C2") %></span></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Margen ($)" ItemStyle-Width="110px" ItemStyle-CssClass="text-right" HeaderStyle-CssClass="text-right">
                                <ItemTemplate>
                                    <%# ((decimal)Eval("Margen") >= 0)
                                        ? "<span class='text-success font-weight-bold'>" + ((decimal)Eval("Margen")).ToString("C2") + "</span>"
                                        : "<span class='text-danger font-weight-bold'>" + ((decimal)Eval("Margen")).ToString("C2") + "</span>" %>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                        <HeaderStyle BackColor="#003366" ForeColor="White" />
                        <AlternatingRowStyle BackColor="#f8f9fa" />
                        <FooterStyle BackColor="#e8ecf4" Font-Bold="True" />
                    </asp:GridView>
                </div>
            </div>

            <!-- Materiales críticos -->
            <div class="col-lg-4 mb-3">
                <div class="sec-title" style="background:#922b21;">
                    <i class="fas fa-exclamation-circle"></i> Materiales Bajo Minimo
                    <span class="badge badge-light float-right" style="color:#922b21;">
                        <asp:Label ID="lblCountCriticosPanel" runat="server" Text="0"></asp:Label>
                    </span>
                </div>
                <div class="sec-card" style="max-height:320px; overflow-y:auto; background:#fff7f7;">
                    <asp:Literal ID="litCriticos" runat="server"></asp:Literal>
                </div>
            </div>
        </div>

        <!-- ══ ROW 4 — ÚLTIMAS ENTREGAS + VALOR POR BASE ═════════════════════ -->
        <div class="row">
            <!-- Últimas entregas -->
            <div class="col-lg-7 mb-3">
                <div class="sec-title">
                    <i class="fas fa-truck"></i> Ultimas Entregas
                </div>
                <div class="sec-card">
                    <asp:GridView ID="gvUltimasEntregas" runat="server"
                        AutoGenerateColumns="False"
                        CssClass="table table-hover table-sm dash-table mb-0"
                        OnRowDataBound="gvEntregas_RowDataBound"
                        EmptyDataText="Sin entregas registradas.">
                        <Columns>
                            <asp:BoundField DataField="Folio"    HeaderText="Folio"   ItemStyle-Width="110px" />
                            <asp:BoundField DataField="Cliente"  HeaderText="Cliente" />
                            <asp:BoundField DataField="Base"     HeaderText="Base"    ItemStyle-Width="100px" />
                            <asp:TemplateField HeaderText="Total ($)" ItemStyle-Width="110px" ItemStyle-CssClass="text-right" HeaderStyle-CssClass="text-right">
                                <ItemTemplate><strong><%# ((decimal)Eval("Total")).ToString("C2") %></strong></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Estado" ItemStyle-Width="105px" ItemStyle-CssClass="text-center" HeaderStyle-CssClass="text-center">
                                <ItemTemplate>
                                    <asp:Label ID="lblEstado" runat="server" Text='<%# Eval("Estado") %>'></asp:Label>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Fecha" ItemStyle-Width="95px">
                                <ItemTemplate><%# ((DateTime)Eval("Fecha")).ToString("dd/MM/yyyy") %></ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                        <HeaderStyle BackColor="#003366" ForeColor="White" />
                        <AlternatingRowStyle BackColor="#f8f9fa" />
                    </asp:GridView>
                </div>
            </div>

            <!-- Valor inventario por base -->
            <div class="col-lg-5 mb-3">
                <div class="sec-title">
                    <i class="fas fa-chart-bar"></i> Valor del Inventario por Base
                </div>
                <div class="sec-card">
                    <asp:GridView ID="gvValorPorBase" runat="server"
                        AutoGenerateColumns="False"
                        CssClass="table table-sm dash-table mb-0"
                        ShowFooter="True"
                        OnRowDataBound="gvValorBase_RowDataBound">
                        <Columns>
                            <asp:BoundField DataField="Base" HeaderText="Base" />
                            <asp:TemplateField HeaderText="Materiales ($)" ItemStyle-CssClass="text-right" HeaderStyle-CssClass="text-right" FooterStyle-CssClass="text-right">
                                <ItemTemplate><%# ((decimal)Eval("ValMat")).ToString("C2") %></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Productos ($)" ItemStyle-CssClass="text-right" HeaderStyle-CssClass="text-right" FooterStyle-CssClass="text-right">
                                <ItemTemplate><%# ((decimal)Eval("ValProd")).ToString("C2") %></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="TOTAL ($)" ItemStyle-CssClass="text-right font-weight-bold" HeaderStyle-CssClass="text-right" FooterStyle-CssClass="text-right font-weight-bold">
                                <ItemTemplate><strong><%# ((decimal)Eval("ValMat") + (decimal)Eval("ValProd")).ToString("C2") %></strong></ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                        <HeaderStyle BackColor="#003366" ForeColor="White" />
                        <AlternatingRowStyle BackColor="#f8f9fa" />
                        <FooterStyle BackColor="#e8ecf4" Font-Bold="True" />
                    </asp:GridView>
                </div>
            </div>
        </div>

        <div style="height:20px;"></div>

    </div><!-- /container-fluid -->

    <script>
        var _ddlPeriodoId = '<%= ddlPeriodo.ClientID %>';
        var _txtDesdeId   = '<%= txtDesde.ClientID %>';
        var _txtHastaId   = '<%= txtHasta.ClientID %>';

        // Calcula el rango de fechas para un período dado (devuelve {desde, hasta} en formato yyyy-MM-dd)
        function calcularRangoPeriodo(periodo) {
            var hoy = new Date();
            var yyyy = hoy.getFullYear();
            var mm   = String(hoy.getMonth() + 1).padStart(2, '0');
            var dd   = String(hoy.getDate()).padStart(2, '0');
            var hoyStr = yyyy + '-' + mm + '-' + dd;

            if (periodo === 'hoy') {
                return { desde: hoyStr, hasta: hoyStr };
            }
            if (periodo === 'semana') {
                var dow   = hoy.getDay() === 0 ? 6 : hoy.getDay() - 1; // 0 = lunes
                var lunes = new Date(hoy);
                lunes.setDate(hoy.getDate() - dow);
                var lStr = lunes.getFullYear() + '-' +
                           String(lunes.getMonth() + 1).padStart(2, '0') + '-' +
                           String(lunes.getDate()).padStart(2, '0');
                return { desde: lStr, hasta: hoyStr };
            }
            if (periodo === 'mes') {
                return { desde: yyyy + '-' + mm + '-01', hasta: hoyStr };
            }
            return null; // personalizado: no tocar los campos
        }

        window.addEventListener('DOMContentLoaded', function () {
            // SweetAlert mensajes pendientes
            var h = document.getElementById('<%= hdnMensajePendiente.ClientID %>');
            if (h && h.value) {
                try {
                    var m = JSON.parse(h.value);
                    if (m && m.title) {
                        Swal.fire({ icon: m.icon, title: m.title, text: m.text, confirmButtonColor: '#003366' });
                        h.value = '';
                    }
                } catch(e) {}
            }

            // Al cambiar el dropdown → actualizar los date pickers
            document.getElementById(_ddlPeriodoId).addEventListener('change', function () {
                var rango = calcularRangoPeriodo(this.value);
                if (rango) {
                    document.getElementById(_txtDesdeId).value = rango.desde;
                    document.getElementById(_txtHastaId).value = rango.hasta;
                }
            });

            // Al editar manualmente cualquier fecha → cambiar dropdown a "personalizado"
            [_txtDesdeId, _txtHastaId].forEach(function (id) {
                document.getElementById(id).addEventListener('change', function () {
                    document.getElementById(_ddlPeriodoId).value = 'personalizado';
                });
            });
        });
    </script>

</asp:Content>

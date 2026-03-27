<%@ Page Title="Inventario" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Inventario.aspx.cs" Inherits="GrupoAnkhalInventario.Inventario" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="css/gridviewPantalla.css" rel="stylesheet" />
    <style>
        /* ── Dashboard cards ─────────────────────────────── */
        .inv-dashboard {
            display: flex;
            gap: 14px;
            margin-bottom: 20px;
            flex-wrap: wrap;
        }
        .inv-card {
            flex: 1;
            min-width: 180px;
            border-radius: 10px;
            padding: 16px 20px;
            color: #fff;
            box-shadow: 0 3px 10px rgba(0,0,0,0.15);
        }
        .inv-card .lbl { font-size: 0.78rem; opacity: 0.85; text-transform: uppercase; letter-spacing: 0.5px; }
        .inv-card .num { font-size: 1.6rem; font-weight: 700; line-height: 1.1; }
        .inv-card .icon-big { font-size: 2.2rem; opacity: 0.25; float: right; margin-top: -8px; }
        .inv-card.total   { background: linear-gradient(135deg,#1a3a6b,#2a5caa); }
        .inv-card.mats    { background: linear-gradient(135deg,#1e6e4a,#27ae60); }
        .inv-card.buenos  { background: linear-gradient(135deg,#1a5276,#2980b9); }
        .inv-card.rechazo { background: linear-gradient(135deg,#6e2c00,#d35400); }

        /* ── Sección títulos ─────────────────────────────── */
        .seccion-titulo {
            background: #003366;
            color: #fff;
            padding: 8px 16px;
            border-radius: 6px 6px 0 0;
            font-size: 0.95rem;
            font-weight: 600;
            margin-top: 18px;
        }
        .seccion-titulo i { margin-right: 6px; }

        /* ── Semáforo badges ─────────────────────────────── */
        .nivel-badge { display:inline-block; padding:2px 8px; border-radius:12px; font-size:0.78rem; font-weight:600; }
        .nivel-critico { background:#fdecea; color:#c0392b; border:1px solid #e74c3c; }
        .nivel-bajo    { background:#fef5e7; color:#d35400; border:1px solid #e67e22; }
        .nivel-optimo  { background:#eafaf1; color:#1e8449; border:1px solid #27ae60; }
        .nivel-sin     { background:#f0f0f0; color:#7f8c8d; border:1px solid #bdc3c7; }

        /* ── Accordion bases ─────────────────────────────── */
        .bases-accordion { padding: 10px 16px; }
        .bases-accordion .table { margin-bottom: 4px; }

        /* ── Filtros bar ─────────────────────────────────── */
        .filtros-bar {
            background: #f8f9fa;
            border: 1px solid #dee2e6;
            border-radius: 8px;
            padding: 14px 18px;
            margin-bottom: 14px;
        }

        /* ── Resumen por base ────────────────────────────── */
        .tbl-resumen th { background:#003366; color:#fff; font-size:0.82rem; }
        .tbl-resumen tfoot td { font-weight:700; background:#e8ecf4; }

        /* ── Impresion ───────────────────────────────────── */
        @media print {
            .main-sidebar, .main-header, .filtros-bar, .btn, button,
            .content-header, .pager-custom { display: none !important; }
            .content-wrapper { margin-left: 0 !important; padding: 10px !important; }
            .inv-card { color: #000 !important; border: 1px solid #ccc !important; background: #fff !important; }
            .inv-card .num { font-size: 1.2rem; }
            .seccion-titulo { background: #003366 !important; -webkit-print-color-adjust: exact; print-color-adjust: exact; }
            .table th { background: #003366 !important; color: #fff !important; -webkit-print-color-adjust: exact; print-color-adjust: exact; }
            body::before { content: "Inventario General - Grupo ANKHAL  |  " attr(data-fecha); display: block; font-weight: bold; font-size: 14px; margin-bottom: 10px; }
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <asp:HiddenField ID="hdnMensajePendiente" runat="server" />

    <div class="container-fluid">

        <!-- ══ HEADER ══════════════════════════════════════════════════════ -->
        <div class="row mb-2">
            <div class="col-12">
                <h4 style="color:#003366; font-weight:700;">
                    <i class="fas fa-boxes"></i> Inventario General
                    <small class="text-muted" style="font-size:0.75rem; font-weight:400; margin-left:8px;">
                        Consulta consolidada de stock
                    </small>
                </h4>
            </div>
        </div>

        <!-- ══ DASHBOARD CARDS ══════════════════════════════════════════════ -->
        <div class="inv-dashboard">
            <div class="inv-card total">
                <i class="fas fa-warehouse icon-big"></i>
                <div class="lbl">Valor Total Inventario</div>
                <div class="num">$<asp:Label ID="lblValorTotal" runat="server" Text="0.00"></asp:Label></div>
            </div>
            <div class="inv-card mats">
                <i class="fas fa-cubes icon-big"></i>
                <div class="lbl">Valor Materiales</div>
                <div class="num">$<asp:Label ID="lblValorMateriales" runat="server" Text="0.00"></asp:Label></div>
            </div>
            <div class="inv-card buenos">
                <i class="fas fa-check-circle icon-big"></i>
                <div class="lbl">Valor Productos Buenos</div>
                <div class="num">$<asp:Label ID="lblValorBuenos" runat="server" Text="0.00"></asp:Label></div>
            </div>
            <div class="inv-card rechazo">
                <i class="fas fa-times-circle icon-big"></i>
                <div class="lbl">Valor Productos Rechazo</div>
                <div class="num">$<asp:Label ID="lblValorRechazo" runat="server" Text="0.00"></asp:Label></div>
            </div>
        </div>

        <!-- ══ FILTROS ══════════════════════════════════════════════════════ -->
        <div class="filtros-bar">
            <div class="row align-items-end">
                <div class="col-md-3">
                    <label class="mb-1" style="font-size:0.85rem;font-weight:600;">Base / Planta</label>
                    <asp:DropDownList ID="ddlBase" runat="server" CssClass="form-control form-control-sm"></asp:DropDownList>
                </div>
                <div class="col-md-3">
                    <label class="mb-1" style="font-size:0.85rem;font-weight:600;">Tipo de Item</label>
                    <asp:DropDownList ID="ddlTipoItem" runat="server" CssClass="form-control form-control-sm">
                        <asp:ListItem Text="-- Todos --" Value="" />
                        <asp:ListItem Text="Materiales" Value="MAT" />
                        <asp:ListItem Text="Productos" Value="PROD" />
                    </asp:DropDownList>
                </div>
                <div class="col-md-3 mt-1">
                    <asp:Button ID="btnFiltrar" runat="server" Text="Filtrar"
                        CssClass="btn btn-primary btn-sm mr-1" OnClick="btnFiltrar_Click" />
                    <asp:Button ID="btnLimpiar" runat="server" Text="Limpiar"
                        CssClass="btn btn-secondary btn-sm mr-1" OnClick="btnLimpiar_Click" />
                    <asp:Button ID="btnExportarExcel" runat="server" Text="Excel"
                        CssClass="btn btn-success btn-sm mr-1" OnClick="btnExportarExcel_Click" />
                    <asp:Button ID="btnExportarPdf" runat="server" Text="Imprimir"
                        CssClass="btn btn-warning btn-sm" OnClick="btnExportarPdf_Click" />
                </div>
            </div>
        </div>

        <!-- ══ SECCIÓN MATERIALES ═══════════════════════════════════════════ -->
        <asp:Panel ID="pnlMateriales" runat="server">
            <div class="seccion-titulo">
                <i class="fas fa-cubes"></i> Materiales
                <span class="badge badge-light float-right" style="color:#003366;">
                    <asp:Label ID="lblTotalMateriales" runat="server" Text="0"></asp:Label> registros
                </span>
            </div>
            <div class="card" style="border-radius:0 0 6px 6px; border-top:none;">
                <div class="card-body p-0">
                    <asp:GridView ID="gvMateriales" runat="server"
                        AllowCustomPaging="True" AllowPaging="True" PageSize="15"
                        AutoGenerateColumns="False"
                        CssClass="table table-hover table-sm mb-0"
                        OnPageIndexChanging="gvMateriales_PageIndexChanging"
                        OnRowDataBound="gvMateriales_RowDataBound"
                        EmptyDataText="Sin materiales con stock.">
                        <Columns>
                            <asp:BoundField DataField="Codigo" HeaderText="Código" ItemStyle-Width="90px" />
                            <asp:BoundField DataField="Descripcion" HeaderText="Descripción" />
                            <asp:BoundField DataField="TipoNombre" HeaderText="Tipo" ItemStyle-Width="110px" />
                            <asp:BoundField DataField="Unidad" HeaderText="Unidad" ItemStyle-Width="70px" ItemStyle-CssClass="text-center" HeaderStyle-CssClass="text-center" />
                            <asp:TemplateField HeaderText="Stock Global" ItemStyle-Width="110px" ItemStyle-CssClass="text-right" HeaderStyle-CssClass="text-right">
                                <ItemTemplate>
                                    <strong><%# Eval("StockGlobal", "{0:N2}") %></strong>
                                    <span class="text-muted" style="font-size:0.78rem;"> <%# Eval("Unidad") %></span>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Nivel" ItemStyle-Width="110px" ItemStyle-CssClass="text-center" HeaderStyle-CssClass="text-center">
                                <ItemTemplate>
                                    <span class='nivel-badge <%# GetNivelCss((decimal)Eval("StockGlobal"),(decimal)Eval("StockMinimo"),(decimal)Eval("StockMaximo"),(decimal)Eval("StockOptimo")) %>'>
                                        <%# GetNivelIcon((decimal)Eval("StockGlobal"),(decimal)Eval("StockMinimo"),(decimal)Eval("StockMaximo"),(decimal)Eval("StockOptimo")) %>
                                        <%# GetNivelTextoCorto((decimal)Eval("StockGlobal"),(decimal)Eval("StockMinimo"),(decimal)Eval("StockMaximo"),(decimal)Eval("StockOptimo")) %>
                                    </span>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Valor ($)" ItemStyle-Width="110px" ItemStyle-CssClass="text-right" HeaderStyle-CssClass="text-right">
                                <ItemTemplate>
                                    <strong><%# ((decimal)Eval("StockGlobal") * (decimal)Eval("PrecioUnitario")).ToString("C2") %></strong>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Por Base" ItemStyle-Width="90px" ItemStyle-CssClass="text-center" HeaderStyle-CssClass="text-center">
                                <ItemTemplate>
                                    <button type="button" class="btn btn-xs btn-outline-secondary"
                                        onclick="toggleAcc('accM_<%# Eval("MaterialID") %>')">
                                        <i class="fas fa-warehouse"></i>
                                    </button>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                        <PagerStyle CssClass="pager-custom" />
                        <HeaderStyle BackColor="#003366" ForeColor="White" />
                        <AlternatingRowStyle BackColor="#f8f9fa" />
                    </asp:GridView>
                </div>
            </div>
        </asp:Panel>

        <!-- ══ SECCIÓN PRODUCTOS ════════════════════════════════════════════ -->
        <asp:Panel ID="pnlProductos" runat="server">
            <div class="seccion-titulo" style="margin-top:22px;">
                <i class="fas fa-box"></i> Productos
                <span class="badge badge-light float-right" style="color:#003366;">
                    <asp:Label ID="lblTotalProductos" runat="server" Text="0"></asp:Label> registros
                </span>
            </div>
            <div class="card" style="border-radius:0 0 6px 6px; border-top:none;">
                <div class="card-body p-0">
                    <asp:GridView ID="gvProductos" runat="server"
                        AllowCustomPaging="True" AllowPaging="True" PageSize="15"
                        AutoGenerateColumns="False"
                        CssClass="table table-hover table-sm mb-0"
                        OnPageIndexChanging="gvProductos_PageIndexChanging"
                        OnRowDataBound="gvProductos_RowDataBound"
                        EmptyDataText="Sin productos con stock.">
                        <Columns>
                            <asp:BoundField DataField="Codigo" HeaderText="Código" ItemStyle-Width="90px" />
                            <asp:BoundField DataField="Descripcion" HeaderText="Descripción" />
                            <asp:BoundField DataField="TipoNombre" HeaderText="Tipo" ItemStyle-Width="110px" />
                            <asp:TemplateField HeaderText="Buenos" ItemStyle-Width="90px" ItemStyle-CssClass="text-right" HeaderStyle-CssClass="text-right">
                                <ItemTemplate>
                                    <span class="badge badge-success"><%# Eval("TotalBuenos") %></span>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Rechazo" ItemStyle-Width="90px" ItemStyle-CssClass="text-right" HeaderStyle-CssClass="text-right">
                                <ItemTemplate>
                                    <span class="badge badge-warning"><%# Eval("TotalRechazo") %></span>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Total" ItemStyle-Width="80px" ItemStyle-CssClass="text-right" HeaderStyle-CssClass="text-right">
                                <ItemTemplate>
                                    <strong><%# (int)Eval("TotalBuenos") + (int)Eval("TotalRechazo") %></strong>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Valor Buenos ($)" ItemStyle-Width="120px" ItemStyle-CssClass="text-right" HeaderStyle-CssClass="text-right">
                                <ItemTemplate>
                                    <strong class="text-success"><%# ((int)Eval("TotalBuenos") * (decimal)Eval("PrecioVenta")).ToString("C2") %></strong>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Valor Rechazo ($)" ItemStyle-Width="130px" ItemStyle-CssClass="text-right" HeaderStyle-CssClass="text-right">
                                <ItemTemplate>
                                    <span class="text-warning"><%# ((int)Eval("TotalRechazo") * (decimal)Eval("PrecioVenta") * 0.5m).ToString("C2") %></span>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Por Base" ItemStyle-Width="80px" ItemStyle-CssClass="text-center" HeaderStyle-CssClass="text-center">
                                <ItemTemplate>
                                    <button type="button" class="btn btn-xs btn-outline-secondary"
                                        onclick="toggleAcc('accP_<%# Eval("ProductoID") %>')">
                                        <i class="fas fa-warehouse"></i>
                                    </button>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                        <PagerStyle CssClass="pager-custom" />
                        <HeaderStyle BackColor="#003366" ForeColor="White" />
                        <AlternatingRowStyle BackColor="#f8f9fa" />
                    </asp:GridView>
                </div>
            </div>
        </asp:Panel>

        <!-- ══ RESUMEN POR BASE ═════════════════════════════════════════════ -->
        <asp:Panel ID="pnlResumen" runat="server">
            <div class="seccion-titulo" style="margin-top:22px;">
                <i class="fas fa-chart-bar"></i> Resumen por Base / Planta
            </div>
            <div class="card" style="border-radius:0 0 6px 6px; border-top:none;">
                <div class="card-body p-0">
                    <asp:GridView ID="gvResumen" runat="server"
                        AutoGenerateColumns="False"
                        CssClass="table table-sm tbl-resumen mb-0"
                        ShowFooter="True"
                        OnRowDataBound="gvResumen_RowDataBound">
                        <Columns>
                            <asp:BoundField DataField="BaseNombre" HeaderText="Base / Planta" />
                            <asp:TemplateField HeaderText="Materiales ($)" ItemStyle-CssClass="text-right" HeaderStyle-CssClass="text-right" FooterStyle-CssClass="text-right">
                                <ItemTemplate><%# ((decimal)Eval("ValorMateriales")).ToString("C2") %></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Prod. Buenos ($)" ItemStyle-CssClass="text-right" HeaderStyle-CssClass="text-right" FooterStyle-CssClass="text-right">
                                <ItemTemplate><%# ((decimal)Eval("ValorBuenos")).ToString("C2") %></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Prod. Rechazo ($)" ItemStyle-CssClass="text-right" HeaderStyle-CssClass="text-right" FooterStyle-CssClass="text-right">
                                <ItemTemplate><%# ((decimal)Eval("ValorRechazo")).ToString("C2") %></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="TOTAL ($)" ItemStyle-CssClass="text-right font-weight-bold" HeaderStyle-CssClass="text-right" FooterStyle-CssClass="text-right font-weight-bold">
                                <ItemTemplate>
                                    <strong><%# ((decimal)Eval("ValorMateriales") + (decimal)Eval("ValorBuenos") + (decimal)Eval("ValorRechazo")).ToString("C2") %></strong>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                        <HeaderStyle BackColor="#003366" ForeColor="White" />
                        <AlternatingRowStyle BackColor="#f8f9fa" />
                        <FooterStyle BackColor="#e8ecf4" Font-Bold="True" />
                    </asp:GridView>
                </div>
            </div>
        </asp:Panel>

        <div style="height:30px;"></div>

    </div><!-- /container-fluid -->

    <!-- ══ JAVASCRIPT ══════════════════════════════════════════════════════ -->
    <script>
        // Mostrar SweetAlert si hay mensaje pendiente
        window.addEventListener('DOMContentLoaded', function () {
            var hdnEl = document.getElementById('<%= hdnMensajePendiente.ClientID %>');
            if (!hdnEl || !hdnEl.value) return;
            try {
                var m = JSON.parse(hdnEl.value);
                if (m && m.title) {
                    Swal.fire({ icon: m.icon, title: m.title, text: m.text, confirmButtonColor: '#003366' });
                    hdnEl.value = '';
                }
            } catch (e) { }
        });

        // Toggle accordion de detalle por base
        function toggleAcc(id) {
            var row = document.getElementById(id);
            if (!row) return;
            row.style.display = (row.style.display === 'none' || row.style.display === '') ? 'table-row' : 'none';
        }
    </script>

</asp:Content>

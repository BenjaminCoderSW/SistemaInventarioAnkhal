<%@ Page Title="Inventario por Base" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="InventarioPorBase.aspx.cs" Inherits="GrupoAnkhalInventario.InventarioPorBase" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="css/gridviewPantalla.css" rel="stylesheet" />
    <style>
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

        /* ── Filtros bar ─────────────────────────────────── */
        .filtros-bar {
            background: #f8f9fa;
            border: 1px solid #dee2e6;
            border-radius: 8px;
            padding: 14px 18px;
            margin-bottom: 14px;
        }

        /* ── Impresión ───────────────────────────────────── */
        @media print {
            .main-sidebar, .main-header, .filtros-bar, .btn, button,
            .content-header, .pager-custom { display: none !important; }
            .content-wrapper { margin-left: 0 !important; padding: 10px !important; }
            .seccion-titulo { background: #003366 !important; -webkit-print-color-adjust: exact; print-color-adjust: exact; }
            .table th { background: #003366 !important; color: #fff !important; -webkit-print-color-adjust: exact; print-color-adjust: exact; }
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
                    <i class="fas fa-warehouse"></i> Inventario por Base / Planta
                    <small class="text-muted" style="font-size:0.75rem; font-weight:400; margin-left:8px;">
                        Desglose de stock ítem × base
                    </small>
                </h4>
            </div>
        </div>

        <!-- ══ FILTROS ══════════════════════════════════════════════════════ -->
        <div class="filtros-bar">
            <div class="row align-items-end">
                <div class="col-md-3">
                    <label class="mb-1" style="font-size:0.85rem;font-weight:600;">Base / Planta</label>
                    <asp:DropDownList ID="ddlBase" runat="server" CssClass="form-control form-control-sm"></asp:DropDownList>
                </div>
                <div class="col-md-2">
                    <label class="mb-1" style="font-size:0.85rem;font-weight:600;">Tipo de Item</label>
                    <asp:DropDownList ID="ddlTipoItem" runat="server" CssClass="form-control form-control-sm">
                        <asp:ListItem Text="-- Todos --" Value="" />
                        <asp:ListItem Text="Materiales" Value="MAT" />
                        <asp:ListItem Text="Productos" Value="PROD" />
                    </asp:DropDownList>
                </div>
                <div class="col-md-3">
                    <label class="mb-1" style="font-size:0.85rem;font-weight:600;">Buscar material</label>
                    <asp:TextBox ID="txtBuscarMateriales" runat="server"
                        CssClass="form-control form-control-sm"
                        placeholder="Código o descripción..."></asp:TextBox>
                </div>
                <div class="col-md-3">
                    <label class="mb-1" style="font-size:0.85rem;font-weight:600;">Buscar producto</label>
                    <asp:TextBox ID="txtBuscarProductos" runat="server"
                        CssClass="form-control form-control-sm"
                        placeholder="Código o descripción..."></asp:TextBox>
                </div>
            </div>
            <div class="row mt-2">
                <div class="col text-md-right">
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
                <i class="fas fa-cubes"></i> Materiales por Base
                <span class="badge badge-light float-right" style="color:#003366;">
                    <asp:Label ID="lblTotalMateriales" runat="server" Text="0"></asp:Label> filas
                </span>
            </div>
            <div class="card" style="border-radius:0 0 6px 6px; border-top:none;">
                <div class="card-body p-0">
                    <div class="table-responsive">
                    <asp:GridView ID="gvMateriales" runat="server"
                        AllowCustomPaging="True" AllowPaging="True" PageSize="20"
                        AutoGenerateColumns="False"
                        CssClass="table table-hover table-sm mb-0"
                        OnPageIndexChanging="gvMateriales_PageIndexChanging"
                        EmptyDataText="Sin resultados.">
                        <Columns>
                            <asp:BoundField DataField="Codigo" HeaderText="Código" ItemStyle-Width="90px" />
                            <asp:BoundField DataField="Descripcion" HeaderText="Descripción" />
                            <asp:BoundField DataField="TipoNombre" HeaderText="Tipo" ItemStyle-Width="110px" />
                            <asp:BoundField DataField="BaseNombre" HeaderText="Base" ItemStyle-Width="150px" />
                            <asp:TemplateField HeaderText="Cantidad" ItemStyle-Width="120px" ItemStyle-CssClass="text-right" HeaderStyle-CssClass="text-right">
                                <ItemTemplate>
                                    <strong><%# Eval("Cantidad", "{0:N2}") %></strong>
                                    <small class="text-muted"> <%# Eval("Unidad") %></small>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Precio Unit." ItemStyle-Width="110px" ItemStyle-CssClass="text-right" HeaderStyle-CssClass="text-right">
                                <ItemTemplate><%# ((decimal)Eval("PrecioUnitario")).ToString("C2") %></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Valor ($)" ItemStyle-Width="110px" ItemStyle-CssClass="text-right font-weight-bold" HeaderStyle-CssClass="text-right">
                                <ItemTemplate>
                                    <strong><%# ((decimal)Eval("ValorItem")).ToString("C2") %></strong>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                        <PagerStyle CssClass="pager-custom" />
                        <HeaderStyle BackColor="#003366" ForeColor="White" />
                        <AlternatingRowStyle BackColor="#f8f9fa" />
                    </asp:GridView>
                    </div>
                </div>
            </div>
        </asp:Panel>

        <!-- ══ SECCIÓN PRODUCTOS ════════════════════════════════════════════ -->
        <asp:Panel ID="pnlProductos" runat="server">
            <div class="seccion-titulo" style="margin-top:22px;">
                <i class="fas fa-box"></i> Productos por Base
                <span class="badge badge-light float-right" style="color:#003366;">
                    <asp:Label ID="lblTotalProductos" runat="server" Text="0"></asp:Label> filas
                </span>
            </div>
            <div class="card" style="border-radius:0 0 6px 6px; border-top:none;">
                <div class="card-body p-0">
                    <div class="table-responsive">
                    <asp:GridView ID="gvProductos" runat="server"
                        AllowCustomPaging="True" AllowPaging="True" PageSize="20"
                        AutoGenerateColumns="False"
                        CssClass="table table-hover table-sm mb-0"
                        OnPageIndexChanging="gvProductos_PageIndexChanging"
                        EmptyDataText="Sin resultados.">
                        <Columns>
                            <asp:BoundField DataField="Codigo" HeaderText="Código" ItemStyle-Width="90px" />
                            <asp:BoundField DataField="Descripcion" HeaderText="Descripción" />
                            <asp:BoundField DataField="TipoNombre" HeaderText="Tipo" ItemStyle-Width="110px" />
                            <asp:BoundField DataField="BaseNombre" HeaderText="Base" ItemStyle-Width="150px" />
                            <asp:TemplateField HeaderText="Buenos" ItemStyle-Width="80px" ItemStyle-CssClass="text-right" HeaderStyle-CssClass="text-right">
                                <ItemTemplate>
                                    <span class="badge badge-success"><%# Eval("Buenos") %></span>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Rechazo" ItemStyle-Width="80px" ItemStyle-CssClass="text-right" HeaderStyle-CssClass="text-right">
                                <ItemTemplate>
                                    <span class="badge badge-warning"><%# Eval("Rechazo") %></span>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Total" ItemStyle-Width="70px" ItemStyle-CssClass="text-right" HeaderStyle-CssClass="text-right">
                                <ItemTemplate>
                                    <strong><%# (int)Eval("Buenos") + (int)Eval("Rechazo") %></strong>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Precio Venta" ItemStyle-Width="110px" ItemStyle-CssClass="text-right" HeaderStyle-CssClass="text-right">
                                <ItemTemplate><%# ((decimal)Eval("PrecioVenta")).ToString("C2") %></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Valor Buenos ($)" ItemStyle-Width="120px" ItemStyle-CssClass="text-right" HeaderStyle-CssClass="text-right">
                                <ItemTemplate>
                                    <strong class="text-success"><%# ((decimal)Eval("ValorBuenos")).ToString("C2") %></strong>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Valor Rechazo ($)" ItemStyle-Width="130px" ItemStyle-CssClass="text-right" HeaderStyle-CssClass="text-right">
                                <ItemTemplate>
                                    <span class="text-warning"><%# ((decimal)Eval("ValorRechazo")).ToString("C2") %></span>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                        <PagerStyle CssClass="pager-custom" />
                        <HeaderStyle BackColor="#003366" ForeColor="White" />
                        <AlternatingRowStyle BackColor="#f8f9fa" />
                    </asp:GridView>
                    </div>
                </div>
            </div>
        </asp:Panel>

        <div style="height:30px;"></div>

    </div><!-- /container-fluid -->

    <script>
        window.addEventListener('DOMContentLoaded', function () {
            $('[data-toggle="tooltip"]').tooltip();
        });
    </script>

</asp:Content>

    <%@ Page Title="Producción" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Produccion.aspx.cs" Inherits="GrupoAnkhalInventario.ProduccionPage" EnableEventValidation="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="css/gridviewPantalla.css" rel="stylesheet" />
    <style>
        /* ── Dashboard de producción ── */
        .stock-dashboard {
            display: flex;
            gap: 14px;
            margin-bottom: 18px;
            flex-wrap: wrap;
        }
        .stock-card {
            flex: 1;
            min-width: 140px;
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
        .stock-card.produccion { background: linear-gradient(135deg,#1a5276,#2980b9); }
        .stock-card.buenos     { background: linear-gradient(135deg,#1e8449,#27ae60); }
        .stock-card.rechazo    { background: linear-gradient(135deg,#922b21,#e74c3c); }
        .stock-card.meta       { background: linear-gradient(135deg,#6c3483,#8e44ad); }
        .stock-card.cumpl      { background: linear-gradient(135deg,#d35400,#e67e22); }
        .stock-card.valor      { background: linear-gradient(135deg,#1c2833,#2c3e50); }
        .stock-card .icon      { font-size: 2.2rem; opacity: .9; }
        .stock-card .info .num { font-size: 1.8rem; font-weight: 700; line-height:1; }
        .stock-card .info .lbl { font-size: .78rem; opacity: .9; text-transform: uppercase; letter-spacing:.5px; }

        /* ── Filtros ── */
        .filtros-bar {
            background:#f8f9fa; border:1px solid #dee2e6;
            border-radius:8px; padding:14px 18px; margin-bottom:14px;
        }
        .filtros-bar label { font-weight:600; font-size:.84rem; color:#003366; margin-bottom:2px; }
        .btn-filtro-rapido { border-radius:20px; font-size:.82rem; padding:4px 14px; margin-right:4px; }
        .btn-filtro-rapido.active { background:#003366; color:#fff; }


        /* ── Paginador ── */
        .pager-custom span {
            background:#003366; color:#fff; font-weight:700;
            border-radius:4px; padding:4px 9px;
        }
        .pager-custom a { padding:4px 9px; border-radius:4px; }

        /* ── Consumo de materiales en modal ── */
        .consumo-table th { background:#003366; color:#fff; font-size:.82rem; padding:6px 10px; }
        .consumo-table td { font-size:.85rem; padding:5px 10px; vertical-align:middle; }
        .consumo-table input[type=number] { width:100px; }
        .stock-ok { color:#27ae60; font-weight:600; }
        .stock-warn { color:#e74c3c; font-weight:600; }
        /* DDL sin conversiones: apariencia deshabilitada pero sigue enviando en el POST */
        .ddl-readonly { pointer-events:none; opacity:.65; background-color:#e9ecef !important; }

        /* ── Badges turno ── */
        .badge-manana  { background:#f39c12; color:#fff; }
        .badge-tarde   { background:#2980b9; color:#fff; }
        .badge-noche   { background:#2c3e50; color:#fff; }
        .badge-unico   { background:#8e44ad; color:#fff; }

        /* ── Filas borrador ── */
        .fila-borrador td { background-color:#fff9e6 !important; }
        .badge-warning    { background:#f39c12; color:#fff; }
        .badge-success    { background:#27ae60; color:#fff; }

    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <!-- ══ DASHBOARD — Fila 1: contadores ══ -->
    <div class="stock-dashboard">
        <div class="stock-card produccion">
            <div class="icon"><i class="fas fa-industry"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblTotalProd" runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Producción Hoy</div>
            </div>
        </div>
        <div class="stock-card buenos">
            <div class="icon"><i class="fas fa-check-circle"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblBuenos" runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Buenos</div>
            </div>
        </div>
        <div class="stock-card rechazo">
            <div class="icon"><i class="fas fa-times-circle"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblRechazo" runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Rechazo</div>
            </div>
        </div>
    </div>

    <!-- ══ DASHBOARD — Fila 2: valor producido (ancho completo) ══ -->
    <div class="stock-dashboard" style="margin-bottom:18px;">
        <div class="stock-card valor" style="flex:0 0 100%;">
            <div class="icon"><i class="fas fa-dollar-sign"></i></div>
            <div class="info">
                <div class="num" style="font-size:2.4rem;">
                    <asp:Label ID="lblValorProd" runat="server" Text="$0.00"></asp:Label>
                </div>
                <div class="lbl">Valor Producido (Unidades Buenas × Precio Venta) del período</div>
            </div>
        </div>
    </div>

    <!-- ══ BARRA DE FILTROS ══ -->
    <div class="filtros-bar">
        <div class="row align-items-end">
            <div class="col-md-2">
                <label>Base</label>
                <asp:DropDownList ID="ddlFiltrBase" runat="server" CssClass="form-control form-control-sm">
                </asp:DropDownList>
            </div>
            <div class="col-md-2">
                <label>Producto</label>
                <div style="position:relative;">
                    <input type="text" id="txtFiltrProducto" class="form-control form-control-sm"
                           placeholder="Buscar producto..." autocomplete="off"
                           oninput="onBuscarFiltrProducto()" />
                    <div id="divResultadosFiltrProducto"
                         style="display:none; position:absolute; z-index:9999; width:100%;
                                background:#fff; border:1px solid #ccc; border-radius:4px;
                                max-height:200px; overflow-y:auto;
                                box-shadow:0 2px 8px rgba(0,0,0,.18);">
                    </div>
                </div>
                <input type="hidden" name="hdnFiltrProductoID"     id="hdnFiltrProductoID"     value="<%= HttpUtility.HtmlEncode(Request.Form["hdnFiltrProductoID"]     ?? "") %>" />
                <input type="hidden" name="hdnFiltrProductoNombre" id="hdnFiltrProductoNombre" value="<%= HttpUtility.HtmlEncode(Request.Form["hdnFiltrProductoNombre"] ?? "") %>" />
            </div>
            <div class="col-md-3">
                <label>Per&iacute;odo r&aacute;pido</label><br />
                <button type="button" class="btn btn-outline-secondary btn-filtro-rapido" onclick="setFiltroRapido('hoy')">Hoy</button>
                <button type="button" class="btn btn-outline-secondary btn-filtro-rapido" onclick="setFiltroRapido('semana')">Esta Semana</button>
                <button type="button" class="btn btn-outline-secondary btn-filtro-rapido" onclick="setFiltroRapido('mes')">Este Mes</button>
            </div>
            <div class="col-md-2">
                <label>Desde</label>
                <asp:TextBox ID="txtFechaDesde" runat="server" CssClass="form-control form-control-sm" TextMode="Date"></asp:TextBox>
            </div>
            <div class="col-md-2">
                <label>Hasta</label>
                <asp:TextBox ID="txtFechaHasta" runat="server" CssClass="form-control form-control-sm" TextMode="Date"></asp:TextBox>
            </div>
            <div class="col-md-1">
                <asp:Button ID="btnBuscar" runat="server" Text="Buscar"
                    CssClass="btn btn-sm btn-primary btn-block mb-1" OnClick="btnBuscar_Click" />
                <asp:Button ID="btnLimpiar" runat="server" Text="Limpiar"
                    CssClass="btn btn-sm btn-outline-secondary btn-block" OnClick="btnLimpiar_Click"
                    OnClientClick="document.getElementById('hdnFiltrProductoID').value=''; document.getElementById('hdnFiltrProductoNombre').value=''; document.getElementById('txtFiltrProducto').value='';" />
            </div>
            <div class="col-md-2 d-flex align-items-center pt-3">
                <div class="form-check">
                    <asp:CheckBox ID="chkMostrarBorradores" runat="server" CssClass="form-check-input" />
                    <label class="form-check-label font-weight-bold text-warning ml-1">Ver Borradores</label>
                </div>
            </div>
        </div>
    </div>

    <!-- ══ ACCIONES ══ -->
    <div class="d-flex justify-content-between align-items-center mb-2">
        <div>
            <asp:Button ID="btnNuevo" runat="server" Text="+ Registrar Produccion"
                CssClass="btn btn-primary" OnClick="btnNuevo_Click" />
            <button type="button" class="btn btn-outline-info ml-2" data-toggle="modal" data-target="#modalHoja">
                <i class="fas fa-file-alt"></i> Hoja de Fabricacion
            </button>
        </div>
        <asp:Label ID="lblResultados" runat="server" CssClass="text-muted small"></asp:Label>
    </div>


    <!-- ══ GRID ══ -->
    <div class="table-responsive">
        <asp:GridView ID="gvProduccion" runat="server" AutoGenerateColumns="False"
            CssClass="table table-bordered table-striped custom-grid"
            AllowCustomPaging="True" AllowPaging="True" PageSize="15"
            OnPageIndexChanging="gvProduccion_PageIndexChanging"
            OnRowDataBound="gvProduccion_RowDataBound"
            OnRowCommand="gvProduccion_RowCommand"
            EmptyDataText="No se encontraron registros de producción."
            PagerStyle-CssClass="pager-custom"
            PagerSettings-Mode="NumericFirstLast"
            PagerSettings-FirstPageText="«"
            PagerSettings-LastPageText="»"
            PagerSettings-PageButtonCount="5">
            <Columns>
                <asp:BoundField DataField="ProduccionID" HeaderText="ID" ItemStyle-Width="50px" />
                <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy}" />
                <asp:BoundField DataField="BaseNombre" HeaderText="Base" />
                <asp:TemplateField HeaderText="Turno">
                    <ItemTemplate>
                        <span class='badge <%# GetBadgeTurno(Eval("Turno").ToString()) %>'>
                            <%# Eval("Turno") %>
                        </span>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Producto">
                    <ItemTemplate>
                        <strong style="color:#003366;"><%# Eval("ProductoCodigo") %></strong>
                        <%# Eval("ProductoNombre") %>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="CantidadBuena" HeaderText="Buenos" ItemStyle-CssClass="text-right" />
                <asp:BoundField DataField="CantidadRechazo" HeaderText="Rechazo" ItemStyle-CssClass="text-right" />
                <asp:BoundField DataField="Total" HeaderText="Total" ItemStyle-CssClass="text-right font-weight-bold" />
                <asp:BoundField DataField="Valor" HeaderText="Valor ($)" DataFormatString="{0:C2}" ItemStyle-CssClass="text-right" />
                <asp:TemplateField HeaderText="Porcentaje de Valor (%)">
                    <HeaderStyle CssClass="text-center" />
                    <ItemStyle CssClass="text-right" Width="140px" />
                    <ItemTemplate>
                        <div class="progress" style="height:18px;">
                            <div class="progress-bar <%# Convert.ToInt32(Eval("CumplPct")) >= 100 ? "bg-success" : Convert.ToInt32(Eval("CumplPct")) >= 70 ? "bg-warning" : "bg-danger" %>"
                                 style="width:<%# Math.Min(Convert.ToInt32(Eval("CumplPct")), 100) %>%">
                                <%# Eval("CumplPct") %>%
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="MetaBase" HeaderText="Meta ($)" DataFormatString="{0:C2}" ItemStyle-CssClass="text-right" />
                <asp:BoundField DataField="RegistradoPor" HeaderText="Registrado Por" />
                <asp:BoundField DataField="Responsable" HeaderText="Responsable" />
                <asp:TemplateField HeaderText="Observaciones">
                    <ItemStyle Width="160px" />
                    <ItemTemplate>
                        <%# string.IsNullOrEmpty(Eval("Observaciones").ToString()) ? "—" : Eval("Observaciones") %>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Consumo de Materiales">
                    <HeaderStyle CssClass="text-center" Width="170px" />
                    <ItemStyle CssClass="text-center" />
                    <ItemTemplate>
                        <button type="button" class="btn btn-sm btn-outline-info"
                            onclick="verConsumos(<%# Eval("ProduccionID") %>, '<%# System.Web.HttpUtility.JavaScriptStringEncode(Eval("ProductoCodigo").ToString() + " " + Eval("ProductoNombre").ToString()) %>')">
                            <i class="fas fa-list-ul mr-1"></i>Ver consumos (<%# Eval("ConsumoCount") %>)
                        </button>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Estado">
                    <HeaderStyle CssClass="text-center" Width="100px" />
                    <ItemStyle CssClass="text-center" />
                    <ItemTemplate>
                        <span class='badge <%# (bool)Eval("EsBorrador") ? "badge-warning" : "badge-success" %>'>
                            <%# Eval("Estado") %>
                        </span>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Acciones">
                    <HeaderStyle CssClass="text-center" Width="130px" />
                    <ItemStyle CssClass="text-center" />
                    <ItemTemplate>
                        <asp:LinkButton ID="lbContinuar" runat="server"
                            CommandName="ContinuarBorrador"
                            CommandArgument='<%# Eval("ProduccionID") %>'
                            CssClass="btn btn-sm btn-warning"
                            Visible='<%# (bool)Eval("EsBorrador") %>'
                            Text="Continuar" />
                        <asp:LinkButton ID="lbEliminar" runat="server"
                            CommandName="EliminarBorrador"
                            CommandArgument='<%# Eval("ProduccionID") %>'
                            CssClass="btn btn-sm btn-outline-danger ml-1"
                            Visible='<%# (bool)Eval("EsBorrador") %>'
                            OnClientClick="return confirm('¿Eliminar este borrador? Esta acción no se puede deshacer.');"
                            Text="Eliminar" />
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </div>

<!-- ══════════════════════════════════════════════════════════════════ -->
<!-- HIDDEN FIELDS + POSTBACK TRIGGERS                                -->
<!-- ══════════════════════════════════════════════════════════════════ -->
<asp:HiddenField ID="hdnMensajePendiente" runat="server" Value="" />
<asp:HiddenField ID="hdnProductoSeleccionado" runat="server" Value="" />
<asp:HiddenField ID="hdnProductoNombre"      runat="server" Value="" />
<asp:HiddenField ID="hdnConfirmarSinConsumos" runat="server" Value="" />
<asp:HiddenField ID="hdnProduccionID" runat="server" Value="0" />
<asp:HiddenField ID="hdnModoEdicion" runat="server" Value="" />
<asp:Button ID="btnCargarConsumos" runat="server" style="display:none" OnClick="btnCargarConsumos_Click" />

<!-- ══════════════════════════════════════════════════════════════════ -->
<!-- MODAL: REGISTRAR PRODUCCIÓN                                      -->
<!-- ══════════════════════════════════════════════════════════════════ -->
<div class="modal fade" id="modalRegistrar" tabindex="-1" data-backdrop="static">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <div class="modal-header bg-primary text-white">
                <h5 class="modal-title"><asp:Label ID="lblModalTitulo" runat="server" Text="Registrar Producción"></asp:Label></h5>
                <button type="button" class="close text-white" data-dismiss="modal">&times;</button>
            </div>
            <div class="modal-body">
                <div class="row mb-3">
                    <div class="col-md-4">
                        <label class="font-weight-bold">Base <span class="text-danger">*</span></label>
                        <asp:DropDownList ID="ddlBase" runat="server" CssClass="form-control">
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-4">
                        <label class="font-weight-bold">Fecha <span class="text-danger">*</span></label>
                        <asp:TextBox ID="txtFecha" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                    </div>
                    <div class="col-md-4">
                        <label class="font-weight-bold">Turno <span class="text-danger">*</span></label>
                        <asp:DropDownList ID="ddlTurno" runat="server" CssClass="form-control">
                            <asp:ListItem Text="-- Seleccione --" Value="" />
                            <asp:ListItem Text="MAÑANA" Value="MAÑANA" />
                            <asp:ListItem Text="TARDE" Value="TARDE" />
                            <asp:ListItem Text="NOCHE" Value="NOCHE" />
                            <asp:ListItem Text="UNICO" Value="UNICO" />
                        </asp:DropDownList>
                    </div>
                </div>

                <div class="row mb-3">
                    <div class="col-md-6">
                        <label class="font-weight-bold">Producto <span class="text-danger">*</span></label>
                        <div style="position:relative;">
                            <input type="text" id="txtProducto" class="form-control"
                                   placeholder="Buscar producto..." autocomplete="off"
                                   oninput="onTxtProductoInput()" />
                            <div id="divResultadosProducto"
                                 style="display:none; position:absolute; z-index:9999; width:100%;
                                        background:#fff; border:1px solid #ccc; border-radius:4px;
                                        max-height:200px; overflow-y:auto;
                                        box-shadow:0 2px 8px rgba(0,0,0,.18);">
                            </div>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <label class="font-weight-bold">Responsable de la producci&oacute;n</label>
                        <asp:TextBox ID="txtResponsable" runat="server" CssClass="form-control" MaxLength="100" placeholder="Nombre del operador..."></asp:TextBox>
                    </div>
                </div>

                <div class="row mb-3">
                    <div class="col-md-4">
                        <label class="font-weight-bold">Cantidad buena <span class="text-danger">*</span></label>
                        <asp:TextBox ID="txtCantBuena" runat="server" CssClass="form-control" TextMode="Number"
                            min="0" placeholder="0" onchange="actualizarTotalProd()"></asp:TextBox>
                    </div>
                    <div class="col-md-4">
                        <label class="font-weight-bold">Cantidad rechazo</label>
                        <asp:TextBox ID="txtCantRechazo" runat="server" CssClass="form-control" TextMode="Number"
                            min="0" placeholder="0" onchange="actualizarTotalProd()"></asp:TextBox>
                    </div>
                    <div class="col-md-4">
                        <label class="font-weight-bold">Total producido</label>
                        <div class="form-control bg-light" id="divTotalProd">0</div>
                    </div>
                </div>

                <!-- Consumo de materiales -->
                <h6 class="text-primary font-weight-bold mt-3 mb-2">Consumo de Materiales</h6>
                <asp:Panel ID="pnlConsumos" runat="server">
                    <div class="d-flex align-items-center flex-wrap mb-2" style="gap:.6rem;">
                        <input type="text" id="txtFiltroConsumo"
                               class="form-control form-control-sm"
                               placeholder="Buscar por código o nombre de material..."
                               oninput="filtrarConsumos(this.value)"
                               style="max-width:320px;" />
                        <span id="spanResultadosFiltro" class="text-muted small"></span>
                    </div>
                    <div class="table-responsive">
                        <table class="table table-sm table-bordered consumo-table" id="tblConsumos">
                            <thead>
                                <tr>
                                    <th>Material</th>
                                    <th>Unidad base</th>
                                    <th class="text-right">Consumo a descontar</th>
                                    <th class="text-right">Stock Actual</th>
                                </tr>
                            </thead>
                            <tbody>
                                <asp:Repeater ID="rptConsumos" runat="server"
                                    OnItemDataBound="rptConsumos_ItemDataBound">
                                    <ItemTemplate>
                                        <tr>
                                            <td>
                                                <%# Eval("MaterialCodigo") %> - <%# Eval("MaterialNombre") %>
                                                <input type="hidden" name="matID" value='<%# Eval("MaterialID") %>' />
                                            </td>
                                            <td><%# Eval("Unidad") %></td>
                                            <td class="text-right consumo-cell"
                                                data-percap='<%# Eval("CantConsumoCapturada", "{0:0.####}") %>'
                                                data-unid='<%# Eval("UnidadCapTexto") %>'>
                                                <strong class="consumo-total">0</strong>
                                                <span class="text-muted" style="font-size:.8rem;"> <%# Eval("UnidadCapTexto") %></span>
                                            </td>
                                            <td class="text-right stock-cell <%# Convert.ToDecimal(Eval("StockActual")) > 0 ? "stock-ok" : "stock-warn" %>">
                                                <%# Eval("StockActual", "{0:N2}") %>
                                                <small class="text-muted"> <%# Eval("UnidadBaseClave") %></small>
                                            </td>
                                        </tr>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </tbody>
                        </table>
                    </div>
                </asp:Panel>
                <asp:Label ID="lblSinConsumos" runat="server" Text="Seleccione un producto para cargar los consumos de materiales."
                    CssClass="text-muted" Visible="true"></asp:Label>

                <div class="form-group mt-3">
                    <label class="font-weight-bold">Observaciones</label>
                    <asp:TextBox ID="txtObservaciones" runat="server" CssClass="form-control" TextMode="MultiLine"
                        Rows="2" MaxLength="500"></asp:TextBox>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancelar</button>
                <asp:Button ID="btnGuardarBorrador" runat="server" Text="Guardar Borrador"
                    CssClass="btn btn-warning" OnClick="btnGuardarBorrador_Click" />
                <asp:Button ID="btnGuardar" runat="server" Text="Confirmar Producción"
                    CssClass="btn btn-primary" OnClick="btnGuardar_Click" />
            </div>
        </div>
    </div>
</div>

<!-- ══════════════════════════════════════════════════════════════════ -->
<!-- MODAL: HOJA DE FABRICACIÓN                                        -->
<!-- ══════════════════════════════════════════════════════════════════ -->
<div class="modal fade" id="modalHoja" tabindex="-1" data-backdrop="static">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header bg-info text-white">
                <h5 class="modal-title"><i class="fas fa-file-alt"></i> Hoja de Fabricacion</h5>
                <button type="button" class="close text-white" data-dismiss="modal">&times;</button>
            </div>
            <div class="modal-body">
                <div class="form-group">
                    <label class="font-weight-bold">Producto</label>
                    <div style="position:relative;">
                        <input type="text" id="txtProductoHoja" class="form-control"
                               placeholder="Buscar producto..." autocomplete="off"
                               oninput="onTxtProductoHojaInput()" />
                        <div id="divResultadosProductoHoja"
                             style="display:none; position:absolute; z-index:9999; width:100%;
                                    background:#fff; border:1px solid #ccc; border-radius:4px;
                                    max-height:200px; overflow-y:auto;
                                    box-shadow:0 2px 8px rgba(0,0,0,.18);">
                        </div>
                    </div>
                    <input type="hidden" name="hdnProductoHojaID" id="hdnProductoHojaID" />
                </div>
                <div class="form-group">
                    <label class="font-weight-bold">Cantidad a fabricar</label>
                    <asp:TextBox ID="txtCantidadHoja" runat="server" CssClass="form-control" TextMode="Number" min="1" placeholder="0"></asp:TextBox>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancelar</button>
                <asp:Button ID="btnGenerarHoja" runat="server" Text="Generar Hoja"
                    CssClass="btn btn-info" OnClick="btnGenerarHoja_Click" />
            </div>
        </div>
    </div>
</div>

<%-- ══ MODAL: Consumo de Materiales (lazy AJAX) ══ --%>
<div class="modal fade" id="modalConsumos" tabindex="-1">
    <div class="modal-dialog modal-xl">
        <div class="modal-content">
            <div class="modal-header" style="background:#003366;">
                <h5 class="modal-title text-white">
                    <i class="fas fa-list-ul mr-2"></i>
                    Consumo de Materiales —
                    <span id="spanConsumoProducto"></span>
                </h5>
                <button type="button" class="close text-white" data-dismiss="modal">&times;</button>
            </div>
            <div class="modal-body">
                <div id="divConsumoSpinner" class="text-center py-4">
                    <div class="spinner-border text-primary" role="status">
                        <span class="sr-only">Cargando...</span>
                    </div>
                    <p class="mt-2 text-muted">Cargando consumos...</p>
                </div>
                <div id="divConsumoError" class="alert alert-danger d-none"></div>
                <div id="divConsumoTabla" class="d-none">
                    <div class="table-responsive">
                        <table class="table table-sm table-bordered mb-0">
                            <thead class="bg-light">
                                <tr>
                                    <th>Material</th>
                                    <th class="text-right">Consumo real</th>
                                </tr>
                            </thead>
                            <tbody id="tbodyConsumoModal"></tbody>
                        </table>
                    </div>
                    <p id="pConsumoVacio" class="text-muted mt-2 d-none">
                        Este registro no tiene consumos de materiales capturados.
                    </p>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-dismiss="modal">Cerrar</button>
            </div>
        </div>
    </div>
</div>

<!-- ══ SCRIPTS ══ -->
<script>
    // SweetAlert mensajes
    window.addEventListener('load', function () {
        var h = document.getElementById('<%= hdnMensajePendiente.ClientID %>');
        if (h && h.value) {
            try {
                var m = JSON.parse(h.value);
                h.value = '';
                Swal.fire({ icon: m.icon, title: m.title, text: m.text, confirmButtonColor: '#003366' }).then(function () {
                    if (m.modal) $('#' + m.modal).modal('show');
                });
            } catch (e) { }
        }
    });

    // ── Autocomplete modal "Registrar nueva producción" ──────────────────────
    var _prodTimer = null;

    function onTxtProductoInput() {
        clearTimeout(_prodTimer);
        var q = document.getElementById('txtProducto').value.trim();
        document.getElementById('<%= hdnProductoSeleccionado.ClientID %>').value = '';
        document.getElementById('<%= hdnProductoNombre.ClientID %>').value = '';
        if (q.length < 2) { ocultarResultadosProducto(); return; }
        _prodTimer = setTimeout(function () { buscarProductoModal(q); }, 300);
    }

    function buscarProductoModal(query) {
        fetch('<%= ResolveUrl("~/Produccion.aspx/BuscarProductosFiltro") %>', {
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify({ query: query })
        })
        .then(function (r) { return r.json(); })
        .then(function (data) { mostrarResultadosProducto(data.d); })
        .catch(function () { ocultarResultadosProducto(); });
    }

    function mostrarResultadosProducto(items) {
        var div = document.getElementById('divResultadosProducto');
        div.innerHTML = '';
        if (!items || items.length === 0) {
            div.innerHTML = '<div style="padding:8px 10px;color:#888;font-size:.84rem;">Sin resultados</div>';
            div.style.display = '';
            return;
        }
        items.forEach(function (item) {
            var el = document.createElement('div');
            el.style.cssText = 'padding:7px 10px;cursor:pointer;font-size:.84rem;border-bottom:1px solid #f0f0f0;';
            el.textContent   = item.nombre;
            el.onmouseenter  = function () { el.style.background = '#e8f0fe'; };
            el.onmouseleave  = function () { el.style.background = ''; };
            el.onmousedown   = function (e) {
                e.preventDefault();
                document.getElementById('txtProducto').value = item.nombre;
                document.getElementById('<%= hdnProductoSeleccionado.ClientID %>').value = item.id;
                document.getElementById('<%= hdnProductoNombre.ClientID %>').value = item.nombre;
                ocultarResultadosProducto();
                var f = document.getElementById('txtFiltroConsumo');
                if (f) { f.value = ''; filtrarConsumos(''); }
                document.getElementById('<%= btnCargarConsumos.ClientID %>').click();
            };
            div.appendChild(el);
        });
        div.style.display = '';
    }

    function ocultarResultadosProducto() {
        document.getElementById('divResultadosProducto').style.display = 'none';
    }

    document.addEventListener('click', function (e) {
        var txt = document.getElementById('txtProducto');
        var div = document.getElementById('divResultadosProducto');
        if (txt && !txt.contains(e.target) && div && !div.contains(e.target))
            ocultarResultadosProducto();
    });

    // Restaurar texto del producto tras postback (carga BOM, continuar borrador)
    (function () {
        var nombre = document.getElementById('<%= hdnProductoNombre.ClientID %>').value;
        if (nombre) document.getElementById('txtProducto').value = nombre;
    })();

    // ── Autocomplete modal "Hoja de Fabricación" ──────────────────────────────
    var _hojaTimer = null;

    function onTxtProductoHojaInput() {
        clearTimeout(_hojaTimer);
        var q = document.getElementById('txtProductoHoja').value.trim();
        document.getElementById('hdnProductoHojaID').value = '';
        if (q.length < 2) { ocultarResultadosProductoHoja(); return; }
        _hojaTimer = setTimeout(function () { buscarProductoHoja(q); }, 300);
    }

    function buscarProductoHoja(query) {
        fetch('<%= ResolveUrl("~/Produccion.aspx/BuscarProductosFiltro") %>', {
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify({ query: query })
        })
        .then(function (r) { return r.json(); })
        .then(function (data) { mostrarResultadosProductoHoja(data.d); })
        .catch(function () { ocultarResultadosProductoHoja(); });
    }

    function mostrarResultadosProductoHoja(items) {
        var div = document.getElementById('divResultadosProductoHoja');
        div.innerHTML = '';
        if (!items || items.length === 0) {
            div.innerHTML = '<div style="padding:8px 10px;color:#888;font-size:.84rem;">Sin resultados</div>';
            div.style.display = '';
            return;
        }
        items.forEach(function (item) {
            var el = document.createElement('div');
            el.style.cssText = 'padding:7px 10px;cursor:pointer;font-size:.84rem;border-bottom:1px solid #f0f0f0;';
            el.textContent   = item.nombre;
            el.onmouseenter  = function () { el.style.background = '#e8f0fe'; };
            el.onmouseleave  = function () { el.style.background = ''; };
            el.onmousedown   = function (e) {
                e.preventDefault();
                document.getElementById('txtProductoHoja').value   = item.nombre;
                document.getElementById('hdnProductoHojaID').value = item.id;
                ocultarResultadosProductoHoja();
            };
            div.appendChild(el);
        });
        div.style.display = '';
    }

    function ocultarResultadosProductoHoja() {
        document.getElementById('divResultadosProductoHoja').style.display = 'none';
    }

    document.addEventListener('click', function (e) {
        var txt2 = document.getElementById('txtProductoHoja');
        var div2 = document.getElementById('divResultadosProductoHoja');
        if (txt2 && !txt2.contains(e.target) && div2 && !div2.contains(e.target))
            ocultarResultadosProductoHoja();
    });

    // Filtrar filas de tblConsumos por código o nombre de material (JS puro, sin postback)
    function filtrarConsumos(valor) {
        var q = valor.trim().toLowerCase();
        var rows = document.querySelectorAll('#tblConsumos tbody tr');
        var visibles = 0;
        rows.forEach(function (tr) {
            var texto = tr.cells[0].textContent.toLowerCase();
            var mostrar = !q || texto.indexOf(q) !== -1;
            tr.style.display = mostrar ? '' : 'none';
            if (mostrar) visibles++;
        });
        var span = document.getElementById('spanResultadosFiltro');
        if (span) span.textContent = q ? (visibles + ' resultado(s)') : '';
    }

    // Actualizar el total producido y recalcular consumos a descontar
    function actualizarTotalProd() {
        var buena   = parseInt(document.getElementById('<%= txtCantBuena.ClientID %>').value) || 0;
        var rechazo = parseInt(document.getElementById('<%= txtCantRechazo.ClientID %>').value) || 0;
        var total   = buena + rechazo;
        document.getElementById('divTotalProd').innerText = total;

        document.querySelectorAll('#tblConsumos tbody .consumo-cell').forEach(function (td) {
            var perCap = parseFloat(td.dataset.percap) || 0;
            var resultado = perCap * total;
            var txt = resultado === 0 ? '0' : resultado.toFixed(2).replace(/\.?0+$/, '');
            td.querySelector('.consumo-total').textContent = txt;
        });
    }



    // Formatea una fecha local como YYYY-MM-DD (sin conversión UTC)
    function fmtFecha(d) {
        var mm = String(d.getMonth() + 1).padStart(2, '0');
        var dd = String(d.getDate()).padStart(2, '0');
        return d.getFullYear() + '-' + mm + '-' + dd;
    }

    // Filtros rápidos de fecha
    function setFiltroRapido(tipo) {
        var desde = document.getElementById('<%= txtFechaDesde.ClientID %>');
        var hasta = document.getElementById('<%= txtFechaHasta.ClientID %>');
        var hoy = new Date();

        if (tipo === 'hoy') {
            desde.value = fmtFecha(hoy);
            hasta.value = fmtFecha(hoy);
        } else if (tipo === 'semana') {
            var lunes = new Date(hoy);
            lunes.setDate(hoy.getDate() - ((hoy.getDay() + 6) % 7));
            desde.value = fmtFecha(lunes);
            hasta.value = fmtFecha(hoy);
        } else if (tipo === 'mes') {
            desde.value = fmtFecha(new Date(hoy.getFullYear(), hoy.getMonth(), 1));
            hasta.value = fmtFecha(hoy);
        }

        document.querySelectorAll('.btn-filtro-rapido').forEach(function (b) { b.classList.remove('active'); });
        event.target.classList.add('active');
    }

    // Resetear estado de borrador cuando el modal se cierra sin postback
    window.addEventListener('load', function () {
        $('#modalRegistrar').on('hidden.bs.modal', function () {
            var hdnID  = document.getElementById('<%= hdnProduccionID.ClientID %>');
            var hdnMod = document.getElementById('<%= hdnModoEdicion.ClientID %>');
            var titulo = document.querySelector('#modalRegistrar .modal-title');
            if (hdnID)  hdnID.value  = '0';
            if (hdnMod) hdnMod.value = '';
            if (titulo) titulo.textContent = 'Registrar Producción';
        });
    });

    // ══ Modal de consumos — carga lazy vía AJAX ══════════════════════════

    function verConsumos(produccionID, producto) {
        document.getElementById('spanConsumoProducto').textContent = producto;
        document.getElementById('divConsumoSpinner').classList.remove('d-none');
        document.getElementById('divConsumoError').classList.add('d-none');
        document.getElementById('divConsumoTabla').classList.add('d-none');
        document.getElementById('tbodyConsumoModal').innerHTML = '';
        document.getElementById('pConsumoVacio').classList.add('d-none');
        $('#modalConsumos').modal('show');

        $.ajax({
            type: 'POST',
            url: 'Produccion.aspx/ObtenerConsumos',
            data: JSON.stringify({ produccionID: produccionID }),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (resp) {
                var consumos = resp.d;
                document.getElementById('divConsumoSpinner').classList.add('d-none');
                if (!consumos || consumos.length === 0) {
                    document.getElementById('divConsumoTabla').classList.remove('d-none');
                    document.getElementById('pConsumoVacio').classList.remove('d-none');
                    return;
                }
                var tbody = document.getElementById('tbodyConsumoModal');
                consumos.forEach(function (c) {
                    tbody.insertAdjacentHTML('beforeend', buildConsumoRow(c));
                });
                document.getElementById('divConsumoTabla').classList.remove('d-none');
            },
            error: function (xhr) {
                document.getElementById('divConsumoSpinner').classList.add('d-none');
                var msg = 'Error al cargar los consumos.';
                try { var p = JSON.parse(xhr.responseText); if (p && p.Message) msg = p.Message; } catch (e) { }
                var errDiv = document.getElementById('divConsumoError');
                errDiv.textContent = msg;
                errDiv.classList.remove('d-none');
            }
        });
    }

    function buildConsumoRow(c) {
        var col1 = '<td style="white-space:nowrap">' + escHtml(c.MaterialCodigo) + ' - ' + escHtml(c.MaterialNombre) + '</td>';

        var realStr = c.TieneCaptura
            ? fmtN2(c.RealCap) + ' <small class="text-muted">' + escHtml(c.UnidadCap) + '</small>'
            : fmtN2(c.Real)    + ' <small class="text-muted">' + escHtml(c.UnidadClave) + '</small>';
        var col2 = '<td class="text-right" style="white-space:nowrap">' + realStr + '</td>';

        return '<tr>' + col1 + col2 + '</tr>';
    }

    function fmtN2(n) { return Number(n).toLocaleString('es-MX', { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
    function escHtml(s) {
        if (!s) return '';
        return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    // ── Autocomplete filtro producto ──────────────────────────────────────────
    var _filtrProductoTimer = null;

    function onBuscarFiltrProducto() {
        clearTimeout(_filtrProductoTimer);
        var q = document.getElementById('txtFiltrProducto').value.trim();
        document.getElementById('hdnFiltrProductoID').value     = '';
        document.getElementById('hdnFiltrProductoNombre').value = '';
        if (q.length < 2) { ocultarResultadosFiltrProducto(); return; }
        _filtrProductoTimer = setTimeout(function () { buscarFiltrProducto(q); }, 300);
    }

    function buscarFiltrProducto(query) {
        fetch('<%= ResolveUrl("~/Produccion.aspx/BuscarProductosFiltro") %>', {
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify({ query: query })
        })
        .then(function (r) { return r.json(); })
        .then(function (data) { mostrarResultadosFiltrProducto(data.d); })
        .catch(function () { ocultarResultadosFiltrProducto(); });
    }

    function mostrarResultadosFiltrProducto(items) {
        var div = document.getElementById('divResultadosFiltrProducto');
        div.innerHTML = '';
        if (!items || items.length === 0) {
            div.innerHTML = '<div style="padding:8px 10px;color:#888;font-size:.84rem;">Sin resultados</div>';
            div.style.display = '';
            return;
        }
        items.forEach(function (item) {
            var el = document.createElement('div');
            el.style.cssText = 'padding:7px 10px;cursor:pointer;font-size:.84rem;border-bottom:1px solid #f0f0f0;';
            el.textContent   = item.nombre;
            el.onmouseenter  = function () { el.style.background = '#e8f0fe'; };
            el.onmouseleave  = function () { el.style.background = ''; };
            el.onmousedown   = function (e) {
                e.preventDefault();
                document.getElementById('txtFiltrProducto').value       = item.nombre;
                document.getElementById('hdnFiltrProductoID').value     = item.id;
                document.getElementById('hdnFiltrProductoNombre').value = item.nombre;
                ocultarResultadosFiltrProducto();
            };
            div.appendChild(el);
        });
        div.style.display = '';
    }

    function ocultarResultadosFiltrProducto() {
        document.getElementById('divResultadosFiltrProducto').style.display = 'none';
    }

    document.addEventListener('click', function (e) {
        var txt = document.getElementById('txtFiltrProducto');
        var div = document.getElementById('divResultadosFiltrProducto');
        if (txt && !txt.contains(e.target) && div && !div.contains(e.target))
            ocultarResultadosFiltrProducto();
    });

    // Restaurar texto del filtro producto tras postback
    (function () {
        var nombre = document.getElementById('hdnFiltrProductoNombre').value;
        if (nombre) document.getElementById('txtFiltrProducto').value = nombre;
    })();
</script>

</asp:Content>

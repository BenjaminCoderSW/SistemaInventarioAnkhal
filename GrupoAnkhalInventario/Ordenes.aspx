<%@ Page Title="Órdenes de Entrega" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Ordenes.aspx.cs" Inherits="GrupoAnkhalInventario.Ordenes" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="css/gridviewPantalla.css" rel="stylesheet" />
    <style>
        /* ── Dashboard ── */
        .stock-dashboard { display:flex; gap:14px; margin-bottom:18px; flex-wrap:wrap; }
        .stock-card {
            flex:1; min-width:140px; border-radius:10px; padding:16px 20px; color:#fff;
            display:flex; align-items:center; gap:14px;
            box-shadow:0 3px 10px rgba(0,0,0,.15);
            transition:transform .15s,box-shadow .15s;
        }
        .stock-card:hover { transform:translateY(-3px); box-shadow:0 6px 16px rgba(0,0,0,.2); }
        .stock-card.total      { background:linear-gradient(135deg,#1a5276,#2980b9); }
        .stock-card.abiertas   { background:linear-gradient(135deg,#0e6655,#1abc9c); }
        .stock-card.parciales  { background:linear-gradient(135deg,#784212,#ca6f1e); }
        .stock-card.completadas{ background:linear-gradient(135deg,#1e8449,#27ae60); }
        .stock-card.canceladas { background:linear-gradient(135deg,#922b21,#e74c3c); }
        .stock-card.valor      { background:linear-gradient(135deg,#1c2833,#2c3e50); }
        .stock-card .icon { font-size:2.2rem; opacity:.9; }
        .stock-card .info .num { font-size:1.8rem; font-weight:700; line-height:1; }
        .stock-card .info .lbl { font-size:.78rem; opacity:.9; text-transform:uppercase; letter-spacing:.5px; }

        /* ── Filtros ── */
        .filtros-bar {
            background:#f8f9fa; border:1px solid #dee2e6;
            border-radius:8px; padding:14px 18px; margin-bottom:14px;
        }
        .filtros-bar label { font-weight:600; font-size:.84rem; color:#003366; margin-bottom:2px; }

        /* ── Paginador ── */
        .pager-custom span { background:#003366; color:#fff; font-weight:700; border-radius:4px; padding:4px 9px; }
        .pager-custom a { padding:4px 9px; border-radius:4px; }

        /* ── Badges estado ── */
        .badge-abierta    { background:#1abc9c; color:#fff; }
        .badge-parcial    { background:#ca6f1e; color:#fff; }
        .badge-completada { background:#27ae60; color:#fff; }
        .badge-cancelada  { background:#e74c3c; color:#fff; }
        .badge-pendiente       { background:#8e44ad; color:#fff; }
        .badge-pendiente-stock { background:#8e44ad; color:#fff; }
        .badge-entregada       { background:#27ae60; color:#fff; }

        /* ── Tabla items del modal ── */
        .items-table th { background:#003366; color:#fff; font-size:.82rem; padding:6px 10px; }
        .items-table td { font-size:.85rem; padding:5px 10px; vertical-align:middle; }

        /* ── Tabla de progreso ── */
        .progress-bar-orden { height:10px; border-radius:5px; }
        .detalle-header { background:#f8f9fa; border-radius:8px; padding:14px 18px; margin-bottom:12px; }
        .detalle-header label { font-weight:600; color:#003366; font-size:.84rem; }

        /* ── Tabla partida: inputs de cantidad ── */
        .input-cant-entregar { width:90px; text-align:right; }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

<!-- ══ DASHBOARD ══ -->
<div class="stock-dashboard">
    <div class="stock-card total">
        <div class="icon"><i class="fas fa-clipboard-list"></i></div>
        <div class="info">
            <div class="num"><asp:Label ID="lblTotal" runat="server" Text="0"></asp:Label></div>
            <div class="lbl">&Oacute;rdenes del per&iacute;odo</div>
        </div>
    </div>
    <div class="stock-card abiertas">
        <div class="icon"><i class="fas fa-folder-open"></i></div>
        <div class="info">
            <div class="num"><asp:Label ID="lblAbiertas" runat="server" Text="0"></asp:Label></div>
            <div class="lbl">Abiertas</div>
        </div>
    </div>
    <div class="stock-card parciales">
        <div class="icon"><i class="fas fa-hourglass-half"></i></div>
        <div class="info">
            <div class="num"><asp:Label ID="lblParciales" runat="server" Text="0"></asp:Label></div>
            <div class="lbl">Parcialmente entregadas</div>
        </div>
    </div>
    <div class="stock-card completadas">
        <div class="icon"><i class="fas fa-check-double"></i></div>
        <div class="info">
            <div class="num"><asp:Label ID="lblCompletadas" runat="server" Text="0"></asp:Label></div>
            <div class="lbl">Completadas</div>
        </div>
    </div>
    <div class="stock-card canceladas">
        <div class="icon"><i class="fas fa-times-circle"></i></div>
        <div class="info">
            <div class="num"><asp:Label ID="lblCanceladas" runat="server" Text="0"></asp:Label></div>
            <div class="lbl">Canceladas</div>
        </div>
    </div>
</div>
<div class="stock-dashboard" style="margin-bottom:18px;">
    <div class="stock-card valor" style="flex:0 0 100%;">
        <div class="icon"><i class="fas fa-dollar-sign"></i></div>
        <div class="info">
            <div class="num" style="font-size:2.4rem;">
                <asp:Label ID="lblValorTotal" runat="server" Text="$0.00"></asp:Label>
            </div>
            <div class="lbl">Valor total entregado en el per&iacute;odo (entregas confirmadas)</div>
        </div>
    </div>
</div>

<!-- ══ FILTROS ══ -->
<div class="filtros-bar">
    <div class="row align-items-end">
        <div class="col-md-2">
            <label>Base</label>
            <asp:DropDownList ID="ddlFiltrBase" runat="server" CssClass="form-control form-control-sm"></asp:DropDownList>
        </div>
        <div class="col-md-2">
            <label>Estado</label>
            <asp:DropDownList ID="ddlFiltrEstado" runat="server" CssClass="form-control form-control-sm">
                <asp:ListItem Text="-- Todos --"  Value="" />
                <asp:ListItem Text="Abierta"      Value="ABIERTA" />
                <asp:ListItem Text="Parcial"      Value="PARCIAL" />
                <asp:ListItem Text="Completada"   Value="COMPLETADA" />
                <asp:ListItem Text="Cancelada"    Value="CANCELADA" />
            </asp:DropDownList>
        </div>
        <div class="col-md-2">
            <label>Cliente</label>
            <asp:TextBox ID="txtFiltrCliente" runat="server" CssClass="form-control form-control-sm" placeholder="Nombre..."></asp:TextBox>
        </div>
        <div class="col-md-1">
            <label>Folio</label>
            <asp:TextBox ID="txtFiltrFolio" runat="server" CssClass="form-control form-control-sm" placeholder="ORD-..."></asp:TextBox>
        </div>
        <div class="col-md-2">
            <label>Desde</label>
            <asp:TextBox ID="txtFiltrDesde" runat="server" CssClass="form-control form-control-sm" TextMode="Date"></asp:TextBox>
        </div>
        <div class="col-md-2">
            <label>Hasta</label>
            <asp:TextBox ID="txtFiltrHasta" runat="server" CssClass="form-control form-control-sm" TextMode="Date"></asp:TextBox>
        </div>
        <div class="col-md-1">
            <asp:Button ID="btnBuscar" runat="server" Text="Buscar"
                CssClass="btn btn-sm btn-primary btn-block mb-1" OnClick="btnBuscar_Click" />
            <asp:Button ID="btnLimpiar" runat="server" Text="Limpiar"
                CssClass="btn btn-sm btn-outline-secondary btn-block" OnClick="btnLimpiar_Click" />
        </div>
    </div>
</div>

<!-- ══ ACCIONES ══ -->
<div class="d-flex justify-content-between align-items-center mb-2">
    <div>
        <asp:Button ID="btnNuevoOrden" runat="server" Text="+ Nueva Orden"
            CssClass="btn btn-primary" OnClick="btnNuevoOrden_Click" />
    </div>
    <asp:Label ID="lblResultados" runat="server" CssClass="text-muted small"></asp:Label>
</div>

<!-- ══ GRID ══ -->
<div class="table-responsive">
    <asp:GridView ID="gvOrdenes" runat="server" AutoGenerateColumns="False"
        CssClass="table table-bordered table-striped custom-grid"
        AllowCustomPaging="True" AllowPaging="True" PageSize="15"
        OnPageIndexChanging="gvOrdenes_PageIndexChanging"
        EmptyDataText="No se encontraron órdenes."
        PagerStyle-CssClass="pager-custom"
        PagerSettings-Mode="NumericFirstLast"
        PagerSettings-FirstPageText="«"
        PagerSettings-LastPageText="»"
        PagerSettings-PageButtonCount="5">
        <Columns>
            <asp:BoundField DataField="Folio"        HeaderText="Folio" />
            <asp:BoundField DataField="FechaOrden"   HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:BoundField DataField="BaseNombre"   HeaderText="Base" />
            <asp:BoundField DataField="ClienteNombre" HeaderText="Cliente" />
            <asp:TemplateField HeaderText="Progreso">
                <ItemStyle CssClass="text-center" Width="130px" />
                <ItemTemplate>
                    <small class="d-block"><%# Eval("ItemsCompletados") %>/<%# Eval("NumItems") %> &iacute;tems</small>
                    <div class="progress" style="height:8px;">
                        <div class="progress-bar bg-success progress-bar-orden"
                             style="width:<%# Eval("PorcentajeAvance") %>%"></div>
                    </div>
                    <small class="text-muted"><%# Eval("PorcentajeAvance", "{0:0}") %>%</small>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="TotalValor" HeaderText="Total ($)" DataFormatString="{0:C2}" ItemStyle-CssClass="text-right font-weight-bold" />
            <asp:TemplateField HeaderText="Estado">
                <ItemStyle CssClass="text-center" />
                <ItemTemplate>
                    <span class='badge <%# GetBadgeEstadoOrden(Eval("Estado").ToString()) %>'>
                        <%# Eval("Estado") %>
                    </span>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Acciones">
                <HeaderStyle CssClass="text-center" Width="220px" />
                <ItemStyle CssClass="text-center" />
                <ItemTemplate>
                    <button type="button" class="btn btn-xs btn-info"
                        onclick="verHistorial(<%# Eval("OrdenID") %>)">
                        <i class="fas fa-eye"></i> Historial
                    </button>
                    <button type="button" class="btn btn-xs btn-secondary"
                        onclick="imprimirOrden(<%# Eval("OrdenID") %>)">
                        <i class="fas fa-print"></i>
                    </button>
                    <%# MostrarBtnRegistrarEntrega(Eval("Estado").ToString(), Eval("OrdenID").ToString()) %>
                    <%# MostrarBtnCancelarOrden(Eval("Estado").ToString(), Eval("OrdenID").ToString()) %>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
</div>

<!-- ══════════════════════════════════════════════════════════════════ -->
<!-- HIDDEN FIELDS Y BOTONES DE ACCIÓN                                -->
<!-- ══════════════════════════════════════════════════════════════════ -->
<asp:HiddenField ID="hdnMensajePendiente"  runat="server" Value="" />
<asp:HiddenField ID="hdnForzarLimiteCredito" runat="server" Value="" />
<asp:HiddenField ID="hdnOrdenIDAccion"     runat="server" Value="" />
<asp:HiddenField ID="hdnAccion"            runat="server" Value="" />
<asp:HiddenField ID="hdnItemsJson"         runat="server" Value="[]" />
<asp:HiddenField ID="hdnDetalleJson"       runat="server" Value="" />
<asp:HiddenField ID="hdnPartidaJson"       runat="server" Value="" />
<asp:HiddenField ID="hdnEntregaItemsJson"  runat="server" Value="[]" />
<asp:HiddenField ID="hdnEntregaIDAccion"   runat="server" Value="" />
<asp:HiddenField ID="hdnModoGuardado"      runat="server" Value="" />
<asp:HiddenField ID="hdnNumeroFactura"     runat="server" Value="" />
<asp:HiddenField ID="hdnFechaEntregaConf"  runat="server" Value="" />

<!-- Botón para acciones del grid (ver, confirmar, cancelar, imprimir) -->
<asp:Button ID="btnProcesarAccion" runat="server" style="display:none"
    OnClick="btnProcesarAccion_Click" />

<!-- Botón para guardar la entrega parcial (desde modal Registrar Entrega) -->
<asp:Button ID="btnGuardarPartida" runat="server" style="display:none"
    OnClick="btnGuardarPartida_Click" />

<!-- ══════════════════════════════════════════════════════════════════ -->
<!-- MODAL: NUEVA ORDEN                                                -->
<!-- ══════════════════════════════════════════════════════════════════ -->
<div class="modal fade" id="modalNuevo" tabindex="-1" data-backdrop="static">
    <div class="modal-dialog modal-xl">
        <div class="modal-content">
            <div class="modal-header bg-primary text-white">
                <h5 class="modal-title"><i class="fas fa-clipboard-list mr-2"></i>Nueva Orden de Entrega</h5>
                <button type="button" class="close text-white" data-dismiss="modal">&times;</button>
            </div>
            <div class="modal-body">

                <!-- Cabecera de la orden -->
                <div class="row mb-3">
                    <div class="col-md-3">
                        <label class="font-weight-bold">Folio <span class="text-muted small">(auto)</span></label>
                        <asp:TextBox ID="txtNuevoFolio" runat="server" CssClass="form-control" ReadOnly="True"
                            placeholder="Se genera automáticamente"></asp:TextBox>
                    </div>
                    <div class="col-md-3">
                        <label class="font-weight-bold">Fecha <span class="text-danger">*</span></label>
                        <asp:TextBox ID="txtNuevoFecha" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                    </div>
                    <div class="col-md-3">
                        <label class="font-weight-bold">Base Origen <span class="text-danger">*</span></label>
                        <asp:DropDownList ID="ddlNuevoBase" runat="server" CssClass="form-control"></asp:DropDownList>
                    </div>
                    <div class="col-md-3">
                        <label class="font-weight-bold">Cliente <span class="text-danger">*</span></label>
                        <asp:DropDownList ID="ddlNuevoCliente" runat="server" CssClass="form-control"
                            onchange="onClienteOrdenChange()"></asp:DropDownList>
                    </div>
                </div>
                <div class="row mb-3">
                    <div class="col-md-12">
                        <label class="font-weight-bold">Observaciones</label>
                        <asp:TextBox ID="txtNuevoObservaciones" runat="server" CssClass="form-control"
                            TextMode="MultiLine" Rows="2" MaxLength="500"></asp:TextBox>
                    </div>
                </div>
                <div class="row mb-3">
                    <div class="col-md-4">
                        <div class="form-check">
                            <asp:CheckBox ID="chkEsCreditoOrden" runat="server" CssClass="form-check-input" />
                            <label class="form-check-label font-weight-bold" for="<%= chkEsCreditoOrden.ClientID %>">
                                Es a crédito (aplica como default a sus entregas parciales)
                            </label>
                        </div>
                    </div>
                </div>

                <hr />

                <!-- Agregar ítems a la orden -->
                <h6 class="text-primary font-weight-bold mb-2">
                    <i class="fas fa-boxes mr-1"></i> &Iacute;tems de la Orden
                </h6>
                <div class="row align-items-end mb-2">
                    <div class="col-md-2">
                        <label class="font-weight-bold small">Tipo</label>
                        <select id="selTipoItem" class="form-control form-control-sm" onchange="onTipoItemChange()">
                            <option value="PRODUCTO">Producto</option>
                            <option value="MATERIAL">Material</option>
                        </select>
                    </div>
                    <div class="col-md-4">
                        <label class="font-weight-bold small">Buscar <span id="lblTipoBusqueda">Producto</span></label>
                        <div style="position:relative;">
                            <input type="text" id="txtBuscarItem" class="form-control form-control-sm"
                                   placeholder="Escribe para buscar..." autocomplete="off"
                                   oninput="onBuscarItem()" />
                            <div id="divResultadosBusqueda"
                                 style="display:none; position:absolute; z-index:9999; width:100%;
                                        background:#fff; border:1px solid #ccc; border-radius:4px;
                                        max-height:220px; overflow-y:auto;
                                        box-shadow:0 2px 8px rgba(0,0,0,.18);">
                            </div>
                        </div>
                        <input type="hidden" id="hdnItemID" />
                    </div>
                    <div class="col-md-2">
                        <label class="font-weight-bold small">Cantidad Pedida <span class="text-danger">*</span></label>
                        <input type="number" id="txtItemCantidad" class="form-control form-control-sm" min="0.01" step="0.01" value="1" />
                    </div>
                    <div class="col-md-2" id="divUnidadMaterial" style="display:none;">
                        <label class="font-weight-bold small">Unidad</label>
                        <select id="selUnidadMaterial" class="form-control form-control-sm" onchange="onUnidadMaterialChange()"></select>
                    </div>
                    <div class="col-md-2">
                        <label class="font-weight-bold small">Precio Unit.</label>
                        <input type="number" id="txtItemPrecio" class="form-control form-control-sm"
                            step="0.01" min="0" value="0.00" />
                    </div>
                    <div class="col-md-2 mt-auto">
                        <button type="button" class="btn btn-success btn-sm btn-block" onclick="agregarItem()">
                            <i class="fas fa-plus"></i> Agregar
                        </button>
                    </div>
                </div>

                <!-- Tabla de ítems agregados -->
                <div class="table-responsive">
                    <table class="table table-sm table-bordered items-table" id="tblItems">
                        <thead>
                            <tr>
                                <th>Tipo</th>
                                <th>Descripci&oacute;n</th>
                                <th class="text-right" style="width:100px">Cant. Pedida</th>
                                <th style="width:90px">Unidad</th>
                                <th class="text-right" style="width:100px">Precio Unit.</th>
                                <th class="text-right" style="width:100px">Subtotal</th>
                                <th style="width:40px"></th>
                            </tr>
                        </thead>
                        <tbody id="tbodyItems">
                            <tr id="trItemsVacios">
                                <td colspan="6" class="text-center" style="color:#868e96;font-style:italic;">
                                    A&uacute;n no hay &iacute;tems. Use el formulario de arriba para agregar.
                                </td>
                            </tr>
                        </tbody>
                        <tfoot>
                            <tr>
                                <td colspan="5" class="text-right font-weight-bold">Total:</td>
                                <td class="text-right font-weight-bold" id="tdTotalItems">$0.00</td>
                                <td></td>
                            </tr>
                        </tfoot>
                    </table>
                </div>

            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancelar</button>
                <asp:Button ID="btnGuardarOrden" runat="server" Text="Guardar Orden"
                    CssClass="btn btn-primary"
                    OnClientClick="return validarModalNuevo();"
                    OnClick="btnGuardarOrden_Click" />
            </div>
        </div>
    </div>
</div>

<!-- ══════════════════════════════════════════════════════════════════ -->
<!-- MODAL: REGISTRAR ENTREGA PARCIAL                                  -->
<!-- ══════════════════════════════════════════════════════════════════ -->
<div class="modal fade" id="modalRegistrarEntrega" tabindex="-1" data-backdrop="static">
    <div class="modal-dialog modal-xl">
        <div class="modal-content">
            <div class="modal-header bg-warning text-dark">
                <h5 class="modal-title"><i class="fas fa-truck mr-2"></i>Registrar Entrega Parcial</h5>
                <button type="button" class="close" data-dismiss="modal">&times;</button>
            </div>
            <div class="modal-body">

                <!-- Cabecera de la orden (solo lectura) -->
                <div class="detalle-header mb-3">
                    <div class="row">
                        <div class="col-md-3"><label>Orden</label><div id="pFolioOrden" class="font-weight-bold">—</div></div>
                        <div class="col-md-3"><label>Cliente</label><div id="pClienteOrden">—</div></div>
                        <div class="col-md-3"><label>Base</label><div id="pBaseOrden">—</div></div>
                        <div class="col-md-3"><label>Estado</label><div id="pEstadoOrden">—</div></div>
                    </div>
                </div>

                <!-- Tabla de ítems con cantidades a entregar -->
                <h6 class="text-primary font-weight-bold mb-2">
                    <i class="fas fa-boxes mr-1"></i> &Iacute;tems &mdash; Ingresa la cantidad a entregar en esta operaci&oacute;n
                </h6>
                <div class="table-responsive mb-3">
                    <table class="table table-sm table-bordered items-table" id="tblPartida">
                        <thead>
                            <tr>
                                <th>Tipo</th>
                                <th>Descripci&oacute;n</th>
                                <th class="text-right">Pedido</th>
                                <th class="text-right">Entregado</th>
                                <th class="text-right">Pendiente <small>(base)</small></th>
                                <th class="text-center" style="width:110px">Entregar Ahora</th>
                                <th style="width:160px">Unidad</th>
                            </tr>
                        </thead>
                        <tbody id="tbodyPartida">
                        </tbody>
                    </table>
                </div>

                <!-- Datos de la entrega -->
                <div class="row">
                    <div class="col-md-3">
                        <label class="font-weight-bold">Fecha de Entrega <span class="text-danger">*</span></label>
                        <asp:TextBox ID="txtPartidaFecha" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                    </div>
                    <div class="col-md-3">
                        <label class="font-weight-bold">N&uacute;mero de Factura <span class="text-muted small">(opcional)</span></label>
                        <asp:TextBox ID="txtPartidaFactura" runat="server" CssClass="form-control"
                            placeholder="FACT-001..." MaxLength="50"></asp:TextBox>
                    </div>
                    <div class="col-md-6">
                        <label class="font-weight-bold">Observaciones</label>
                        <asp:TextBox ID="txtPartidaObs" runat="server" CssClass="form-control"
                            TextMode="MultiLine" Rows="2" MaxLength="500"></asp:TextBox>
                    </div>
                </div>
                <div class="row mt-2">
                    <div class="col-md-4">
                        <div class="form-check">
                            <asp:CheckBox ID="chkEsCreditoPartida" runat="server" CssClass="form-check-input" />
                            <label class="form-check-label font-weight-bold" for="<%= chkEsCreditoPartida.ClientID %>">
                                Es a crédito (genera Cuenta por Cobrar para esta entrega)
                            </label>
                        </div>
                    </div>
                </div>

            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancelar</button>
                <button type="button" class="btn btn-warning" onclick="guardarPartida('PENDIENTE')">
                    <i class="fas fa-save mr-1"></i> Guardar como PENDIENTE
                </button>
                <button type="button" class="btn btn-success" onclick="guardarPartida('CONFIRMAR')">
                    <i class="fas fa-check mr-1"></i> Confirmar y Entregar
                </button>
            </div>
        </div>
    </div>
</div>

<!-- ══════════════════════════════════════════════════════════════════ -->
<!-- MODAL: VER HISTORIAL DE LA ORDEN                                  -->
<!-- ══════════════════════════════════════════════════════════════════ -->
<div class="modal fade" id="modalHistorial" tabindex="-1">
    <div class="modal-dialog modal-xl">
        <div class="modal-content">
            <div class="modal-header bg-info text-white">
                <h5 class="modal-title"><i class="fas fa-history mr-2"></i>Historial de la Orden</h5>
                <button type="button" class="close text-white" data-dismiss="modal">&times;</button>
            </div>
            <div class="modal-body">

                <!-- Datos de la orden -->
                <div class="detalle-header">
                    <div class="row">
                        <div class="col-md-3"><label>Folio</label><div id="hFolio">—</div></div>
                        <div class="col-md-3"><label>Fecha</label><div id="hFecha">—</div></div>
                        <div class="col-md-3"><label>Base</label><div id="hBase">—</div></div>
                        <div class="col-md-3"><label>Estado</label><div id="hEstado">—</div></div>
                    </div>
                    <div class="row mt-2">
                        <div class="col-md-6"><label>Cliente</label><div id="hCliente">—</div></div>
                        <div class="col-md-6"><label>Observaciones</label><div id="hObs">—</div></div>
                    </div>
                </div>

                <!-- Ítems de la orden con progreso -->
                <h6 class="text-primary font-weight-bold mt-3 mb-2">
                    <i class="fas fa-boxes mr-1"></i> &Iacute;tems de la Orden
                </h6>
                <div class="table-responsive mb-3">
                    <table class="table table-sm table-bordered">
                        <thead>
                            <tr style="background:#003366;color:#fff;">
                                <th>Tipo</th>
                                <th>Descripci&oacute;n</th>
                                <th class="text-right">Pedido</th>
                                <th class="text-right">Entregado</th>
                                <th class="text-right">Pendiente</th>
                                <th class="text-center">Avance</th>
                                <th class="text-center">Estado</th>
                            </tr>
                        </thead>
                        <tbody id="tbodyDetalleOrden"></tbody>
                    </table>
                </div>

                <!-- Entregas vinculadas -->
                <h6 class="text-primary font-weight-bold mb-2">
                    <i class="fas fa-truck mr-1"></i> Entregas Registradas
                </h6>
                <div class="table-responsive">
                    <table class="table table-sm table-bordered" id="tblEntregasVinculadas">
                        <thead>
                            <tr style="background:#003366;color:#fff;">
                                <th>Folio Entrega</th>
                                <th class="text-center">Fecha</th>
                                <th>Factura</th>
                                <th class="text-right">Total</th>
                                <th class="text-center">Estado</th>
                                <th class="text-center" style="width:180px">Acciones</th>
                            </tr>
                        </thead>
                        <tbody id="tbodyEntregasVinculadas"></tbody>
                    </table>
                    <div id="divSinEntregas" class="text-muted text-center py-3" style="display:none;">
                        Esta orden no tiene entregas registradas todav&iacute;a.
                    </div>
                </div>

            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-dismiss="modal">Cerrar</button>
            </div>
        </div>
    </div>
</div>

<!-- ══════════════════════════════════════════════════════════════════ -->
<!-- MODAL: CONFIRMAR ENTREGA (mini-modal)                             -->
<!-- ══════════════════════════════════════════════════════════════════ -->
<div class="modal fade" id="modalConfirmar" tabindex="-1" data-backdrop="static">
    <div class="modal-dialog modal-md">
        <div class="modal-content">
            <div class="modal-header bg-success text-white">
                <h5 class="modal-title"><i class="fas fa-check-circle mr-2"></i>Confirmar Entrega</h5>
                <button type="button" class="close text-white" data-dismiss="modal">&times;</button>
            </div>
            <div class="modal-body">
                <p class="mb-3">
                    Entrega: <strong id="cfFolioEntrega">—</strong><br/>
                    Al confirmar se descontar&aacute; el stock correspondiente.
                </p>
                <div class="form-group">
                    <label class="font-weight-bold">Fecha real de entrega <span class="text-danger">*</span></label>
                    <input type="date" id="cfFechaEntrega" class="form-control" />
                </div>
                <div class="form-group">
                    <label class="font-weight-bold">N&uacute;mero de Factura <span class="text-muted small">(opcional)</span></label>
                    <input type="text" id="cfNumeroFactura" class="form-control" placeholder="FACT-001..." maxlength="50" />
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancelar</button>
                <button type="button" class="btn btn-success" onclick="ejecutarConfirmacion()">
                    <i class="fas fa-check mr-1"></i> Confirmar Entrega
                </button>
            </div>
        </div>
    </div>
</div>

<!-- ══ SCRIPTS ══ -->
<script>
// ─────────────────────────────────────────────────────────────────
// Inicialización: SweetAlert, items guardados, modales
// ─────────────────────────────────────────────────────────────────
window.addEventListener('load', function () {
    // ── Fix Bootstrap 4 + ASP.NET WebForms ────────────────────────
    // Bootstrap pone aria-hidden="true" en <form#form1> mientras un modal
    // está abierto (para ocultar el fondo a screen-readers). Si el modal
    // también está DENTRO de form#form1, Chrome bloquea esa operación
    // porque un descendiente enfocado quedaría oculto. Resultado: el evento
    // hidden.bs.modal nunca dispara y el siguiente modal no puede abrirse.
    // Solución: mover los modales "de tránsito" fuera del form, directamente
    // bajo <body>. Así aria-hidden cae sobre form#form1 sin conflicto alguno.
    ['modalHistorial', 'modalConfirmar', 'modalDetalleEntrega'].forEach(function (id) {
        var el = document.getElementById(id);
        if (el && el.parentNode !== document.body) document.body.appendChild(el);
    });
    // ──────────────────────────────────────────────────────────────

    // SweetAlert desde servidor
    var h = document.getElementById('<%= hdnMensajePendiente.ClientID %>');
    if (h && h.value) {
        try {
            var m = JSON.parse(h.value);
            h.value = '';
            if (m.confirmar) {
                Swal.fire({
                    icon: 'warning', title: m.title, html: m.text,
                    showCancelButton: true, confirmButtonColor: '#e67e22',
                    confirmButtonText: 'Sí, continuar', cancelButtonText: 'Cancelar'
                }).then(function (r) {
                    if (!r.isConfirmed) return;
                    document.getElementById('<%= hdnForzarLimiteCredito.ClientID %>').value = 'true';
                    if (m.confirmar.accion === 'reintentar-guardar-partida') {
                        document.getElementById('<%= btnGuardarPartida.ClientID %>').click();
                    } else if (m.confirmar.accion === 'reintentar-confirmar-entrega-orden') {
                        document.getElementById('<%= hdnAccion.ClientID %>').value = 'confirmar-entrega';
                        document.getElementById('<%= hdnEntregaIDAccion.ClientID %>').value = m.confirmar.entregaId;
                        document.getElementById('<%= btnProcesarAccion.ClientID %>').click();
                    }
                });
            } else {
                Swal.fire({ icon: m.icon, title: m.title, text: m.text, confirmButtonColor: '#003366' })
                    .then(function () { if (m.modal) $('#' + m.modal).modal('show'); });
            }
        } catch (e) { }
    }

    // Restaurar ítems en modal Nueva Orden (postback con error)
    var hdnItems = document.getElementById('<%= hdnItemsJson.ClientID %>');
    if (hdnItems && hdnItems.value && hdnItems.value !== '[]') {
        try {
            var items = JSON.parse(hdnItems.value);
            items.forEach(function (it) { _items.push(it); });
            renderizarTabla();
        } catch (e) { }
    }

    // Abrir modal Registrar Entrega si hay datos de partida
    var hdnPartida = document.getElementById('<%= hdnPartidaJson.ClientID %>');
    if (hdnPartida && hdnPartida.value) {
        try {
            var p = JSON.parse(hdnPartida.value);
            if (p && p.OrdenID) {
                mostrarModalRegistrarEntrega(p);
            }
        } catch (e) { }
    }

    // Abrir modal Historial si hay datos de detalle
    var hdnDetalle = document.getElementById('<%= hdnDetalleJson.ClientID %>');
    if (hdnDetalle && hdnDetalle.value) {
        try {
            var d = JSON.parse(hdnDetalle.value);
            if (d && d.OrdenID) {
                mostrarModalHistorial(d);
                $('#modalHistorial').modal('show');
                hdnDetalle.value = '';
            }
        } catch (e) { }
    }
});

// ─────────────────────────────────────────────────────────────────
// Items del modal Nueva Orden (con soporte de conversiones de unidad)
// ─────────────────────────────────────────────────────────────────
var _items = [];
var _buscarTimer      = null;
var _itemSeleccionado = null;

function onTipoItemChange() {
    var tipo = document.getElementById('selTipoItem').value;
    document.getElementById('divUnidadMaterial').style.display = 'none';
    document.getElementById('txtBuscarItem').value = '';
    document.getElementById('hdnItemID').value     = '';
    _itemSeleccionado = null;
    ocultarResultados();
    document.getElementById('lblTipoBusqueda').textContent =
        tipo === 'PRODUCTO' ? 'Producto' : 'Material';
}

function onBuscarItem() {
    clearTimeout(_buscarTimer);
    var q = document.getElementById('txtBuscarItem').value.trim();
    _itemSeleccionado = null;
    document.getElementById('hdnItemID').value = '';
    if (q.length < 2) { ocultarResultados(); return; }
    _buscarTimer = setTimeout(function () { ejecutarBusqueda(q); }, 300);
}

function ejecutarBusqueda(query) {
    var tipo = document.getElementById('selTipoItem').value;
    fetch('<%= ResolveUrl("~/Ordenes.aspx/BuscarItems") %>', {
        method:  'POST',
        headers: { 'Content-Type': 'application/json' },
        body:    JSON.stringify({ query: query, tipo: tipo })
    })
    .then(function (r) { return r.json(); })
    .then(function (data) { mostrarResultados(data.d || data); })
    .catch(function () { ocultarResultados(); });
}

function mostrarResultados(items) {
    var div = document.getElementById('divResultadosBusqueda');
    div.innerHTML = '';
    if (!items || items.length === 0) {
        div.innerHTML = '<div style="padding:8px 10px;color:#888;font-size:.84rem;">Sin resultados</div>';
        div.style.display = ''; return;
    }
    items.forEach(function (item) {
        var el = document.createElement('div');
        el.style.cssText = 'padding:7px 10px;cursor:pointer;font-size:.84rem;border-bottom:1px solid #f0f0f0;';
        el.textContent   = item.nombre;
        el.onmouseenter  = function () { el.style.background = '#e8f0fe'; };
        el.onmouseleave  = function () { el.style.background = ''; };
        el.onmousedown   = function (ev) { ev.preventDefault(); seleccionarItem(item); };
        div.appendChild(el);
    });
    div.style.display = '';
}

function ocultarResultados() {
    document.getElementById('divResultadosBusqueda').style.display = 'none';
}

function seleccionarItem(item) {
    _itemSeleccionado = item;
    document.getElementById('txtBuscarItem').value = item.nombre;
    document.getElementById('hdnItemID').value     = item.id;
    document.getElementById('txtItemPrecio').value = item.precio;
    ocultarResultados();

    var tipo = document.getElementById('selTipoItem').value;
    if (tipo === 'MATERIAL') {
        poblarUnidadesMaterial(item.conversiones || []);
        document.getElementById('divUnidadMaterial').style.display = '';
    } else {
        document.getElementById('divUnidadMaterial').style.display = 'none';
    }
}

function poblarUnidadesMaterial(conversiones) {
    var sel = document.getElementById('selUnidadMaterial');
    sel.innerHTML = '';
    if (!conversiones || conversiones.length === 0) {
        var opt = document.createElement('option');
        opt.value = ''; opt.text = '— sin unidad —';
        opt.setAttribute('data-factor', '1');
        sel.appendChild(opt); return;
    }
    conversiones.forEach(function (op) {
        var opt = document.createElement('option');
        opt.value = op.valor; opt.text = op.texto;
        opt.setAttribute('data-factor', op.factor);
        sel.appendChild(opt);
    });
}

function onUnidadMaterialChange() { /* factor se lee al agregar */ }

document.addEventListener('click', function (e) {
    var inp = document.getElementById('txtBuscarItem');
    var res = document.getElementById('divResultadosBusqueda');
    if (inp && res && !inp.contains(e.target) && !res.contains(e.target))
        ocultarResultados();
});

function agregarItem() {
    var tipo = document.getElementById('selTipoItem').value;
    if (!_itemSeleccionado || !_itemSeleccionado.id) {
        Swal.fire('Campo requerido',
            'Busque y seleccione un ' + (tipo === 'PRODUCTO' ? 'producto' : 'material') + ' de la lista.',
            'warning');
        return;
    }
    var itemID   = parseInt(_itemSeleccionado.id);
    var nombre   = _itemSeleccionado.nombre;
    var cantidad = parseFloat(document.getElementById('txtItemCantidad').value) || 0;
    var precio   = parseFloat(document.getElementById('txtItemPrecio').value) || 0;
    if (cantidad <= 0) { alert('La cantidad debe ser mayor a 0.'); return; }

    var unidadVal = '', factor = 1, unidadTexto = '';
    if (tipo === 'MATERIAL') {
        var selU = document.getElementById('selUnidadMaterial');
        if (selU && selU.options.length > 0 && selU.value) {
            unidadVal   = selU.value;
            factor      = parseFloat(selU.options[selU.selectedIndex].getAttribute('data-factor')) || 1;
            unidadTexto = selU.options[selU.selectedIndex].text;
        }
    }

    // Acumular si ya existe el mismo tipo + item + unidad
    for (var i = 0; i < _items.length; i++) {
        if (_items[i].TipoItem === tipo && _items[i].ItemID === itemID &&
            _items[i].UnidadVal === unidadVal) {
            _items[i].Cantidad += cantidad;
            _items[i].PrecioUnitario = precio;
            renderizarTabla(); sincronizarHidden(); return;
        }
    }
    _items.push({ TipoItem: tipo, ItemID: itemID, Nombre: nombre,
                  Cantidad: cantidad, PrecioUnitario: precio,
                  UnidadVal: unidadVal, Factor: factor, UnidadTexto: unidadTexto });
    renderizarTabla(); sincronizarHidden();
    document.getElementById('txtItemCantidad').value = 1;
}

function eliminarItem(idx) {
    _items.splice(idx, 1);
    renderizarTabla(); sincronizarHidden();
}

function renderizarTabla() {
    var tbody = document.getElementById('tbodyItems');
    tbody.innerHTML = '';
    if (_items.length === 0) {
        tbody.innerHTML = '<tr><td colspan="7" class="text-center" style="color:#868e96;font-style:italic;">Aún no hay ítems.</td></tr>';
        actualizarTotal(); return;
    }
    _items.forEach(function (it, idx) { renderizarFila(it, idx); });
    actualizarTotal();
}

function renderizarFila(it, idx) {
    var cantBase = it.Cantidad * (it.Factor > 0 ? it.Factor : 1);
    var subtotal = cantBase * it.PrecioUnitario;
    var badgeCls = it.TipoItem === 'PRODUCTO' ? 'badge-primary' : 'badge-warning';
    var unidCell = (it.TipoItem === 'MATERIAL' && it.UnidadTexto) ? escHtml(it.UnidadTexto) : '&mdash;';
    var tbody = document.getElementById('tbodyItems');
    var tr = document.createElement('tr');
    tr.innerHTML =
        '<td><span class="badge ' + badgeCls + '">' + it.TipoItem + '</span></td>' +
        '<td>' + escHtml(it.Nombre) + '</td>' +
        '<td class="text-right">' + fmtNum(it.Cantidad) + '</td>' +
        '<td><small>' + unidCell + '</small></td>' +
        '<td class="text-right">$' + fmtMoney(it.PrecioUnitario) + '</td>' +
        '<td class="text-right font-weight-bold">$' + fmtMoney(subtotal) + '</td>' +
        '<td class="text-center"><button type="button" class="btn btn-xs btn-danger" onclick="eliminarItem(' + idx + ')">&times;</button></td>';
    tbody.appendChild(tr);
}

function actualizarTotal() {
    var total = _items.reduce(function (s, it) { return s + (it.Cantidad * (it.Factor > 0 ? it.Factor : 1) * it.PrecioUnitario); }, 0);
    document.getElementById('tdTotalItems').textContent = '$' + fmtMoney(total);
}

function sincronizarHidden() {
    document.getElementById('<%= hdnItemsJson.ClientID %>').value = JSON.stringify(_items);
}

function validarModalNuevo() {
    var fecha = document.getElementById('<%= txtNuevoFecha.ClientID %>').value;
    var base  = document.getElementById('<%= ddlNuevoBase.ClientID %>').value;
    var cli   = document.getElementById('<%= ddlNuevoCliente.ClientID %>').value;
    if (!fecha) { Swal.fire('Campo requerido','Seleccione la fecha.','warning'); return false; }
    if (!base)  { Swal.fire('Campo requerido','Seleccione la base origen.','warning'); return false; }
    if (!cli)   { Swal.fire('Campo requerido','Seleccione el cliente.','warning'); return false; }
    if (_items.length === 0) { Swal.fire('Sin ítems','Agregue al menos un ítem a la orden.','warning'); return false; }
    sincronizarHidden();
    return true;
}

// ─────────────────────────────────────────────────────────────────
// Acciones desde el grid
// ─────────────────────────────────────────────────────────────────
function verHistorial(id) {
    document.getElementById('<%= hdnAccion.ClientID %>').value         = 'detalle';
    document.getElementById('<%= hdnOrdenIDAccion.ClientID %>').value  = id;
    document.getElementById('<%= btnProcesarAccion.ClientID %>').click();
}

function registrarEntrega(id) {
    document.getElementById('<%= hdnAccion.ClientID %>').value         = 'registrar-entrega';
    document.getElementById('<%= hdnOrdenIDAccion.ClientID %>').value  = id;
    document.getElementById('<%= btnProcesarAccion.ClientID %>').click();
}

function imprimirOrden(id) {
    document.getElementById('<%= hdnAccion.ClientID %>').value         = 'imprimir';
    document.getElementById('<%= hdnOrdenIDAccion.ClientID %>').value  = id;
    document.getElementById('<%= btnProcesarAccion.ClientID %>').click();
}

function cancelarOrden(id) {
    Swal.fire({
        title: '¿Cancelar la orden completa?',
        html: 'Se cancelarán todas las entregas vinculadas.<br>Si hay entregas confirmadas, se devolverá el stock.',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#e74c3c',
        cancelButtonColor: '#6c757d',
        confirmButtonText: '<i class="fas fa-times mr-1"></i> Sí, cancelar orden',
        cancelButtonText: 'No'
    }).then(function (r) {
        if (r.isConfirmed) {
            document.getElementById('<%= hdnAccion.ClientID %>').value        = 'cancelar-orden';
            document.getElementById('<%= hdnOrdenIDAccion.ClientID %>').value = id;
            document.getElementById('<%= btnProcesarAccion.ClientID %>').click();
        }
    });
}

// ─────────────────────────────────────────────────────────────────
// Modal: Registrar Entrega Parcial
// ─────────────────────────────────────────────────────────────────
var _partidaData = null;

function mostrarModalRegistrarEntrega(p) {
    _partidaData = p;
    document.getElementById('pFolioOrden').textContent  = p.OrdenFolio || '—';
    document.getElementById('pClienteOrden').textContent = p.ClienteNombre || '—';
    document.getElementById('pBaseOrden').textContent   = p.BaseNombre || '—';
    document.getElementById('pEstadoOrden').innerHTML   = '<span class="badge badge-' +
        (p.EstadoOrden || 'abierta').toLowerCase() + '">' + (p.EstadoOrden || '') + '</span>';

    var tbody = document.getElementById('tbodyPartida');
    tbody.innerHTML = '';
    (p.Items || []).forEach(function (it, idx) {
        var badgeCls = it.TipoItem === 'PRODUCTO' ? 'badge-primary' : 'badge-warning';
        var pend = Math.max(0, it.CantidadPendiente);

        // Celda de unidad: dropdown para materiales, vacía para productos
        var unidadCell = '';
        if (it.TipoItem === 'MATERIAL' && it.Unidades && it.Unidades.length > 0) {
            var selOpts = '';
            it.Unidades.forEach(function (u) {
                selOpts += '<option value="' + escHtml(u.Valor) + '" data-factor="' + u.Factor + '">' + escHtml(u.Texto) + '</option>';
            });
            unidadCell = '<select class="form-control form-control-sm" id="unidadSel_' + idx + '" ' +
                         'onchange="onUnidadPartidaChange(' + idx + ',' + JSON.stringify(it.CantidadPendiente) + ')">' +
                         selOpts + '</select>';
        } else {
            unidadCell = '<span class="text-muted small">&mdash;</span>';
        }

        var unitSuffix = (it.TipoItem === 'MATERIAL' && it.UnidadBaseNombre)
            ? ' <small class="text-muted">' + escHtml(it.UnidadBaseNombre) + '</small>'
            : '';

        var tr = document.createElement('tr');
        tr.innerHTML =
            '<td><span class="badge ' + badgeCls + '">' + escHtml(it.TipoItem) + '</span></td>' +
            '<td>' + escHtml(it.NombreItem) + '</td>' +
            '<td class="text-right">' + fmtNum(it.CantidadPedida) + unitSuffix + '</td>' +
            '<td class="text-right">' + fmtNum(it.CantidadEntregada) + unitSuffix + '</td>' +
            '<td class="text-right font-weight-bold' + (pend > 0 ? ' text-warning' : ' text-success') + '" id="pendCell_' + idx + '">' + fmtNum(pend) + unitSuffix + '</td>' +
            '<td class="text-center">' +
              (pend > 0
                ? '<input type="number" class="form-control form-control-sm input-cant-entregar" ' +
                  'id="cantEntregar_' + idx + '" min="0" step="0.01" value="0" />'
                : '<span class="text-success"><i class="fas fa-check"></i> Completo</span>') +
            '</td>' +
            '<td>' + unidadCell + '</td>';
        tbody.appendChild(tr);

        // Calcular max inicial según unidad base
        if (pend > 0) recalcularMaxPartida(idx, pend);
    });

    document.getElementById('<%= txtPartidaFecha.ClientID %>').value = getTodayDate();
    document.getElementById('<%= txtPartidaFactura.ClientID %>').value = '';
    document.getElementById('<%= txtPartidaObs.ClientID %>').value    = '';
    document.getElementById('<%= chkEsCreditoPartida.ClientID %>').checked = !!p.EsCredito;
    document.getElementById('<%= hdnPartidaJson.ClientID %>').value   = '';
    $('#modalRegistrarEntrega').modal('show');
}

// Auto-marcar "Es a crédito" en la orden según los días de crédito del cliente
function onClienteOrdenChange() {
    var ddl = document.getElementById('<%= ddlNuevoCliente.ClientID %>');
    var opt = ddl.options[ddl.selectedIndex];
    var dias = opt ? parseInt(opt.getAttribute('data-dias') || '0', 10) : 0;
    document.getElementById('<%= chkEsCreditoOrden.ClientID %>').checked = dias > 0;
}

// Recalcular el max del input según la unidad seleccionada
function recalcularMaxPartida(idx, pendienteBase) {
    var inp = document.getElementById('cantEntregar_' + idx);
    if (!inp) return;
    var selU = document.getElementById('unidadSel_' + idx);
    var factor = selU ? (parseFloat(selU.options[selU.selectedIndex].getAttribute('data-factor')) || 1) : 1;
    var maxEnUnidad = pendienteBase / factor;
    inp.max  = maxEnUnidad;
    inp.step = (factor === 1) ? '0.01' : '0.001';
}

function onUnidadPartidaChange(idx, pendienteBase) {
    recalcularMaxPartida(idx, pendienteBase);
    // Resetear el input al cambiar unidad para evitar valores inválidos
    var inp = document.getElementById('cantEntregar_' + idx);
    if (inp) inp.value = 0;
}

function guardarPartida(modo) {
    if (!_partidaData) return;

    var fecha = document.getElementById('<%= txtPartidaFecha.ClientID %>').value;
    if (!fecha) { Swal.fire('Campo requerido','Seleccione la fecha de entrega.','warning'); return; }

    var entregaItems = [];
    var alguno = false;
    var errorValidacion = false;
    (_partidaData.Items || []).forEach(function (it, idx) {
        var inp = document.getElementById('cantEntregar_' + idx);
        if (!inp) return;
        var cant = parseFloat(inp.value) || 0;
        if (cant <= 0) return;

        // Leer unidad y factor
        var unidadVal = '', factor = 1;
        var selU = document.getElementById('unidadSel_' + idx);
        if (selU && selU.options.length > 0 && selU.value) {
            unidadVal = selU.value;
            factor    = parseFloat(selU.options[selU.selectedIndex].getAttribute('data-factor')) || 1;
        }

        // Validar contra pendiente en unidad base
        var cantBase = cant * factor;
        var pendBase = it.CantidadPendiente || 0;
        if (cantBase > pendBase + 0.0001) {  // pequeño margen por decimales
            Swal.fire('Cantidad inválida',
                'La cantidad a entregar de "' + it.NombreItem + '" supera el pendiente disponible.',
                'warning');
            errorValidacion = true;
            return;
        }

        alguno = true;
        entregaItems.push({
            DetalleOrdenID:   it.DetalleOrdenID,
            TipoItem:         it.TipoItem,
            ItemID:           it.ItemID,
            CantidadAEntregar: cant,
            PrecioUnitario:   it.PrecioUnitario,
            UnidadVal:        unidadVal,
            Factor:           factor
        });
    });

    if (errorValidacion) return;
    if (!alguno) { Swal.fire('Sin cantidades','Ingrese al menos una cantidad mayor a 0.','warning'); return; }

    document.getElementById('<%= hdnEntregaItemsJson.ClientID %>').value = JSON.stringify(entregaItems);
    document.getElementById('<%= hdnModoGuardado.ClientID %>').value     = modo;
    document.getElementById('<%= hdnNumeroFactura.ClientID %>').value    =
        document.getElementById('<%= txtPartidaFactura.ClientID %>').value;
    document.getElementById('<%= hdnFechaEntregaConf.ClientID %>').value  = fecha;
    document.getElementById('<%= hdnOrdenIDAccion.ClientID %>').value     = _partidaData.OrdenID;
    $('#modalRegistrarEntrega').modal('hide');
    document.getElementById('<%= btnGuardarPartida.ClientID %>').click();
}

// ─────────────────────────────────────────────────────────────────
// Modal: Historial de la Orden
// ─────────────────────────────────────────────────────────────────
function mostrarModalHistorial(d) {
    document.getElementById('hFolio').textContent   = d.Folio   || '—';
    document.getElementById('hFecha').textContent   = d.Fecha   || '—';
    document.getElementById('hBase').textContent    = d.Base    || '—';
    document.getElementById('hCliente').textContent = d.Cliente || '—';
    document.getElementById('hObs').textContent     = d.Obs     || '—';
    document.getElementById('hEstado').innerHTML    =
        '<span class="badge badge-' + (d.Estado || '').toLowerCase() + '">' + d.Estado + '</span>';

    // Ítems con progreso
    var tbodyO = document.getElementById('tbodyDetalleOrden');
    tbodyO.innerHTML = '';
    (d.Items || []).forEach(function (it) {
        var pend     = Math.max(0, it.CantidadPedida - it.CantidadEntregada);
        var pct      = it.CantidadPedida > 0
            ? Math.min(100, Math.round(it.CantidadEntregada / it.CantidadPedida * 100)) : 0;
        var badgeCls  = it.TipoItem === 'PRODUCTO' ? 'badge-primary' : 'badge-warning';
        var estadoCls = it.Estado === 'COMPLETADO' ? 'badge-completada'
                      : it.Estado === 'PARCIAL'    ? 'badge-parcial'
                      :                              'badge-pendiente';

        // Sufijo de unidad para Entregado y Pendiente (siempre en unidad base)
        var unidSufijo = it.UnidadBaseNombre
            ? ' <small class="text-muted">' + escHtml(it.UnidadBaseNombre) + '</small>'
            : '';

        // Columna Pedido: si hubo conversión, mostrar cantidad original capturada;
        // si no, mostrar la cantidad base con su unidad.
        var pedidoHtml;
        if (it.Descripcion) {
            // Ej: "6 Kilogramo/s" (= 6000 gramo/s)
            pedidoHtml = '<strong>' + escHtml(it.Descripcion) + '</strong>' +
                         (it.UnidadBaseNombre
                            ? '<small class="d-block text-muted">= ' + fmtNum(it.CantidadPedida) + ' ' + escHtml(it.UnidadBaseNombre) + '</small>'
                            : '');
        } else {
            pedidoHtml = fmtNum(it.CantidadPedida) + unidSufijo;
        }

        var tr = document.createElement('tr');
        tr.innerHTML =
            '<td><span class="badge ' + badgeCls + '">' + escHtml(it.TipoItem) + '</span></td>' +
            '<td>' + escHtml(it.NombreItem) + '</td>' +
            '<td class="text-right">' + pedidoHtml + '</td>' +
            '<td class="text-right">' + fmtNum(it.CantidadEntregada) + unidSufijo + '</td>' +
            '<td class="text-right">' + fmtNum(pend) + unidSufijo + '</td>' +
            '<td class="text-center"><div class="progress" style="height:8px;">' +
              '<div class="progress-bar bg-success" style="width:' + pct + '%"></div></div>' +
              '<small>' + pct + '%</small></td>' +
            '<td class="text-center"><span class="badge ' + estadoCls + '">' + it.Estado + '</span></td>';
        tbodyO.appendChild(tr);
    });

    // Entregas vinculadas
    var tbodyE = document.getElementById('tbodyEntregasVinculadas');
    tbodyE.innerHTML = '';
    var sinEntregas = document.getElementById('divSinEntregas');
    if (!d.Entregas || d.Entregas.length === 0) {
        sinEntregas.style.display = '';
    } else {
        sinEntregas.style.display = 'none';
        d.Entregas.forEach(function (e) {
            var estadoCls = e.Estado === 'ENTREGADA'       ? 'badge-entregada'
                           : e.Estado === 'PENDIENTE'      ? 'badge-pendiente'
                           : e.Estado === 'PENDIENTE_STOCK'? 'badge-pendiente-stock'
                           :                                  'badge-cancelada';
            var btnDetalle  = '<button class="btn btn-xs btn-info mr-1" title="Ver detalle" onclick="verDetalleEntrega(' + e.EntregaID + ')"><i class="fas fa-eye"></i></button>';
            var btnImprimir = '<button class="btn btn-xs btn-secondary mr-1" title="Imprimir comprobante" onclick="imprimirEntregaDesdeHistorial(' + e.EntregaID + ')"><i class="fas fa-print"></i></button>';
            var btnConf = (e.Estado === 'PENDIENTE' || e.Estado === 'PENDIENTE_STOCK')
                ? '<button class="btn btn-xs btn-success mr-1" onclick="abrirConfirmacion(' + e.EntregaID + ',\'' + escHtml(e.Folio) + '\')"><i class="fas fa-check"></i> Confirmar</button>'
                : '';
            var btnCan = (e.Estado !== 'CANCELADA')
                ? '<button class="btn btn-xs btn-danger" onclick="cancelarEntregaVinculada(' + e.EntregaID + ',\'' + escHtml(e.Folio) + '\')"><i class="fas fa-ban"></i></button>'
                : '';
            var tr = document.createElement('tr');
            tr.innerHTML =
                '<td>' + escHtml(e.Folio) + '</td>' +
                '<td class="text-center">' + (e.Fecha || '—') + '</td>' +
                '<td>' + escHtml(e.NumeroFactura || '—') + '</td>' +
                '<td class="text-right">$' + fmtMoney(e.Total) + '</td>' +
                '<td class="text-center"><span class="badge ' + estadoCls + '">' + e.Estado + '</span></td>' +
                '<td class="text-center">' + btnDetalle + btnImprimir + btnConf + btnCan + '</td>';
            tbodyE.appendChild(tr);
        });
    }
}

// ─────────────────────────────────────────────────────────────────
// Modal: Confirmar Entrega individual
// ─────────────────────────────────────────────────────────────────
var _entregaAConfirmar = 0;

function abrirConfirmacion(entregaID, folio) {
    _entregaAConfirmar = entregaID;
    document.getElementById('cfFolioEntrega').textContent = folio;
    document.getElementById('cfFechaEntrega').value       = getTodayDate();
    document.getElementById('cfNumeroFactura').value      = '';
    // Con los modales fuera de form#form1 (ver fix en load), Bootstrap puede
    // gestionar aria-hidden sin conflicto y hidden.bs.modal dispara limpio.
    $('#modalHistorial').one('hidden.bs.modal', function () {
        $('#modalConfirmar').modal('show');
    });
    $('#modalHistorial').modal('hide');
}

function ejecutarConfirmacion() {
    var fecha = document.getElementById('cfFechaEntrega').value;
    if (!fecha) { Swal.fire('Campo requerido', 'Seleccione la fecha de entrega.', 'warning'); return; }

    document.getElementById('<%= hdnAccion.ClientID %>').value           = 'confirmar-entrega';
    document.getElementById('<%= hdnEntregaIDAccion.ClientID %>').value  = _entregaAConfirmar;
    document.getElementById('<%= hdnNumeroFactura.ClientID %>').value     = document.getElementById('cfNumeroFactura').value;
    document.getElementById('<%= hdnFechaEntregaConf.ClientID %>').value  = fecha;
    $('#modalConfirmar').modal('hide');
    document.getElementById('<%= btnProcesarAccion.ClientID %>').click();
}

function cancelarEntregaVinculada(entregaID, folio) {
    Swal.fire({
        title: '¿Cancelar esta entrega?',
        html: 'Folio: <strong>' + folio + '</strong><br>Si estaba confirmada, se devolverá el stock.',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#e74c3c',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Sí, cancelar',
        cancelButtonText: 'No'
    }).then(function (r) {
        if (r.isConfirmed) {
            $('#modalHistorial').modal('hide');
            document.getElementById('<%= hdnAccion.ClientID %>').value          = 'cancelar-entrega';
            document.getElementById('<%= hdnEntregaIDAccion.ClientID %>').value = entregaID;
            document.getElementById('<%= btnProcesarAccion.ClientID %>').click();
        }
    });
}

// ─────────────────────────────────────────────────────────────────
// Utilidades
// ─────────────────────────────────────────────────────────────────
function fmtMoney(v) {
    return parseFloat(v || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}
function fmtNum(v) {
    var n = parseFloat(v || 0);
    return n % 1 === 0 ? n.toLocaleString('es-MX') : n.toLocaleString('es-MX', { minimumFractionDigits: 2, maximumFractionDigits: 4 });
}
function escHtml(s) {
    return (s || '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}
function getTodayDate() {
    var d = new Date(); var mm = d.getMonth()+1; var dd = d.getDate();
    return d.getFullYear() + '-' + (mm<10?'0'+mm:mm) + '-' + (dd<10?'0'+dd:dd);
}

// ─────────────────────────────────────────────────────────────────
// Ver Detalle de Entrega individual (desde historial, vía AJAX)
// ─────────────────────────────────────────────────────────────────
var _historialDataActual = null;  // para botón "Volver" del modal de detalle

// Sobreescribir mostrarModalHistorial para guardar referencia
var _fnHistorialOriginal = mostrarModalHistorial;
mostrarModalHistorial = function (d) {
    _historialDataActual = d;
    _fnHistorialOriginal(d);
};

function verDetalleEntrega(entregaID) {
    fetch('<%= ResolveUrl("~/Ordenes.aspx/ObtenerDetalleEntrega") %>', {
        method:  'POST',
        headers: { 'Content-Type': 'application/json' },
        body:    JSON.stringify({ entregaID: entregaID })
    })
    .then(function (r) { return r.json(); })
    .then(function (data) {
        var d = data.d !== undefined ? data.d : data;
        if (!d) { Swal.fire('Error', 'No se encontró la entrega.', 'error'); return; }

        document.getElementById('deEntFolio').textContent   = d.Folio   || '—';
        document.getElementById('deEntFecha').textContent   = d.Fecha   || '—';
        document.getElementById('deEntBase').textContent    = d.Base    || '—';
        document.getElementById('deEntCliente').textContent = d.Cliente || '—';
        document.getElementById('deEntFactura').textContent = d.NumeroFactura || '—';
        document.getElementById('deEntObs').textContent     = d.Obs     || '—';

        var badgeMap = { 'ENTREGADA':'badge-entregada','PENDIENTE':'badge-pendiente',
                         'PENDIENTE_STOCK':'badge-pendiente-stock','CANCELADA':'badge-cancelada' };
        var bc = badgeMap[d.Estado] || 'badge-secondary';
        document.getElementById('deEntEstado').innerHTML =
            '<span class="badge ' + bc + '">' + escHtml(d.Estado) + '</span>';

        var tbody = document.getElementById('tbodyDetalleEnt');
        tbody.innerHTML = '';
        var total = 0;
        (d.Items || []).forEach(function (it) {
            var badgeCls = it.TipoItem === 'PRODUCTO' ? 'badge-primary' : 'badge-warning';
            var cantHtml;
            if (it.TuvoConversion) {
                cantHtml = '<strong>' + fmtNum(it.CantidadCapturada) + ' ' + escHtml(it.UnidadCaptura) + '</strong>' +
                           '<small class="d-block text-muted">= ' + fmtNum(it.Cantidad) + ' ' + escHtml(it.UnidadBase) + '</small>';
            } else if (it.UnidadBase) {
                cantHtml = fmtNum(it.Cantidad) + ' ' + escHtml(it.UnidadBase);
            } else {
                cantHtml = fmtNum(it.Cantidad);
            }
            var sub = it.Cantidad * it.PrecioUnitario;
            total  += sub;
            var tr  = document.createElement('tr');
            tr.innerHTML =
                '<td><span class="badge ' + badgeCls + '">' + escHtml(it.TipoItem) + '</span></td>' +
                '<td>' + escHtml(it.Nombre) + '</td>' +
                '<td class="text-right">' + cantHtml + '</td>' +
                '<td class="text-right">$' + fmtMoney(it.PrecioUnitario) + '</td>' +
                '<td class="text-right font-weight-bold">$' + fmtMoney(sub) + '</td>';
            tbody.appendChild(tr);
        });
        document.getElementById('tdTotalDetalleEnt').textContent = '$' + fmtMoney(total);

        // Cerrar historial y abrir detalle de entrega
        $('#modalHistorial').one('hidden.bs.modal', function () {
            $('#modalDetalleEntrega').modal('show');
        });
        $('#modalHistorial').modal('hide');
    })
    .catch(function () { Swal.fire('Error', 'No se pudo cargar el detalle.', 'error'); });
}

function volverAlHistorial() {
    $('#modalDetalleEntrega').one('hidden.bs.modal', function () {
        if (_historialDataActual) {
            mostrarModalHistorial(_historialDataActual);
            $('#modalHistorial').modal('show');
        }
    });
    $('#modalDetalleEntrega').modal('hide');
}

// ─────────────────────────────────────────────────────────────────
// Imprimir comprobante de entrega individual (el historial NO se cierra)
// ─────────────────────────────────────────────────────────────────
function imprimirEntregaDesdeHistorial(entregaID) {
    fetch('<%= ResolveUrl("~/Ordenes.aspx/GenerarHtmlImpresionEntrega") %>', {
        method:  'POST',
        headers: { 'Content-Type': 'application/json' },
        body:    JSON.stringify({ entregaID: entregaID })
    })
    .then(function (r) { return r.json(); })
    .then(function (data) {
        var html = data.d !== undefined ? data.d : data;
        if (!html) { Swal.fire('Error', 'No se pudo generar el comprobante.', 'error'); return; }
        var w = window.open('', '_blank', 'width=860,height=700,scrollbars=yes');
        w.document.write(html);
        w.document.close();
        w.focus();
    })
    .catch(function () { Swal.fire('Error', 'No se pudo generar el comprobante.', 'error'); });
}
</script>

<!-- ══════════════════════════════════════════════════════════════════ -->
<!-- MODAL: DETALLE DE ENTREGA INDIVIDUAL (desde historial)           -->
<!-- ══════════════════════════════════════════════════════════════════ -->
<div class="modal fade" id="modalDetalleEntrega" tabindex="-1">
    <div class="modal-dialog modal-xl">
        <div class="modal-content">
            <div class="modal-header bg-info text-white">
                <h5 class="modal-title"><i class="fas fa-eye mr-2"></i>Detalle de Entrega</h5>
                <button type="button" class="close text-white" data-dismiss="modal">&times;</button>
            </div>
            <div class="modal-body">
                <div class="detalle-header">
                    <div class="row">
                        <div class="col-md-3"><label>Folio</label><div id="deEntFolio" class="font-weight-bold">—</div></div>
                        <div class="col-md-3"><label>Fecha</label><div id="deEntFecha">—</div></div>
                        <div class="col-md-3"><label>Base</label><div id="deEntBase">—</div></div>
                        <div class="col-md-3"><label>Estado</label><div id="deEntEstado">—</div></div>
                    </div>
                    <div class="row mt-2">
                        <div class="col-md-4"><label>Cliente</label><div id="deEntCliente">—</div></div>
                        <div class="col-md-4"><label>N&uacute;m. Factura</label><div id="deEntFactura">—</div></div>
                        <div class="col-md-4"><label>Observaciones</label><div id="deEntObs">—</div></div>
                    </div>
                </div>
                <h6 class="text-primary font-weight-bold mt-3 mb-2">
                    <i class="fas fa-boxes mr-1"></i> &Iacute;tems Entregados
                </h6>
                <div class="table-responsive">
                    <table class="table table-sm table-bordered">
                        <thead>
                            <tr style="background:#003366;color:#fff;">
                                <th>Tipo</th>
                                <th>Descripci&oacute;n</th>
                                <th class="text-right">Cantidad</th>
                                <th class="text-right">Precio Unit.</th>
                                <th class="text-right">Subtotal</th>
                            </tr>
                        </thead>
                        <tbody id="tbodyDetalleEnt"></tbody>
                        <tfoot>
                            <tr style="background:#003366;color:#fff;font-weight:bold;">
                                <td colspan="4" class="text-right">TOTAL</td>
                                <td class="text-right" id="tdTotalDetalleEnt">$0.00</td>
                            </tr>
                        </tfoot>
                    </table>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-outline-secondary" onclick="volverAlHistorial()">
                    <i class="fas fa-arrow-left mr-1"></i> Volver al historial
                </button>
                <button type="button" class="btn btn-secondary" data-dismiss="modal">Cerrar</button>
            </div>
        </div>
    </div>
</div>

</asp:Content>

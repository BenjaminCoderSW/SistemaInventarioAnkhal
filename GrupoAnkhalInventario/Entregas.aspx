<%@ Page Title="Entregas" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Entregas.aspx.cs" Inherits="GrupoAnkhalInventario.Entregas" %>

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
        .stock-card.total       { background:linear-gradient(135deg,#1a5276,#2980b9); }
        .stock-card.programadas { background:linear-gradient(135deg,#784212,#ca6f1e); }
        .stock-card.entregadas  { background:linear-gradient(135deg,#1e8449,#27ae60); }
        .stock-card.canceladas  { background:linear-gradient(135deg,#922b21,#e74c3c); }
        .stock-card.valor       { background:linear-gradient(135deg,#1c2833,#2c3e50); }
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
        .badge-programada     { background:#ca6f1e; color:#fff; }
        .badge-entregada      { background:#27ae60; color:#fff; }
        .badge-cancelada      { background:#e74c3c; color:#fff; }
        .badge-pendiente-stock{ background:#8e44ad; color:#fff; }

        /* ── Tabla items del modal ── */
        .items-table th { background:#003366; color:#fff; font-size:.82rem; padding:6px 10px; }
        .items-table td { font-size:.85rem; padding:5px 10px; vertical-align:middle; }
        #divItemsVacios { color:#868e96; font-style:italic; padding:8px 0; }

        /* ── Detalle de entrega ── */
        .detalle-header { background:#f8f9fa; border-radius:8px; padding:14px 18px; margin-bottom:12px; }
        .detalle-header label { font-weight:600; color:#003366; font-size:.84rem; }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

<!-- ══ DASHBOARD ══ -->
<div class="stock-dashboard">
    <div class="stock-card total">
        <div class="icon"><i class="fas fa-truck"></i></div>
        <div class="info">
            <div class="num"><asp:Label ID="lblTotalHoy" runat="server" Text="0"></asp:Label></div>
            <div class="lbl">Entregas del per&iacute;odo</div>
        </div>
    </div>
    <div class="stock-card programadas">
        <div class="icon"><i class="fas fa-clock"></i></div>
        <div class="info">
            <div class="num"><asp:Label ID="lblProgramadas" runat="server" Text="0"></asp:Label></div>
            <div class="lbl">Programadas</div>
        </div>
    </div>
    <div class="stock-card entregadas">
        <div class="icon"><i class="fas fa-check-circle"></i></div>
        <div class="info">
            <div class="num"><asp:Label ID="lblEntregadas" runat="server" Text="0"></asp:Label></div>
            <div class="lbl">Entregadas</div>
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
                <asp:ListItem Text="-- Todos --" Value="" />
                <asp:ListItem Text="Programada" Value="PROGRAMADA" />
                <asp:ListItem Text="Entregada" Value="ENTREGADA" />
                <asp:ListItem Text="Cancelada" Value="CANCELADA" />
                <asp:ListItem Text="Pendiente Stock" Value="PENDIENTE_STOCK" />
            </asp:DropDownList>
        </div>
        <div class="col-md-2">
            <label>Cliente</label>
            <asp:TextBox ID="txtFiltrCliente" runat="server" CssClass="form-control form-control-sm" placeholder="Nombre..."></asp:TextBox>
        </div>
        <div class="col-md-1">
            <label>Folio</label>
            <asp:TextBox ID="txtFiltrFolio" runat="server" CssClass="form-control form-control-sm" placeholder="ENT-..."></asp:TextBox>
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
        <asp:Button ID="btnNuevo" runat="server" Text="+ Nueva Entrega"
            CssClass="btn btn-primary" OnClick="btnNuevo_Click" />
    </div>
    <asp:Label ID="lblResultados" runat="server" CssClass="text-muted small"></asp:Label>
</div>

<!-- ══ GRID ══ -->
<div class="table-responsive">
    <asp:GridView ID="gvEntregas" runat="server" AutoGenerateColumns="False"
        CssClass="table table-bordered table-striped custom-grid"
        AllowCustomPaging="True" AllowPaging="True" PageSize="15"
        OnPageIndexChanging="gvEntregas_PageIndexChanging"
        EmptyDataText="No se encontraron entregas."
        PagerStyle-CssClass="pager-custom"
        PagerSettings-Mode="NumericFirstLast"
        PagerSettings-FirstPageText="«"
        PagerSettings-LastPageText="»"
        PagerSettings-PageButtonCount="5">
        <Columns>
            <asp:BoundField DataField="Folio"       HeaderText="Folio" />
            <asp:BoundField DataField="FechaEntrega" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:BoundField DataField="BaseNombre"  HeaderText="Base" />
            <asp:BoundField DataField="ClienteNombre" HeaderText="Cliente" />
            <asp:BoundField DataField="NumItems"    HeaderText="Items" ItemStyle-CssClass="text-center" />
            <asp:BoundField DataField="TotalValor"  HeaderText="Total ($)" DataFormatString="{0:C2}" ItemStyle-CssClass="text-right font-weight-bold" />
            <asp:TemplateField HeaderText="Estado">
                <ItemStyle CssClass="text-center" />
                <ItemTemplate>
                    <span class='badge <%# GetBadgeEstado(Eval("Estado").ToString()) %>'>
                        <%# Eval("Estado") %>
                    </span>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Acciones">
                <HeaderStyle CssClass="text-center" Width="200px" />
                <ItemStyle CssClass="text-center" />
                <ItemTemplate>
                    <button type="button" class="btn btn-xs btn-info"
                        onclick="verDetalle(<%# Eval("EntregaID") %>)">
                        <i class="fas fa-eye"></i> Detalle
                    </button>
                    <button type="button" class="btn btn-xs btn-secondary"
                        onclick="imprimirEntrega(<%# Eval("EntregaID") %>)">
                        <i class="fas fa-print"></i>
                    </button>
                    <%# MostrarBtnConfirmar(Eval("Estado").ToString(), Eval("EntregaID").ToString()) %>
                    <%# MostrarBtnCancelar(Eval("Estado").ToString(), Eval("EntregaID").ToString()) %>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
</div>

<!-- ══════════════════════════════════════════════════════════════════ -->
<!-- HIDDEN FIELDS Y BOTONES DE ACCIÓN                                -->
<!-- ══════════════════════════════════════════════════════════════════ -->
<asp:HiddenField ID="hdnMensajePendiente"  runat="server" Value="" />
<asp:HiddenField ID="hdnItemsJson"         runat="server" Value="[]" />
<asp:HiddenField ID="hdnAccion"            runat="server" Value="" />
<asp:HiddenField ID="hdnEntregaIDAccion"   runat="server" Value="" />
<asp:HiddenField ID="hdnDetalleJson"       runat="server" Value="" />

<!-- Botón de acción oculto (confirmar, cancelar, ver detalle, imprimir) -->
<asp:Button ID="btnProcesarAccion" runat="server" style="display:none"
    OnClick="btnProcesarAccion_Click" />

<!-- ══════════════════════════════════════════════════════════════════ -->
<!-- MODAL: NUEVA ENTREGA                                              -->
<!-- ══════════════════════════════════════════════════════════════════ -->
<div class="modal fade" id="modalNuevo" tabindex="-1" data-backdrop="static">
    <div class="modal-dialog modal-xl">
        <div class="modal-content">
            <div class="modal-header bg-primary text-white">
                <h5 class="modal-title"><i class="fas fa-truck mr-2"></i>Nueva Entrega</h5>
                <button type="button" class="close text-white" data-dismiss="modal">&times;</button>
            </div>
            <div class="modal-body">

                <!-- Cabecera de la entrega -->
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
                            onchange="onClienteNuevoChange()"></asp:DropDownList>
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
                            <asp:CheckBox ID="chkEsCredito" runat="server" CssClass="form-check-input" />
                            <label class="form-check-label font-weight-bold" for="<%= chkEsCredito.ClientID %>">
                                Es a crédito (genera Cuenta por Cobrar)
                            </label>
                        </div>
                    </div>
                </div>

                <hr />

                <!-- Agregar items -->
                <h6 class="text-primary font-weight-bold mb-2">
                    <i class="fas fa-boxes mr-1"></i> Items de la Entrega
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
                        <label class="font-weight-bold small">Cantidad <span class="text-danger">*</span></label>
                        <input type="number" id="txtItemCantidad" class="form-control form-control-sm" min="0.01" step="0.01" value="1" />
                    </div>
                    <div class="col-md-2" id="divUnidadMaterial" style="display:none;">
                        <label class="font-weight-bold small">Unidad</label>
                        <select id="selUnidadMaterial" class="form-control form-control-sm"
                                onchange="onUnidadMaterialChange()"></select>
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

                <!-- Tabla de items agregados -->
                <div class="table-responsive">
                    <table class="table table-sm table-bordered items-table" id="tblItems">
                        <thead>
                            <tr>
                                <th>Tipo</th>
                                <th>Descripci&oacute;n</th>
                                <th class="text-right" style="width:80px">Cantidad</th>
                                <th style="width:70px">Unidad</th>
                                <th class="text-right" style="width:100px">Precio Unit.</th>
                                <th class="text-right" style="width:100px">Subtotal</th>
                                <th style="width:40px"></th>
                            </tr>
                        </thead>
                        <tbody id="tbodyItems">
                            <tr id="trItemsVacios">
                                <td colspan="7" class="text-center" id="divItemsVacios">
                                    A&uacute;n no hay items. Use el formulario de arriba para agregar.
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
                <asp:Button ID="btnGuardarProgramada" runat="server" Text="Guardar como Programada"
                    CssClass="btn btn-warning"
                    OnClientClick="return validarModalNuevo();"
                    OnClick="btnGuardarProgramada_Click" />
                <asp:Button ID="btnConfirmarEntregar" runat="server" Text="Confirmar y Entregar"
                    CssClass="btn btn-success"
                    OnClientClick="return validarModalNuevo();"
                    OnClick="btnConfirmarEntregar_Click" />
            </div>
        </div>
    </div>
</div>

<!-- ══════════════════════════════════════════════════════════════════ -->
<!-- MODAL: VER DETALLE                                                -->
<!-- ══════════════════════════════════════════════════════════════════ -->
<div class="modal fade" id="modalDetalle" tabindex="-1">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <div class="modal-header bg-info text-white">
                <h5 class="modal-title"><i class="fas fa-eye mr-2"></i>Detalle de Entrega</h5>
                <button type="button" class="close text-white" data-dismiss="modal">&times;</button>
            </div>
            <div class="modal-body" id="divContenidoDetalle">
                <div class="detalle-header">
                    <div class="row">
                        <div class="col-md-3"><label>Folio</label><div id="dFolio">—</div></div>
                        <div class="col-md-3"><label>Fecha</label><div id="dFecha">—</div></div>
                        <div class="col-md-3"><label>Base</label><div id="dBase">—</div></div>
                        <div class="col-md-3"><label>Estado</label><div id="dEstado">—</div></div>
                    </div>
                    <div class="row mt-2">
                        <div class="col-md-6"><label>Cliente</label><div id="dCliente">—</div></div>
                        <div class="col-md-6"><label>Registrado por</label><div id="dRegistrado">—</div></div>
                    </div>
                    <div class="row mt-2">
                        <div class="col-md-12"><label>Observaciones</label><div id="dObs">—</div></div>
                    </div>
                </div>
                <h6 class="text-primary font-weight-bold mb-2">Items</h6>
                <div class="table-responsive">
                    <table class="table table-sm table-bordered" id="tblDetalle">
                        <thead>
                            <tr style="background:#003366;color:#fff;">
                                <th>Tipo</th>
                                <th>Descripci&oacute;n</th>
                                <th class="text-right">Cantidad</th>
                                <th class="text-right">Precio Unit.</th>
                                <th class="text-right">Subtotal</th>
                            </tr>
                        </thead>
                        <tbody id="tbodyDetalle"></tbody>
                        <tfoot>
                            <tr>
                                <td colspan="4" class="text-right font-weight-bold">Total:</td>
                                <td class="text-right font-weight-bold" id="tdTotalDetalle">$0.00</td>
                            </tr>
                        </tfoot>
                    </table>
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
    // ─────────────────────────────────────────────────────────────────
    // ── Restricción: máximo 2 decimales en campos numéricos ────────────
    window.addEventListener('load', function () {
        document.querySelectorAll('input[type="number"][step="0.01"]').forEach(function (el) {
            el.addEventListener('blur', function () {
                if (this.value === '') return;
                var num = parseFloat(this.value);
                if (!isNaN(num)) this.value = parseFloat(num.toFixed(2));
            });
        });
    });

    // SweetAlert: mensajes pendientes del servidor
    // ─────────────────────────────────────────────────────────────────
    window.addEventListener('load', function () {
        var h = document.getElementById('<%= hdnMensajePendiente.ClientID %>');
        if (h && h.value) {
            try {
                var m = JSON.parse(h.value);
                h.value = '';
                Swal.fire({
                    icon: m.icon, title: m.title, text: m.text,
                    confirmButtonColor: '#003366'
                }).then(function () {
                    if (m.modal) $('#' + m.modal).modal('show');
                });
            } catch (e) { }
        }

        // Re-renderizar items si hay JSON guardado (postback con error)
        var hdnItems = document.getElementById('<%= hdnItemsJson.ClientID %>');
        if (hdnItems && hdnItems.value && hdnItems.value !== '[]') {
            try {
                var items = JSON.parse(hdnItems.value);
                items.forEach(function (it) { renderizarFila(it); });
                actualizarTotal();
            } catch (e) { }
        }

        // Renderizar detalle si hay JSON
        var hdnDetalle = document.getElementById('<%= hdnDetalleJson.ClientID %>');
        if (hdnDetalle && hdnDetalle.value) {
            try {
                var d = JSON.parse(hdnDetalle.value);
                if (d && d.EntregaID) {
                    mostrarDetalleModal(d);
                    $('#modalDetalle').modal('show');
                    hdnDetalle.value = '';
                }
            } catch (e) { }
        }
    });

    // ─────────────────────────────────────────────────────────────────
    // Items del modal: agregar / eliminar / actualizar JSON oculto
    // ─────────────────────────────────────────────────────────────────
    var _items = []; // array en memoria de items actuales

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

    // \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500
    // Autocomplete AJAX
    // \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500
    var _buscarTimer      = null;
    var _itemSeleccionado = null;

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
        fetch('<%= ResolveUrl("~/Entregas.aspx/BuscarItems") %>', {
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify({ query: query, tipo: tipo })
        })
        .then(function (r) { return r.json(); })
        .then(function (data) { mostrarResultados(data.d); })
        .catch(function () { ocultarResultados(); });
    }

    function mostrarResultados(items) {
        var div = document.getElementById('divResultadosBusqueda');
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
            el.onmousedown   = function (e) { e.preventDefault(); seleccionarItem(item); };
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
            opt.value = '';
            opt.text  = '\u2014 sin unidad \u2014';
            opt.setAttribute('data-factor', '1');
            sel.appendChild(opt);
            return;
        }
        conversiones.forEach(function (op) {
            var opt = document.createElement('option');
            opt.value = op.valor;
            opt.text  = op.texto;
            opt.setAttribute('data-factor', op.factor);
            sel.appendChild(opt);
        });
    }

    function onUnidadMaterialChange() { /* factor se lee al agregar */ }

    // Cerrar lista al hacer clic fuera del autocomplete
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
        var itemID = parseInt(_itemSeleccionado.id);
        var nombre = _itemSeleccionado.nombre;

        var cantidad = parseFloat(document.getElementById('txtItemCantidad').value) || 0;
        var precio   = parseFloat(document.getElementById('txtItemPrecio').value) || 0;

        if (cantidad <= 0) { alert('La cantidad debe ser mayor a 0.'); return; }

        var unidadVal = '', factor = 1, unidadTexto = '';
        if (tipo === 'MATERIAL') {
            var selU = document.getElementById('selUnidadMaterial');
            if (selU.options.length > 0 && selU.value) {
                unidadVal   = selU.value;
                factor      = parseFloat(selU.options[selU.selectedIndex].getAttribute('data-factor')) || 1;
                unidadTexto = selU.options[selU.selectedIndex].text;
            }
        }

        // Acumular solo si coinciden tipo, item Y unidad (distinta unidad = fila separada)
        for (var i = 0; i < _items.length; i++) {
            if (_items[i].TipoItem === tipo && _items[i].ItemID === itemID &&
                _items[i].UnidadVal === unidadVal) {
                _items[i].Cantidad += cantidad;
                _items[i].PrecioUnitario = precio;
                renderizarTabla();
                sincronizarHidden();
                return;
            }
        }

        var item = {
            TipoItem: tipo, ItemID: itemID, Nombre: nombre,
            Cantidad: cantidad, PrecioUnitario: precio,
            UnidadVal: unidadVal, Factor: factor, UnidadTexto: unidadTexto
        };
        _items.push(item);
        renderizarTabla();
        sincronizarHidden();
        document.getElementById('txtItemCantidad').value = 1;
    }

    function eliminarItem(idx) {
        _items.splice(idx, 1);
        renderizarTabla();
        sincronizarHidden();
    }

    function renderizarTabla() {
        var tbody = document.getElementById('tbodyItems');
        tbody.innerHTML = '';
        if (_items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="text-center" style="color:#868e96;font-style:italic;">A\u00FAn no hay items. Use el formulario de arriba para agregar.</td></tr>';
            actualizarTotal();
            return;
        }
        _items.forEach(function (it, idx) { renderizarFila(it, idx); });
        actualizarTotal();
    }

    function renderizarFila(it, idx) {
        var subtotal = it.Cantidad * it.PrecioUnitario;
        var badgeCls = it.TipoItem === 'PRODUCTO' ? 'badge-primary' : 'badge-warning';
        var unidadCell = (it.TipoItem === 'MATERIAL' && it.UnidadTexto) ? escHtml(it.UnidadTexto) : '\u2014';
        var tbody = document.getElementById('tbodyItems');
        var tr = document.createElement('tr');
        tr.innerHTML =
            '<td><span class="badge ' + badgeCls + '">' + it.TipoItem + '</span></td>' +
            '<td>' + escHtml(it.Nombre) + '</td>' +
            '<td class="text-right">' + it.Cantidad + '</td>' +
            '<td>' + unidadCell + '</td>' +
            '<td class="text-right">$' + it.PrecioUnitario.toFixed(2) + '</td>' +
            '<td class="text-right font-weight-bold">$' + subtotal.toFixed(2) + '</td>' +
            '<td class="text-center"><button type="button" class="btn btn-xs btn-danger" onclick="eliminarItem(' + idx + ')">&times;</button></td>';
        tbody.appendChild(tr);
    }

    function actualizarTotal() {
        var total = _items.reduce(function (s, it) { return s + (it.Cantidad * it.PrecioUnitario); }, 0);
        document.getElementById('tdTotalItems').textContent = '$' + total.toFixed(2);
    }

    function sincronizarHidden() {
        var h = document.getElementById('<%= hdnItemsJson.ClientID %>');
        h.value = JSON.stringify(_items);
    }

    function escHtml(s) {
        return (s || '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
    }

    // ─────────────────────────────────────────────────────────────────
    // Auto-marcar "Es a crédito" según los días de crédito del cliente
    // ─────────────────────────────────────────────────────────────────
    function onClienteNuevoChange() {
        var ddl = document.getElementById('<%= ddlNuevoCliente.ClientID %>');
        var opt = ddl.options[ddl.selectedIndex];
        var dias = opt ? parseInt(opt.getAttribute('data-dias') || '0', 10) : 0;
        document.getElementById('<%= chkEsCredito.ClientID %>').checked = dias > 0;
    }

    // ─────────────────────────────────────────────────────────────────
    // Validar modal antes de guardar
    // ─────────────────────────────────────────────────────────────────
    function validarModalNuevo() {
        var fecha = document.getElementById('<%= txtNuevoFecha.ClientID %>').value;
        var base  = document.getElementById('<%= ddlNuevoBase.ClientID %>').value;
        var cli   = document.getElementById('<%= ddlNuevoCliente.ClientID %>').value;
        if (!fecha) { Swal.fire('Campo requerido','Seleccione la fecha.','warning'); return false; }
        if (!base)  { Swal.fire('Campo requerido','Seleccione la base origen.','warning'); return false; }
        if (!cli)   { Swal.fire('Campo requerido','Seleccione el cliente.','warning'); return false; }
        if (_items.length === 0) { Swal.fire('Sin items','Agregue al menos un producto o material a la entrega.','warning'); return false; }
        // Asegurar que el JSON está sincronizado
        sincronizarHidden();
        return true;
    }

    // ─────────────────────────────────────────────────────────────────
    // Acciones desde el grid
    // ─────────────────────────────────────────────────────────────────
    function verDetalle(id) {
        document.getElementById('<%= hdnAccion.ClientID %>').value = 'detalle';
        document.getElementById('<%= hdnEntregaIDAccion.ClientID %>').value = id;
        document.getElementById('<%= btnProcesarAccion.ClientID %>').click();
    }

    function imprimirEntrega(id) {
        document.getElementById('<%= hdnAccion.ClientID %>').value = 'imprimir';
        document.getElementById('<%= hdnEntregaIDAccion.ClientID %>').value = id;
        document.getElementById('<%= btnProcesarAccion.ClientID %>').click();
    }

    function imprimirEntregaDesdeDetalle() {
        var hdnDetalle = document.getElementById('<%= hdnDetalleJson.ClientID %>');
        try {
            var d = JSON.parse(hdnDetalle.value || '{}');
            if (d && d.EntregaID) { imprimirEntrega(d.EntregaID); }
        } catch(e) {}
    }

    function confirmarEntrega(id, folio) {
        Swal.fire({
            title: '\u00bfConfirmar entrega?',
            html: 'Se descontar\u00e1 el stock para el folio <strong>' + folio + '</strong>.<br>Esta acci\u00f3n no se puede deshacer f\u00e1cilmente.',
            icon: 'question',
            showCancelButton: true,
            confirmButtonColor: '#27ae60',
            cancelButtonColor: '#6c757d',
            confirmButtonText: '<i class="fas fa-check mr-1"></i> S\u00ed, confirmar',
            cancelButtonText: 'Cancelar'
        }).then(function (r) {
            if (r.isConfirmed) {
                document.getElementById('<%= hdnAccion.ClientID %>').value = 'confirmar';
                document.getElementById('<%= hdnEntregaIDAccion.ClientID %>').value = id;
                document.getElementById('<%= btnProcesarAccion.ClientID %>').click();
            }
        });
    }

    function cancelarEntrega(id, folio) {
        Swal.fire({
            title: '\u00bfCancelar entrega?',
            html: 'Se cancelar\u00e1 el folio <strong>' + folio + '</strong>.' +
                  '<br>Si ya estaba entregada, se devolver\u00e1 el stock.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#e74c3c',
            cancelButtonColor: '#6c757d',
            confirmButtonText: '<i class="fas fa-times mr-1"></i> S\u00ed, cancelar',
            cancelButtonText: 'No'
        }).then(function (r) {
            if (r.isConfirmed) {
                document.getElementById('<%= hdnAccion.ClientID %>').value = 'cancelar';
                document.getElementById('<%= hdnEntregaIDAccion.ClientID %>').value = id;
                document.getElementById('<%= btnProcesarAccion.ClientID %>').click();
            }
        });
    }

    // ─────────────────────────────────────────────────────────────────
    // Renderizar modal de detalle desde JSON
    // ─────────────────────────────────────────────────────────────────
    function fmtMoney(value) {
        return parseFloat(value).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function mostrarDetalleModal(d) {
        document.getElementById('dFolio').textContent     = d.Folio || '—';
        document.getElementById('dFecha').textContent     = d.Fecha || '—';
        document.getElementById('dBase').textContent      = d.Base  || '—';
        document.getElementById('dCliente').textContent   = d.Cliente || '—';
        document.getElementById('dRegistrado').textContent= d.Registrado || '—';
        document.getElementById('dObs').textContent       = d.Obs  || '—';
        var estadoEl = document.getElementById('dEstado');
        estadoEl.innerHTML = '<span class="badge badge-' + (d.Estado || '').toLowerCase().replace('_','-') + '">' + d.Estado + '</span>';

        var tbody = document.getElementById('tbodyDetalle');
        tbody.innerHTML = '';
        var total = 0;
        (d.Items || []).forEach(function (it) {
            var sub = it.Cantidad * it.PrecioUnitario;
            total += sub;
            var badgeCls = it.TipoItem === 'PRODUCTO' ? 'badge-primary' : 'badge-warning';

            // Celda de cantidad: capturada como primaria, base como secundaria pequeña
            var cantHtml;
            if (it.TuvoConversion && it.UnidadCaptura) {
                var capStr = parseFloat(it.CantidadCapturada).toString().replace(/\.?0+$/, '') || it.CantidadCapturada;
                cantHtml = '<strong>' + escHtml(String(capStr)) + ' ' + escHtml(it.UnidadCaptura) + '</strong>' +
                           '<small class="text-muted d-block">= ' + it.Cantidad + ' ' + escHtml(it.UnidadBase || '') + '</small>';
            } else {
                cantHtml = it.Cantidad;
                if (it.UnidadBase) cantHtml += ' <small class="text-muted">' + escHtml(it.UnidadBase) + '</small>';
            }

            var tr = document.createElement('tr');
            tr.innerHTML =
                '<td><span class="badge ' + badgeCls + '">' + escHtml(it.TipoItem) + '</span></td>' +
                '<td>' + escHtml(it.Nombre) + '</td>' +
                '<td class="text-right">' + cantHtml + '</td>' +
                '<td class="text-right">$' + fmtMoney(it.PrecioUnitario) + '</td>' +
                '<td class="text-right font-weight-bold">$' + fmtMoney(sub) + '</td>';
            tbody.appendChild(tr);
        });
        document.getElementById('tdTotalDetalle').textContent = '$' + fmtMoney(total);

        // Guardar ID para poder imprimir desde el modal
        document.getElementById('<%= hdnDetalleJson.ClientID %>').value = JSON.stringify(d);
    }
</script>

</asp:Content>

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
                        <asp:DropDownList ID="ddlNuevoCliente" runat="server" CssClass="form-control"></asp:DropDownList>
                    </div>
                </div>
                <div class="row mb-3">
                    <div class="col-md-12">
                        <label class="font-weight-bold">Observaciones</label>
                        <asp:TextBox ID="txtNuevoObservaciones" runat="server" CssClass="form-control"
                            TextMode="MultiLine" Rows="2" MaxLength="500"></asp:TextBox>
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
                    <div class="col-md-4" id="divSelProducto">
                        <label class="font-weight-bold small">Producto</label>
                        <asp:DropDownList ID="ddlItemProducto" runat="server" CssClass="form-control form-control-sm"
                            onchange="onItemChange()"></asp:DropDownList>
                    </div>
                    <div class="col-md-4" id="divSelMaterial" style="display:none;">
                        <label class="font-weight-bold small">Material</label>
                        <asp:DropDownList ID="ddlItemMaterial" runat="server" CssClass="form-control form-control-sm"
                            onchange="onItemChange()"></asp:DropDownList>
                    </div>
                    <div class="col-md-2">
                        <label class="font-weight-bold small">Cantidad <span class="text-danger">*</span></label>
                        <input type="number" id="txtItemCantidad" class="form-control form-control-sm" min="1" value="1" />
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
                                <th class="text-right" style="width:100px">Precio Unit.</th>
                                <th class="text-right" style="width:100px">Subtotal</th>
                                <th style="width:40px"></th>
                            </tr>
                        </thead>
                        <tbody id="tbodyItems">
                            <tr id="trItemsVacios">
                                <td colspan="6" class="text-center" id="divItemsVacios">
                                    A&uacute;n no hay items. Use el formulario de arriba para agregar.
                                </td>
                            </tr>
                        </tbody>
                        <tfoot>
                            <tr>
                                <td colspan="4" class="text-right font-weight-bold">Total:</td>
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
        document.getElementById('divSelProducto').style.display = tipo === 'PRODUCTO' ? '' : 'none';
        document.getElementById('divSelMaterial').style.display = tipo === 'MATERIAL' ? '' : 'none';
        onItemChange();
    }

    function onItemChange() {
        var tipo = document.getElementById('selTipoItem').value;
        var precio = 0;
        if (tipo === 'PRODUCTO') {
            var sel = document.getElementById('<%= ddlItemProducto.ClientID %>');
            var id = sel.value;
            precio = (window.prodPrecios && window.prodPrecios[id]) ? window.prodPrecios[id] : 0;
        } else {
            var sel2 = document.getElementById('<%= ddlItemMaterial.ClientID %>');
            var id2 = sel2.value;
            precio = (window.matPrecios && window.matPrecios[id2]) ? window.matPrecios[id2] : 0;
        }
        document.getElementById('txtItemPrecio').value = precio.toFixed(2);
    }

    function agregarItem() {
        var tipo = document.getElementById('selTipoItem').value;
        var itemID, nombre;

        if (tipo === 'PRODUCTO') {
            var sel = document.getElementById('<%= ddlItemProducto.ClientID %>');
            if (!sel.value) { alert('Seleccione un producto.'); return; }
            itemID = parseInt(sel.value);
            nombre = sel.options[sel.selectedIndex].text;
        } else {
            var sel2 = document.getElementById('<%= ddlItemMaterial.ClientID %>');
            if (!sel2.value) { alert('Seleccione un material.'); return; }
            itemID = parseInt(sel2.value);
            nombre = sel2.options[sel2.selectedIndex].text;
        }

        var cantidad = parseInt(document.getElementById('txtItemCantidad').value) || 0;
        var precio   = parseFloat(document.getElementById('txtItemPrecio').value) || 0;

        if (cantidad <= 0) { alert('La cantidad debe ser mayor a 0.'); return; }

        // Evitar duplicado del mismo item+tipo
        for (var i = 0; i < _items.length; i++) {
            if (_items[i].TipoItem === tipo && _items[i].ItemID === itemID) {
                _items[i].Cantidad += cantidad;
                _items[i].PrecioUnitario = precio;
                renderizarTabla();
                sincronizarHidden();
                return;
            }
        }

        var item = { TipoItem: tipo, ItemID: itemID, Nombre: nombre, Cantidad: cantidad, PrecioUnitario: precio };
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
            tbody.innerHTML = '<tr><td colspan="6" class="text-center" style="color:#868e96;font-style:italic;">A\u00FAn no hay items. Use el formulario de arriba para agregar.</td></tr>';
            actualizarTotal();
            return;
        }
        _items.forEach(function (it, idx) { renderizarFila(it, idx); });
        actualizarTotal();
    }

    function renderizarFila(it, idx) {
        var subtotal = it.Cantidad * it.PrecioUnitario;
        var badgeCls = it.TipoItem === 'PRODUCTO' ? 'badge-primary' : 'badge-warning';
        var tbody = document.getElementById('tbodyItems');
        var tr = document.createElement('tr');
        tr.innerHTML =
            '<td><span class="badge ' + badgeCls + '">' + it.TipoItem + '</span></td>' +
            '<td>' + escHtml(it.Nombre) + '</td>' +
            '<td class="text-right">' + it.Cantidad + '</td>' +
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
            var tr = document.createElement('tr');
            tr.innerHTML =
                '<td><span class="badge ' + badgeCls + '">' + escHtml(it.TipoItem) + '</span></td>' +
                '<td>' + escHtml(it.Nombre) + '</td>' +
                '<td class="text-right">' + it.Cantidad + '</td>' +
                '<td class="text-right">$' + parseFloat(it.PrecioUnitario).toFixed(2) + '</td>' +
                '<td class="text-right font-weight-bold">$' + sub.toFixed(2) + '</td>';
            tbody.appendChild(tr);
        });
        document.getElementById('tdTotalDetalle').textContent = '$' + total.toFixed(2);

        // Guardar ID para poder imprimir desde el modal
        document.getElementById('<%= hdnDetalleJson.ClientID %>').value = JSON.stringify(d);
    }
</script>

</asp:Content>

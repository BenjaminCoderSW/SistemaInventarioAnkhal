<%@ Page Title="Movimientos" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Movimientos.aspx.cs" Inherits="GrupoAnkhalInventario.Movimientos" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="css/gridviewPantalla.css" rel="stylesheet" />
    <style>
        /* ── Dashboard de movimientos ── */
        .stock-dashboard {
            display: flex;
            gap: 14px;
            margin-bottom: 18px;
            flex-wrap: wrap;
        }
        .stock-card {
            flex: 1;
            min-width: 160px;
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
        .stock-card.total      { background: linear-gradient(135deg,#1a5276,#2980b9); }
        .stock-card.entradas   { background: linear-gradient(135deg,#1e8449,#27ae60); }
        .stock-card.traspasos  { background: linear-gradient(135deg,#d35400,#e67e22); }
        .stock-card.ajustes    { background: linear-gradient(135deg,#7d6608,#d4ac0d); }
        .stock-card.mermas     { background: linear-gradient(135deg,#922b21,#e74c3c); }
        .stock-card.valor      { background: linear-gradient(135deg,#1c2833,#2c3e50); }
        .stock-card .icon      { font-size: 2.2rem; opacity: .9; }
        .stock-card .info .num { font-size: 2rem; font-weight: 700; line-height:1; }
        .stock-card .info .lbl { font-size: .78rem; opacity: .9; text-transform: uppercase; letter-spacing:.5px; }

        /* ── Filtros ── */
        .filtros-bar {
            background:#f8f9fa; border:1px solid #dee2e6;
            border-radius:8px; padding:14px 18px; margin-bottom:14px;
        }
        .filtros-bar label { font-weight:600; font-size:.84rem; color:#003366; margin-bottom:2px; }

        /* ── Paginador ── */
        .pager-custom span {
            background:#003366; color:#fff; font-weight:700;
            border-radius:4px; padding:4px 9px;
        }
        .pager-custom a { padding:4px 9px; border-radius:4px; }

        /* ── Badges de tipo movimiento ── */
        .badge-entrada        { background:#27ae60; color:#fff; }
        .badge-salida         { background:#e74c3c; color:#fff; }
        .badge-transferencia  { background:#3498db; color:#fff; }
        .badge-consumo        { background:#8e44ad; color:#fff; }
        .badge-merma          { background:#e67e22; color:#fff; }
        .badge-ajuste-pos     { background:#2ecc71; color:#fff; }
        .badge-ajuste-neg     { background:#c0392b; color:#fff; }

        /* ── Modal radio buttons ── */
        .tipo-item-radio label { margin-right: 20px; font-weight: 500; cursor: pointer; }
        .tipo-item-radio input[type="radio"] { margin-right: 5px; }

        /* ── Total calculado ── */
        .total-display {
            font-size: 1.4rem;
            font-weight: 700;
            color: #003366;
            padding: 8px 12px;
            background: #eaf2f8;
            border-radius: 6px;
            text-align: center;
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
<div class="container-fluid">
<div class="row">
<div class="col-12">

    <!-- ══ DASHBOARD DE MOVIMIENTOS — Fila 1: contadores ══════════ -->
    <div class="stock-dashboard">
        <div class="stock-card total">
            <div class="icon"><i class="fas fa-exchange-alt"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblTotalHoy" runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Total Hoy</div>
            </div>
        </div>
        <div class="stock-card entradas">
            <div class="icon"><i class="fas fa-arrow-circle-down"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblEntradas" runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Entradas</div>
            </div>
        </div>
        <div class="stock-card traspasos">
            <div class="icon"><i class="fas fa-random"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblTraspasos" runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Traspasos</div>
            </div>
        </div>
        <div class="stock-card ajustes">
            <div class="icon"><i class="fas fa-sliders-h"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblAjustes" runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Ajustes</div>
            </div>
        </div>
        <div class="stock-card mermas">
            <div class="icon"><i class="fas fa-trash-alt"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblMermas" runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Mermas</div>
            </div>
        </div>
    </div>
    <!-- ══ DASHBOARD — Fila 2: valor total (ancho completo) ════════ -->
    <div class="stock-dashboard" style="margin-bottom:18px;">
        <div class="stock-card valor" style="flex:0 0 100%;">
            <div class="icon"><i class="fas fa-dollar-sign"></i></div>
            <div class="info">
                <div class="num" style="font-size:2.4rem;">
                    <asp:Label ID="lblValorHoy" runat="server" Text="$0.00"></asp:Label>
                </div>
                <div class="lbl">Valor Total del Día (Entradas + Ajustes − Mermas/Ajustes negativos)</div>
            </div>
        </div>
    </div>

    <div class="card">
        <div class="card-header" style="background-color:#003366;color:white;">
            <h3 class="card-title"><i class="fas fa-exchange-alt"></i> Movimientos de Inventario</h3>
        </div>
        <div class="card-body">

            <div class="mb-3">
                <asp:Button ID="btnNuevo" runat="server" Text="+ Nuevo Movimiento"
                    CssClass="btn btn-success"
                    OnClientClick="abrirModalNuevo(); return false;" />
            </div>

            <!-- ── FILTROS ── -->
            <div class="filtros-bar">
                <div class="row align-items-end">
                    <div class="col-md-2">
                        <label>Tipo de movimiento</label>
                        <asp:DropDownList ID="ddlFiltrTipo" runat="server" CssClass="form-control form-control-sm">
                            <asp:ListItem Value="">-- Todos --</asp:ListItem>
                            <asp:ListItem Value="1">Entrada</asp:ListItem>
                            <asp:ListItem Value="3">Transferencia</asp:ListItem>
                            <asp:ListItem Value="6">Ajuste positivo</asp:ListItem>
                            <asp:ListItem Value="7">Ajuste negativo</asp:ListItem>
                            <asp:ListItem Value="5">Merma</asp:ListItem>
                            <asp:ListItem Value="4">Consumo</asp:ListItem>
                            <asp:ListItem Value="2">Salida</asp:ListItem>
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-2">
                        <label>Base</label>
                        <asp:DropDownList ID="ddlFiltrBase" runat="server" CssClass="form-control form-control-sm">
                            <asp:ListItem Value="">-- Todas --</asp:ListItem>
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-2">
                        <label>Tipo de item</label>
                        <asp:DropDownList ID="ddlFiltrItem" runat="server" CssClass="form-control form-control-sm">
                            <asp:ListItem Value="">-- Todos --</asp:ListItem>
                            <asp:ListItem Value="Material">Material</asp:ListItem>
                            <asp:ListItem Value="Producto">Producto</asp:ListItem>
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-2">
                        <label>Fecha desde</label>
                        <asp:TextBox ID="txtFechaDesde" runat="server" CssClass="form-control form-control-sm" TextMode="Date"></asp:TextBox>
                    </div>
                    <div class="col-md-2">
                        <label>Fecha hasta</label>
                        <asp:TextBox ID="txtFechaHasta" runat="server" CssClass="form-control form-control-sm" TextMode="Date"></asp:TextBox>
                    </div>
                    <div class="col-md-2 mt-1">
                        <asp:Button ID="btnBuscar" runat="server" Text="Buscar"
                            CssClass="btn btn-primary btn-sm mr-1" OnClick="btnBuscar_Click" />
                        <asp:Button ID="btnLimpiar" runat="server" Text="Limpiar"
                            CssClass="btn btn-secondary btn-sm" OnClick="btnLimpiar_Click" />
                    </div>
                </div>
            </div>

            <div class="mb-2">
                <small class="text-muted">
                    <asp:Label ID="lblResultados" runat="server"></asp:Label>
                </small>
            </div>

            <!-- ── GRID ── -->
            <div class="table-responsive">
                <asp:GridView ID="gvMovimientos" runat="server" AutoGenerateColumns="False"
                    CssClass="table table-bordered table-striped custom-grid"
                    AllowPaging="True" AllowCustomPaging="True" PageSize="15"
                    OnPageIndexChanging="gvMovimientos_PageIndexChanging"
                    DataKeyNames="MovimientoID"
                    PagerStyle-CssClass="pager-custom"
                    PagerSettings-Mode="NumericFirstLast"
                    PagerSettings-FirstPageText="«"
                    PagerSettings-LastPageText="»"
                    PagerSettings-PageButtonCount="5">
                    <Columns>
                        <asp:BoundField DataField="MovimientoID" HeaderText="ID" Visible="false" />

                        <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy HH:mm}" />

                        <asp:TemplateField HeaderText="Tipo">
                            <ItemTemplate>
                                <span class='badge <%# GetBadgeTipo(Eval("TipoClave")) %>'>
                                    <%# Eval("Tipo") %>
                                </span>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:BoundField DataField="ItemNombre" HeaderText="Item" />

                        <asp:BoundField DataField="BaseOrigen" HeaderText="Base Origen" />

                        <asp:BoundField DataField="BaseDestino" HeaderText="Base Destino" />

                        <asp:BoundField DataField="Cantidad" HeaderText="Cantidad" DataFormatString="{0:N2}" />

                        <asp:BoundField DataField="Costo" HeaderText="Costo Unit." DataFormatString="{0:C2}" />

                        <asp:BoundField DataField="Total" HeaderText="Total ($)" DataFormatString="{0:C2}" />

                        <asp:BoundField DataField="RegistradoPor" HeaderText="Registrado Por" />

                        <asp:BoundField DataField="Observaciones" HeaderText="Observaciones" />
                    </Columns>
                </asp:GridView>
            </div>

        </div><!-- /card-body -->
    </div><!-- /card -->
</div>
</div>
</div>

<!-- ── HIDDEN FIELDS ────────────────────────────── -->
<asp:HiddenField ID="hdnMensajePendiente" runat="server" Value="" />
<asp:HiddenField ID="hdnTipoItemSeleccionado" runat="server" Value="Material" />
<asp:Button ID="btnCargarItems" runat="server" style="display:none" OnClick="btnCargarItems_Click" />

<!-- ══ MODAL NUEVO MOVIMIENTO ═════════════════════════ -->
<div class="modal fade" id="modalNuevo" tabindex="-1" role="dialog" data-backdrop="static">
  <div class="modal-dialog modal-lg" role="document">
    <div class="modal-content">
      <div class="modal-header" style="background-color:#003366;color:white;">
        <h5 class="modal-title"><i class="fas fa-exchange-alt"></i> Nuevo Movimiento</h5>
        <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
      </div>
      <div class="modal-body">
        <!-- Fila 1: Tipo de movimiento -->
        <div class="row">
          <div class="col-md-6">
            <div class="form-group">
              <label>Tipo de movimiento <span style="color:red">*</span></label>
              <asp:DropDownList ID="ddlTipoMovimiento" runat="server" CssClass="form-control"
                  onchange="onTipoMovimientoChange();">
                <asp:ListItem Value="">-- Seleccione --</asp:ListItem>
                <asp:ListItem Value="1">Entrada de proveedor</asp:ListItem>
                <asp:ListItem Value="3">Transferencia entre bases</asp:ListItem>
                <asp:ListItem Value="6">Ajuste positivo</asp:ListItem>
                <asp:ListItem Value="7">Ajuste negativo</asp:ListItem>
                <asp:ListItem Value="5">Merma</asp:ListItem>
              </asp:DropDownList>
            </div>
          </div>
          <div class="col-md-6">
            <div class="form-group">
              <label>Tipo de item <span style="color:red">*</span></label>
              <div class="tipo-item-radio mt-2">
                <label>
                  <input type="radio" name="rbTipoItem" id="rbMaterial" value="Material" checked="checked"
                      onclick="onTipoItemChange();" />
                  Material
                </label>
                <label>
                  <input type="radio" name="rbTipoItem" id="rbProducto" value="Producto"
                      onclick="onTipoItemChange();" />
                  Producto
                </label>
              </div>
            </div>
          </div>
        </div>

        <!-- Fila 2: Item -->
        <div class="row">
          <div class="col-md-12">
            <div class="form-group">
              <label>Item <span style="color:red">*</span></label>
              <asp:DropDownList ID="ddlItem" runat="server" CssClass="form-control"
                  onchange="onItemChange();">
                <asp:ListItem Value="">-- Seleccione un item --</asp:ListItem>
              </asp:DropDownList>
            </div>
          </div>
        </div>

        <!-- Fila 3: Bases -->
        <div class="row">
          <div class="col-md-6" id="divBaseOrigen" style="display:none;">
            <div class="form-group">
              <label>Base Origen <span style="color:red">*</span></label>
              <asp:DropDownList ID="ddlBaseOrigen" runat="server" CssClass="form-control">
                <asp:ListItem Value="">-- Seleccione --</asp:ListItem>
              </asp:DropDownList>
            </div>
          </div>
          <div class="col-md-6" id="divBaseDestino" style="display:none;">
            <div class="form-group">
              <label>Base Destino <span style="color:red">*</span></label>
              <asp:DropDownList ID="ddlBaseDestino" runat="server" CssClass="form-control">
                <asp:ListItem Value="">-- Seleccione --</asp:ListItem>
              </asp:DropDownList>
            </div>
          </div>
        </div>

        <!-- Fila 4: Cantidad + Costo -->
        <div class="row">
          <div class="col-md-4">
            <div class="form-group">
              <label>Cantidad <span style="color:red">*</span></label>
              <asp:TextBox ID="txtCantidad" runat="server" CssClass="form-control" TextMode="Number"
                  Placeholder="0" min="0.01" step="0.01"
                  onkeyup="calcularTotal();" onchange="calcularTotal();"></asp:TextBox>
            </div>
          </div>
          <div class="col-md-4">
            <div class="form-group">
              <label>Costo unitario <span style="color:red">*</span></label>
              <div class="input-group">
                <div class="input-group-prepend"><span class="input-group-text">$</span></div>
                <asp:TextBox ID="txtCosto" runat="server" CssClass="form-control" TextMode="Number"
                    Placeholder="0.00" min="0" step="0.01"
                    onkeyup="calcularTotal();" onchange="calcularTotal();"></asp:TextBox>
              </div>
            </div>
          </div>
          <div class="col-md-4">
            <div class="form-group">
              <label>Total</label>
              <div class="total-display">
                $<asp:Label ID="lblTotal" runat="server" Text="0.00"></asp:Label>
              </div>
            </div>
          </div>
        </div>

        <!-- Fila 5: Observaciones -->
        <div class="row">
          <div class="col-md-12">
            <div class="form-group">
              <label>Observaciones</label>
              <asp:TextBox ID="txtObservaciones" runat="server" CssClass="form-control" TextMode="MultiLine"
                  Rows="3" Placeholder="Observaciones opcionales..." MaxLength="500"></asp:TextBox>
            </div>
          </div>
        </div>
      </div>
      <div class="modal-footer">
        <asp:Button ID="btnGuardar" runat="server" Text="Guardar Movimiento"
            CssClass="btn btn-success"
            OnClientClick="return validarNuevo();"
            OnClick="btnGuardar_Click" />
        <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancelar</button>
      </div>
    </div>
  </div>
</div>

<asp:Literal ID="litJsData" runat="server"></asp:Literal>

<script>
    // ── Mensaje pendiente (SweetAlert) ──────────────────────────────
    window.addEventListener('load', function () {
        // Restaurar estado del radio al volver de postback
        var tipoItem = document.getElementById('<%= hdnTipoItemSeleccionado.ClientID %>').value;
        if (tipoItem === 'Producto') document.getElementById('rbProducto').checked = true;
        else                         document.getElementById('rbMaterial').checked  = true;

        // Mostrar mensaje pendiente
        var hdnMsg = document.getElementById('<%= hdnMensajePendiente.ClientID %>');
        if (!hdnMsg || !hdnMsg.value) return;
        try {
            var msg = JSON.parse(hdnMsg.value);
            hdnMsg.value = '';
            var opts = { icon: msg.icon, title: msg.title, text: msg.text, confirmButtonColor: '#003366' };
            if (msg.icon === 'success') { opts.showConfirmButton = false; opts.timer = 2000; }
            if (msg.modal) {
                opts.showConfirmButton = true;
                Swal.fire(opts).then(function () { $('#' + msg.modal).modal('show'); });
            } else { Swal.fire(opts); }
        } catch (e) { }
    });

    // ── Abrir modal nuevo ───────────────────────────────────────────
    function abrirModalNuevo() {
        document.getElementById('<%= ddlTipoMovimiento.ClientID %>').value = '';
        document.getElementById('rbMaterial').checked = true;
        document.getElementById('<%= hdnTipoItemSeleccionado.ClientID %>').value = 'Material';
        document.getElementById('<%= ddlItem.ClientID %>').selectedIndex = 0;
        document.getElementById('<%= ddlBaseOrigen.ClientID %>').selectedIndex = 0;
        document.getElementById('<%= ddlBaseDestino.ClientID %>').selectedIndex = 0;
        document.getElementById('<%= txtCantidad.ClientID %>').value = '';
        var txtCosto = document.getElementById('<%= txtCosto.ClientID %>');
        txtCosto.value = '';
        txtCosto.disabled = false;
        document.getElementById('<%= lblTotal.ClientID %>').innerText = '0.00';
        document.getElementById('<%= txtObservaciones.ClientID %>').value = '';
        document.getElementById('divBaseOrigen').style.display = 'none';
        document.getElementById('divBaseDestino').style.display = 'none';
        $('#modalNuevo').modal('show');
    }

    // ── Mostrar/ocultar bases y bloquear costo en transferencia ────
    function onTipoMovimientoChange() {
        var tipo       = document.getElementById('<%= ddlTipoMovimiento.ClientID %>').value;
        var divOrigen  = document.getElementById('divBaseOrigen');
        var divDestino = document.getElementById('divBaseDestino');
        var txtCosto   = document.getElementById('<%= txtCosto.ClientID %>');

        divOrigen.style.display  = 'none';
        divDestino.style.display = 'none';

        // Transferencia interna → costo siempre $0 (no hay ganancia ni pérdida)
        if (tipo === '3') {
            txtCosto.value    = '0.00';
            txtCosto.disabled = true;
        } else {
            txtCosto.disabled = false;
        }

        switch (tipo) {
            case '1': divDestino.style.display = 'block'; break;
            case '3': divOrigen.style.display = 'block'; divDestino.style.display = 'block'; break;
            case '6': divDestino.style.display = 'block'; break;
            case '7': divOrigen.style.display  = 'block'; break;
            case '5': divOrigen.style.display  = 'block'; break;
        }
        calcularTotal();
    }

    // ── Cambio tipo item (radio) → poblar ddlItem ──────────────────
    function onTipoItemChange() {
        var seleccion = document.querySelector('input[name="rbTipoItem"]:checked').value;
        document.getElementById('<%= hdnTipoItemSeleccionado.ClientID %>').value = seleccion;
        var ddl = document.getElementById('<%= ddlItem.ClientID %>');
        ddl.innerHTML = '<option value="">-- Seleccione un item --</option>';
        var lista = seleccion === 'Producto' ? window._productosData
                  : window._materialesData;
        if (lista) {
            lista.forEach(function (item) {
                var opt = document.createElement('option');
                opt.value = item.id;
                opt.text  = item.nombre + (item.unidad ? ' (' + item.unidad + ')' : '');
                ddl.appendChild(opt);
            });
        }
    }

    // ── Auto-llenar costo al seleccionar item ───────────────────────
    function onItemChange() {
        var txtCosto = document.getElementById('<%= txtCosto.ClientID %>');
        if (txtCosto.disabled) return; // Transferencia: no modificar
        var ddl  = document.getElementById('<%= ddlItem.ClientID %>');
        var id   = parseInt(ddl.value);
        if (!id) return;
        var tipo  = document.getElementById('<%= hdnTipoItemSeleccionado.ClientID %>').value;
        var lista = tipo === 'Producto' ? window._productosData
                  : window._materialesData;
        var item  = lista && lista.find(function (i) { return i.id === id; });
        if (item) {
            txtCosto.value = (item.costo || 0).toFixed(2);
            calcularTotal();
        }
    }

    // ── Auto-calcular total ─────────────────────────────────────────
    function calcularTotal() {
        var cantidad = parseFloat(document.getElementById('<%= txtCantidad.ClientID %>').value) || 0;
        var costo    = parseFloat(document.getElementById('<%= txtCosto.ClientID %>').value) || 0;
        document.getElementById('<%= lblTotal.ClientID %>').innerText = (cantidad * costo).toFixed(2);
    }

    // ── Validación cliente antes de guardar ─────────────────────────
    function validarNuevo() {
        var tipo     = document.getElementById('<%= ddlTipoMovimiento.ClientID %>').value;
        var item     = document.getElementById('<%= ddlItem.ClientID %>').value;
        var cantidad = parseFloat(document.getElementById('<%= txtCantidad.ClientID %>').value) || 0;
        var costo    = parseFloat(document.getElementById('<%= txtCosto.ClientID %>').value) || 0;
        var origen   = document.getElementById('<%= ddlBaseOrigen.ClientID %>').value;
        var destino  = document.getElementById('<%= ddlBaseDestino.ClientID %>').value;

        function warn(txt) {
            Swal.fire({ icon: 'warning', title: 'Campo inválido', text: txt, confirmButtonColor: '#003366' })
                .then(function () { $('#modalNuevo').modal('show'); });
            return false;
        }

        if (!tipo)       return warn('Debe seleccionar el tipo de movimiento.');
        if (!item)       return warn('Debe seleccionar un item.');
        if (cantidad <= 0) return warn('La cantidad debe ser mayor a cero.');
        // El costo de una transferencia siempre es $0, no validar
        if (tipo !== '3' && costo < 0) return warn('El costo unitario no puede ser negativo.');

        var divOrigen  = document.getElementById('divBaseOrigen');
        var divDestino = document.getElementById('divBaseDestino');
        if (divOrigen.style.display  !== 'none' && !origen)  return warn('Debe seleccionar la base de origen.');
        if (divDestino.style.display !== 'none' && !destino) return warn('Debe seleccionar la base de destino.');
        if (tipo === '3' && origen && destino && origen === destino)
            return warn('La base de origen y destino no pueden ser la misma.');

        return true;
    }
</script>

</asp:Content>

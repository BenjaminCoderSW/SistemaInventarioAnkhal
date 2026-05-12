<%@ Page Title="Movimientos" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Movimientos.aspx.cs" Inherits="GrupoAnkhalInventario.Movimientos" EnableEventValidation="false" %>

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
        .stock-card.consumos   { background: linear-gradient(135deg,#6c3483,#8e44ad); }
        .stock-card.salidas    { background: linear-gradient(135deg,#7b241c,#c0392b); }
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

        /* ── Checkboxes de tipo movimiento ── */
        .tipos-check-group span { display: inline-flex; align-items: center; margin-right: 14px; margin-bottom: 2px; }
        .tipos-check-group input[type=checkbox] { margin-right: 4px; }

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

        /* ── Lote: sección agregar ítem ── */
        .lote-seccion-header {
            background: #eaf2f8;
            border-left: 4px solid #2980b9;
            padding: 8px 12px;
            margin-bottom: 12px;
            border-radius: 4px;
            font-weight: 600;
            color: #003366;
            font-size: .9rem;
        }
        .lote-seccion-items {
            background: #f0fff4;
            border-left: 4px solid #27ae60;
            padding: 8px 12px;
            margin-bottom: 12px;
            border-radius: 4px;
            font-weight: 600;
            color: #1e8449;
            font-size: .9rem;
        }
        #tblItemsLote { font-size: .88rem; }
        #tblItemsLote thead th { background: #003366; color: #fff; font-size:.8rem; }
        .btn-quitar-item { padding: 2px 7px; font-size: .78rem; }
        #divItemsAcumulados { display: none; }
        .lote-subtotal {
            text-align: right;
            font-weight: 700;
            font-size: 1rem;
            color: #003366;
            padding: 4px 8px;
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
                <div class="lbl"><asp:Label ID="lblTituloTotal" runat="server" Text="TOTAL HOY"></asp:Label></div>
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
        <div class="stock-card consumos">
            <div class="icon"><i class="fas fa-industry"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblConsumos" runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Consumos</div>
            </div>
        </div>
        <div class="stock-card salidas">
            <div class="icon"><i class="fas fa-sign-out-alt"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblSalidas" runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Salidas</div>
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
                <div class="lbl"><asp:Label ID="lblDescValor" runat="server" Text="Valor Total del Día (Entradas + Ajustes − Mermas/Ajustes negativos)"></asp:Label></div>
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
                <!-- Fila 1: tipos de movimiento (multi-selección) -->
                <div class="row mb-2">
                    <div class="col-12">
                        <label>Tipo de movimiento</label><br />
                        <asp:CheckBoxList ID="cblFiltrTipo" runat="server"
                            RepeatDirection="Horizontal" RepeatLayout="Flow"
                            CssClass="tipos-check-group">
                            <asp:ListItem Value="1">Entrada</asp:ListItem>
                            <asp:ListItem Value="3">Transferencia</asp:ListItem>
                            <asp:ListItem Value="6">Ajuste positivo</asp:ListItem>
                            <asp:ListItem Value="7">Ajuste negativo</asp:ListItem>
                            <asp:ListItem Value="5">Merma</asp:ListItem>
                            <asp:ListItem Value="4">Consumo</asp:ListItem>
                            <asp:ListItem Value="2">Salida</asp:ListItem>
                        </asp:CheckBoxList>
                        <small class="text-muted">(Sin selecci&oacute;n = todos los tipos)</small>
                    </div>
                </div>
                <!-- Fila 2: resto de filtros -->
                <div class="row align-items-end">
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

                        <asp:TemplateField HeaderText="Item">
                            <ItemTemplate>
                                <strong style="color:#003366;"><%# Eval("ItemCodigo") %></strong>
                                <%# Eval("ItemNombre") %>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:BoundField DataField="BaseOrigen" HeaderText="Base Origen" />

                        <asp:BoundField DataField="BaseDestino" HeaderText="Base Destino" />

                        <asp:TemplateField HeaderText="Proveedor">
                            <ItemTemplate>
                                <%# !string.IsNullOrEmpty(Eval("ProveedorNombre") as string)
                                    ? "<span class='text-muted small'>" + System.Web.HttpUtility.HtmlEncode(Eval("ProveedorNombre").ToString()) + "</span>"
                                    : "<span class='text-muted small'>—</span>" %>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Cantidad / Unidad">
                            <ItemTemplate>
                                <%# FormatCantidadGrilla(
                                        Eval("CantidadCapturada"),
                                        Eval("UnidadCapturaNombre"),
                                        Eval("Cantidad"),
                                        Eval("Unidad"),
                                        Eval("TuvoConversion")) %>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:BoundField DataField="Costo" HeaderText="Costo Unit." DataFormatString="{0:C2}" />

                        <asp:BoundField DataField="Total" HeaderText="Total ($)" DataFormatString="{0:C2}" />

                        <asp:BoundField DataField="RegistradoPor" HeaderText="Registrado Por" />

                        <asp:TemplateField HeaderText="Folio Lote">
                            <ItemTemplate>
                                <%# !string.IsNullOrEmpty(Eval("FolioLote") as string)
                                    ? "<span class='badge' style='background:#555;color:#fff;font-size:.78rem;'>" +
                                      System.Web.HttpUtility.HtmlEncode(Eval("FolioLote").ToString()) + "</span>"
                                    : "<span class='text-muted small'>—</span>" %>
                            </ItemTemplate>
                        </asp:TemplateField>

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
<asp:HiddenField ID="hdnItemsJson" runat="server" Value="" />
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

        <!-- ── ZONA ENCABEZADO DEL LOTE ─────────────────────────────── -->
        <div class="lote-seccion-header"><i class="fas fa-tag mr-1"></i> Encabezado del movimiento</div>

        <!-- Tipo de movimiento + bases -->
        <div class="row">
          <div class="col-md-4">
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
          <div class="col-md-4" id="divBaseOrigen" style="display:none;">
            <div class="form-group">
              <label>Base Origen <span style="color:red">*</span></label>
              <asp:DropDownList ID="ddlBaseOrigen" runat="server" CssClass="form-control">
                <asp:ListItem Value="">-- Seleccione --</asp:ListItem>
              </asp:DropDownList>
            </div>
          </div>
          <div class="col-md-4" id="divBaseDestino" style="display:none;">
            <div class="form-group">
              <label>Base Destino <span style="color:red">*</span></label>
              <asp:DropDownList ID="ddlBaseDestino" runat="server" CssClass="form-control">
                <asp:ListItem Value="">-- Seleccione --</asp:ListItem>
              </asp:DropDownList>
            </div>
          </div>
        </div>

        <!-- ── PROVEEDOR (solo visible en ENTRADA) ─────────────────── -->
        <div class="row" id="rowProveedor" style="display:none;">
          <div class="col-md-6">
            <div class="form-group">
              <label>Proveedor</label>
              <asp:DropDownList ID="ddlProveedor" runat="server" CssClass="form-control">
                <asp:ListItem Value="">-- Sin proveedor --</asp:ListItem>
              </asp:DropDownList>
            </div>
          </div>
        </div>

        <!-- ── ZONA AGREGAR ÍTEM ─────────────────────────────────────── -->
        <div class="lote-seccion-items"><i class="fas fa-plus-circle mr-1"></i> Agregar ítem al lote</div>

        <div class="row">
          <div class="col-md-3">
            <div class="form-group">
              <label>Tipo de item</label>
              <div class="tipo-item-radio mt-1">
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
          <div class="col-md-9">
            <div class="form-group">
              <label>Item <span style="color:red">*</span></label>
              <asp:DropDownList ID="ddlItem" runat="server" CssClass="form-control"
                  onchange="onItemChange();">
                <asp:ListItem Value="">-- Seleccione un item --</asp:ListItem>
              </asp:DropDownList>
            </div>
          </div>
        </div>

        <div class="row">
          <div class="col-md-3">
            <div class="form-group">
              <label>Cantidad <span style="color:red">*</span></label>
              <asp:TextBox ID="txtCantidad" runat="server" CssClass="form-control" TextMode="Number"
                  Placeholder="0" min="0.01" step="0.01"
                  onkeyup="calcularTotal(); actualizarInfoConversion();"
                  onchange="calcularTotal(); actualizarInfoConversion();"></asp:TextBox>
            </div>
          </div>
          <div class="col-md-3" id="divUnidadCaptura" style="display:none;">
            <div class="form-group">
              <label>Unidad de captura</label>
              <asp:DropDownList ID="ddlUnidadCaptura" runat="server" CssClass="form-control"
                  onchange="actualizarInfoConversion(); calcularTotal();"></asp:DropDownList>
              <asp:Label ID="lblConversionInfo" runat="server" CssClass="text-muted small mt-1 d-block"
                  Text=""></asp:Label>
            </div>
          </div>
          <div class="col-md-2" id="divCosto">
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
          <div class="col-md-2">
            <div class="form-group">
              <label>Subtotal ítem</label>
              <div class="total-display" style="font-size:1.1rem;">
                $<asp:Label ID="lblTotal" runat="server" Text="0.00"></asp:Label>
              </div>
            </div>
          </div>
          <div class="col-md-2 d-flex align-items-end pb-3">
            <button type="button" class="btn btn-primary btn-block"
                onclick="agregarItemAlLote();">
              <i class="fas fa-plus"></i> Agregar
            </button>
          </div>
        </div>

        <!-- ── TABLA DE ÍTEMS ACUMULADOS ─────────────────────────────── -->
        <div class="mb-1">
          <small class="text-muted"><i class="fas fa-info-circle"></i> Puedes agregar hasta 50 ítems por movimiento.</small>
        </div>
        <div id="divItemsAcumulados" class="mb-2">
          <table id="tblItemsLote" class="table table-bordered table-sm mb-1">
            <thead>
              <tr>
                <th>#</th>
                <th>Tipo</th>
                <th>Item</th>
                <th>Cant. capturada</th>
                <th>Unidad</th>
                <th>Costo Unit.</th>
                <th>Subtotal</th>
                <th></th>
              </tr>
            </thead>
            <tbody id="tbodyItems"></tbody>
          </table>
          <div class="lote-subtotal">Total lote: $<span id="spnTotalLote">0.00</span></div>
        </div>

        <!-- ── OBSERVACIONES (nivel lote) ───────────────────────────── -->
        <div class="row">
          <div class="col-md-12">
            <div class="form-group">
              <label>Observaciones del lote</label>
              <asp:TextBox ID="txtObservaciones" runat="server" CssClass="form-control" TextMode="MultiLine"
                  Rows="2" Placeholder="Observaciones opcionales..." MaxLength="500"></asp:TextBox>
            </div>
          </div>
        </div>

      </div><!-- /modal-body -->
      <div class="modal-footer">
        <asp:Button ID="btnGuardar" runat="server" Text="Guardar Lote"
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
    // ── Array acumulado de ítems del lote ───────────────────────────
    var _items = [];
    var _prevTipoMovimiento = "";

    // ── Mensaje pendiente (SweetAlert) ──────────────────────────────
    window.addEventListener('load', function () {
        var tipoItem = document.getElementById('<%= hdnTipoItemSeleccionado.ClientID %>').value;
        if (tipoItem === 'Producto') document.getElementById('rbProducto').checked = true;
        else                         document.getElementById('rbMaterial').checked  = true;

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
        _items = [];
        _prevTipoMovimiento = "";
        document.getElementById('<%= hdnItemsJson.ClientID %>').value = '[]';
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
        document.getElementById('divUnidadCaptura').style.display = 'none';
        document.getElementById('<%= lblConversionInfo.ClientID %>').innerText = '';
        var ddlUC = document.getElementById('<%= ddlUnidadCaptura.ClientID %>');
        if (ddlUC) ddlUC.innerHTML = '';
        renderTablaItems();
        $('#modalNuevo').modal('show');
    }

    // ── Agregar ítem al array _items ────────────────────────────────
    function agregarItemAlLote() {
        var tipo     = document.getElementById('<%= hdnTipoItemSeleccionado.ClientID %>').value;
        var ddlItem  = document.getElementById('<%= ddlItem.ClientID %>');
        var itemId   = parseInt(ddlItem.value);
        var cant     = parseFloat(document.getElementById('<%= txtCantidad.ClientID %>').value) || 0;
        var costo    = parseFloat(document.getElementById('<%= txtCosto.ClientID %>').value) || 0;
        var itemText = ddlItem.options[ddlItem.selectedIndex] ? ddlItem.options[ddlItem.selectedIndex].text : '';

        function warn(txt) {
            Swal.fire({ icon: 'warning', title: 'Campo inválido', text: txt, confirmButtonColor: '#003366' })
                .then(function () { $('#modalNuevo').modal('show'); });
        }
        if (!itemId)     { warn('Debe seleccionar un item.'); return; }
        if (cant <= 0)   { warn('La cantidad debe ser mayor a cero.'); return; }
        var tipoMov = document.getElementById('<%= ddlTipoMovimiento.ClientID %>').value;
        if (!tipoMov)    { warn('Debe seleccionar el tipo de movimiento primero.'); return; }
        if (tipoMov !== '3' && costo < 0) { warn('El costo unitario no puede ser negativo.'); return; }

        // Unidad de captura
        var unidadId  = 0;
        var unidadTxt = '';
        var factor    = 1;
        var divUC     = document.getElementById('divUnidadCaptura');
        var ddlUC     = document.getElementById('<%= ddlUnidadCaptura.ClientID %>');
        if (divUC && divUC.style.display !== 'none' && ddlUC && ddlUC.options.length > 0) {
            var selOpt = ddlUC.options[ddlUC.selectedIndex];
            unidadId   = parseInt(selOpt.value) || 0;
            unidadTxt  = selOpt.text;
            factor     = parseFloat(selOpt.getAttribute('data-factor')) || 1;
        }

        // Verificar límite antes de agregar
        var MAX_ITEMS = 50;
        if (_items.length >= MAX_ITEMS) {
            Swal.fire({ icon: 'warning', title: 'Límite alcanzado',
                text: 'Un lote no puede contener más de ' + MAX_ITEMS + ' ítems.',
                confirmButtonColor: '#003366' })
                .then(function () { $('#modalNuevo').modal('show'); });
            return;
        }

        // Acumular si mismo ítem ya existe (mismo tipo+id+unidad)
        var existing = null;
        for (var i = 0; i < _items.length; i++) {
            if (_items[i].tipoItem === tipo && _items[i].itemId === itemId && _items[i].unidadId === unidadId) {
                existing = _items[i];
                break;
            }
        }

        if (existing) {
            existing.cantidadCapturada += cant;
        } else {
            _items.push({
                tipoItem:          tipo,
                itemId:            itemId,
                itemTexto:         itemText,
                cantidadCapturada: cant,
                costo:             costo,
                unidadId:          unidadId,
                unidadTexto:       unidadTxt,
                factor:            factor
            });
        }

        document.getElementById('<%= hdnItemsJson.ClientID %>').value = JSON.stringify(_items);
        renderTablaItems();
        limpiarCamposItem();
    }

    // ── Eliminar ítem del array ─────────────────────────────────────
    function quitarItem(idx) {
        _items.splice(idx, 1);
        document.getElementById('<%= hdnItemsJson.ClientID %>').value = JSON.stringify(_items);
        renderTablaItems();
    }

    // ── Renderizar tabla de ítems acumulados ────────────────────────
    function renderTablaItems() {
        var tbody = document.getElementById('tbodyItems');
        var div   = document.getElementById('divItemsAcumulados');
        tbody.innerHTML = '';
        if (_items.length === 0) { div.style.display = 'none'; return; }
        div.style.display = 'block';

        var totalLote = 0;
        for (var i = 0; i < _items.length; i++) {
            var it      = _items[i];
            var cantBase = it.cantidadCapturada * it.factor;
            var subtotal = cantBase * it.costo;
            totalLote   += subtotal;

            var unidTxt = it.unidadTexto
                ? it.unidadTexto.replace(/\s*\[.*\]$/, '').replace(/\s*—\s*base\s*$/i, '').trim()
                : (it.tipoItem === 'Producto' ? 'Und.' : 'base');

            var cantMostrar = it.factor !== 1
                ? it.cantidadCapturada.toFixed(4).replace(/\.?0+$/, '') + ' → ' +
                  cantBase.toFixed(4).replace(/\.?0+$/, '')
                : it.cantidadCapturada.toFixed(4).replace(/\.?0+$/, '');

            var tr = '<tr>' +
                '<td>' + (i + 1) + '</td>' +
                '<td>' + it.tipoItem + '</td>' +
                '<td>' + it.itemTexto + '</td>' +
                '<td>' + cantMostrar + '</td>' +
                '<td>' + unidTxt + '</td>' +
                '<td>$' + it.costo.toFixed(2) + '</td>' +
                '<td>$' + subtotal.toFixed(2) + '</td>' +
                '<td><button type="button" class="btn btn-danger btn-quitar-item" ' +
                    'onclick="quitarItem(' + i + ')"><i class="fas fa-times"></i></button></td>' +
                '</tr>';
            tbody.innerHTML += tr;
        }
        document.getElementById('spnTotalLote').innerText = totalLote.toFixed(2);
    }

    // ── Limpiar campos de ítem (después de agregar) ─────────────────
    function limpiarCamposItem() {
        document.getElementById('<%= ddlItem.ClientID %>').selectedIndex = 0;
        document.getElementById('<%= txtCantidad.ClientID %>').value = '';
        document.getElementById('<%= txtCosto.ClientID %>').value = '';
        document.getElementById('<%= lblTotal.ClientID %>').innerText = '0.00';
        document.getElementById('divUnidadCaptura').style.display = 'none';
        document.getElementById('<%= lblConversionInfo.ClientID %>').innerText = '';
        var ddlUC = document.getElementById('<%= ddlUnidadCaptura.ClientID %>');
        if (ddlUC) ddlUC.innerHTML = '';
    }

    // ── Mostrar/ocultar bases y bloquear costo en transferencia ────
    function onTipoMovimientoChange() {
        var ddlTipo = document.getElementById('<%= ddlTipoMovimiento.ClientID %>');
        var tipo    = ddlTipo.value;

        if (_items.length > 0) {
            var ok = confirm("Ya tienes ítems agregados al lote. Cambiar el tipo de movimiento vaciará la lista. ¿Deseas continuar?");
            if (!ok) {
                ddlTipo.value = _prevTipoMovimiento;
                return;
            }
            _items = [];
            document.getElementById('<%= hdnItemsJson.ClientID %>').value = '[]';
            renderTablaItems();
        }

        _prevTipoMovimiento = tipo;
        var divOrigen    = document.getElementById('divBaseOrigen');
        var divDestino   = document.getElementById('divBaseDestino');
        var rowProveedor = document.getElementById('rowProveedor');
        var txtCosto     = document.getElementById('<%= txtCosto.ClientID %>');

        divOrigen.style.display    = 'none';
        divDestino.style.display   = 'none';
        rowProveedor.style.display = 'none';

        if (tipo === '3') {
            txtCosto.value    = '0.00';
            txtCosto.disabled = true;
        } else {
            txtCosto.disabled = false;
        }

        switch (tipo) {
            case '1': divDestino.style.display = 'block'; rowProveedor.style.display = 'block'; break;
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
                opt.text  = '[' + item.codigo + '] ' + item.nombre + (item.unidad ? ' (' + item.unidad + ')' : '');
                ddl.appendChild(opt);
            });
        }
        document.getElementById('divUnidadCaptura').style.display = 'none';
        document.getElementById('<%= lblConversionInfo.ClientID %>').innerText = '';
    }

    // ── Auto-llenar costo al seleccionar item ───────────────────────
    function onItemChange() {
        var txtCosto = document.getElementById('<%= txtCosto.ClientID %>');
        var ddl  = document.getElementById('<%= ddlItem.ClientID %>');
        var id   = parseInt(ddl.value);
        var divUC = document.getElementById('divUnidadCaptura');
        var ddlUC = document.getElementById('<%= ddlUnidadCaptura.ClientID %>');
        var lblCI = document.getElementById('<%= lblConversionInfo.ClientID %>');

        if (!id) {
            divUC.style.display = 'none';
            lblCI.innerText = '';
            return;
        }
        var tipo  = document.getElementById('<%= hdnTipoItemSeleccionado.ClientID %>').value;
        var lista = tipo === 'Producto' ? window._productosData
                  : window._materialesData;
        var item  = lista && lista.find(function (i) { return i.id === id; });
        if (item && !txtCosto.disabled) {
            txtCosto.value = (item.costo || 0).toFixed(2);
            calcularTotal();
        }

        var convKey = id.toString();
        if (tipo === 'Material' && window._conversionesMat &&
            window._conversionesMat[convKey] && window._conversionesMat[convKey].length > 1) {
            ddlUC.innerHTML = '';
            window._conversionesMat[convKey].forEach(function (op) {
                var opt = document.createElement('option');
                opt.value = op.val;
                opt.text  = op.txt;
                opt.setAttribute('data-factor', op.factor);
                ddlUC.appendChild(opt);
            });
            divUC.style.display = 'block';
            actualizarInfoConversion();
        } else {
            divUC.style.display = 'none';
            lblCI.innerText = '';
        }
    }

    // ── Actualizar label de conversión en tiempo real ───────────────
    function actualizarInfoConversion() {
        var divUC = document.getElementById('divUnidadCaptura');
        var lblCI = document.getElementById('<%= lblConversionInfo.ClientID %>');
        if (!divUC || divUC.style.display === 'none') return;

        var ddlUC = document.getElementById('<%= ddlUnidadCaptura.ClientID %>');
        if (!ddlUC || ddlUC.options.length === 0) { lblCI.innerText = ''; return; }

        var selectedOpt = ddlUC.options[ddlUC.selectedIndex];
        var factor   = parseFloat(selectedOpt.getAttribute('data-factor')) || 1;
        var cantidad = parseFloat(document.getElementById('<%= txtCantidad.ClientID %>').value) || 0;

        if (factor === 1) {
            lblCI.innerText = 'Cantidad en unidad base. Sin conversión.';
        } else {
            var cantBase   = cantidad * factor;
            var baseNombre = ddlUC.options[0].text.replace(/\s*[—\-]\s*base\s*$/i, '').trim();
            var cantStr    = (cantBase % 1 === 0) ? cantBase.toString() : cantBase.toFixed(4).replace(/\.?0+$/, '');
            lblCI.innerText = 'Captura: ' + cantidad + ' → ' + cantStr + ' ' + baseNombre + ' en stock';
        }
    }

    // ── Auto-calcular subtotal del ítem actual ──────────────────────
    function calcularTotal() {
        var cantCapturada = parseFloat(document.getElementById('<%= txtCantidad.ClientID %>').value) || 0;
        var costo         = parseFloat(document.getElementById('<%= txtCosto.ClientID %>').value) || 0;

        var cantBase = cantCapturada;
        var divUC = document.getElementById('divUnidadCaptura');
        if (divUC && divUC.style.display !== 'none') {
            var ddlUC = document.getElementById('<%= ddlUnidadCaptura.ClientID %>');
            if (ddlUC && ddlUC.options.length > 0) {
                var factor = parseFloat(ddlUC.options[ddlUC.selectedIndex].getAttribute('data-factor')) || 1;
                cantBase = cantCapturada * factor;
            }
        }
        document.getElementById('<%= lblTotal.ClientID %>').innerText = (cantBase * costo).toFixed(2);
    }

    // ── Validación antes de postback de guardar ─────────────────────
    function validarNuevo() {
        var tipo    = document.getElementById('<%= ddlTipoMovimiento.ClientID %>').value;
        var origen  = document.getElementById('<%= ddlBaseOrigen.ClientID %>').value;
        var destino = document.getElementById('<%= ddlBaseDestino.ClientID %>').value;

        function warn(txt) {
            Swal.fire({ icon: 'warning', title: 'Campo inválido', text: txt, confirmButtonColor: '#003366' })
                .then(function () { $('#modalNuevo').modal('show'); });
            return false;
        }

        if (!tipo) return warn('Debe seleccionar el tipo de movimiento.');
        if (_items.length === 0) return warn('Debe agregar al menos un ítem al lote antes de guardar.');

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

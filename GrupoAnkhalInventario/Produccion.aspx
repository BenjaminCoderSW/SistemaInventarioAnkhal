<%@ Page Title="Producción" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Produccion.aspx.cs" Inherits="GrupoAnkhalInventario.ProduccionPage" %>

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

        /* ── Badges turno ── */
        .badge-manana  { background:#f39c12; color:#fff; }
        .badge-tarde   { background:#2980b9; color:#fff; }
        .badge-noche   { background:#2c3e50; color:#fff; }
        .badge-unico   { background:#8e44ad; color:#fff; }

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
                <asp:DropDownList ID="ddlFiltrProducto" runat="server" CssClass="form-control form-control-sm">
                </asp:DropDownList>
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
                    CssClass="btn btn-sm btn-outline-secondary btn-block" OnClick="btnLimpiar_Click" />
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
                <asp:BoundField DataField="ProductoNombre" HeaderText="Producto" />
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
                <asp:TemplateField HeaderText="Consumo de Materiales">
                    <HeaderStyle CssClass="text-center" Width="280px" />
                    <ItemTemplate>
                        <asp:Repeater ID="rptDetalleConsumos" runat="server">
                            <HeaderTemplate>
                                <table class="table table-sm mb-0" style="font-size:0.76rem;">
                                    <thead>
                                        <tr class="bg-light">
                                            <th>Material</th>
                                            <th class="text-right">Rango</th>
                                            <th class="text-right">Real</th>
                                            <th class="text-right">Excedente</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                            </HeaderTemplate>
                            <ItemTemplate>
                                <tr>
                                    <td style="white-space:nowrap"><%# Eval("MaterialCodigo") %> - <%# Eval("MaterialNombre") %></td>
                                    <td class="text-right text-muted" style="white-space:nowrap">
                                        <%# string.Format("{0:N0}–{1:N0}", Eval("TeoMin"), Eval("TeoMax")) %>
                                        <small><%# Eval("Unidad") %></small>
                                    </td>
                                    <td class="text-right <%# (bool)Eval("EsMerma") ? "text-danger font-weight-bold" : "" %>">
                                        <%# string.Format("{0:N2}", Eval("Real")) %>
                                    </td>
                                    <td class="text-right">
                                        <%# (decimal)Eval("Excedente") > 0
                                            ? string.Format("<span class='text-danger font-weight-bold'>+{0:N2}</span>", Eval("Excedente"))
                                            : "<span class='text-success'>—</span>" %>
                                    </td>
                                </tr>
                            </ItemTemplate>
                            <FooterTemplate>
                                    </tbody>
                                </table>
                            </FooterTemplate>
                        </asp:Repeater>
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
<asp:HiddenField ID="hdnConfirmarSinConsumos" runat="server" Value="" />
<asp:Button ID="btnCargarConsumos" runat="server" style="display:none" OnClick="btnCargarConsumos_Click" />

<!-- ══════════════════════════════════════════════════════════════════ -->
<!-- MODAL: REGISTRAR PRODUCCIÓN                                      -->
<!-- ══════════════════════════════════════════════════════════════════ -->
<div class="modal fade" id="modalRegistrar" tabindex="-1" data-backdrop="static">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <div class="modal-header bg-primary text-white">
                <h5 class="modal-title">Registrar Producción</h5>
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
                        <asp:DropDownList ID="ddlProducto" runat="server" CssClass="form-control"
                            onchange="onProductoChange()">
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-6">
                        <label class="font-weight-bold">Tu meta de unidades buenas</label>
                        <asp:TextBox ID="txtMetaDia" runat="server" CssClass="form-control" TextMode="Number" placeholder="0"></asp:TextBox>
                    </div>
                </div>

                <div class="row mb-3">
                    <div class="col-md-4">
                        <label class="font-weight-bold">Cantidad buena <span class="text-danger">*</span></label>
                        <asp:TextBox ID="txtCantBuena" runat="server" CssClass="form-control" TextMode="Number"
                            min="0" placeholder="0" onchange="recalcConsumosTeoricos()"></asp:TextBox>
                    </div>
                    <div class="col-md-4">
                        <label class="font-weight-bold">Cantidad rechazo</label>
                        <asp:TextBox ID="txtCantRechazo" runat="server" CssClass="form-control" TextMode="Number"
                            min="0" placeholder="0" onchange="recalcConsumosTeoricos()"></asp:TextBox>
                    </div>
                    <div class="col-md-4">
                        <label class="font-weight-bold">Total producido</label>
                        <div class="form-control bg-light" id="divTotalProd">0</div>
                    </div>
                </div>

                <!-- Consumo de materiales -->
                <h6 class="text-primary font-weight-bold mt-3 mb-2">Consumo de Materiales</h6>
                <asp:Panel ID="pnlConsumos" runat="server">
                    <div class="table-responsive">
                        <table class="table table-sm table-bordered consumo-table" id="tblConsumos">
                            <thead>
                                <tr>
                                    <th>Material</th>
                                    <th>Unidad</th>
                                    <th>Teórico Mín</th>
                                    <th>Teórico Máx</th>
                                    <th>Consumo Real</th>
                                    <th>Stock Actual</th>
                                </tr>
                            </thead>
                            <tbody>
                                <asp:Repeater ID="rptConsumos" runat="server">
                                    <ItemTemplate>
                                        <tr>
                                            <td>
                                                <%# Eval("MaterialCodigo") %> - <%# Eval("MaterialNombre") %>
                                                <input type="hidden" name="matID" value='<%# Eval("MaterialID") %>' />
                                                <input type="hidden" name="cantMin" value='<%# Eval("CantidadMin") %>' />
                                                <input type="hidden" name="cantMax" value='<%# Eval("CantidadMax") %>' />
                                            </td>
                                            <td><%# Eval("Unidad") %></td>
                                            <td class="text-right teorico-min"><%# Eval("TeoricoMin", "{0:N2}") %></td>
                                            <td class="text-right teorico-max"><%# Eval("TeoricoMax", "{0:N2}") %></td>
                                            <td>
                                                <input type="number" name="consumoReal" step="0.01" min="0"
                                                    class="form-control form-control-sm consumo-input"
                                                    value='<%# Eval("ConsumoReal", "{0:0.##}") %>'
                                                    data-stock='<%# Eval("StockActual") %>'
                                                    onchange="validarConsumoStock(this)" />
                                            </td>
                                            <td class="text-right stock-cell <%# Convert.ToDecimal(Eval("StockActual")) > 0 ? "stock-ok" : "stock-warn" %>">
                                                <%# Eval("StockActual", "{0:N2}") %>
                                            </td>
                                        </tr>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </tbody>
                        </table>
                    </div>
                    <div id="divAlertaStock" class="alert alert-warning d-none">
                        <i class="fas fa-exclamation-triangle"></i> Algunos consumos exceden el stock disponible.
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
                <asp:Button ID="btnGuardar" runat="server" Text="Guardar Producción"
                    CssClass="btn btn-primary" OnClick="btnGuardar_Click"
                    OnClientClick="return validarAntesDeGuardar();" />
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
                    <asp:DropDownList ID="ddlProductoHoja" runat="server" CssClass="form-control">
                    </asp:DropDownList>
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

    // Al cambiar producto, hacer postback para cargar consumos
    function onProductoChange() {
        var ddl = document.getElementById('<%= ddlProducto.ClientID %>');
        document.getElementById('<%= hdnProductoSeleccionado.ClientID %>').value = ddl.value;
        document.getElementById('<%= btnCargarConsumos.ClientID %>').click();
    }

    // Recalcular teóricos con la cantidad total
    function recalcConsumosTeoricos() {
        var buena = parseInt(document.getElementById('<%= txtCantBuena.ClientID %>').value) || 0;
        var rechazo = parseInt(document.getElementById('<%= txtCantRechazo.ClientID %>').value) || 0;
        var total = buena + rechazo;
        document.getElementById('divTotalProd').innerText = total;

        var rows = document.querySelectorAll('#tblConsumos tbody tr');
        rows.forEach(function (row) {
            var cantMin = parseFloat(row.querySelector('input[name="cantMin"]').value) || 0;
            var cantMax = parseFloat(row.querySelector('input[name="cantMax"]').value) || 0;
            row.querySelector('.teorico-min').innerText = (cantMin * total).toFixed(2);
            row.querySelector('.teorico-max').innerText = (cantMax * total).toFixed(2);
        });
    }

    // Confirmar si todos los consumos reales están en cero antes de guardar
    function validarAntesDeGuardar() {
        var hdnConfirmar = document.getElementById('<%= hdnConfirmarSinConsumos.ClientID %>');

        // Si el usuario ya confirmó en el diálogo anterior, permitir el postback y limpiar la bandera
        if (hdnConfirmar.value === '1') {
            hdnConfirmar.value = '';
            return true;
        }

        var inputs = document.querySelectorAll('input.consumo-input');
        if (inputs.length === 0) return true; // Sin materiales → dejar pasar

        var todosEnCero = true;
        inputs.forEach(function (inp) {
            if ((parseFloat(inp.value) || 0) > 0) todosEnCero = false;
        });

        if (todosEnCero) {
            Swal.fire({
                title: '¿Registrar sin consumos de materiales?',
                text: 'No capturaste la cantidad real de ningún material. El sistema no descontará materiales del inventario. ¿Deseas continuar de todas formas?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#e08e0b',
                cancelButtonColor: '#6c757d',
                confirmButtonText: '<i class="fas fa-check mr-1"></i> Sí, registrar así',
                cancelButtonText: 'Cancelar, voy a capturar'
            }).then(function (result) {
                if (result.isConfirmed) {
                    hdnConfirmar.value = '1';
                    document.getElementById('<%= btnGuardar.ClientID %>').click();
                }
            });
            return false; // Detener postback hasta que el usuario confirme
        }

        return true;
    }

    // Validar consumo vs stock
    function validarConsumoStock(input) {
        var stock = parseFloat(input.dataset.stock) || 0;
        var consumo = parseFloat(input.value) || 0;
        var cell = input.closest('tr').querySelector('.stock-cell');
        if (consumo > stock) {
            cell.className = 'text-right stock-cell stock-warn';
            document.getElementById('divAlertaStock').classList.remove('d-none');
        } else {
            cell.className = 'text-right stock-cell stock-ok';
            // Verificar si queda alguna alerta
            var hayAlerta = false;
            document.querySelectorAll('.consumo-input').forEach(function (inp) {
                if (parseFloat(inp.value) > parseFloat(inp.dataset.stock)) hayAlerta = true;
            });
            if (!hayAlerta) document.getElementById('divAlertaStock').classList.add('d-none');
        }
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
</script>

</asp:Content>

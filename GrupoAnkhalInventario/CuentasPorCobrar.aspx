<%@ Page Title="Cuentas por Cobrar" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="CuentasPorCobrar.aspx.cs" Inherits="GrupoAnkhalInventario.CuentasPorCobrar" EnableEventValidation="false" %>
<%@ MasterType VirtualPath="~/Site.Master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="css/gridviewPantalla.css" rel="stylesheet" />
    <style>
        /* ── Cards de resumen ── */
        .cxc-dashboard {
            display: flex;
            gap: 14px;
            margin-bottom: 18px;
            flex-wrap: wrap;
        }
        .cxc-card {
            flex: 1;
            min-width: 200px;
            border-radius: 10px;
            padding: 18px 22px;
            color: #fff;
            display: flex;
            align-items: center;
            gap: 14px;
            box-shadow: 0 3px 10px rgba(0,0,0,0.15);
            transition: transform .15s, box-shadow .15s;
        }
        .cxc-card:hover { transform: translateY(-3px); box-shadow: 0 6px 16px rgba(0,0,0,0.2); }
        .cxc-card.total-monto    { background: linear-gradient(135deg,#1c2833,#2c3e50); flex-basis: 100%; }
        .cxc-card.al-dia         { background: linear-gradient(135deg,#1a5276,#2980b9); }
        .cxc-card.vencidas       { background: linear-gradient(135deg,#922b21,#e74c3c); }
        .cxc-card.por-vencer     { background: linear-gradient(135deg,#7d6608,#d4ac0d); }
        .cxc-card .icon      { font-size: 2.2rem; opacity: .9; }
        .cxc-card .info .num { font-size: 2rem; font-weight: 700; line-height:1; }
        .cxc-card .info .lbl { font-size: .78rem; opacity: .9; text-transform: uppercase; letter-spacing:.5px; }

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

        /* ── Badges de estado ── */
        .badge-primary { background:#2980b9; color:#fff; }
        .badge-warning { background:#d4ac0d; color:#fff; }
        .badge-danger  { background:#e74c3c; color:#fff; }
        .badge-success { background:#27ae60; color:#fff; }

        /* ── Modal detalle ── */
        #tblDetalleEntrega thead th { background:#003366; color:#fff; font-size:.82rem; }
        #tblDetalleEntrega tbody td { font-size:.88rem; }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
<div class="container-fluid">
<div class="row">
<div class="col-12">

    <!-- ══ CARDS DE RESUMEN ════════════════════════════════════════ -->
    <div class="cxc-dashboard">
        <div class="cxc-card al-dia">
            <div class="icon"><i class="fas fa-check-circle"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblNotasPendientes" runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Cuentas Pendientes en Tiempo</div>
            </div>
        </div>
        <div class="cxc-card vencidas">
            <div class="icon"><i class="fas fa-exclamation-circle"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblNotasVencidas" runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Cuentas Vencidas</div>
            </div>
        </div>
        <div class="cxc-card por-vencer">
            <div class="icon"><i class="fas fa-clock"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblPorVencer" runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Cuentas Pendientes por Vencer</div>
            </div>
        </div>
        <div class="cxc-card total-monto">
            <div class="icon"><i class="fas fa-dollar-sign"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblTotalPendiente" runat="server" Text="$0.00"></asp:Label></div>
                <div class="lbl">Total Pendiente</div>
            </div>
        </div>
    </div>

    <!-- ══ TARJETA PRINCIPAL ═══════════════════════════════════════ -->
    <div class="card card-outline card-primary">
        <div class="card-header">
            <h3 class="card-title"><i class="fas fa-hand-holding-usd mr-2"></i>Cuentas por Cobrar</h3>
        </div>
        <div class="card-body">

            <!-- ── Filtros ── -->
            <div class="filtros-bar">
                <div class="row align-items-end">
                    <div class="col-md-2">
                        <label>Buscar por Folio o Factura</label>
                        <asp:TextBox ID="txtFiltroBusqueda" runat="server" CssClass="form-control form-control-sm"
                            Placeholder="Folio o No. Factura..."></asp:TextBox>
                    </div>
                    <div class="col-md-2">
                        <label>Cliente</label>
                        <asp:DropDownList ID="ddlFiltroCliente" runat="server" CssClass="form-control form-control-sm">
                            <asp:ListItem Value="">-- Todos --</asp:ListItem>
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-2">
                        <label>Estado</label>
                        <asp:DropDownList ID="ddlFiltroEstado" runat="server" CssClass="form-control form-control-sm">
                            <asp:ListItem Value="SINCOBRAR">Sin Cobrar (todas)</asp:ListItem>
                            <asp:ListItem Value="VENCIDA">Vencidas</asp:ListItem>
                            <asp:ListItem Value="PORVENCER">Por Vencer (7 días)</asp:ListItem>
                            <asp:ListItem Value="PENDIENTE">Pendientes (al día)</asp:ListItem>
                            <asp:ListItem Value="PARCIAL">Parciales (con cobros)</asp:ListItem>
                            <asp:ListItem Value="PAGADA">Pagadas</asp:ListItem>
                            <asp:ListItem Value="">Todas</asp:ListItem>
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-2">
                        <label>F. Entrega desde</label>
                        <asp:TextBox ID="txtFiltroFechaDesde" runat="server" CssClass="form-control form-control-sm" TextMode="Date"></asp:TextBox>
                    </div>
                    <div class="col-md-2">
                        <label>F. Entrega hasta</label>
                        <asp:TextBox ID="txtFiltroFechaHasta" runat="server" CssClass="form-control form-control-sm" TextMode="Date"></asp:TextBox>
                    </div>
                    <div class="col-md-2 mt-2 mt-md-0">
                        <asp:Button ID="btnFiltrar" runat="server" Text="Buscar"
                            CssClass="btn btn-primary btn-sm mr-1" OnClick="btnFiltrar_Click" />
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
                <asp:GridView ID="gvCxC" runat="server" AutoGenerateColumns="False"
                    CssClass="table table-bordered table-striped custom-grid"
                    AllowPaging="True" AllowCustomPaging="True" PageSize="15"
                    OnPageIndexChanging="gvCxC_PageIndexChanging"
                    DataKeyNames="CuentaPorCobrarID"
                    PagerStyle-CssClass="pager-custom"
                    PagerSettings-Mode="NumericFirstLast"
                    PagerSettings-FirstPageText="«"
                    PagerSettings-LastPageText="»"
                    PagerSettings-PageButtonCount="5">
                    <Columns>
                        <asp:BoundField DataField="CuentaPorCobrarID" HeaderText="ID" Visible="false" />

                        <asp:TemplateField HeaderText="Folio Entrega">
                            <ItemTemplate>
                                <span class="badge" style="background:#555;color:#fff;font-size:.78rem;">
                                    <%# System.Web.HttpUtility.HtmlEncode(Eval("FolioEntrega") ?? "") %>
                                </span>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="No. Factura">
                            <ItemTemplate>
                                <%# !string.IsNullOrEmpty(Eval("NumeroFactura") as string)
                                    ? System.Web.HttpUtility.HtmlEncode(Eval("NumeroFactura").ToString())
                                    : "<span class='text-muted'>—</span>" %>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:BoundField DataField="ClienteNombre" HeaderText="Cliente" />

                        <asp:BoundField DataField="FechaEntrega" HeaderText="F. Entrega" DataFormatString="{0:dd/MM/yyyy}" />

                        <asp:TemplateField HeaderText="F. Vencimiento">
                            <ItemTemplate>
                                <span class='badge <%# Eval("BadgeClass") %>' style="font-size:.82rem;">
                                    <%# ((DateTime)Eval("FechaVencimiento")).ToString("dd/MM/yyyy") %>
                                </span>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Días Crédito">
                            <ItemTemplate>
                                <span class="text-muted">
                                    <%# Eval("DiasCreditoAplicados") %> día(s)
                                </span>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:BoundField DataField="MontoTotal" HeaderText="Monto Total" DataFormatString="{0:C2}" />

                        <asp:TemplateField HeaderText="Saldo Pendiente">
                            <ItemTemplate>
                                <span class="font-weight-bold">
                                    <%# ((decimal)Eval("SaldoPendiente")).ToString("C2") %>
                                </span>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Estado">
                            <ItemTemplate>
                                <span class='badge <%# Eval("BadgeClass") %>'>
                                    <%# Eval("EstadoVisual") %>
                                </span>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Acciones">
                            <ItemTemplate>
                                <button type="button" class="btn btn-sm btn-info mr-1"
                                    title="Ver Detalle"
                                    onclick="abrirDetalle(<%# Eval("CuentaPorCobrarID") %>, <%# Eval("EntregaID") %>, '<%# System.Web.HttpUtility.JavaScriptStringEncode(Eval("FolioEntrega")?.ToString() ?? "") %>', '<%# System.Web.HttpUtility.JavaScriptStringEncode(Eval("ClienteNombre")?.ToString() ?? "") %>', '<%# System.Web.HttpUtility.JavaScriptStringEncode(Eval("NumeroFactura")?.ToString() ?? "") %>', '<%# ((decimal)Eval("MontoTotal")).ToString("C2") %>')">
                                    <i class="fas fa-eye"></i>
                                </button>
                                <%# GetBotonesAccion(Eval("CuentaPorCobrarID"), Eval("PuedeAbonar"), Eval("ClienteNombre"), Eval("NumeroFactura"), Eval("SaldoPendiente")) %>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>

        </div><!-- /card-body -->
    </div><!-- /card -->
</div>
</div>
</div>

<!-- ── HIDDEN FIELDS ───────────────────────────────────────── -->
<asp:HiddenField ID="hdnMensajePendiente" runat="server" Value="" />

<!-- ══ MODAL VER DETALLE ══════════════════════════════════════ -->
<div class="modal fade" id="modalDetalle" tabindex="-1" role="dialog">
  <div class="modal-dialog modal-lg" role="document">
    <div class="modal-content">
      <div class="modal-header" style="background-color:#003366;color:white;">
        <h5 class="modal-title"><i class="fas fa-eye mr-1"></i> Detalle de la Entrega</h5>
        <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
      </div>
      <div class="modal-body">
        <div class="row mb-3">
          <div class="col-md-4">
            <label class="font-weight-bold text-muted" style="font-size:.8rem;">FOLIO ENTREGA</label>
            <div id="detFolioEntrega" class="font-weight-bold"></div>
          </div>
          <div class="col-md-4">
            <label class="font-weight-bold text-muted" style="font-size:.8rem;">CLIENTE</label>
            <div id="detCliente"></div>
          </div>
          <div class="col-md-4">
            <label class="font-weight-bold text-muted" style="font-size:.8rem;">NO. FACTURA</label>
            <div id="detNumeroFactura"></div>
          </div>
        </div>
        <div class="row mb-3">
          <div class="col-md-4">
            <label class="font-weight-bold text-muted" style="font-size:.8rem;">MONTO TOTAL</label>
            <div id="detMonto" class="font-weight-bold text-primary"></div>
          </div>
        </div>
        <hr />
        <h6 class="font-weight-bold" style="color:#003366;"><i class="fas fa-boxes mr-1"></i> Ítems de la Entrega</h6>
        <div id="divCargandoDetalle" class="text-center py-3" style="display:none;">
            <i class="fas fa-spinner fa-spin"></i> Cargando...
        </div>
        <div class="table-responsive">
          <table class="table table-sm table-bordered" id="tblDetalleEntrega">
            <thead>
              <tr>
                <th>Ítem</th>
                <th>Cantidad</th>
                <th>Precio Unit.</th>
                <th>Subtotal</th>
              </tr>
            </thead>
            <tbody id="tbodyDetalle"></tbody>
          </table>
        </div>
      </div>
      <div class="modal-footer">
        <button type="button" class="btn btn-secondary" data-dismiss="modal">Cerrar</button>
      </div>
    </div>
  </div>
</div>

<!-- ══ MODAL REGISTRAR COBRO ══════════════════════════════════ -->
<div class="modal fade" id="modalPago" tabindex="-1" role="dialog" data-backdrop="static">
  <div class="modal-dialog" role="document">
    <div class="modal-content">
      <div class="modal-header" style="background-color:#1e8449;color:white;">
        <h5 class="modal-title"><i class="fas fa-money-bill-wave mr-1"></i> Registrar Cobro</h5>
        <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
      </div>
      <div class="modal-body">
        <input type="hidden" id="hdnCxCIDPago" value="" />
        <div class="row mb-2">
          <div class="col-md-6">
            <label class="font-weight-bold text-muted" style="font-size:.8rem;">CLIENTE</label>
            <div id="pagoCliente" class="font-weight-bold"></div>
          </div>
          <div class="col-md-6">
            <label class="font-weight-bold text-muted" style="font-size:.8rem;">NO. FACTURA</label>
            <div id="pagoFactura"></div>
          </div>
        </div>
        <div class="row mb-3">
          <div class="col-md-6">
            <label class="font-weight-bold text-muted" style="font-size:.8rem;">SALDO PENDIENTE</label>
            <div id="pagoSaldo" class="font-weight-bold text-success" style="font-size:1.3rem;"></div>
          </div>
        </div>
        <hr />
        <div class="form-group">
          <label class="font-weight-bold">Monto a Cobrar <span class="text-danger">*</span></label>
          <input type="number" id="txtMontoAbono" class="form-control" step="0.01" min="0.01" />
          <small class="form-text text-muted">Por defecto se prellena con el saldo pendiente; redúzcalo para un cobro parcial.</small>
        </div>
        <div class="form-group">
          <label class="font-weight-bold">Referencia de Cobro <span class="text-muted font-weight-normal">(opcional)</span></label>
          <input type="text" id="txtReferencia" class="form-control" maxlength="100"
              placeholder="Ej: No. de transferencia, folio de depósito..." />
          <small class="form-text text-muted">Número de transferencia, depósito o cualquier referencia bancaria.</small>
        </div>
        <div class="form-group">
          <label class="font-weight-bold">Observaciones <span class="text-muted font-weight-normal">(opcional)</span></label>
          <textarea id="txtObservacionesAbono" class="form-control" maxlength="500" rows="2"></textarea>
        </div>
        <div class="alert alert-warning" style="font-size:.88rem;">
          <i class="fas fa-info-circle mr-1"></i>
          El cobro se suma al historial de la cuenta. Si el monto cubre el saldo, la cuenta quedará marcada como PAGADA.
          Un cobro ya registrado no se puede editar; solo se puede cancelar (queda un registro de auditoría).
        </div>
      </div>
      <div class="modal-footer">
        <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancelar</button>
        <button type="button" class="btn btn-success" id="btnConfirmarPago" onclick="confirmarAbono()">
          <i class="fas fa-check mr-1"></i> Confirmar Cobro
        </button>
      </div>
    </div>
  </div>
</div>

<!-- ══ MODAL VER COBROS ═══════════════════════════════════════ -->
<div class="modal fade" id="modalAbonos" tabindex="-1" role="dialog">
  <div class="modal-dialog modal-lg" role="document">
    <div class="modal-content">
      <div class="modal-header" style="background-color:#003366;color:white;">
        <h5 class="modal-title"><i class="fas fa-list mr-1"></i> Historial de Cobros</h5>
        <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
      </div>
      <div class="modal-body">
        <input type="hidden" id="hdnCxCIDHistorial" value="" />
        <div class="row mb-3">
          <div class="col-md-6">
            <label class="font-weight-bold text-muted" style="font-size:.8rem;">CLIENTE</label>
            <div id="histCliente" class="font-weight-bold"></div>
          </div>
          <div class="col-md-6">
            <label class="font-weight-bold text-muted" style="font-size:.8rem;">NO. FACTURA</label>
            <div id="histFactura"></div>
          </div>
        </div>
        <div id="divCargandoAbonos" class="text-center py-3" style="display:none;">
            <i class="fas fa-spinner fa-spin"></i> Cargando...
        </div>
        <div class="table-responsive">
          <table class="table table-sm table-bordered" id="tblAbonos">
            <thead>
              <tr>
                <th>Fecha</th>
                <th>Monto</th>
                <th>Referencia</th>
                <th>Observaciones</th>
                <th>Registrado por</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody id="tbodyAbonos"></tbody>
          </table>
        </div>
      </div>
      <div class="modal-footer">
        <button type="button" class="btn btn-secondary" data-dismiss="modal">Cerrar</button>
      </div>
    </div>
  </div>
</div>

<!-- ══ SCRIPTS ════════════════════════════════════════════════ -->
<script type="text/javascript">

    // ── SweetAlert pendiente (desde SetMsg en code-behind) ─────────
    window.addEventListener('load', function () {
        var hdn = document.getElementById('<%= hdnMensajePendiente.ClientID %>');
        if (!hdn || !hdn.value) return;
        try {
            var msg = JSON.parse(hdn.value);
            Swal.fire({ icon: msg.icon, title: msg.title, text: msg.text, confirmButtonColor: '#003366' });
            hdn.value = '';
        } catch (e) {}
    });

    // ── Abrir modal Ver Detalle ──────────────────────────────────────
    function abrirDetalle(cxcID, entregaID, folio, cliente, factura, monto) {
        document.getElementById('detFolioEntrega').textContent = folio || '—';
        document.getElementById('detCliente').textContent = cliente || '—';
        document.getElementById('detNumeroFactura').textContent = factura || '—';
        document.getElementById('detMonto').textContent = monto;
        document.getElementById('tbodyDetalle').innerHTML = '';
        document.getElementById('divCargandoDetalle').style.display = 'block';
        $('#modalDetalle').modal('show');

        fetch('<%= ResolveUrl("~/CuentasPorCobrar.aspx/ObtenerDetalleEntrega") %>', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ entregaID: entregaID })
        })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                document.getElementById('divCargandoDetalle').style.display = 'none';
                var items = data.d;
                var tbody = document.getElementById('tbodyDetalle');
                if (!items || items.length === 0) {
                    tbody.innerHTML = '<tr><td colspan="4" class="text-center text-muted">Sin ítems registrados.</td></tr>';
                    return;
                }
                var html = '';
                items.forEach(function (it) {
                    html += '<tr>'
                        + '<td>' + escHtml(it.nombre) + '</td>'
                        + '<td>' + parseFloat(it.cantidad).toLocaleString('es-MX', { minimumFractionDigits: 2 }) + '</td>'
                        + '<td>' + formatCurrency(it.precio) + '</td>'
                        + '<td>' + formatCurrency(it.subtotal) + '</td>'
                        + '</tr>';
                });
                tbody.innerHTML = html;
            })
            .catch(function () {
                document.getElementById('divCargandoDetalle').style.display = 'none';
                document.getElementById('tbodyDetalle').innerHTML =
                    '<tr><td colspan="4" class="text-center text-danger">Error al cargar detalle.</td></tr>';
            });
    }

    // ── Abrir modal Registrar Cobro ──────────────────────────────────
    function abrirAbono(cxcID, cliente, factura, saldo) {
        document.getElementById('hdnCxCIDPago').value = cxcID;
        document.getElementById('pagoCliente').textContent = cliente || '—';
        document.getElementById('pagoFactura').textContent = factura || '—';
        document.getElementById('pagoSaldo').textContent = saldo;
        var saldoNum = parseFloat(String(saldo).replace(/[^0-9.-]+/g, ''));
        document.getElementById('txtMontoAbono').value = saldoNum.toFixed(2);
        document.getElementById('txtMontoAbono').max = saldoNum.toFixed(2);
        document.getElementById('txtReferencia').value = '';
        document.getElementById('txtObservacionesAbono').value = '';
        $('#modalPago').modal('show');
    }

    // ── Confirmar cobro ───────────────────────────────────────────────
    function confirmarAbono() {
        var cxcID = parseInt(document.getElementById('hdnCxCIDPago').value);
        var monto = parseFloat(document.getElementById('txtMontoAbono').value);
        var ref = document.getElementById('txtReferencia').value.trim();
        var obs = document.getElementById('txtObservacionesAbono').value.trim();
        if (!cxcID) return;

        if (!(monto > 0)) {
            Swal.fire({ icon: 'warning', title: 'Monto inválido', text: 'El monto debe ser mayor a cero.', confirmButtonColor: '#003366' });
            return;
        }
        var maxSaldo = parseFloat(document.getElementById('txtMontoAbono').max);
        if (maxSaldo && monto > maxSaldo + 0.001) {
            Swal.fire({ icon: 'warning', title: 'Monto inválido', text: 'El monto no puede exceder el saldo pendiente.', confirmButtonColor: '#003366' });
            return;
        }

        document.getElementById('btnConfirmarPago').disabled = true;

        fetch('<%= ResolveUrl("~/CuentasPorCobrar.aspx/RegistrarAbono") %>', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ cuentaPorCobrarID: cxcID, monto: monto, referencia: ref, observaciones: obs })
        })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                document.getElementById('btnConfirmarPago').disabled = false;
                var res = data.d;
                if (res && res.ok) {
                    $('#modalPago').modal('hide');
                    Swal.fire({
                        icon: 'success', title: 'Cobro registrado',
                        text: 'El cobro se aplicó correctamente.',
                        confirmButtonColor: '#003366'
                    }).then(function () { location.reload(); });
                } else {
                    Swal.fire({
                        icon: 'warning', title: 'No se pudo registrar',
                        text: (res && res.msg) ? res.msg : 'Ocurrió un error inesperado.',
                        confirmButtonColor: '#003366'
                    }).then(function () { $('#modalPago').modal('show'); });
                }
            })
            .catch(function () {
                document.getElementById('btnConfirmarPago').disabled = false;
                Swal.fire({
                    icon: 'error', title: 'Error',
                    text: 'No se pudo comunicar con el servidor.',
                    confirmButtonColor: '#003366'
                }).then(function () { $('#modalPago').modal('show'); });
            });
    }

    // ── Abrir modal Ver Cobros ────────────────────────────────────────
    function abrirHistorialAbonos(cxcID, cliente, factura) {
        document.getElementById('hdnCxCIDHistorial').value = cxcID;
        document.getElementById('histCliente').textContent = cliente || '—';
        document.getElementById('histFactura').textContent = factura || '—';
        document.getElementById('tbodyAbonos').innerHTML = '';
        document.getElementById('divCargandoAbonos').style.display = 'block';
        $('#modalAbonos').modal('show');
        cargarAbonos(cxcID);
    }

    function cargarAbonos(cxcID) {
        fetch('<%= ResolveUrl("~/CuentasPorCobrar.aspx/ObtenerAbonos") %>', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ cuentaPorCobrarID: cxcID })
        })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                document.getElementById('divCargandoAbonos').style.display = 'none';
                var abonos = data.d;
                var tbody = document.getElementById('tbodyAbonos');
                if (!abonos || abonos.length === 0) {
                    tbody.innerHTML = '<tr><td colspan="7" class="text-center text-muted">Sin cobros registrados.</td></tr>';
                    return;
                }
                var html = '';
                abonos.forEach(function (a) {
                    var badge = a.estado === 'ACTIVO'
                        ? '<span class="badge badge-success">ACTIVO</span>'
                        : '<span class="badge badge-secondary">CANCELADO</span>';
                    var infoCancel = a.estado === 'CANCELADO'
                        ? '<br><small class="text-muted">Por: ' + escHtml(a.canceladoPor || '—') +
                          ' el ' + escHtml(a.fechaCancelacion || '—') +
                          '<br>Motivo: ' + escHtml(a.motivoCancelacion || '—') + '</small>'
                        : '';
                    var btnCancelar = a.puedeCancelar
                        ? '<button type="button" class="btn btn-sm btn-outline-danger" title="Cancelar" onclick="cancelarAbono(' + a.abonoID + ', ' + cxcID + ')"><i class="fas fa-times"></i></button>'
                        : '';
                    html += '<tr>'
                        + '<td>' + escHtml(a.fecha) + '</td>'
                        + '<td>' + formatCurrency(a.monto) + '</td>'
                        + '<td>' + escHtml(a.referencia || '—') + '</td>'
                        + '<td>' + escHtml(a.observaciones || '—') + '</td>'
                        + '<td>' + escHtml(a.registradoPor || '—') + '</td>'
                        + '<td>' + badge + infoCancel + '</td>'
                        + '<td>' + btnCancelar + '</td>'
                        + '</tr>';
                });
                tbody.innerHTML = html;
            })
            .catch(function () {
                document.getElementById('divCargandoAbonos').style.display = 'none';
                document.getElementById('tbodyAbonos').innerHTML =
                    '<tr><td colspan="7" class="text-center text-danger">Error al cargar cobros.</td></tr>';
            });
    }

    // ── Cancelar cobro (crea registro de auditoría, no borra el cobro) ──
    function cancelarAbono(abonoID, cxcID) {
        Swal.fire({
            title: 'Cancelar cobro',
            input: 'textarea',
            inputLabel: 'Motivo de la cancelación',
            inputPlaceholder: 'Escriba el motivo...',
            showCancelButton: true,
            confirmButtonText: 'Cancelar cobro',
            confirmButtonColor: '#e74c3c',
            target: document.getElementById('modalAbonos'),
            inputValidator: function (value) {
                if (!value || !value.trim()) return 'Debe indicar un motivo.';
            }
        }).then(function (result) {
            if (!result.isConfirmed) return;
            fetch('<%= ResolveUrl("~/CuentasPorCobrar.aspx/CancelarAbono") %>', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ abonoID: abonoID, motivo: result.value.trim() })
            })
                .then(function (r) { return r.json(); })
                .then(function (data) {
                    var res = data.d;
                    if (res && res.ok) {
                        Swal.fire({
                            icon: 'success', title: 'Cobro cancelado',
                            confirmButtonColor: '#003366'
                        }).then(function () { location.reload(); });
                    } else {
                        Swal.fire({
                            icon: 'warning', title: 'No se pudo cancelar',
                            text: (res && res.msg) || 'Error inesperado.',
                            confirmButtonColor: '#003366'
                        });
                    }
                })
                .catch(function () {
                    Swal.fire({ icon: 'error', title: 'Error', text: 'No se pudo comunicar con el servidor.' });
                });
        });
    }

    // ── Helpers ────────────────────────────────────────────────────
    function escHtml(s) {
        return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;')
            .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    function formatCurrency(n) {
        return '$' + parseFloat(n).toLocaleString('es-MX', {
            minimumFractionDigits: 2, maximumFractionDigits: 2
        });
    }

</script>

</asp:Content>

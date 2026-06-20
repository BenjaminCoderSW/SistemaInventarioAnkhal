<%@ Page Title="Cuentas por Pagar" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="CuentasPorPagar.aspx.cs" Inherits="GrupoAnkhalInventario.CuentasPorPagar" EnableEventValidation="false" %>
<%@ MasterType VirtualPath="~/Site.Master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="css/gridviewPantalla.css" rel="stylesheet" />
    <style>
        /* ── Cards de resumen ── */
        .cxp-dashboard {
            display: flex;
            gap: 14px;
            margin-bottom: 18px;
            flex-wrap: wrap;
        }
        .cxp-card {
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
        .cxp-card:hover { transform: translateY(-3px); box-shadow: 0 6px 16px rgba(0,0,0,0.2); }
        .cxp-card.total-monto    { background: linear-gradient(135deg,#1c2833,#2c3e50); flex-basis: 100%; }
        .cxp-card.al-dia         { background: linear-gradient(135deg,#1a5276,#2980b9); }
        .cxp-card.vencidas       { background: linear-gradient(135deg,#922b21,#e74c3c); }
        .cxp-card.por-vencer     { background: linear-gradient(135deg,#7d6608,#d4ac0d); }
        .cxp-card .icon      { font-size: 2.2rem; opacity: .9; }
        .cxp-card .info .num { font-size: 2rem; font-weight: 700; line-height:1; }
        .cxp-card .info .lbl { font-size: .78rem; opacity: .9; text-transform: uppercase; letter-spacing:.5px; }

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
        #tblDetalleLote thead th { background:#003366; color:#fff; font-size:.82rem; }
        #tblDetalleLote tbody td { font-size:.88rem; }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
<div class="container-fluid">
<div class="row">
<div class="col-12">

    <!-- ══ CARDS DE RESUMEN ════════════════════════════════════════ -->
    <div class="cxp-dashboard">
        <div class="cxp-card al-dia">
            <div class="icon"><i class="fas fa-check-circle"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblNotasPendientes" runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Notas Pendientes en Tiempo</div>
            </div>
        </div>
        <div class="cxp-card vencidas">
            <div class="icon"><i class="fas fa-exclamation-circle"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblNotasVencidas" runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Notas Vencidas</div>
            </div>
        </div>
        <div class="cxp-card por-vencer">
            <div class="icon"><i class="fas fa-clock"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblPorVencer" runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Notas Pendientes por Vencer</div>
            </div>
        </div>
        <div class="cxp-card total-monto">
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
            <h3 class="card-title"><i class="fas fa-file-invoice-dollar mr-2"></i>Cuentas por Pagar</h3>
        </div>
        <div class="card-body">

            <!-- ── Filtros ── -->
            <div class="filtros-bar">
                <div class="row align-items-end">
                    <div class="col-md-2">
                        <label>Proveedor</label>
                        <asp:DropDownList ID="ddlFiltroProveedor" runat="server" CssClass="form-control form-control-sm">
                            <asp:ListItem Value="">-- Todos --</asp:ListItem>
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-2">
                        <label>Estado</label>
                        <asp:DropDownList ID="ddlFiltroEstado" runat="server" CssClass="form-control form-control-sm">
                            <asp:ListItem Value="SINPAGAR">Sin Pagar (todas)</asp:ListItem>
                            <asp:ListItem Value="VENCIDA">Vencidas</asp:ListItem>
                            <asp:ListItem Value="PORVENCER">Por Vencer (7 días)</asp:ListItem>
                            <asp:ListItem Value="PENDIENTE">Pendientes (al día)</asp:ListItem>
                            <asp:ListItem Value="PAGADA">Pagadas</asp:ListItem>
                            <asp:ListItem Value="">Todas</asp:ListItem>
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-2">
                        <label>F. Recepción desde</label>
                        <asp:TextBox ID="txtFiltroFechaDesde" runat="server" CssClass="form-control form-control-sm" TextMode="Date"></asp:TextBox>
                    </div>
                    <div class="col-md-2">
                        <label>F. Recepción hasta</label>
                        <asp:TextBox ID="txtFiltroFechaHasta" runat="server" CssClass="form-control form-control-sm" TextMode="Date"></asp:TextBox>
                    </div>
                    <div class="col-md-4 mt-2 mt-md-0">
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
                <asp:GridView ID="gvCxP" runat="server" AutoGenerateColumns="False"
                    CssClass="table table-bordered table-striped custom-grid"
                    AllowPaging="True" AllowCustomPaging="True" PageSize="15"
                    OnPageIndexChanging="gvCxP_PageIndexChanging"
                    DataKeyNames="CuentaPorPagarID"
                    PagerStyle-CssClass="pager-custom"
                    PagerSettings-Mode="NumericFirstLast"
                    PagerSettings-FirstPageText="«"
                    PagerSettings-LastPageText="»"
                    PagerSettings-PageButtonCount="5">
                    <Columns>
                        <asp:BoundField DataField="CuentaPorPagarID" HeaderText="ID" Visible="false" />

                        <asp:TemplateField HeaderText="Folio Lote">
                            <ItemTemplate>
                                <span class="badge" style="background:#555;color:#fff;font-size:.78rem;">
                                    <%# System.Web.HttpUtility.HtmlEncode(Eval("FolioLote") ?? "") %>
                                </span>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="No. Nota">
                            <ItemTemplate>
                                <%# !string.IsNullOrEmpty(Eval("NumeroNota") as string)
                                    ? System.Web.HttpUtility.HtmlEncode(Eval("NumeroNota").ToString())
                                    : "<span class='text-muted'>—</span>" %>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:BoundField DataField="ProveedorNombre" HeaderText="Proveedor" />

                        <asp:BoundField DataField="FechaRecepcion" HeaderText="F. Recepción" DataFormatString="{0:dd/MM/yyyy}" />

                        <asp:TemplateField HeaderText="F. Vencimiento">
                            <ItemTemplate>
                                <span class='badge <%# Eval("BadgeClass") %>' style="font-size:.82rem;">
                                    <%# ((DateTime)Eval("FechaVencimiento")).ToString("dd/MM/yyyy") %>
                                </span>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:BoundField DataField="MontoTotal" HeaderText="Monto Total" DataFormatString="{0:C2}" />

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
                                    onclick="abrirDetalle(<%# Eval("CuentaPorPagarID") %>, <%# Eval("LoteID") %>, '<%# System.Web.HttpUtility.JavaScriptStringEncode(Eval("FolioLote")?.ToString() ?? "") %>', '<%# System.Web.HttpUtility.JavaScriptStringEncode(Eval("ProveedorNombre")?.ToString() ?? "") %>', '<%# System.Web.HttpUtility.JavaScriptStringEncode(Eval("NumeroNota")?.ToString() ?? "") %>', '<%# ((decimal)Eval("MontoTotal")).ToString("C2") %>')">
                                    <i class="fas fa-eye"></i>
                                </button>
                                <%# GetBtnPago(Eval("CuentaPorPagarID"), Eval("PuedeRegistrarPago"), Eval("ProveedorNombre"), Eval("NumeroNota"), Eval("MontoTotal")) %>
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
        <h5 class="modal-title"><i class="fas fa-eye mr-1"></i> Detalle de Nota</h5>
        <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
      </div>
      <div class="modal-body">
        <div class="row mb-3">
          <div class="col-md-4">
            <label class="font-weight-bold text-muted" style="font-size:.8rem;">FOLIO LOTE</label>
            <div id="detFolioLote" class="font-weight-bold"></div>
          </div>
          <div class="col-md-4">
            <label class="font-weight-bold text-muted" style="font-size:.8rem;">PROVEEDOR</label>
            <div id="detProveedor"></div>
          </div>
          <div class="col-md-4">
            <label class="font-weight-bold text-muted" style="font-size:.8rem;">NO. NOTA</label>
            <div id="detNumeroNota"></div>
          </div>
        </div>
        <div class="row mb-3">
          <div class="col-md-4">
            <label class="font-weight-bold text-muted" style="font-size:.8rem;">MONTO TOTAL</label>
            <div id="detMonto" class="font-weight-bold text-primary"></div>
          </div>
        </div>
        <hr />
        <h6 class="font-weight-bold" style="color:#003366;"><i class="fas fa-boxes mr-1"></i> Ítems del Lote</h6>
        <div id="divCargandoDetalle" class="text-center py-3" style="display:none;">
            <i class="fas fa-spinner fa-spin"></i> Cargando...
        </div>
        <div class="table-responsive">
          <table class="table table-sm table-bordered" id="tblDetalleLote">
            <thead>
              <tr>
                <th>Ítem</th>
                <th>Cantidad</th>
                <th>Costo Unit.</th>
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

<!-- ══ MODAL REGISTRAR PAGO ═══════════════════════════════════ -->
<div class="modal fade" id="modalPago" tabindex="-1" role="dialog" data-backdrop="static">
  <div class="modal-dialog" role="document">
    <div class="modal-content">
      <div class="modal-header" style="background-color:#1e8449;color:white;">
        <h5 class="modal-title"><i class="fas fa-check-circle mr-1"></i> Registrar Pago</h5>
        <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
      </div>
      <div class="modal-body">
        <input type="hidden" id="hdnCxPIDPago" value="" />
        <div class="row mb-2">
          <div class="col-md-6">
            <label class="font-weight-bold text-muted" style="font-size:.8rem;">PROVEEDOR</label>
            <div id="pagoProveedor" class="font-weight-bold"></div>
          </div>
          <div class="col-md-6">
            <label class="font-weight-bold text-muted" style="font-size:.8rem;">NO. NOTA</label>
            <div id="pagoNota"></div>
          </div>
        </div>
        <div class="row mb-3">
          <div class="col-md-6">
            <label class="font-weight-bold text-muted" style="font-size:.8rem;">MONTO A PAGAR</label>
            <div id="pagoMonto" class="font-weight-bold text-success" style="font-size:1.3rem;"></div>
          </div>
        </div>
        <hr />
        <div class="form-group">
          <label class="font-weight-bold">Referencia de Pago <span class="text-muted font-weight-normal">(opcional)</span></label>
          <input type="text" id="txtReferencia" class="form-control" maxlength="100"
              placeholder="Ej: No. de transferencia, folio de cheque..." />
          <small class="form-text text-muted">Número de transferencia, cheque o cualquier referencia bancaria.</small>
        </div>
        <div class="alert alert-warning" style="font-size:.88rem;">
          <i class="fas fa-info-circle mr-1"></i>
          Esta acción registra la nota como pagada. No se puede deshacer.
        </div>
      </div>
      <div class="modal-footer">
        <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancelar</button>
        <button type="button" class="btn btn-success" id="btnConfirmarPago" onclick="confirmarPago()">
          <i class="fas fa-check mr-1"></i> Confirmar Pago
        </button>
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
    function abrirDetalle(cxpID, loteID, folio, proveedor, nota, monto) {
        document.getElementById('detFolioLote').textContent = folio || '—';
        document.getElementById('detProveedor').textContent = proveedor || '—';
        document.getElementById('detNumeroNota').textContent = nota || '—';
        document.getElementById('detMonto').textContent = monto;
        document.getElementById('tbodyDetalle').innerHTML = '';
        document.getElementById('divCargandoDetalle').style.display = 'block';
        $('#modalDetalle').modal('show');

        fetch('<%= ResolveUrl("~/CuentasPorPagar.aspx/ObtenerDetalleLote") %>', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ loteID: loteID })
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
                        + '<td>' + formatCurrency(it.costo) + '</td>'
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

    // ── Abrir modal Registrar Pago ───────────────────────────────────
    function abrirPago(cxpID, proveedor, nota, monto) {
        document.getElementById('hdnCxPIDPago').value = cxpID;
        document.getElementById('pagoProveedor').textContent = proveedor || '—';
        document.getElementById('pagoNota').textContent = nota || '—';
        document.getElementById('pagoMonto').textContent = monto;
        document.getElementById('txtReferencia').value = '';
        $('#modalPago').modal('show');
    }

    // ── Confirmar pago ──────────────────────────────────────────────
    function confirmarPago() {
        var cxpID = parseInt(document.getElementById('hdnCxPIDPago').value);
        var ref = document.getElementById('txtReferencia').value.trim();
        if (!cxpID) return;

        document.getElementById('btnConfirmarPago').disabled = true;

        fetch('<%= ResolveUrl("~/CuentasPorPagar.aspx/RegistrarPago") %>', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ cuentaPorPagarID: cxpID, referencia: ref })
        })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                document.getElementById('btnConfirmarPago').disabled = false;
                var res = data.d;
                if (res && res.ok) {
                    $('#modalPago').modal('hide');
                    Swal.fire({
                        icon: 'success', title: 'Pago registrado',
                        text: 'La nota quedó marcada como PAGADA.',
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

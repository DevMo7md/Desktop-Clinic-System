using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Clinic_Management_System.Models;
using Clinic_Management_System.Services;

namespace Clinic_Management_System.Forms
{
    public class PaymentUserControl : UserControl
    {
        // ── Service ───────────────────────────────────────────
        private readonly ClinicService _clinicService;

        // ── Toolbar ───────────────────────────────────────────
        private Button btnAdd = null!;
        private Button btnEdit = null!;
        private Button btnDelete = null!;
        private Label lblFilter = null!;
        private ComboBox cmbFilter = null!;

        // ── Stat cards ────────────────────────────────────────
        private Label lblCollected = null!;
        private Label lblRefunded = null!;
        private Label lblNet = null!;

        // ── Payments list ─────────────────────────────────────
        private GroupBox grpList = null!;
        private DataGridView dgv = null!;

        // ── Form fields (matches Appointment form style) ──────
        private GroupBox grpForm = null!;
        private TextBox txtAppId = null!;
        private TextBox txtAmount = null!;
        private Button btnSave = null!;
        private Button btnClear = null!;

        private int? _editingPaymentId = null;

        // ══════════════════════════════════════════════════════
        public PaymentUserControl(ClinicService clinicService)
        {
            _clinicService = clinicService;
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.DoubleBuffered = true;

            var toolbarPanel = BuildToolbarPanel();
            var statPanel = BuildStatPanel();
            var contentPanel = BuildContentPanel();

            // Add in reverse — Fill first, then Top panels stack down
            this.Controls.Add(contentPanel);
            this.Controls.Add(statPanel);
            this.Controls.Add(toolbarPanel);

            LoadData();
        }

        public new void Refresh() => LoadData();

        // ══════════════════════════════════════════════════════
        // TOOLBAR
        // ══════════════════════════════════════════════════════
        private Panel BuildToolbarPanel()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(245, 245, 245),
                Padding = new Padding(8, 0, 8, 0)
            };
            pnl.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(Color.FromArgb(210, 210, 210)),
                    0, pnl.Height - 1, pnl.Width, pnl.Height - 1);

            btnAdd = MakeBtn("+ Add Payment", true);
            btnEdit = MakeBtn("✏  Edit", false);
            btnDelete = MakeBtn("🗑  Delete", false);

            btnDelete.ForeColor = Color.FromArgb(192, 57, 43);
            btnDelete.FlatAppearance.BorderColor = Color.FromArgb(192, 57, 43);

            btnAdd.Size = new Size(122, 28);
            btnEdit.Size = new Size(90, 28);
            btnDelete.Size = new Size(120, 28);

            btnAdd.Location = new Point(8, 8);
            btnEdit.Location = new Point(btnAdd.Right + 6, 8);
            btnDelete.Location = new Point(btnEdit.Right + 6, 8);

            var sep = new Panel
            {
                Location = new Point(btnDelete.Right + 12, 10),
                Size = new Size(1, 22),
                BackColor = Color.FromArgb(200, 200, 200)
            };

            lblFilter = new Label
            {
                Text = "Filter:",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(85, 85, 85),
                AutoSize = true,
                Location = new Point(sep.Right + 10, 16)
            };

            cmbFilter = new ComboBox
            {
                Font = new Font("Segoe UI", 9F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(lblFilter.Right + 6, 10),
                Size = new Size(110, 26)
            };
            cmbFilter.Items.AddRange(new object[] { "All", "Paid", "Refunded" });
            cmbFilter.SelectedIndex = 0;
            cmbFilter.SelectedIndexChanged += (s, e) => FilterGrid();

            btnAdd.Click += BtnAdd_Click;
            btnEdit.Click += BtnEdit_Click;
            btnDelete.Click += BtnDelete_Click;

            pnl.Controls.AddRange(new Control[]
                { btnAdd, btnEdit, btnDelete, sep, lblFilter, cmbFilter });

            return pnl;
        }

        // ══════════════════════════════════════════════════════
        // STAT CARDS  — height 100 so number + subtitle both show
        // ══════════════════════════════════════════════════════
        private Panel BuildStatPanel()
        {
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 120,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(8, 8, 8, 6)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4F));

            tbl.Controls.Add(MakeCard("Collected Today (EGP)", "0",
                Color.FromArgb(46, 125, 50), out lblCollected), 0, 0);
            tbl.Controls.Add(MakeCard("Refunded Today (EGP)", "0",
                Color.FromArgb(192, 57, 43), out lblRefunded), 1, 0);
            tbl.Controls.Add(MakeCard("Net Revenue (EGP)", "0",
                Color.FromArgb(53, 122, 189), out lblNet), 2, 0);

            return tbl;
        }

        private static Panel MakeCard(string title, string def, Color col, out Label valLbl)
        {
            var p = new Panel
            {
                BackColor = Color.White,
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                BorderStyle = BorderStyle.FixedSingle
            };
            // Use a nested TableLayoutPanel so labels scale with card width
            var inner = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = Color.White
            };
            inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            inner.RowStyles.Add(new RowStyle(SizeType.Percent, 65F));
            inner.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));

            var v = new Label
            {
                Text = def,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = col,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomCenter
            };
            var t = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 7.5F),
                ForeColor = Color.Gray,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopCenter
            };
            inner.Controls.Add(v, 0, 0);
            inner.Controls.Add(t, 0, 1);
            p.Controls.Add(inner);
            valLbl = v;
            return p;
        }

        // ══════════════════════════════════════════════════════
        // CONTENT PANEL — list fills space, form docked bottom
        // ══════════════════════════════════════════════════════
        private Panel BuildContentPanel()
        {
            BuildList();
            BuildForm();

            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(8, 4, 8, 8)
            };

            grpForm.Dock = DockStyle.Bottom;
            grpForm.Height = 160;   // tall enough for 2 input rows + buttons

            grpList.Dock = DockStyle.Fill;

            pnl.Controls.Add(grpList);
            pnl.Controls.Add(grpForm);
            return pnl;
        }

        // ══════════════════════════════════════════════════════
        // PAYMENTS LIST
        // ══════════════════════════════════════════════════════
        private void BuildList()
        {
            grpList = new GroupBox
            {
                Text = "Payments List",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(85, 85, 85),
                BackColor = Color.White,
                Padding = new Padding(6)
            };

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9F),
                GridColor = Color.FromArgb(220, 220, 220)
            };

            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(227, 238, 250);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(51, 51, 51);
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            dgv.ColumnHeadersHeight = 32;
            dgv.EnableHeadersVisualStyles = false;

            dgv.RowsDefaultCellStyle.BackColor = Color.White;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 249, 255);
            dgv.RowTemplate.Height = 30;
            dgv.RowsDefaultCellStyle.Padding = new Padding(4, 0, 0, 0);

            dgv.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "colId", HeaderText = "ID", FillWeight = 6 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "colAppId", HeaderText = "Appointment ID", FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "colPat", HeaderText = "Patient", FillWeight = 34 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "colAmt", HeaderText = "Amount", FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "colDate", HeaderText = "Date", FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "colStat", HeaderText = "Status", FillWeight = 14 });

            dgv.CellFormatting += Dgv_CellFormatting;
            dgv.SelectionChanged += Dgv_SelectionChanged;

            grpList.Controls.Add(dgv);
        }

        // ══════════════════════════════════════════════════════
        // FORM  — Appointment-style layout:
        //   Row 1: [Appointment ID]  [Amount (EGP)]  (2 columns, same as appt top row)
        //   Row 2: [Save] [Clear]
        // ══════════════════════════════════════════════════════
        private void BuildForm()
        {
            grpForm = new GroupBox
            {
                Text = "Add / Edit Payment",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(85, 85, 85),
                BackColor = Color.White,
                Padding = new Padding(10, 4, 10, 8)
            };

            // ── Row 1 labels ──────────────────────────────────
            var lblAppId = MakeLbl("Appointment ID");
            var lblAmount = MakeLbl("Amount (EGP)");

            // ── Row 1 inputs ──────────────────────────────────
            txtAppId = MakeTxt("e.g. 205");
            txtAmount = MakeTxt("e.g. 500");

            // Two-column table: label row + input row
            var row1 = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 58,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Color.White,
                Margin = new Padding(0)
            };
            row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            row1.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            row1.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));

            row1.Controls.Add(lblAppId, 0, 0);
            row1.Controls.Add(lblAmount, 1, 0);
            row1.Controls.Add(txtAppId, 0, 1);
            row1.Controls.Add(txtAmount, 1, 1);

            // ── Button row ────────────────────────────────────
            var btnRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.White,
                Padding = new Padding(0, 8, 0, 0)
            };

            btnSave = MakeBtn("Save", true);
            btnClear = MakeBtn("Clear", false);
            btnSave.Size = new Size(90, 26);
            btnClear.Size = new Size(72, 26);
            btnSave.Location = new Point(0, 8);
            btnClear.Location = new Point(btnSave.Right + 8, 8);

            btnSave.Click += BtnSave_Click;
            btnClear.Click += (s, e) => ClearForm();

            btnRow.Controls.Add(btnSave);
            btnRow.Controls.Add(btnClear);

            // Stacked top-down inside GroupBox
            grpForm.Controls.Add(btnRow);   // added first → rendered at bottom (DockStyle.Top stacks)
            grpForm.Controls.Add(row1);     // added second → rendered above btnRow
        }

        // ══════════════════════════════════════════════════════
        // DATA LOADING
        // ══════════════════════════════════════════════════════
        private void LoadData()
        {
            UpdateStatCards();
            PopulateGrid(cmbFilter?.SelectedItem?.ToString() ?? "All");
        }

        private void UpdateStatCards()
        {
            var today = DateTime.Today;
            var payments = _clinicService.GetPayments()
                .Where(p => p.PaymentDate.Date == today).ToList();

            decimal collected = payments
                .Where(p => p.Status == PaymentStatus.Paid).Sum(p => p.Amount);
            decimal refunded = payments
                .Where(p => p.Status == PaymentStatus.Refunded).Sum(p => p.Amount);

            lblCollected.Text = collected.ToString("N0");
            lblRefunded.Text = refunded.ToString("N0");
            lblNet.Text = (collected - refunded).ToString("N0");
        }

        private void PopulateGrid(string filter)
        {
            dgv.Rows.Clear();

            var payments = _clinicService.GetPayments();

            if (filter == "Paid")
                payments = payments.Where(p => p.Status == PaymentStatus.Paid).ToList();
            else if (filter == "Refunded")
                payments = payments.Where(p => p.Status == PaymentStatus.Refunded).ToList();

            foreach (var p in payments)
            {
                dgv.Rows.Add(
                    p.ID,
                    p.PaymentAppointment.ID,
                    p.PaymentAppointment.AppointmentPatient.Name,
                    p.Amount.ToString("N0") + " EGP",
                    p.PaymentDate.ToString("dd/MM HH:mm"),
                    p.Status.ToString()
                );
                dgv.Rows[dgv.Rows.Count - 1].Tag = p.ID;
            }
        }

        private void FilterGrid()
            => PopulateGrid(cmbFilter.SelectedItem?.ToString() ?? "All");

        // ══════════════════════════════════════════════════════
        // CELL FORMATTING
        // ══════════════════════════════════════════════════════
        private void Dgv_CellFormatting(object? sender,
            DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || dgv.Columns[e.ColumnIndex].Name != "colStat") return;
            string val = e.Value?.ToString() ?? "";

            if (val == "Paid")
            {
                e.CellStyle.ForeColor = Color.FromArgb(12, 84, 96);
                e.CellStyle.BackColor = Color.FromArgb(209, 236, 241);
                e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            }
            else if (val == "Refunded")
            {
                e.CellStyle.ForeColor = Color.FromArgb(114, 28, 36);
                e.CellStyle.BackColor = Color.FromArgb(248, 215, 218);
                e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            }
        }

        private void Dgv_SelectionChanged(object? sender, EventArgs e)
        {
            bool hasRow = dgv.SelectedRows.Count > 0;
            btnEdit.Enabled = hasRow;
            btnDelete.Enabled = hasRow;
        }

        // ══════════════════════════════════════════════════════
        // BUTTON EVENTS
        // ══════════════════════════════════════════════════════
        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            _editingPaymentId = null;
            grpForm.Text = "Add / Edit Payment";
            btnSave.Text = "Save";
            ClearForm();
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0) return;
            int id = (int)dgv.SelectedRows[0].Tag!;
            var pay = _clinicService.GetPayments().FirstOrDefault(p => p.ID == id);
            if (pay == null) return;

            _editingPaymentId = id;
            grpForm.Text = "Add / Edit Payment";
            btnSave.Text = "Update";
            txtAppId.Text = pay.PaymentAppointment.ID.ToString();
            txtAppId.Enabled = false;   // appointment link cannot change
            txtAmount.Text = pay.Amount.ToString();
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0) return;
            int id = (int)dgv.SelectedRows[0].Tag!;

            var result = MessageBox.Show(
                "Are you sure you want to delete this payment?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            try
            {
                _clinicService.DeletePayment(id);
                LoadData();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!int.TryParse(txtAppId.Text.Trim(), out int appId))
                    throw new ArgumentException("Please enter a valid Appointment ID.");

                if (!decimal.TryParse(txtAmount.Text.Trim(), out decimal amount) || amount <= 0)
                    throw new ArgumentException("Please enter a valid positive amount.");

                if (_editingPaymentId.HasValue)
                    _clinicService.EditPayment(_editingPaymentId.Value, amount);
                else
                    _clinicService.AddPayment(appId, amount);

                LoadData();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ClearForm()
        {
            txtAppId.Clear();
            txtAmount.Clear();
            txtAppId.Enabled = true;
            _editingPaymentId = null;
            grpForm.Text = "Add / Edit Payment";
            btnSave.Text = "Save";
        }

        // ══════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════
        private static Button MakeBtn(string text, bool primary)
        {
            var b = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 9F,
                                primary ? FontStyle.Bold : FontStyle.Regular),
                BackColor = primary ? Color.FromArgb(53, 122, 189) : Color.White,
                ForeColor = primary ? Color.White : Color.FromArgb(51, 51, 51),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderColor = primary
                ? Color.FromArgb(53, 122, 189)
                : Color.FromArgb(170, 170, 170);
            b.FlatAppearance.BorderSize = 1;
            return b;
        }

        // Plain label — no bold, matches appointment form field labels
        private static Label MakeLbl(string text) => new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(68, 68, 68),
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft,
            Padding = new Padding(2, 0, 0, 2)
        };

        private static TextBox MakeTxt(string placeholder) => new TextBox
        {
            Font = new Font("Segoe UI", 9.5F),
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = placeholder
        };
    }
}
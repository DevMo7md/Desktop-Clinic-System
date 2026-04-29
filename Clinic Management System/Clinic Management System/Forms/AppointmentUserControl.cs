using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Clinic_Management_System.Models;
using Clinic_Management_System.Services;

namespace Clinic_Management_System.Forms
{
    public class AppointmentUserControl : UserControl
    {
        // ── Services ──────────────────────────────────────────────────────────
        private readonly ClinicService _clinicService;

        // ── Toolbar controls ──────────────────────────────────────────────────
        private Button btnAdd;
        private Button btnEdit;
        private Button btnComplete;
        private Button btnCancel;
        private ComboBox cmbFilter;
        private TextBox txtSearch;
        private Button btnFind;

        // ── DataGridView ──────────────────────────────────────────────────────
        private GroupBox grpAllAppointments;
        private DataGridView dgvAppointments;

        // ── Form (Add / Edit) ─────────────────────────────────────────────────
        private GroupBox grpForm;
        private Label lblPatientId;
        private TextBox txtPatientId;
        private Label lblDoctorId;
        private TextBox txtDoctorId;
        private Label lblFee;
        private TextBox txtFee;
        private Label lblDate;
        private DateTimePicker dtpDate;
        private Label lblTime;
        private DateTimePicker dtpTime;
        private Button btnSave;
        private Button btnClear;

        // ── State ─────────────────────────────────────────────────────────────
        private int? _editingId = null;

        // ══════════════════════════════════════════════════════════════════════
        //  CONSTRUCTORS
        // ══════════════════════════════════════════════════════════════════════
        public AppointmentUserControl(ClinicService clinicService)
        {
            _clinicService = clinicService;
            BuildUI();
            LoadGrid();
        }

        public AppointmentUserControl()
        {
            _clinicService = new ClinicService();
            BuildUI();
            LoadGrid();
        }

        // ══════════════════════════════════════════════════════════════════════
        //  BUILD UI
        // ══════════════════════════════════════════════════════════════════════
        private void BuildUI()
        {
            BackColor = Color.White;
            Padding = new Padding(10);

            BuildGrid();
            BuildForm();

            Panel pToolbar = BuildToolbarPanel();

            // Dock order: Fill first, then Bottom, then Top
            Controls.Add(grpAllAppointments);
            Controls.Add(grpForm);
            Controls.Add(pToolbar);
        }

        private Panel BuildToolbarPanel()
        {
            var p = new Panel { Dock = DockStyle.Top, Height = 46 };

            btnAdd = Btn("+ Add Appointment",Color.FromArgb(13, 110, 253), Color.White,   200);
            btnAdd.Click += (s, e) => { _editingId = null; ClearForm(); };

            btnEdit = Btn("✎  Edit",Color.White, Color.FromArgb(108, 117, 125),  80);
            btnEdit.Click += OnEdit;

            btnComplete = Btn("✔ Complete", Color.White, Color.FromArgb(25, 135, 84),  120);
            btnComplete.Click += OnComplete;

            btnCancel = Btn("✘  Cancel",Color.White, Color.FromArgb(220, 53, 69),  90);
            btnCancel.Click += OnCancelAppointment;

            var lblFilter = new Label
            {
                Text = "Filter:",
                AutoSize = true,
                Font = new Font("Segoe UI", 9f),
                TextAlign = ContentAlignment.MiddleLeft
            };

            cmbFilter = new ComboBox
            {
                Width = 110,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f)
            };
            cmbFilter.Items.AddRange(new object[] { "All", "Pending", "Completed", "Cancelled" });
            cmbFilter.SelectedIndex = 0;
            cmbFilter.SelectedIndexChanged += (s, e) => LoadGrid();

            txtSearch = new TextBox
            {
                Width = 160,
                Font = new Font("Segoe UI", 9f),
                PlaceholderText = "Search..."
            };

            btnFind = Btn("Find", Color.FromArgb(13, 110, 253), Color.White, 60);
            btnFind.Click += (s, e) => LoadGrid(txtSearch.Text.Trim());

            // layout controls left-to-right
            int x = 0;
            void Place(Control c, int gap = 6)
            {
                c.Location = new Point(x, 7);
                c.Height = 30;
                p.Controls.Add(c);
                x += c.Width + gap;
            }

            Place(btnAdd, 10);
            Place(btnEdit, 6);
            Place(btnComplete, 6);
            Place(btnCancel, 18);

            lblFilter.Location = new Point(x, 12);
            lblFilter.Height = 20;
            p.Controls.Add(lblFilter);
            x += lblFilter.PreferredWidth + 5;

            Place(cmbFilter, 8);
            Place(txtSearch, 6);
            Place(btnFind);

            return p;
        }

        private void BuildGrid()
        {
            grpAllAppointments = new GroupBox
            {
                Text = "All Appointments",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f),
                Padding = new Padding(8)
            };

            dgvAppointments = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9f),
                GridColor = Color.FromArgb(222, 226, 230),
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            };

            dgvAppointments.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
            dgvAppointments.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            dgvAppointments.EnableHeadersVisualStyles = false;
            dgvAppointments.DefaultCellStyle.SelectionBackColor = Color.FromArgb(207, 226, 255);
            dgvAppointments.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvAppointments.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);

            AddCol("colId", "ID", 6);
            AddCol("colPatient", "Patient", 22);
            AddCol("colDoctor", "Doctor", 22);
            AddCol("colDate", "Date & Time", 18);
            AddCol("colFee", "Fee", 10);
            AddCol("colStatus", "Status", 12);

            dgvAppointments.CellFormatting += OnCellFormatting;

            grpAllAppointments.Controls.Add(dgvAppointments);
        }

        private void AddCol(string name, string header, int weight)
        {
            dgvAppointments.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                FillWeight = weight
            });
        }

        private void BuildForm()
        {
            grpForm = new GroupBox
            {
                Text = "Add / Edit Appointment",
                Dock = DockStyle.Bottom,
                Height = 175,
                Font = new Font("Segoe UI", 9f),
                Padding = new Padding(12, 8, 12, 8)
            };

            const int lY = 22, r1Y = 42, lY2 = 90, r2Y = 110;

            lblPatientId = Lbl("Patient ID", 0, lY);
            txtPatientId = Txt("e.g. 101", 0, r1Y, 200);

            lblDoctorId = Lbl("Doctor ID", 216, lY);
            txtDoctorId = Txt("e.g. 01", 216, r1Y, 200);

            lblFee = Lbl("Fee (EGP)", 432, lY);
            txtFee = Txt("e.g. 500", 432, r1Y, 200);

            lblDate = Lbl("Date", 0, lY2);
            dtpDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Location = new Point(0, r2Y),
                Size = new Size(300, 28),
                Font = new Font("Segoe UI", 9f)
            };

            lblTime = Lbl("Time", 316, lY2);
            dtpTime = new DateTimePicker
            {
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Location = new Point(316, r2Y),
                Size = new Size(200, 28),
                Font = new Font("Segoe UI", 9f)
            };

            btnSave = Btn("Save", Color.FromArgb(13, 110, 253), Color.White, 80);
            btnSave.Location = new Point(0, 142);
            btnSave.Height = 30;
            btnSave.Click += OnSave;

            btnClear = Btn("Clear", Color.FromArgb(108, 117, 125), Color.White, 80);
            btnClear.Location = new Point(92, 142);
            btnClear.Height = 30;
            btnClear.Click += (s, e) => ClearForm();

            grpForm.Controls.AddRange(new Control[]
            {
                lblPatientId, txtPatientId,
                lblDoctorId,  txtDoctorId,
                lblFee,       txtFee,
                lblDate,      dtpDate,
                lblTime,      dtpTime,
                btnSave,      btnClear
            });
        }

        // ══════════════════════════════════════════════════════════════════════
        //  FACTORY HELPERS
        // ══════════════════════════════════════════════════════════════════════
        private Button Btn(string text, Color back, Color fore, int width = 130) => new Button
        {
            Text = text,
            BackColor = back,
            ForeColor = fore,
            FlatStyle = FlatStyle.Flat,
            Width = width,
            Font = new Font("Segoe UI", 9f),
            Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 1, BorderColor = fore }
        };

        private Label Lbl(string text, int x, int y) => new Label
        {
            Text = text,
            Location = new Point(x, y),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(73, 80, 87)
        };

        private TextBox Txt(string placeholder, int x, int y, int width) => new TextBox
        {
            PlaceholderText = placeholder,
            Location = new Point(x, y),
            Size = new Size(width, 28),
            Font = new Font("Segoe UI", 9f)
        };

        // ══════════════════════════════════════════════════════════════════════
        //  DATA
        // ══════════════════════════════════════════════════════════════════════
        private void LoadGrid(string query = "")
        {
            var list = _clinicService.GetAppointments();

            string filter = cmbFilter.SelectedItem?.ToString() ?? "All";
            if (filter != "All" && Enum.TryParse(filter, out AppointmentStatus st))
                list = list.Where(a => a.Status == st).ToList();

            if (!string.IsNullOrWhiteSpace(query))
                list = list.Where(a =>
                    a.ID.ToString() == query ||
                    a.AppointmentPatient.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    a.AppointmentDoctor.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                ).ToList();

            dgvAppointments.Rows.Clear();
            foreach (var a in list)
            {
                int i = dgvAppointments.Rows.Add(
                    a.ID,
                    a.AppointmentPatient.Name,
                    "Dr. " + a.AppointmentDoctor.Name,
                    a.AppointmentDate.ToString("dd/MM HH:mm"),
                    a.Fee.ToString("F0"),
                    a.Status.ToString()
                );
                dgvAppointments.Rows[i].Tag = a.ID;
            }
        }

        private void OnCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvAppointments.Columns[e.ColumnIndex].Name != "colStatus" || e.Value == null) return;
            var cell = dgvAppointments.Rows[e.RowIndex].Cells[e.ColumnIndex];
            switch (e.Value.ToString())
            {
                case "Completed":
                    cell.Style.BackColor = Color.FromArgb(209, 231, 221);
                    cell.Style.ForeColor = Color.FromArgb(21, 87, 36);
                    break;
                case "Pending":
                    cell.Style.BackColor = Color.FromArgb(255, 243, 205);
                    cell.Style.ForeColor = Color.FromArgb(133, 100, 4);
                    break;
                case "Cancelled":
                    cell.Style.BackColor = Color.FromArgb(248, 215, 218);
                    cell.Style.ForeColor = Color.FromArgb(114, 28, 36);
                    break;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  SELECTION HELPERS
        // ══════════════════════════════════════════════════════════════════════
        private int? SelectedId()
        {
            if (dgvAppointments.SelectedRows.Count == 0) return null;
            return dgvAppointments.SelectedRows[0].Tag as int?;
        }

        private Appointment SelectedAppointment()
        {
            int? id = SelectedId();
            return id == null ? null : _clinicService.GetAppointments().FirstOrDefault(a => a.ID == id);
        }

        private bool RequireSelection(out int id)
        {
            int? v = SelectedId();
            id = v ?? 0;
            if (v == null)
            {
                MessageBox.Show("Please select an appointment first.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  EVENT HANDLERS
        // ══════════════════════════════════════════════════════════════════════
        private void OnEdit(object sender, EventArgs e)
        {
            var app = SelectedAppointment();
            if (app == null)
            {
                MessageBox.Show("Please select an appointment to edit.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (app.Status == AppointmentStatus.Cancelled)
            {
                MessageBox.Show("Cannot edit a cancelled appointment.", "Invalid Operation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _editingId = app.ID;
            grpForm.Text = $"Editing Appointment #{app.ID}";
            txtPatientId.Text = app.AppointmentPatient.ID.ToString();
            txtPatientId.Enabled = false;  // patient cannot change on edit
            txtDoctorId.Text = app.AppointmentDoctor.ID.ToString();
            txtFee.Text = app.Fee.ToString("F0");
            dtpDate.Value = app.AppointmentDate.Date;
            dtpTime.Value = app.AppointmentDate;
        }

        private void OnComplete(object sender, EventArgs e)
        {
            if (!RequireSelection(out int id)) return;
            try
            {
                _clinicService.CompleteAppointment(id);
                LoadGrid();
                MessageBox.Show("Appointment marked as Completed.", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnCancelAppointment(object sender, EventArgs e)
        {
            if (!RequireSelection(out int id)) return;
            if (MessageBox.Show("Are you sure you want to cancel this appointment?",
                    "Confirm Cancel", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                _clinicService.DeleteAppointment(id);
                LoadGrid();
                MessageBox.Show("Appointment cancelled.", "Done",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnSave(object sender, EventArgs e)
        {
            if (!int.TryParse(txtPatientId.Text.Trim(), out int pId))
            {
                MessageBox.Show("Enter a valid Patient ID.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!int.TryParse(txtDoctorId.Text.Trim(), out int dId))
            {
                MessageBox.Show("Enter a valid Doctor ID.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!decimal.TryParse(txtFee.Text.Trim(), out decimal fee) || fee < 0)
            {
                MessageBox.Show("Enter a valid Fee amount.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DateTime dt = dtpDate.Value.Date.Add(dtpTime.Value.TimeOfDay);

            try
            {
                if (_editingId.HasValue)
                    _clinicService.EditAppointment(_editingId.Value, dId, dt, fee);
                else
                    _clinicService.AddAppointment(pId, dId, fee, dt);

                MessageBox.Show(_editingId.HasValue ? "Appointment updated." : "Appointment added.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearForm();
                LoadGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  FORM RESET
        // ══════════════════════════════════════════════════════════════════════
        private void ClearForm()
        {
            _editingId = null;
            grpForm.Text = "Add / Edit Appointment";
            txtPatientId.Text = string.Empty;
            txtPatientId.Enabled = true;
            txtDoctorId.Text = string.Empty;
            txtFee.Text = string.Empty;
            dtpDate.Value = DateTime.Now;
            dtpTime.Value = DateTime.Now;
        }
    }
}
using System;
using System.Drawing;
using System.Windows.Forms;
using Clinic_Management_System.Models;
using Clinic_Management_System.Services;

namespace Clinic_Management_System
{
    public class DoctorUserControl : UserControl
    {
        // ── Service ───────────────────────────────────────────────────────────────
        private readonly ClinicService _service;

        // ── Toolbar ───────────────────────────────────────────────────────────────
        private Panel pnlToolbar;
        private Button btnAdd, btnEdit, btnDelete;
        private TextBox txtSearch;
        private Button btnFind;

        // ── Doctor List (left) ────────────────────────────────────────────────────
        private GroupBox grpList;
        private DataGridView dgvDoctors;

        // ── Doctor Details (right) ────────────────────────────────────────────────
        private GroupBox grpDetails;
        private TextBox txtName, txtAge, txtSpecialty;
        private ComboBox cmbGender;
        private Button btnSave, btnCancel;

        // ── Appointments tab ──────────────────────────────────────────────────────
        private GroupBox grpAppointments;
        private DataGridView dgvAppointments;

        // ── State ─────────────────────────────────────────────────────────────────
        private Doctor _selected = null;
        private bool _isEditing = false;

        // ─────────────────────────────────────────────────────────────────────────
        public DoctorUserControl(ClinicService service)
        {
            _service = service;
            InitializeComponent();
            LoadDoctors();
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  UI INITIALISATION
        // ═════════════════════════════════════════════════════════════════════════
        private void InitializeComponent()
        {
            this.BackColor = Color.White;

            BuildToolbar();

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 230,
                Panel1MinSize = 180,
                Panel2MinSize = 150,
                BorderStyle = BorderStyle.None
            };

            BuildTopSection(split.Panel1);
            BuildAppointmentsSection(split.Panel2);

            this.Controls.Add(split);
        }

        // ── Toolbar ───────────────────────────────────────────────────────────────
        private void BuildToolbar()
        {
            pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.FromArgb(245, 245, 245),
                Padding = new Padding(4)
            };
            pnlToolbar.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(Color.FromArgb(221, 221, 221)),
                    0, pnlToolbar.Height - 1, pnlToolbar.Width, pnlToolbar.Height - 1);

            btnAdd = MakePrimaryButton("+ Add Doctor");
            btnEdit = MakeButton("✏ Edit");
            btnDelete = MakeButton("🗑 Delete");
            btnDelete.ForeColor = Color.FromArgb(192, 57, 43);

            var sep = new Panel { Width = 1, Height = 20, BackColor = Color.FromArgb(204, 204, 204), Margin = new Padding(4, 0, 4, 0) };

            var lblSearch = new Label { Text = "Search:", Width = 50, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9) };
            txtSearch = new TextBox { Width = 160, Font = new Font("Segoe UI", 9), PlaceholderText = "Name, ID, or specialty..." };
            btnFind = MakeButton("Find");

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(4, 4, 4, 0)
            };
            flow.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, sep, lblSearch, txtSearch, btnFind });
            pnlToolbar.Controls.Add(flow);
            this.Controls.Add(pnlToolbar);

            btnAdd.Click += BtnAdd_Click;
            btnEdit.Click += BtnEdit_Click;
            btnDelete.Click += BtnDelete_Click;
            btnFind.Click += BtnFind_Click;
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnFind_Click(s, e); };
        }

        // ── Top section: list + details ───────────────────────────────────────────
        private void BuildTopSection(SplitterPanel container)
        {
            var pnlTop = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10, 10, 10, 0),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            pnlTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pnlTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            // ── Doctors List ────────────────────────────────────────────────────
            grpList = new GroupBox { Text = "Doctors List", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9), Margin = new Padding(0, 0, 5, 0) };
            dgvDoctors = MakeGrid(new[] { "ID", "Name", "Age", "Specialty" });
            dgvDoctors.Dock = DockStyle.Fill;
            dgvDoctors.SelectionChanged += DgvDoctors_SelectionChanged;
            grpList.Controls.Add(dgvDoctors);
            pnlTop.Controls.Add(grpList, 0, 0);

            // ── Doctor Details ───────────────────────────────────────────────────
            grpDetails = new GroupBox { Text = "Doctor Details", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9), Margin = new Padding(5, 0, 0, 0) };

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                Padding = new Padding(6)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            // Row 0: labels
            tbl.Controls.Add(MakeLabel("Full Name"), 0, 0);
            tbl.Controls.Add(MakeLabel("Age"), 1, 0);
            // Row 1: inputs
            txtName = MakeTextBox(); txtAge = MakeTextBox();
            tbl.Controls.Add(txtName, 0, 1);
            tbl.Controls.Add(txtAge, 1, 1);
            // Row 2-3: Gender + Specialty labels
            tbl.Controls.Add(MakeLabel("Gender"), 0, 2);
            tbl.Controls.Add(MakeLabel("Specialty"), 1, 2);
            // Row 3: Gender combo + Specialty input
            cmbGender = new ComboBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbGender.Items.AddRange(new object[] { "Male", "Female" });
            cmbGender.SelectedIndex = 0;
            txtSpecialty = MakeTextBox();
            tbl.Controls.Add(cmbGender, 0, 3);
            tbl.Controls.Add(txtSpecialty, 1, 3);
            // Row 4: Save / Cancel
            var pnlBtns = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            btnSave = MakePrimaryButton("Save");
            btnCancel = MakeButton("Cancel");
            pnlBtns.Controls.Add(btnSave);
            pnlBtns.Controls.Add(btnCancel);
            tbl.SetColumnSpan(pnlBtns, 2);
            tbl.Controls.Add(pnlBtns, 0, 4);

            grpDetails.Controls.Add(tbl);
            pnlTop.Controls.Add(grpDetails, 1, 0);
            container.Controls.Add(pnlTop);

            btnSave.Click += BtnSave_Click;
            btnCancel.Click += BtnCancel_Click;

            SetDetailsEditable(false);
        }

        // ── Appointments section (bottom) ─────────────────────────────────────────
        private void BuildAppointmentsSection(SplitterPanel container)
        {
            grpAppointments = new GroupBox
            {
                Text = "Doctor Appointments",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9),
                Padding = new Padding(10, 10, 10, 0),
                Margin = new Padding(10)
            };

            dgvAppointments = MakeGrid(new[] { "App ID", "Patient", "Date & Time", "Fee", "Status" });
            dgvAppointments.Dock = DockStyle.Fill;
            grpAppointments.Controls.Add(dgvAppointments);
            container.Controls.Add(grpAppointments);
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  DATA LOADING
        // ═════════════════════════════════════════════════════════════════════════
        private void LoadDoctors(string query = "")
        {
            dgvDoctors.Rows.Clear();
            var doctors = string.IsNullOrWhiteSpace(query)
                ? _service.GetDoctors()
                : _service.SearchDoctors(query);

            foreach (var d in doctors)
                dgvDoctors.Rows.Add(d.ID, d.Name, d.Age, d.Specialty);
        }

        private void LoadDoctorAppointments()
        {
            dgvAppointments.Rows.Clear();
            if (_selected == null) return;

            grpAppointments.Text = $"Dr. {_selected.Name} — Appointments";

            foreach (var a in _service.GetDoctorAppointments(_selected.ID))
            {
                string status = a.Status.ToString();
                dgvAppointments.Rows.Add(
                    a.ID,
                    a.AppointmentPatient.Name,
                    a.AppointmentDate.ToString("dd/MM/yyyy HH:mm"),
                    a.Fee,
                    status);
            }
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  EVENTS
        // ═════════════════════════════════════════════════════════════════════════
        private void DgvDoctors_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvDoctors.SelectedRows.Count == 0) return;

            int id = Convert.ToInt32(dgvDoctors.SelectedRows[0].Cells[0].Value);
            _selected = _service.GetDoctors().Find(d => d.ID == id);

            if (_selected != null)
            {
                grpDetails.Text = $"Doctor Details — #{_selected.ID}";
                txtName.Text = _selected.Name;
                txtAge.Text = _selected.Age.ToString();
                txtSpecialty.Text = _selected.Specialty;
                cmbGender.SelectedItem = _selected.PersonGender.ToString();
                LoadDoctorAppointments();
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            _isEditing = false;
            _selected = null;
            grpDetails.Text = "New Doctor";
            txtName.Clear(); txtAge.Clear(); txtSpecialty.Clear();
            cmbGender.SelectedIndex = 0;
            SetDetailsEditable(true);
            txtName.Focus();
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (!RequireSelection()) return;
            _isEditing = true;
            SetDetailsEditable(true);
            txtName.Focus();
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (!RequireSelection()) return;
            if (ConfirmAction($"Delete Dr. '{_selected.Name}'?"))
            {
                try
                {
                    _service.DeleteDoctor(_selected.ID);
                    _selected = null;
                    dgvAppointments.Rows.Clear();
                    grpDetails.Text = "Doctor Details";
                    grpAppointments.Text = "Doctor Appointments";
                    LoadDoctors();
                }
                catch (Exception ex) { ShowError(ex.Message); }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!TryParseForm(out string name, out int age, out Gender gender, out string specialty)) return;

            try
            {
                if (_isEditing && _selected != null)
                    _service.EditDoctor(_selected.ID, name, age, gender, specialty);
                else
                    _service.AddDoctor(name, age, gender, specialty);

                SetDetailsEditable(false);
                LoadDoctors();

                // Re-select saved doctor
                _selected = _service.GetDoctors().Find(d => d.Name == name && d.Specialty == specialty);
                LoadDoctorAppointments();
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            SetDetailsEditable(false);
            if (_selected != null)
            {
                txtName.Text = _selected.Name;
                txtAge.Text = _selected.Age.ToString();
                txtSpecialty.Text = _selected.Specialty;
                cmbGender.SelectedItem = _selected.PersonGender.ToString();
            }
        }

        private void BtnFind_Click(object sender, EventArgs e) => LoadDoctors(txtSearch.Text.Trim());

        // ═════════════════════════════════════════════════════════════════════════
        //  HELPERS
        // ═════════════════════════════════════════════════════════════════════════
        private void SetDetailsEditable(bool editable)
        {
            txtName.ReadOnly = !editable;
            txtAge.ReadOnly = !editable;
            txtSpecialty.ReadOnly = !editable;
            cmbGender.Enabled = editable;
            btnSave.Visible = editable;
            btnCancel.Visible = editable;
        }

        private bool TryParseForm(out string name, out int age, out Gender gender, out string specialty)
        {
            name = txtName.Text.Trim();
            specialty = txtSpecialty.Text.Trim();
            age = 0;
            gender = Gender.Male;

            if (string.IsNullOrWhiteSpace(name)) { ShowError("Name cannot be empty."); return false; }
            if (!int.TryParse(txtAge.Text, out age)
                || age <= 0 || age > 120) { ShowError("Enter a valid age (1–120)."); return false; }
            if (string.IsNullOrWhiteSpace(specialty)) { ShowError("Specialty cannot be empty."); return false; }

            gender = cmbGender.SelectedItem?.ToString() == "Female" ? Gender.Female : Gender.Male;
            return true;
        }

        private bool RequireSelection()
        {
            if (_selected != null) return true;
            ShowInfo("Please select a doctor first.");
            return false;
        }

        // ── UI Factory ────────────────────────────────────────────────────────────
        private static Button MakePrimaryButton(string text) => new Button
        {
            Text = text,
            Height = 26,
            AutoSize = true,
            BackColor = Color.FromArgb(53, 122, 189),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9),
            Margin = new Padding(2)
        };

        private static Button MakeButton(string text) => new Button
        {
            Text = text,
            Height = 26,
            AutoSize = true,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9),
            Margin = new Padding(2)
        };

        private static Label MakeLabel(string text) => new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(68, 68, 68),
            TextAlign = ContentAlignment.BottomLeft
        };

        private static TextBox MakeTextBox() => new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9),
            Margin = new Padding(0, 0, 0, 6)
        };

        private static DataGridView MakeGrid(string[] columns)
        {
            var dgv = new DataGridView
            {
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(227, 238, 250),
                    Font = new Font("Segoe UI", 9)
                },
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false
            };
            foreach (var col in columns) dgv.Columns.Add(col, col);
            return dgv;
        }

        private static void ShowError(string msg) =>
            MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        private static void ShowInfo(string msg) =>
            MessageBox.Show(msg, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        private static bool ConfirmAction(string msg) =>
            MessageBox.Show(msg, "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
    }
}
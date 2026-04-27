using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Clinic_Management_System.Models;
using Clinic_Management_System.Services;

namespace Clinic_Management_System
{
    public partial class PatientUserControl : UserControl
    {
        // ── Services ────────────────────────────────────────────────────────────
        private readonly ClinicService _service;

        // ── Top Toolbar ─────────────────────────────────────────────────────────
        private Panel pnlToolbar;
        private Button btnAddPatient;
        private Button btnEditPatient;
        private Button btnDeletePatient;
        private Label lblSearch;
        private TextBox txtSearch;
        private Button btnFind;

        // ── Patient List (left) ──────────────────────────────────────────────────
        private GroupBox grpPatientList;
        private DataGridView dgvPatients;

        // ── Patient Details (right) ──────────────────────────────────────────────
        private GroupBox grpDetails;
        private Label lblName;
        private TextBox txtName;
        private Label lblAge;
        private TextBox txtAge;
        private Label lblGender;
        private ComboBox cmbGender;
        private Button btnSave;
        private Button btnCancel;

        // ── Tabs ─────────────────────────────────────────────────────────────────
        private TabControl tabPatientData;
        private TabPage tabMedicalRecords;
        private TabPage tabChronicDiseases;
        private TabPage tabHistory;

        // ── Medical Records tab ───────────────────────────────────────────────────
        private Panel pnlRecordToolbar;
        private Button btnAddRecord;
        private Button btnEditRecord;
        private Button btnDeleteRecord;
        private DataGridView dgvRecords;

        // ── Chronic Diseases tab ──────────────────────────────────────────────────
        private Panel pnlDiseaseToolbar;
        private Button btnAddDisease;
        private Button btnEditDisease;
        private Button btnDeleteDisease;
        private DataGridView dgvDiseases;

        // ── Appointment History tab ───────────────────────────────────────────────
        private DataGridView dgvHistory;

        // ── State ─────────────────────────────────────────────────────────────────
        private Patient _selectedPatient = null;
        private bool _isEditing = false;   // true = editing existing patient

        // ─────────────────────────────────────────────────────────────────────────
        public PatientUserControl(ClinicService service)
        {
            _service = service;
            InitializeComponent();
            LoadPatients();
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  UI INITIALISATION
        // ═════════════════════════════════════════════════════════════════════════
        private void InitializeComponent()
        {
            this.BackColor = Color.White;

            // Toolbar docked to top
            BuildToolbar();

            // SplitContainer fills the rest: top=patient list+details, bottom=tabs
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
            BuildTabs(split.Panel2);

            this.Controls.Add(split);
        }

        // ── Toolbar ──────────────────────────────────────────────────────────────
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
            {
                e.Graphics.DrawLine(new Pen(Color.FromArgb(221, 221, 221)),
                    0, pnlToolbar.Height - 1, pnlToolbar.Width, pnlToolbar.Height - 1);
            };

            btnAddPatient = MakePrimaryButton("+ Add Patient");
            btnEditPatient = MakeButton("✏ Edit");
            btnDeletePatient = MakeButton("🗑 Delete");
            btnDeletePatient.ForeColor = Color.FromArgb(192, 57, 43);

            var sep = new Panel { Width = 1, Height = 20, BackColor = Color.FromArgb(204, 204, 204), Margin = new Padding(4, 0, 4, 0) };

            lblSearch = new Label { Text = "Search:", Width = 50, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9) };
            txtSearch = new TextBox { Width = 160, Font = new Font("Segoe UI", 9), PlaceholderText = "Name or ID..." };
            btnFind = MakeButton("Find");

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false,
                Padding = new Padding(4, 4, 4, 0)
            };

            flow.Controls.Add(btnAddPatient);
            flow.Controls.Add(btnEditPatient);
            flow.Controls.Add(btnDeletePatient);
            flow.Controls.Add(sep);
            flow.Controls.Add(lblSearch);
            flow.Controls.Add(txtSearch);
            flow.Controls.Add(btnFind);

            pnlToolbar.Controls.Add(flow);
            this.Controls.Add(pnlToolbar);

            // Events
            btnAddPatient.Click += BtnAddPatient_Click;
            btnEditPatient.Click += BtnEditPatient_Click;
            btnDeletePatient.Click += BtnDeletePatient_Click;
            btnFind.Click += BtnFind_Click;
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnFind_Click(s, e); };
        }

        // ── Top section: list + details side by side ──────────────────────────────
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

            // ── Patient List ────────────────────────────────────────────────────
            grpPatientList = new GroupBox { Text = "Patients List", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9), Margin = new Padding(0, 0, 5, 0) };

            dgvPatients = MakeGrid(new[] { "ID", "Name", "Age", "Gender" });
            dgvPatients.Dock = DockStyle.Fill;
            dgvPatients.SelectionChanged += DgvPatients_SelectionChanged;

            grpPatientList.Controls.Add(dgvPatients);
            pnlTop.Controls.Add(grpPatientList, 0, 0);

            // ── Details Panel ───────────────────────────────────────────────────
            grpDetails = new GroupBox { Text = "Patient Details", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9), Margin = new Padding(5, 0, 0, 0) };

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(6)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            lblName = MakeLabel("Full Name");
            txtName = MakeTextBox();
            lblAge = MakeLabel("Age");
            txtAge = MakeTextBox();
            tbl.Controls.Add(lblName, 0, 0);
            tbl.Controls.Add(lblAge, 1, 0);
            tbl.Controls.Add(txtName, 0, 1);
            tbl.Controls.Add(txtAge, 1, 1);

            lblGender = MakeLabel("Gender");
            cmbGender = new ComboBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbGender.Items.AddRange(new object[] { "Male", "Female" });
            cmbGender.SelectedIndex = 0;
            tbl.SetColumnSpan(lblGender, 2);
            tbl.SetColumnSpan(cmbGender, 2);
            tbl.Controls.Add(lblGender, 0, 2);
            tbl.Controls.Add(cmbGender, 0, 3);

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

            // Events
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += BtnCancel_Click;

            SetDetailsEditable(false);
        }

        // ── Tabs: Medical Records / Chronic Diseases / History ────────────────────
        private void BuildTabs(SplitterPanel container)
        {
            tabPatientData = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9),
                Margin = new Padding(10)
            };

            // ── Medical Records ─────────────────────────────────────────────────
            tabMedicalRecords = new TabPage("Medical Records");
            pnlRecordToolbar = BuildSubToolbar(
                out btnAddRecord, out btnEditRecord, out btnDeleteRecord,
                "Add Record", "Edit Record", "Delete Record");
            dgvRecords = MakeGrid(new[] { "ID", "Diagnosis", "Treatment", "Date" });
            dgvRecords.Dock = DockStyle.Fill;

            var pnlRecords = new Panel { Dock = DockStyle.Fill };
            pnlRecords.Controls.Add(dgvRecords);
            pnlRecords.Controls.Add(pnlRecordToolbar);
            tabMedicalRecords.Controls.Add(pnlRecords);

            // ── Chronic Diseases ────────────────────────────────────────────────
            tabChronicDiseases = new TabPage("Chronic Diseases");
            pnlDiseaseToolbar = BuildSubToolbar(
                out btnAddDisease, out btnEditDisease, out btnDeleteDisease,
                "Add Disease", "Edit Disease", "Delete Disease");
            dgvDiseases = MakeGrid(new[] { "ID", "Name", "Notes" });
            dgvDiseases.Dock = DockStyle.Fill;

            var pnlDiseases = new Panel { Dock = DockStyle.Fill };
            pnlDiseases.Controls.Add(dgvDiseases);
            pnlDiseases.Controls.Add(pnlDiseaseToolbar);
            tabChronicDiseases.Controls.Add(pnlDiseases);

            // ── Appointment History ─────────────────────────────────────────────
            tabHistory = new TabPage("Appointment History");
            dgvHistory = MakeGrid(new[] { "ID", "Doctor", "Date & Time", "Fee", "Status" });
            dgvHistory.Dock = DockStyle.Fill;
            tabHistory.Controls.Add(dgvHistory);

            tabPatientData.TabPages.Add(tabMedicalRecords);
            tabPatientData.TabPages.Add(tabChronicDiseases);
            tabPatientData.TabPages.Add(tabHistory);

            container.Controls.Add(tabPatientData);

            // Events – Medical Records
            btnAddRecord.Click += BtnAddRecord_Click;
            btnEditRecord.Click += BtnEditRecord_Click;
            btnDeleteRecord.Click += BtnDeleteRecord_Click;

            // Events – Chronic Diseases
            btnAddDisease.Click += BtnAddDisease_Click;
            btnEditDisease.Click += BtnEditDisease_Click;
            btnDeleteDisease.Click += BtnDeleteDisease_Click;
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  DATA LOADING
        // ═════════════════════════════════════════════════════════════════════════
        private void LoadPatients(string query = "")
        {
            dgvPatients.Rows.Clear();
            var patients = string.IsNullOrWhiteSpace(query)
                ? _service.GetPatients()
                : _service.SearchPatients(query);

            foreach (var p in patients)
                dgvPatients.Rows.Add(p.ID, p.Name, p.Age, p.PersonGender);
        }

        private void LoadPatientSubData()
        {
            if (_selectedPatient == null) { ClearSubGrids(); return; }

            // Update tab headers
            tabMedicalRecords.Text = $"Medical Records ({_selectedPatient.medical_record.Count})";
            tabChronicDiseases.Text = $"Chronic Diseases ({_selectedPatient.chronic_diseases.Count})";

            // Medical records
            dgvRecords.Rows.Clear();
            foreach (var r in _selectedPatient.medical_record)
                dgvRecords.Rows.Add(r.ID, r.Diagnosis, r.Treatment, r.Date.ToShortDateString());

            // Chronic diseases
            dgvDiseases.Rows.Clear();
            foreach (var c in _selectedPatient.chronic_diseases)
                dgvDiseases.Rows.Add(c.ID, c.Name, c.Notes);

            // Appointment history
            dgvHistory.Rows.Clear();
            foreach (var a in _service.GetPatientHistory(_selectedPatient.ID))
            {
                string status = a.Status.ToString();
                dgvHistory.Rows.Add(a.ID, a.AppointmentDoctor.Name,
                    a.AppointmentDate.ToString("dd/MM/yyyy HH:mm"), a.Fee, status);
            }
        }

        private void ClearSubGrids()
        {
            dgvRecords.Rows.Clear();
            dgvDiseases.Rows.Clear();
            dgvHistory.Rows.Clear();
            tabMedicalRecords.Text = "Medical Records";
            tabChronicDiseases.Text = "Chronic Diseases";
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  PATIENT CRUD EVENTS
        // ═════════════════════════════════════════════════════════════════════════
        private void DgvPatients_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvPatients.SelectedRows.Count == 0) return;

            var row = dgvPatients.SelectedRows[0];
            int id = Convert.ToInt32(row.Cells[0].Value);
            _selectedPatient = _service.GetPatients().Find(p => p.ID == id);

            if (_selectedPatient != null)
            {
                grpDetails.Text = $"Patient Details — #{_selectedPatient.ID}";
                txtName.Text = _selectedPatient.Name;
                txtAge.Text = _selectedPatient.Age.ToString();
                cmbGender.SelectedItem = _selectedPatient.PersonGender.ToString();
                LoadPatientSubData();
            }
        }

        private void BtnAddPatient_Click(object sender, EventArgs e)
        {
            _isEditing = false;
            _selectedPatient = null;
            grpDetails.Text = "New Patient";
            txtName.Clear();
            txtAge.Clear();
            cmbGender.SelectedIndex = 0;
            SetDetailsEditable(true);
            txtName.Focus();
        }

        private void BtnEditPatient_Click(object sender, EventArgs e)
        {
            if (_selectedPatient == null) { ShowInfo("Please select a patient first."); return; }
            _isEditing = true;
            SetDetailsEditable(true);
            txtName.Focus();
        }

        private void BtnDeletePatient_Click(object sender, EventArgs e)
        {
            if (_selectedPatient == null) { ShowInfo("Please select a patient first."); return; }

            if (ConfirmAction($"Delete patient '{_selectedPatient.Name}'? This cannot be undone."))
            {
                try
                {
                    _service.DeletePatient(_selectedPatient.ID);
                    _selectedPatient = null;
                    ClearSubGrids();
                    grpDetails.Text = "Patient Details";
                    LoadPatients();
                }
                catch (Exception ex) { ShowError(ex.Message); }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!TryParseForm(out string name, out int age, out Gender gender)) return;

            try
            {
                if (_isEditing && _selectedPatient != null)
                    _service.EditPatient(_selectedPatient.ID, name, age, gender);
                else
                    _service.AddPatient(name, age, gender);

                SetDetailsEditable(false);
                LoadPatients();

                // Re-select the saved patient
                var saved = _service.GetPatients().Find(p => p.Name == name && p.Age == age);
                if (saved != null) _selectedPatient = saved;
                LoadPatientSubData();
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            SetDetailsEditable(false);
            if (_selectedPatient != null)
            {
                txtName.Text = _selectedPatient.Name;
                txtAge.Text = _selectedPatient.Age.ToString();
                cmbGender.SelectedItem = _selectedPatient.PersonGender.ToString();
            }
        }

        private void BtnFind_Click(object sender, EventArgs e)
        {
            LoadPatients(txtSearch.Text.Trim());
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  MEDICAL RECORDS EVENTS
        // ═════════════════════════════════════════════════════════════════════════
        private void BtnAddRecord_Click(object sender, EventArgs e)
        {
            if (!RequirePatient()) return;

            using var dlg = new RecordDialog("Add Medical Record");
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    _service.AddMedicalRecord(_selectedPatient.ID, dlg.Diagnosis, dlg.Treatment);
                    RefreshSelectedPatient();
                    LoadPatientSubData();
                }
                catch (Exception ex) { ShowError(ex.Message); }
            }
        }

        private void BtnEditRecord_Click(object sender, EventArgs e)
        {
            if (!RequirePatient()) return;
            if (dgvRecords.SelectedRows.Count == 0) { ShowInfo("Select a record first."); return; }

            int recordId = Convert.ToInt32(dgvRecords.SelectedRows[0].Cells[0].Value);
            var record = _selectedPatient.medical_record.Find(r => r.ID == recordId);
            if (record == null) return;

            using var dlg = new RecordDialog("Edit Medical Record", record.Diagnosis, record.Treatment);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    _service.EditMedicalRecord(_selectedPatient.ID, recordId, dlg.Diagnosis, dlg.Treatment);
                    RefreshSelectedPatient();
                    LoadPatientSubData();
                }
                catch (Exception ex) { ShowError(ex.Message); }
            }
        }

        private void BtnDeleteRecord_Click(object sender, EventArgs e)
        {
            if (!RequirePatient()) return;
            if (dgvRecords.SelectedRows.Count == 0) { ShowInfo("Select a record first."); return; }

            int recordId = Convert.ToInt32(dgvRecords.SelectedRows[0].Cells[0].Value);
            if (ConfirmAction("Delete this medical record?"))
            {
                try
                {
                    _service.DeleteMedicalRecord(_selectedPatient.ID, recordId);
                    RefreshSelectedPatient();
                    LoadPatientSubData();
                }
                catch (Exception ex) { ShowError(ex.Message); }
            }
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  CHRONIC DISEASES EVENTS
        // ═════════════════════════════════════════════════════════════════════════
        private void BtnAddDisease_Click(object sender, EventArgs e)
        {
            if (!RequirePatient()) return;

            using var dlg = new DiseaseDialog("Add Chronic Disease");
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    _service.AddChronicDisease(_selectedPatient.ID, dlg.DiseaseName, dlg.Notes);
                    RefreshSelectedPatient();
                    LoadPatientSubData();
                }
                catch (Exception ex) { ShowError(ex.Message); }
            }
        }

        private void BtnEditDisease_Click(object sender, EventArgs e)
        {
            if (!RequirePatient()) return;
            if (dgvDiseases.SelectedRows.Count == 0) { ShowInfo("Select a disease first."); return; }

            int diseaseId = Convert.ToInt32(dgvDiseases.SelectedRows[0].Cells[0].Value);
            var disease = _selectedPatient.chronic_diseases.Find(c => c.ID == diseaseId);
            if (disease == null) return;

            using var dlg = new DiseaseDialog("Edit Chronic Disease", disease.Name, disease.Notes);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    _service.EditChronicDisease(_selectedPatient.ID, diseaseId, dlg.DiseaseName, dlg.Notes);
                    RefreshSelectedPatient();
                    LoadPatientSubData();
                }
                catch (Exception ex) { ShowError(ex.Message); }
            }
        }

        private void BtnDeleteDisease_Click(object sender, EventArgs e)
        {
            if (!RequirePatient()) return;
            if (dgvDiseases.SelectedRows.Count == 0) { ShowInfo("Select a disease first."); return; }

            int diseaseId = Convert.ToInt32(dgvDiseases.SelectedRows[0].Cells[0].Value);
            if (ConfirmAction("Delete this chronic disease?"))
            {
                try
                {
                    _service.DeleteChronicDisease(_selectedPatient.ID, diseaseId);
                    RefreshSelectedPatient();
                    LoadPatientSubData();
                }
                catch (Exception ex) { ShowError(ex.Message); }
            }
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  HELPERS
        // ═════════════════════════════════════════════════════════════════════════
        private void SetDetailsEditable(bool editable)
        {
            txtName.ReadOnly = !editable;
            txtAge.ReadOnly = !editable;
            cmbGender.Enabled = editable;
            btnSave.Visible = editable;
            btnCancel.Visible = editable;
        }

        private bool TryParseForm(out string name, out int age, out Gender gender)
        {
            name = txtName.Text.Trim();
            age = 0;
            gender = Gender.Male;

            if (string.IsNullOrWhiteSpace(name)) { ShowError("Name cannot be empty."); return false; }
            if (!int.TryParse(txtAge.Text, out age)
                || age <= 0 || age > 120) { ShowError("Enter a valid age (1–120)."); return false; }
            gender = cmbGender.SelectedItem?.ToString() == "Female" ? Gender.Female : Gender.Male;
            return true;
        }

        private void RefreshSelectedPatient()
        {
            if (_selectedPatient == null) return;
            _selectedPatient = _service.GetPatients().Find(p => p.ID == _selectedPatient.ID);
        }

        private bool RequirePatient()
        {
            if (_selectedPatient != null) return true;
            ShowInfo("Please select a patient first.");
            return false;
        }

        // ── UI factory methods ────────────────────────────────────────────────────
        private static Button MakePrimaryButton(string text)
        {
            return new Button
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
        }

        private static Button MakeButton(string text)
        {
            return new Button
            {
                Text = text,
                Height = 26,
                AutoSize = true,
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Margin = new Padding(2)
            };
        }

        private static Label MakeLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(68, 68, 68),
                TextAlign = ContentAlignment.BottomLeft
            };
        }

        private static TextBox MakeTextBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9),
                Margin = new Padding(0, 0, 0, 6)
            };
        }

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
                    Font = new Font("Segoe UI", 9, FontStyle.Regular)
                },
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false
            };

            foreach (var col in columns)
                dgv.Columns.Add(col, col);

            return dgv;
        }

        private static Panel BuildSubToolbar(
            out Button btnAdd, out Button btnEdit, out Button btnDelete,
            string addLabel, string editLabel, string deleteLabel)
        {
            btnAdd = MakePrimaryButton("+ " + addLabel);
            btnEdit = MakeButton("✏ " + editLabel);
            btnDelete = MakeButton("🗑 " + deleteLabel);
            btnDelete.ForeColor = Color.FromArgb(192, 57, 43);

            var pnl = new Panel { Dock = DockStyle.Top, Height = 34 };
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(4, 4, 4, 0)
            };
            flow.Controls.Add(btnAdd);
            flow.Controls.Add(btnEdit);
            flow.Controls.Add(btnDelete);
            pnl.Controls.Add(flow);
            return pnl;
        }

        private static void ShowError(string msg) => MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        private static void ShowInfo(string msg) => MessageBox.Show(msg, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        private static bool ConfirmAction(string msg)
            => MessageBox.Show(msg, "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
    }


    // ═════════════════════════════════════════════════════════════════════════════
    //  DIALOG: Add / Edit Medical Record
    // ═════════════════════════════════════════════════════════════════════════════
    public class RecordDialog : Form
    {
        public string Diagnosis { get; private set; }
        public string Treatment { get; private set; }

        public RecordDialog(string title, string diagnosis = "", string treatment = "")
        {
            Text = title;
            Size = new Size(360, 210);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            var lbl1 = new Label { Text = "Diagnosis", Left = 12, Top = 14, Width = 320 };
            var txt1 = new TextBox { Left = 12, Top = 32, Width = 320, Text = diagnosis };
            var lbl2 = new Label { Text = "Treatment", Left = 12, Top = 66, Width = 320 };
            var txt2 = new TextBox { Left = 12, Top = 84, Width = 320, Text = treatment };

            var btnOk = new Button { Text = "Save", Left = 172, Top = 130, Width = 75, DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "Cancel", Left = 257, Top = 130, Width = 75, DialogResult = DialogResult.Cancel };

            btnOk.Click += (s, e) =>
            {
                Diagnosis = txt1.Text.Trim();
                Treatment = txt2.Text.Trim();
                if (string.IsNullOrWhiteSpace(Diagnosis) || string.IsNullOrWhiteSpace(Treatment))
                {
                    MessageBox.Show("Both fields are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                }
            };

            Controls.AddRange(new Control[] { lbl1, txt1, lbl2, txt2, btnOk, btnCancel });
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }
    }


    // ═════════════════════════════════════════════════════════════════════════════
    //  DIALOG: Add / Edit Chronic Disease
    // ═════════════════════════════════════════════════════════════════════════════
    public class DiseaseDialog : Form
    {
        public string DiseaseName { get; private set; }
        public string Notes { get; private set; }

        public DiseaseDialog(string title, string name = "", string notes = "")
        {
            Text = title;
            Size = new Size(360, 210);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            var lbl1 = new Label { Text = "Disease Name", Left = 12, Top = 14, Width = 320 };
            var txt1 = new TextBox { Left = 12, Top = 32, Width = 320, Text = name };
            var lbl2 = new Label { Text = "Notes", Left = 12, Top = 66, Width = 320 };
            var txt2 = new TextBox { Left = 12, Top = 84, Width = 320, Text = notes };

            var btnOk = new Button { Text = "Save", Left = 172, Top = 130, Width = 75, DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "Cancel", Left = 257, Top = 130, Width = 75, DialogResult = DialogResult.Cancel };

            btnOk.Click += (s, e) =>
            {
                DiseaseName = txt1.Text.Trim();
                Notes = txt2.Text.Trim();
                if (string.IsNullOrWhiteSpace(DiseaseName) || string.IsNullOrWhiteSpace(Notes))
                {
                    MessageBox.Show("Both fields are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                }
            };

            Controls.AddRange(new Control[] { lbl1, txt1, lbl2, txt2, btnOk, btnCancel });
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }
    }
}

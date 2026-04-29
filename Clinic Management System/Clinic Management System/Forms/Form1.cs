using System;
using System.Windows.Forms;
using Clinic_Management_System.Services;
using Clinic_Management_System.Forms;

namespace Clinic_Management_System
{
    public partial class Form1 : Form
    {
        // ── ONE shared ClinicService for the whole app ────────
        private readonly ClinicService _clinicService;

        // ── Pages ─────────────────────────────────────────────
        private AnalyticsUserControl _analyticsPage;
        private Panel pnlSidebar;
        private Panel pnlNavIndicator;
        private PatientUserControl _patientsPage;
        private DoctorUserControl _doctorsPage;
        private AppointmentUserControl _appointmentsPage;
        private PaymentUserControl     _paymentsPage;
        private DashboardUserControl    _dashboardPage;

        public Form1()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;
            SetupLayout();

            // ── Create service ────────────────────────────────
            _clinicService = new ClinicService();

            // ── Create Analytics page ─────────────────────────
            _dashboardPage = new DashboardUserControl(_clinicService);
            ConfigurePage(_dashboardPage);
            pnlContent.Controls.Add(_dashboardPage);

            _analyticsPage = new AnalyticsUserControl(_clinicService);
            ConfigurePage(_analyticsPage);
            pnlContent.Controls.Add(_analyticsPage);


            _patientsPage = new PatientUserControl(_clinicService);
            ConfigurePage(_patientsPage);
            pnlContent.Controls.Add(_patientsPage);

            _doctorsPage = new DoctorUserControl(_clinicService);
            ConfigurePage(_doctorsPage);
            pnlContent.Controls.Add(_doctorsPage);

            _appointmentsPage = new AppointmentUserControl(_clinicService);
            ConfigurePage(_appointmentsPage);
            pnlContent.Controls.Add(_appointmentsPage);

            _paymentsPage = new PaymentUserControl(_clinicService);
            ConfigurePage(_paymentsPage);
            pnlContent.Controls.Add(_paymentsPage);

            ShowPage(_dashboardPage);
        }

        // ── Helpers ───────────────────────────────────────────
        private void ConfigurePage(UserControl page)
        {
            page.Dock = DockStyle.Fill;
            page.Visible = false;
        }

        private void ShowPage(UserControl page)
        {
            foreach (Control c in pnlContent.Controls)
                c.Visible = false;

            page.Visible = true;
            page.BringToFront();

            if (page is AnalyticsUserControl a)
                a.Refresh();
            if (page is PatientUserControl b)
                b.Refresh();
            if (page is PaymentUserControl d)
                d.Refresh();
            if (page is DoctorUserControl f)
                f.Refresh();
            if (page is AppointmentUserControl m)
                m.Refresh();

            if (page is DashboardUserControl n)
            n.Refresh();
        }


        private void button1_Click(object sender, EventArgs e)
        {
            ShowPage(_analyticsPage);
        }

     

        // ── Designer event (keep as-is) ───────────────────────
        private void panel1_Paint(object sender, PaintEventArgs e) { }
        #region SidePar

        private void SetupLayout()
        {
            pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 210,
                BackColor = Color.FromArgb(240, 240, 240),
                Padding = new Padding(0, 20, 0, 0)
            };

            pnlContent.BackColor = Color.White;
            pnlContent.Dock = DockStyle.Fill;

            // ── ORDER MATTERS in WinForms ──────────────────────────
            this.Controls.Add(pnlContent);  
            this.Controls.Add(pnlSidebar);   

            AddSectionLabel("MAIN");
            AddNavButton("Dashboard", Properties.Resources.home_color_icon, 0);

            AddSectionLabel("MANAGEMENT");
            AddNavButton("Patients", Properties.Resources.man_user_circle_icon, 1);
            AddNavButton("Doctors", Properties.Resources.doctor_color_icon, 2);
            AddNavButton("Appointments", Properties.Resources.calenar_color_icon, 3);
            AddNavButton("Payments", Properties.Resources.credit_card_color_icon, 4);

            AddSectionLabel("REPORTS");
            AddNavButton("Analytics", Properties.Resources.analytics_color_icon, 5);
        }

        private void AddSectionLabel(string text)
        {
            var lbl = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(130, 130, 130),
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.BottomLeft,
                Padding = new Padding(16, 0, 0, 4),
                BackColor = Color.FromArgb(240, 240, 240)
            };
            pnlSidebar.Controls.Add(lbl);
            lbl.BringToFront();
        }

        private void AddNavButton(string text, Image? icon, int index)
        {
            Button btn = new Button
            {
                Text = "   " + text,
                Image = icon,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Dock = DockStyle.Top,
                Height = 50,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand,
                Tag = index,
                Padding = new Padding(15, 0, 0, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += NavButton_Click;

            pnlSidebar.Controls.Add(btn);
            
            btn.BringToFront();
        }

        private void NavButton_Click(object sender, EventArgs e)
        {
            Button clickedBtn = (Button)sender;

            
            foreach (Control ctrl in pnlSidebar.Controls)
            {
                if (ctrl is Button b)
                {
                    b.BackColor = Color.Transparent;
                    b.ForeColor = Color.Black;
                }
            }
            clickedBtn.BackColor = Color.FromArgb(53, 122, 189); 
            clickedBtn.ForeColor = Color.White;

            
            int index = (int)clickedBtn.Tag;
            switch (index)
            {
                case 0:
                    ShowPage(_dashboardPage);
                    break;
                case 1:
                    ShowPage(_patientsPage);
                    break;
                case 2:
                    ShowPage(_doctorsPage);
                    break;
                case 3:
                    ShowPage(_appointmentsPage);
                    break;
                case 4: 
                    ShowPage(_paymentsPage);
                    break;
                case 5: 
                    ShowPage(_analyticsPage);
                    break;
            }
        }
        #endregion
    }
}
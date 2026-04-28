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

        // teammates will add theirs here later:
        // private PatientsUserControl     _patientsPage;
        // private DoctorsUserControl      _doctorsPage;
        // private AppointmentsUserControl _appointmentsPage;
        // private PaymentsUserControl     _paymentsPage;
        // private DashboardUserControl    _dashboardPage;

        public Form1()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;
            SetupLayout();

            // ── Create service ────────────────────────────────
            _clinicService = new ClinicService();

            // ── Create Analytics page ─────────────────────────
            _analyticsPage = new AnalyticsUserControl(_clinicService);
            ConfigurePage(_analyticsPage);
            pnlContent.Controls.Add(_analyticsPage);


            // teammates: same pattern ─────────────────────────
            // _patientsPage = new PatientsUserControl(_clinicService);
            // ConfigurePage(_patientsPage);
            // pnlContent.Controls.Add(_patientsPage);

            // ── Show Analytics as default page ────────────────
            ShowPage(_analyticsPage);
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

            // إظهار الصفحة المطلوبة فقط
            page.Visible = true;
            page.BringToFront();

            // تحديث الداتا إذا كانت صفحة Analytics
            if (page is AnalyticsUserControl a)
                a.Refresh();
        }


        // ── Navigation button events ──────────────────────────
        // button1 is already wired in Designer — point it to Analytics for now
        private void button1_Click(object sender, EventArgs e)
        {
            ShowPage(_analyticsPage);
        }

        // teammates: add their nav buttons here, same pattern:
        // private void btnPatients_Click(object sender, EventArgs e)
        //     => ShowPage(_patientsPage);

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
            // Fill must be added FIRST, then Left — WinForms docks
            // in REVERSE order of Controls.Add
            this.Controls.Add(pnlContent);   // ← added first = behind
            this.Controls.Add(pnlSidebar);   // ← added second = left side

            AddNavButton("Dashboard", Properties.Resources.home_color_icon, 0);
            AddNavButton("Patients", Properties.Resources.man_user_circle_icon, 1);
            AddNavButton("Doctors", Properties.Resources.doctor_color_icon, 2);
            AddNavButton("Appointments", Properties.Resources.calenar_color_icon, 3);
            AddNavButton("Payments", Properties.Resources.credit_card_color_icon, 4);
            AddNavButton("Analytics", Properties.Resources.analytics_color_icon, 5);
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
            // لترتيب الأزرار من الأعلى للأسفل
            btn.BringToFront();
        }

        private void NavButton_Click(object sender, EventArgs e)
        {
            Button clickedBtn = (Button)sender;

            // تغيير شكل الزر النشط
            foreach (Control ctrl in pnlSidebar.Controls)
            {
                if (ctrl is Button b)
                {
                    b.BackColor = Color.Transparent;
                    b.ForeColor = Color.Black;
                }
            }
            clickedBtn.BackColor = Color.FromArgb(53, 122, 189); // أزرق زي الـ HTML
            clickedBtn.ForeColor = Color.White;

            // إظهار الصفحة المناسبة
            int index = (int)clickedBtn.Tag;
            switch (index)
            {
                case 5: // Analytics
                    ShowPage(_analyticsPage);
                    break;
                    // هنا زمايلك هيضيفوا حالاتهم بنفس الطريقة
                    // case 1: ShowPage(_patientsPage); break;
            }
        }
        #endregion
    }
}
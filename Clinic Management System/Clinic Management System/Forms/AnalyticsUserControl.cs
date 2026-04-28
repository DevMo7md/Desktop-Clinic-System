using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Clinic_Management_System.Models;
using Clinic_Management_System.Services;

namespace Clinic_Management_System.Forms
{
    public class AnalyticsUserControl : UserControl
    {
        // ── Services ──────────────────────────────────────────
        private readonly ClinicService _clinicService;
        private AnalyticsService? _analytics;

        // ── Toolbar ───────────────────────────────────────────
        private Panel pnlToolbar = null!;
        private DateTimePicker dtpFrom = null!;
        private DateTimePicker dtpTo = null!;
        private Button btnGenerate = null!;

        // ── Stat cards ────────────────────────────────────────
        private Panel pnlStats = null!;
        private Label lblRevenueVal = null!;
        private Label lblGrowthVal = null!;
        private Label lblPeakVal = null!;
        private Label lblPatientsVal = null!;

        // ── Revenue chart ─────────────────────────────────────
        private GroupBox grpRevenue = null!;
        private Panel pnlRevenueChart = null!;
        private readonly List<(string Month, decimal Revenue)> _revenueData = new List<(string, decimal)>();
        private decimal _revenueMax = 1;

        // ── Right panel ───────────────────────────────────────
        private GroupBox grpRight = null!;
        private ProgressBar pbChildren = null!;
        private ProgressBar pbYouth = null!;
        private ProgressBar pbSeniors = null!;
        private Label lblChildrenVal = null!;
        private Label lblYouthVal = null!;
        private Label lblSeniorsVal = null!;
        private ProgressBar pbCompleted = null!;
        private ProgressBar pbCancelled = null!;
        private Label lblCompletedVal = null!;
        private Label lblCancelledVal = null!;

        // ══════════════════════════════════════════════════════
        public AnalyticsUserControl(ClinicService clinicService)
        {
            _clinicService = clinicService;
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.DoubleBuffered = true;

            BuildToolbar();
            BuildStatRow();
            BuildMainContent();        // builds grpRevenue + grpRight
            var wrapper = BuildContentWrapper(); // wraps grpRevenue + grpRight in TableLayoutPanel

            // ── ORDER MATTERS in WinForms Dock ───────────────
            // DockStyle.Fill must be added FIRST (ends up behind)
            // DockStyle.Top controls added AFTER (processed last = dock from top)
            this.Controls.Add(wrapper);    // Fill  → fills remaining space below stats
            this.Controls.Add(pnlStats);   // Top   → docks below toolbar
            this.Controls.Add(pnlToolbar); // Top   → docks at very top

            LoadAnalytics();
        }

        public new void Refresh() => LoadAnalytics();

        // ══════════════════════════════════════════════════════
        // TOOLBAR
        // ══════════════════════════════════════════════════════
        private void BuildToolbar()
        {
            pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.FromArgb(245, 245, 245)
            };
            pnlToolbar.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(Color.FromArgb(210, 210, 210)),
                    0, pnlToolbar.Height - 1,
                    pnlToolbar.Width, pnlToolbar.Height - 1);

            dtpFrom = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 9F),
                Location = new Point(55, 9),
                Width = 110,
                Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
            };

            dtpTo = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 9F),
                Location = new Point(210, 9),
                Width = 110,
                Value = DateTime.Today
            };

            btnGenerate = new Button
            {
                Text = "Generate",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(53, 122, 189),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(334, 8),
                Size = new Size(90, 24),
                Cursor = Cursors.Hand
            };
            btnGenerate.FlatAppearance.BorderSize = 0;
            btnGenerate.Click += BtnGenerate_Click;

            pnlToolbar.Controls.AddRange(new Control[]
            {
                MakeLbl("From:", 10, 12),
                dtpFrom,
                MakeLbl("To:", 178, 12),
                dtpTo,
                btnGenerate
            });
        }

        // ══════════════════════════════════════════════════════
        // STAT CARDS
        // ══════════════════════════════════════════════════════
        private void BuildStatRow()
        {

            var tlpStats = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 150,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(10, 8, 10, 8),
                BackColor = Color.FromArgb(245, 247, 250)
            };

            for (int i = 0; i < 4; i++)
                tlpStats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            (string title, string def, Color col)[] defs =
            {
                ("Net Revenue (EGP)", "0",   Color.FromArgb(46,  125, 50)),
                ("Monthly Growth",    "0%",  Color.FromArgb(53,  122, 189)),
                ("Peak Hour",         "--",  Color.FromArgb(245, 124, 0)),
                ("Total Patients",    "0",   Color.FromArgb(106, 27,  154)),
            };

            Label[] vals = new Label[4];
            for (int i = 0; i < 4; i++)
            {
                var card = MakeCard(defs[i].title, defs[i].def, defs[i].col, out vals[i]);
                tlpStats.Controls.Add(card, i, 0); // إضافة الكارت للخلية المناسبة
            }
            lblRevenueVal = vals[0];
            lblGrowthVal = vals[1];
            lblPeakVal = vals[2];
            lblPatientsVal = vals[3];

            pnlStats = tlpStats;
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
        // MAIN CONTENT — TableLayoutPanel 60/40 split
        // ══════════════════════════════════════════════════════
        private Control BuildContentWrapper()
        {
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(8, 8, 8, 4),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            tbl.Controls.Add(grpRevenue, 0, 0);
            tbl.Controls.Add(grpRight, 1, 0);

            return tbl;
        }

        private void BuildMainContent()
        {
            // ── Left: Revenue GroupBox ────────────────────────
            grpRevenue = new GroupBox
            {
                Text = "Monthly Revenue Chart",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(85, 85, 85),
                BackColor = Color.White,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 4, 0)
            };
            pnlRevenueChart = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            pnlRevenueChart.Paint += DrawRevenueChart;
            grpRevenue.Controls.Add(pnlRevenueChart);

            // ── Right: Demographics GroupBox ──────────────────
            grpRight = new GroupBox
            {
                Text = "Patient Demographics",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(85, 85, 85),
                BackColor = Color.White,
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 0, 0, 0)
            };
            BuildRightPanel();
        }

        // ══════════════════════════════════════════════════════
        // RIGHT PANEL
        // ══════════════════════════════════════════════════════
        private void BuildRightPanel()
        {
            // FlowLayoutPanel stacks items top-to-bottom with no overlap
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false,
                BackColor = Color.White,
                Padding = new Padding(10, 8, 10, 8)
            };
            flow.Resize += (s, e) => ResizeFlowChildren(flow);

            flow.Controls.Add(SectionLabel("Patient Age Groups", flow));

            var rowC = MakeProgressRow("Children (0–15)", Color.FromArgb(92, 184, 92), flow);
            pbChildren = (ProgressBar)rowC.Tag!;
            lblChildrenVal = (Label)rowC.Controls[2];
            flow.Controls.Add(rowC);

            var rowY = MakeProgressRow("Youth (16–40)", Color.FromArgb(53, 122, 189), flow);
            pbYouth = (ProgressBar)rowY.Tag!;
            lblYouthVal = (Label)rowY.Controls[2];
            flow.Controls.Add(rowY);

            var rowS = MakeProgressRow("Seniors (41+)", Color.FromArgb(217, 83, 79), flow);
            pbSeniors = (ProgressBar)rowS.Tag!;
            lblSeniorsVal = (Label)rowS.Controls[2];
            flow.Controls.Add(rowS);

            // Divider
            flow.Controls.Add(new Panel
            {
                Height = 1,
                BackColor = Color.FromArgb(210, 210, 210),
                Margin = new Padding(0, 6, 0, 6)
            });

            flow.Controls.Add(SectionLabel("Appointments Breakdown", flow));

            var rowComp = MakeProgressRow("Completed", Color.FromArgb(53, 122, 189), flow);
            pbCompleted = (ProgressBar)rowComp.Tag!;
            lblCompletedVal = (Label)rowComp.Controls[2];
            flow.Controls.Add(rowComp);

            var rowCanc = MakeProgressRow("Cancelled", Color.FromArgb(217, 83, 79), flow);
            pbCancelled = (ProgressBar)rowCanc.Tag!;
            lblCancelledVal = (Label)rowCanc.Controls[2];
            flow.Controls.Add(rowCanc);

            grpRight.Controls.Add(flow);
        }

        // keeps all FlowLayout children the same width as the panel
        private static void ResizeFlowChildren(FlowLayoutPanel flow)
        {
            int w = flow.ClientSize.Width - flow.Padding.Horizontal - 4;
            foreach (Control c in flow.Controls)
                c.Width = Math.Max(w, 40);
        }

        private static Label SectionLabel(string text, FlowLayoutPanel? flow = null)
        {
            int w = flow != null ? Math.Max(flow.ClientSize.Width - flow.Padding.Horizontal - 4, 40) : 200;
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(53, 122, 189),
                AutoSize = false,
                Width = w,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 4, 0, 2)
            };
        }

        /// <summary>
        /// Returns a Panel with:
        ///   Controls[0] = Label (text)
        ///   Controls[1] = ProgressBar  (also stored in Tag)
        ///   Controls[2] = Label (value)
        /// </summary>
        private static Panel MakeProgressRow(string label, Color barColor, FlowLayoutPanel? flow = null)
        {
            int initW = flow != null ? Math.Max(flow.ClientSize.Width - flow.Padding.Horizontal - 4, 100) : 200;
            var row = new Panel
            {
                Width = initW,
                Height = 44,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 4)
            };

            var lbl = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(85, 85, 85),
                AutoSize = false,
                Location = new Point(0, 2),
                Width = initW,
                Height = 16
            };

            var pb = new ProgressBar
            {
                Location = new Point(0, 20),
                Width = Math.Max(initW - 62, 20),
                Height = 14,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Style = ProgressBarStyle.Continuous
            };
            ApplyProgressColor(pb, barColor);

            var val = new Label
            {
                Text = "0%  (0)",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.DimGray,
                AutoSize = false,
                Location = new Point(pb.Width + 2, 20),
                Width = 60,
                Height = 16,
                TextAlign = ContentAlignment.MiddleLeft
            };

            row.Tag = pb;

            // Resize: keep controls in sync with row width
            row.Resize += (s, e) =>
            {
                var r = (Panel)s!;
                lbl.Width = r.Width;
                pb.Width = Math.Max(r.Width - 62, 20);
                val.Left = pb.Width + 2;
            };

            row.Controls.AddRange(new Control[] { lbl, pb, val });
            return row;
        }

        // ══════════════════════════════════════════════════════
        // LOAD DATA
        // ══════════════════════════════════════════════════════
        private void LoadAnalytics()
        {
            _analytics = new AnalyticsService(
                _clinicService.GetAppointments(),
                _clinicService.GetPayments(),
                _clinicService.GetPatients(),
                _clinicService.GetDoctors()
            );

            UpdateStatCards();
            UpdateRevenueData();
            UpdateDemographics();
            UpdateAppointments();
        }

        private void UpdateStatCards()
        {
            try
            {
                decimal net = _analytics!.GetNetRevenue(dtpFrom.Value, dtpTo.Value);
                lblRevenueVal.Text = net.ToString("N0");

                double g = _analytics.GetMonthlyGrowthRate();
                lblGrowthVal.Text = (g >= 0 ? "+" : "") + g.ToString("F1") + "%";
                lblGrowthVal.ForeColor = g >= 0
                    ? Color.FromArgb(46, 125, 50)
                    : Color.FromArgb(192, 57, 43);

                int peak = _analytics.GetPeakHour();
                lblPeakVal.Text = peak == 0 ? "--"
                    : DateTime.Today.AddHours(peak).ToString("hh:00 tt");

                lblPatientsVal.Text = _clinicService.GetPatients().Count.ToString();
            }
            catch { /* keep zeros on empty data */ }
        }

        private void UpdateRevenueData()
        {
            _revenueData.Clear();
            var today = DateTime.Today;
            for (int i = 6; i >= 0; i--)
            {
                var s = new DateTime(today.Year, today.Month, 1).AddMonths(-i);
                var e = i == 0 ? today : s.AddMonths(1).AddDays(-1);
                _revenueData.Add((s.ToString("MMM"), _analytics!.GetNetRevenue(s, e)));
            }
            _revenueMax = _revenueData.Max(r => r.Revenue);
            if (_revenueMax <= 0) _revenueMax = 1;
            pnlRevenueChart.Invalidate();
        }

        private void UpdateDemographics()
        {
            var demo = _analytics!.GetPatientAgeDemographics();
            int total = Math.Max(demo.Values.Sum(), 1);
            var keys = demo.Keys.ToList();

            SetProgress(0, keys, demo, total, pbChildren, lblChildrenVal);
            SetProgress(1, keys, demo, total, pbYouth, lblYouthVal);
            SetProgress(2, keys, demo, total, pbSeniors, lblSeniorsVal);
        }

        private static void SetProgress(int ki,
            List<string> keys,
            Dictionary<string, int> demo,
            int total,
            ProgressBar pb,
            Label lbl)
        {
            int val = ki < keys.Count ? demo[keys[ki]] : 0;
            int pct = val * 100 / total;
            pb.Value = Math.Min(pct, 100);
            lbl.Text = $"{pct}%  ({val})";
        }

        private void UpdateAppointments()
        {
            var appts = _clinicService.GetAppointments()
                .Where(a => a.AppointmentDate.Date >= dtpFrom.Value.Date
                         && a.AppointmentDate.Date <= dtpTo.Value.Date)
                .ToList();

            int tot = Math.Max(appts.Count, 1);
            int comp = appts.Count(a => a.Status == AppointmentStatus.Completed);
            int canc = appts.Count(a => a.Status == AppointmentStatus.Cancelled);

            pbCompleted.Value = Math.Min(comp * 100 / tot, 100);
            lblCompletedVal.Text = $"{comp * 100 / tot}%  ({comp})";
            pbCancelled.Value = Math.Min(canc * 100 / tot, 100);
            lblCancelledVal.Text = $"{canc * 100 / tot}%  ({canc})";
        }

        // ══════════════════════════════════════════════════════
        // BAR CHART PAINT
        // ══════════════════════════════════════════════════════
        private void DrawRevenueChart(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var rc = pnlRevenueChart.ClientRectangle;
            g.Clear(Color.White);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            if (_revenueData.Count == 0) return;

            int padL = 44, padR = 10, padT = 12, padB = 28;
            int cW = rc.Width - padL - padR;
            int cH = rc.Height - padT - padB;
            if (cW < 10 || cH < 10) return;

            using var gridPen = new Pen(Color.FromArgb(235, 235, 235));
            using var axPen = new Pen(Color.FromArgb(200, 200, 200));
            using var font = new Font("Segoe UI", 7.5F);
            using var gray = new SolidBrush(Color.FromArgb(130, 130, 130));
            using var barBr = new SolidBrush(Color.FromArgb(53, 122, 189));
            using var hiBr = new SolidBrush(Color.FromArgb(26, 90, 154));

            var sfR = new StringFormat
            {
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Center
            };
            var sfC = new StringFormat { Alignment = StringAlignment.Center };

            for (int i = 0; i <= 4; i++)
            {
                int gy = padT + (int)(cH * (1 - i / 4.0));
                g.DrawLine(gridPen, padL, gy, padL + cW, gy);
                decimal yv = _revenueMax * i / 4;
                string ylb = yv >= 1000
                    ? (yv / 1000).ToString("F0") + "k"
                    : yv.ToString("F0");
                g.DrawString(ylb, font, gray,
                    new RectangleF(0, gy - 8, padL - 4, 16), sfR);
            }

            g.DrawLine(axPen, padL, padT, padL, padT + cH);
            g.DrawLine(axPen, padL, padT + cH, padL + cW, padT + cH);

            int n = _revenueData.Count;
            float step = cW / (float)n;
            float bW = step * 0.55f;
            float off = (step - bW) / 2f;

            for (int i = 0; i < n; i++)
            {
                float x = padL + i * step + off;
                float bH = (float)(_revenueData[i].Revenue / _revenueMax * cH);
                bH = Math.Max(bH, 2);
                float y = padT + cH - bH;

                g.FillRectangle(i == n - 1 ? hiBr : barBr, x, y, bW, bH);
                g.DrawString(_revenueData[i].Month, font, gray,
                    new RectangleF(x - 2, padT + cH + 4, bW + 4, 18), sfC);
            }
        }

        // ══════════════════════════════════════════════════════
        // EVENTS + HELPERS
        // ══════════════════════════════════════════════════════
        private void BtnGenerate_Click(object? sender, EventArgs e)
        {
            if (dtpFrom.Value > dtpTo.Value)
            {
                MessageBox.Show("'From' date cannot be after 'To' date.",
                    "Invalid Range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            LoadAnalytics();
        }

        private static Label MakeLbl(string text, int x, int y) => new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(85, 85, 85),
            AutoSize = true,
            Location = new Point(x, y)
        };

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        private static extern IntPtr SendMessage(
            IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);


        private static void ApplyProgressColor(ProgressBar pb, Color c)
        {
            const uint PBM_SETBARCOLOR = 0x040A;
            if (pb.IsHandleCreated)
                SendMessage(pb.Handle, PBM_SETBARCOLOR, IntPtr.Zero,
                    (IntPtr)ColorTranslator.ToWin32(c));
            else
                pb.HandleCreated += (s, _) =>
                    SendMessage(((ProgressBar)s!).Handle, PBM_SETBARCOLOR,
                        IntPtr.Zero, (IntPtr)ColorTranslator.ToWin32(c));
        }
    }
}
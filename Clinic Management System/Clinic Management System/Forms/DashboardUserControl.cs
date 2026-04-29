using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Clinic_Management_System.Models;
using Clinic_Management_System.Services;

namespace Clinic_Management_System.Forms
{
    public class DashboardUserControl : UserControl
    {
        // ── Services ──────────────────────────────────────────
        private readonly ClinicService _clinicService;

        // ── Stat labels ───────────────────────────────────────
        private Label lblPatients, lblPending, lblRevenue, lblDoctors;

        // ── Grid ──────────────────────────────────────────────
        private DataGridView dgvAppointments;

        // ── Chart ─────────────────────────────────────────────
        private Panel pnlBarChart;
        private List<decimal> _weeklyRevenue = new List<decimal>();

        // ── Age bars ──────────────────────────────────────────
        private Panel pnlFillChildren, pnlFillYouth, pnlFillSeniors;
        private Label lblChildrenPct, lblYouthPct, lblSeniorsPct;
        private int _pctC, _pctY, _pctS;          // stored so resize can re-apply

        // ── Section Labels ─────────────────────────────────────
        private Label lblSectionStats, lblSectionAppt, lblSectionAnalytics;

        // ── Toolbar ───────────────────────────────────────────
        private Label lblDate;

        // ── Layout constants ──────────────────────────────────
        private const int PAD = 14;        // outer horizontal padding
        private const int GAP = 12;        // vertical gap between sections
        private const int SECTION_LBL_H = 24; // height of section label
        private const int SECTION_LBL_GAP = 6; // gap between label and content
        private const int STAT_H = 110;     // tall enough for number + subtitle
        private const int STAT_BOTTOM_GAP = 24; // extra breathing room below stat cards
        private const int APPT_H = 320;   // taller middle section — grows with appointments
        private const int BOT_H = 220;
        private const int TOOLBAR_H = 44; // matches Payment/Appointment pages

        public DashboardUserControl(ClinicService clinicService)
        {
            _clinicService = clinicService;
            this.DoubleBuffered = true;
            BuildUI();
            LoadData();
        }

        // ══════════════════════════════════════════════════════
        //  BUILD UI
        // ══════════════════════════════════════════════════════
        private void BuildUI()
        {
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Dock = DockStyle.Fill;

            // Root splits toolbar / scroll
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, TOOLBAR_H));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            this.Controls.Add(root);

            root.Controls.Add(BuildToolbar(), 0, 0);

            // Scroll container
            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(0)
            };
            root.Controls.Add(scroll, 0, 1);

            // Inner canvas – we set its width & height explicitly
            int innerH = PAD + STAT_H + GAP + APPT_H + GAP + BOT_H + PAD;
            var inner = new Panel
            {
                BackColor = Color.FromArgb(245, 247, 250),
                Location = new Point(0, 0),
                Height = innerH
            };
            scroll.Controls.Add(inner);

            // Helper: keep inner width = scroll client width
            void SyncWidth()
            {
                inner.Width = scroll.ClientSize.Width;
                LayoutSections(inner);
            }
            scroll.Resize += (s, e) => SyncWidth();
            inner.Width = 900; // initial placeholder

            // ── Section Labels ────────────────────────────────
            lblSectionStats = MakeSectionLabel("📊  Today's Overview");
            lblSectionAppt  = MakeSectionLabel("📅  Today's Appointments");
            lblSectionAnalytics = MakeSectionLabel("📈  Analytics");
            inner.Controls.Add(lblSectionStats);
            inner.Controls.Add(lblSectionAppt);
            inner.Controls.Add(lblSectionAnalytics);

            // Tag them
            lblSectionStats.Tag      = "lbl_stats";
            lblSectionAppt.Tag       = "lbl_appt";
            lblSectionAnalytics.Tag  = "lbl_analytics";

            // ── Stat cards ────────────────────────────────────
            var statsPanel = BuildStatsRow();
            inner.Controls.Add(statsPanel);

            // ── Appointments group box ────────────────────────
            var apptGroup = new SectionGroupBox("Today's Appointments");
            inner.Controls.Add(apptGroup);

            dgvAppointments = BuildGrid();
            dgvAppointments.Dock = DockStyle.Fill;
            apptGroup.InnerPanel.Controls.Add(dgvAppointments);

            // ── Bottom row ────────────────────────────────────
            var chartGroup = new SectionGroupBox("Weekly Revenue");
            inner.Controls.Add(chartGroup);

            pnlBarChart = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlBarChart.Paint += PnlBarChart_Paint;
            chartGroup.InnerPanel.Controls.Add(pnlBarChart);

            var ageGroup = new SectionGroupBox("Patient Age Groups");
            inner.Controls.Add(ageGroup);
            BuildAgeRows(ageGroup.InnerPanel);

            // Tag controls so LayoutSections can find them
            statsPanel.Tag = "stats";
            apptGroup.Tag = "appt";
            chartGroup.Tag = "chart";
            ageGroup.Tag = "age";

            // Do first layout after a tick (so scroll.ClientSize is known)
            this.Load += (s, e) => SyncWidth();
        }

        // ── Position all sections inside inner ───────────────
        private void LayoutSections(Panel inner)
        {
            int w = inner.Width;
            int cw = w - PAD * 2;    // content width
            int y = PAD;

            Control Find(string tag) =>
                inner.Controls.Cast<Control>().FirstOrDefault(c => c.Tag?.ToString() == tag);

            // ── "Today's Overview" label + Stats ──────────────
            var lblStats = Find("lbl_stats");
            if (lblStats != null) { lblStats.SetBounds(PAD, y, cw, SECTION_LBL_H); }
            y += SECTION_LBL_H + SECTION_LBL_GAP;

            var stats = Find("stats");
            if (stats != null) { stats.SetBounds(PAD, y, cw, STAT_H); }
            y += STAT_H + STAT_BOTTOM_GAP;

            // ── "Today's Appointments" label + Grid ───────────
            var lblAppt = Find("lbl_appt");
            if (lblAppt != null) { lblAppt.SetBounds(PAD, y, cw, SECTION_LBL_H); }
            y += SECTION_LBL_H + SECTION_LBL_GAP;

            var appt = Find("appt");
            if (appt != null) { appt.SetBounds(PAD, y, cw, APPT_H); }
            y += APPT_H + GAP;

            // ── "Analytics" label + Bottom row ────────────────
            var lblAnalytics = Find("lbl_analytics");
            if (lblAnalytics != null) { lblAnalytics.SetBounds(PAD, y, cw, SECTION_LBL_H); }
            y += SECTION_LBL_H + SECTION_LBL_GAP;

            // Bottom: chart 55 % | age 45 %
            int chartW = (int)(cw * 0.55) - 5;
            int ageW = cw - chartW - 10;

            var chart = Find("chart");
            if (chart != null) { chart.SetBounds(PAD, y, chartW, BOT_H); }

            var age = Find("age");
            if (age != null) { age.SetBounds(PAD + chartW + 10, y, ageW, BOT_H); }

            inner.Height = y + BOT_H + PAD;

            // Re-apply progress widths after resize
            ApplyProgress(pnlFillChildren, _pctC);
            ApplyProgress(pnlFillYouth, _pctY);
            ApplyProgress(pnlFillSeniors, _pctS);

            pnlBarChart?.Invalidate();
        }

        // ══════════════════════════════════════════════════════
        //  SECTION LABEL HELPER
        // ══════════════════════════════════════════════════════
        private Label MakeSectionLabel(string text) => new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(55, 55, 55),
            BackColor = Color.Transparent,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(2, 0, 0, 0)
        };

        // ══════════════════════════════════════════════════════
        //  TOOLBAR
        // ══════════════════════════════════════════════════════
        private Panel BuildToolbar()
        {
            var bar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 245, 245)
            };
            bar.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(Color.FromArgb(210, 210, 210)),
                    0, bar.Height - 1, bar.Width, bar.Height - 1);

            lblDate = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                Text = DateText(),
                Location = new Point(12, 13)
            };

            var btn = new Button
            {
                Text = "Refresh",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(53, 122, 189),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Height = 28,
                Width = 82,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(53, 122, 189);
            btn.FlatAppearance.BorderSize = 1;
            btn.Click += (s, e) => Refresh();
            bar.Controls.Add(lblDate);
            bar.Controls.Add(btn);
            bar.Resize += (s, e) => btn.Location = new Point(bar.Width - 94, 8);
            return bar;
        }

        // ══════════════════════════════════════════════════════
        //  STAT CARDS  (Analytics-style: FixedSingle border, no accent line)
        // ══════════════════════════════════════════════════════
        private TableLayoutPanel BuildStatsRow()
        {
            var tbl = new TableLayoutPanel
            {
                ColumnCount = 4,
                RowCount = 1,
                Height = STAT_H,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(0, 0, 0, 0),
                Margin = new Padding(0),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            for (int i = 0; i < 4; i++)
                tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            Color[] colors = {
                Color.FromArgb(46,  125, 50),
                Color.FromArgb(245, 124, 0),
                Color.FromArgb(53,  122, 189),
                Color.FromArgb(106, 27,  154)
            };
            string[] titles = { "Patients Today", "Pending", "Revenue (EGP)", "Doctors Present" };
            Label[] nums = new Label[4];

            for (int i = 0; i < 4; i++)
            {
                int idx = i;

                // Outer card — FixedSingle border, same as Analytics
                var card = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    Margin = new Padding(idx == 0 ? 0 : 5, 0, idx == 3 ? 0 : 5, 0)
                };

                // Inner TableLayoutPanel: value (65%) / subtitle (35%)
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

                var numLbl = new Label
                {
                    Text = "—",
                    Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                    ForeColor = colors[idx],
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.BottomCenter,
                    BackColor = Color.Transparent
                };
                var capLbl = new Label
                {
                    Text = titles[idx],
                    Font = new Font("Segoe UI", 8F),
                    ForeColor = Color.Gray,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.TopCenter,
                    BackColor = Color.Transparent
                };

                inner.Controls.Add(numLbl, 0, 0);
                inner.Controls.Add(capLbl, 0, 1);
                card.Controls.Add(inner);

                nums[idx] = numLbl;
                tbl.Controls.Add(card, i, 0);
            }

            lblPatients = nums[0]; lblPending = nums[1];
            lblRevenue = nums[2]; lblDoctors = nums[3];
            return tbl;
        }

        // ══════════════════════════════════════════════════════
        //  GRID
        // ══════════════════════════════════════════════════════
        private DataGridView BuildGrid()
        {
            var dgv = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BorderStyle = BorderStyle.None,
                BackgroundColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                GridColor = Color.FromArgb(225, 225, 225),
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(230, 241, 252),
                    ForeColor = Color.FromArgb(40, 40, 40),
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    SelectionBackColor = Color.FromArgb(230, 241, 252),
                    SelectionForeColor = Color.FromArgb(40, 40, 40),
                    Padding = new Padding(6, 4, 6, 4)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(40, 40, 40),
                    SelectionBackColor = Color.FromArgb(184, 212, 240),
                    SelectionForeColor = Color.Black,
                    Padding = new Padding(6, 3, 6, 3)
                },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 250, 255)
                },
                ColumnHeadersHeight = 30,
                MultiSelect = false
            };
            dgv.RowTemplate.Height = 28;

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colID", HeaderText = "#", FillWeight = 6 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPat", HeaderText = "Patient", FillWeight = 24 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDoc", HeaderText = "Doctor", FillWeight = 24 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTime", HeaderText = "Time", FillWeight = 11 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colFee", HeaderText = "Fee", FillWeight = 11 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "Status", FillWeight = 16 });

            dgv.CellPainting += GridCellPainting;
            return dgv;
        }

        private void GridCellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.ColumnIndex != 5 || e.RowIndex < 0) return;
            e.PaintBackground(e.ClipBounds, true);

            string status = e.Value?.ToString() ?? "";
            Color bg, fg, border;
            switch (status)
            {
                case "Completed": bg = Color.FromArgb(209, 236, 214); fg = Color.FromArgb(21, 87, 36); border = Color.FromArgb(40, 167, 69); break;
                case "Cancelled": bg = Color.FromArgb(248, 215, 218); fg = Color.FromArgb(114, 28, 36); border = Color.FromArgb(220, 53, 69); break;
                default: bg = Color.FromArgb(255, 243, 205); fg = Color.FromArgb(130, 96, 0); border = Color.FromArgb(255, 193, 7); break;
            }

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int px = 8, py = 5;
            var r = new Rectangle(e.CellBounds.X + px, e.CellBounds.Y + py,
                                   e.CellBounds.Width - px * 2, e.CellBounds.Height - py * 2);
            using (var b = new SolidBrush(bg)) g.FillRectangle(b, r);
            using (var p = new Pen(border, 1f)) g.DrawRectangle(p, r);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using (var b = new SolidBrush(fg))
                g.DrawString(status, new Font("Segoe UI", 8.5F, FontStyle.Bold), b, r, sf);
            e.Handled = true;
        }

        // ══════════════════════════════════════════════════════
        //  BAR CHART
        // ══════════════════════════════════════════════════════
        private void PnlBarChart_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            string[] days = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            int n = Math.Min(_weeklyRevenue?.Count ?? 0, 7);
            if (n == 0) return;

            decimal maxV = _weeklyRevenue.Take(n).DefaultIfEmpty(1).Max();
            if (maxV == 0) maxV = 1;

            int todayIdx = ((int)DateTime.Today.DayOfWeek + 6) % 7;
            int padX = 14, padTop = 10, labelH = 20;
            int chartH = pnlBarChart.Height - padTop - labelH - 4;
            int chartW = pnlBarChart.Width - padX * 2;
            int barW = Math.Max(chartW / n - 8, 6);
            int totalBarW = barW * n;
            int spacing = (chartW - totalBarW) / (n + 1);

            // Subtle grid lines
            using var gridPen = new Pen(Color.FromArgb(235, 235, 235));
            for (int gi = 1; gi <= 4; gi++)
            {
                int gy = padTop + (int)(chartH * (1 - gi / 4.0));
                g.DrawLine(gridPen, padX, gy, padX + chartW, gy);
            }

            for (int i = 0; i < n; i++)
            {
                int barH = (int)(chartH * (_weeklyRevenue[i] / maxV));
                int x = padX + spacing + i * (barW + spacing);
                int y = padTop + chartH - barH;
                bool hi = i == todayIdx;

                // Bar gradient
                var barRect = new Rectangle(x, y, barW, barH);
                if (barH > 0)
                {
                    Color c1 = hi ? Color.FromArgb(26, 90, 154) : Color.FromArgb(74, 144, 217);
                    Color c2 = hi ? Color.FromArgb(40, 110, 180) : Color.FromArgb(53, 122, 189);
                    using var brush = new LinearGradientBrush(barRect, c1, c2, LinearGradientMode.Vertical);
                    g.FillRectangle(brush, barRect);

                    // Revenue label above bar (only if enough room)
                    if (barH > 20 && _weeklyRevenue[i] > 0)
                    {
                        string val = _weeklyRevenue[i] >= 1000
                            ? (_weeklyRevenue[i] / 1000M).ToString("0.#") + "k"
                            : _weeklyRevenue[i].ToString("0");
                        using var vFont = new Font("Segoe UI", 7F, FontStyle.Bold);
                        using var vBrush = new SolidBrush(Color.White);
                        var vsf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };
                        g.DrawString(val, vFont, vBrush, new RectangleF(x, y + 3, barW, 14), vsf);
                    }
                }

                // Day label
                using var lFont = new Font("Segoe UI", 8F, hi ? FontStyle.Bold : FontStyle.Regular);
                using var lBrush = new SolidBrush(hi ? Color.FromArgb(40, 40, 40) : Color.FromArgb(130, 130, 130));
                var lsf = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString(days[i], lFont, lBrush,
                    new RectangleF(x, pnlBarChart.Height - labelH, barW, labelH), lsf);
            }
        }

        // ══════════════════════════════════════════════════════
        //  AGE GROUP PROGRESS BARS
        // ══════════════════════════════════════════════════════
        private void BuildAgeRows(Panel parent)
        {
            parent.BackColor = Color.White;
            parent.Padding = new Padding(10, 8, 10, 8);

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.White,
                Padding = new Padding(0)
            };
            parent.Controls.Add(flow);

            var (pbC, lC) = MakeProgressRow(flow, "Children (0\u201315)", Color.FromArgb(92, 184, 92));
            var (pbY, lY) = MakeProgressRow(flow, "Youth (16\u201340)", Color.FromArgb(53, 122, 189));
            var (pbS, lS) = MakeProgressRow(flow, "Seniors (41+)", Color.FromArgb(217, 83, 79));

            pnlFillChildren = pbC; lblChildrenPct = lC;
            pnlFillYouth = pbY; lblYouthPct = lY;
            pnlFillSeniors = pbS; lblSeniorsPct = lS;
        }

        private (Panel fill, Label pct) MakeProgressRow(FlowLayoutPanel parent, string title, Color color)
        {
            // Row wrapper
            var row = new Panel
            {
                BackColor = Color.White,
                Height = 52,
                Margin = new Padding(0, 0, 0, 4)
            };
            parent.Controls.Add(row);

            // Keep row width = parent width
            parent.Resize += (s, e) => row.Width = parent.ClientSize.Width;

            // Title + pct header
            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(60, 60, 60),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Height = 20,
                Location = new Point(0, 0)
            };
            var lblPct = new Label
            {
                Text = "0%",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
                Height = 20,
                Location = new Point(0, 0)
            };
            row.Controls.Add(lblTitle);
            row.Controls.Add(lblPct);

            // Track panel
            var track = new Panel
            {
                BackColor = Color.FromArgb(230, 230, 230),
                Height = 16,
                Location = new Point(0, 24),
                BorderStyle = BorderStyle.None
            };
            // Coloured fill
            var fill = new Panel { Dock = DockStyle.Left, Width = 0, BackColor = color };
            track.Controls.Add(fill);
            row.Controls.Add(track);

            // Sync widths on row resize
            row.Resize += (s, e) =>
            {
                int rw = row.ClientSize.Width;
                lblTitle.Width = (int)(rw * 0.75);
                lblTitle.Location = new Point(0, 0);
                lblPct.Width = (int)(rw * 0.25);
                lblPct.Location = new Point(lblTitle.Right, 0);
                track.SetBounds(0, 24, rw, 16);
            };

            return (fill, lblPct);
        }

        // Apply a percentage to a progress fill panel
        private void ApplyProgress(Panel fill, int pct)
        {
            if (fill == null) return;
            var track = fill.Parent;
            if (track == null || track.Width <= 0) return;
            fill.Width = (int)(track.Width * Math.Max(0, Math.Min(100, pct)) / 100.0);
        }

        private void SetAllProgress(int pC, int pY, int pS)
        {
            _pctC = pC; _pctY = pY; _pctS = pS;
            lblChildrenPct.Text = pC + "%";
            lblYouthPct.Text = pY + "%";
            lblSeniorsPct.Text = pS + "%";
            ApplyProgress(pnlFillChildren, pC);
            ApplyProgress(pnlFillYouth, pY);
            ApplyProgress(pnlFillSeniors, pS);
        }

        // ══════════════════════════════════════════════════════
        //  DATA
        // ══════════════════════════════════════════════════════
        private void LoadData()
        {
            try
            {
                var analytics = new AnalyticsService(
                    _clinicService.GetAppointments(),
                    _clinicService.GetPayments(),
                    _clinicService.GetPatients(),
                    _clinicService.GetDoctors()
                );

                // Stat cards
                lblPatients.Text = analytics.GetTodayTotalPatientsCount().ToString();
                lblPending.Text = analytics.GetTodayPendingAppointmentsCount().ToString();
                lblRevenue.Text = analytics.GetTodayTotalRevenue().ToString("N0");
                lblDoctors.Text = analytics.GetTodayPresentDoctorsCount().ToString();

                // Grid
                dgvAppointments.Rows.Clear();
                foreach (var a in _clinicService.GetTodayAppointments())
                    dgvAppointments.Rows.Add(
                        a.ID,
                        a.AppointmentPatient.Name,
                        "Dr. " + a.AppointmentDoctor.Name,
                        a.AppointmentDate.ToString("HH:mm"),
                        a.Fee.ToString("N0"),
                        a.Status.ToString());

                // Chart
                _weeklyRevenue = BuildWeeklyRevenue(_clinicService.GetPayments());
                pnlBarChart?.Invalidate();

                // Age demographics
                var demo = analytics.GetPatientAgeDemographics();
                int total = demo.Values.Sum();
                if (total > 0)
                {
                    var v = demo.Values.ToList();
                    int pC = (int)Math.Round(v[0] * 100.0 / total);
                    int pY = (int)Math.Round(v[1] * 100.0 / total);
                    int pS = 100 - pC - pY;
                    SetAllProgress(pC, pY, pS);
                }
                else
                    SetAllProgress(0, 0, 0);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading dashboard:\n" + ex.Message,
                    "Dashboard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private List<decimal> BuildWeeklyRevenue(List<Payment> payments)
        {
            var result = new List<decimal>();
            int offset = ((int)DateTime.Today.DayOfWeek + 6) % 7;
            var weekStart = DateTime.Today.AddDays(-offset);
            for (int i = 0; i < 7; i++)
            {
                var day = weekStart.AddDays(i);
                result.Add(payments
                    .Where(p => p.PaymentDate.Date == day && p.Status == PaymentStatus.Paid)
                    .Sum(p => p.Amount));
            }
            return result;
        }

        // ── Public Refresh ────────────────────────────────────
        public new void Refresh()
        {
            lblDate.Text = DateText();
            LoadData();
        }

        private string DateText() =>
            $"Dashboard \u2014 Today: {DateTime.Today:dddd d MMM yyyy}";
    }

    // ══════════════════════════════════════════════════════════
    //  Helper: custom GroupBox with inner content panel
    // ══════════════════════════════════════════════════════════
    internal class SectionGroupBox : Panel
    {
        public Panel InnerPanel { get; }

        public SectionGroupBox(string title)
        {
            this.BackColor = Color.White;
            this.Padding = new Padding(0);

            InnerPanel = new Panel
            {
                BackColor = Color.White,
                Dock = DockStyle.Fill,
                Padding = new Padding(4, 20, 4, 4)
            };
            this.Controls.Add(InnerPanel);

            string _title = title;
            this.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var borderRect = new Rectangle(0, 10, this.Width - 1, this.Height - 11);
                g.DrawRectangle(new Pen(Color.FromArgb(190, 190, 190)), borderRect);

                string txt = $"  {_title}  ";
                using var f = new Font("Segoe UI", 9F, FontStyle.Bold);
                SizeF sz = g.MeasureString(txt, f);
                g.FillRectangle(new SolidBrush(Color.White), 10, 0, sz.Width, 20);
                g.DrawString(txt, f, new SolidBrush(Color.FromArgb(85, 85, 85)), 10, 1);
            };
        }
    }
}
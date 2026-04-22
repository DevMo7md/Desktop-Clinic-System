using System;
using System.Collections.Generic;
using System.Linq;
using Clinic_Management_System.Models;

namespace Clinic_Management_System.Services
{
    public class AnalyticsService
    {
        private readonly List<Appointment> _appointments;
        private readonly List<Payment> _payments;
        private readonly List<Patient> _patients;
        private readonly List<Doctor> _doctors;

        public AnalyticsService(List<Appointment> appointments, List<Payment> payments, List<Patient> patients, List<Doctor> doctors)
        {
            _appointments = appointments;
            _payments = payments;
            _patients = patients;
            _doctors = doctors;
        }

        #region Home Dashboard

        public int GetTodayTotalPatientsCount()
        {
            return _appointments
                .Where(a => a.AppointmentDate.Date == DateTime.Today && a.Status != AppointmentStatus.Cancelled)
                .Select(a => a.AppointmentPatient.ID)
                .Distinct()
                .Count();
        }

        public int GetTodayPendingAppointmentsCount()
        {
            return _appointments
                .Count(a => a.AppointmentDate.Date == DateTime.Today && a.Status == AppointmentStatus.Pending);
        }

        public decimal GetTodayTotalRevenue()
        {
            return _payments
                .Where(p => p.PaymentDate.Date == DateTime.Today && p.Status == PaymentStatus.Paid)
                .Sum(p => p.Amount);
        }

        public int GetTodayPresentDoctorsCount()
        {
            return _appointments
                .Where(a => a.AppointmentDate.Date == DateTime.Today && a.Status != AppointmentStatus.Cancelled)
                .Select(a => a.AppointmentDoctor.ID)
                .Distinct()
                .Count();
        }

        #endregion

        #region Financial Analytics

        public decimal GetNetRevenue(DateTime start, DateTime end)
        {
            return _payments
                .Where(p => p.PaymentDate.Date >= start.Date && p.PaymentDate.Date <= end.Date)
                .Sum(p => p.Status == PaymentStatus.Paid ? p.Amount : -p.Amount);
        }

        public double GetMonthlyGrowthRate()
        {
            var now = DateTime.Today;
            var currentMonthStart = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = currentMonthStart.AddMonths(-1);
            var lastMonthEnd = currentMonthStart.AddDays(-1);

            decimal currentMonthRevenue = GetNetRevenue(currentMonthStart, now);
            decimal lastMonthRevenue = GetNetRevenue(lastMonthStart, lastMonthEnd);

            if (lastMonthRevenue == 0) return currentMonthRevenue > 0 ? 100 : 0;

            return (double)((currentMonthRevenue - lastMonthRevenue) / lastMonthRevenue) * 100;
        }

        #endregion

        #region Insights

        public int GetPeakHour()
        {
            if (!_appointments.Any()) return 0;
            return _appointments
                .GroupBy(a => a.AppointmentDate.Hour)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();
        }

        public Dictionary<string, int> GetPatientAgeDemographics()
        {
            return new Dictionary<string, int>
            {
                { "أطفال (0-15)", _patients.Count(p => p.Age <= 15) },
                { "شباب (16-40)", _patients.Count(p => p.Age > 15 && p.Age <= 40) },
                { "كبار سن (41+)", _patients.Count(p => p.Age > 40) }
            };
        }

        #endregion
    }
}
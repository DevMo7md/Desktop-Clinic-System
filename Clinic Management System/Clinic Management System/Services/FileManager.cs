using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Clinic_Management_System.Models;

namespace Clinic_Management_System.Services
{
    public class FileManager
    {
        private const string DoctorFile = "doctors.txt";
        private const string PatientFile = "patients.txt";
        private const string RecordFile = "medical_records.txt";
        private const string ChronicFile = "chronic_diseases.txt";
        private const string AppointmentFile = "appointments.txt";
        private const string PaymentFile = "payments.txt";

        public void SaveData(List<Doctor> doctors, List<Patient> patients, List<Appointment> appointments, List<Payment> payments)
        {
            try
            {

                File.WriteAllLines(DoctorFile, doctors.Select(d => $"{d.ID};{d.Name};{d.Age};{d.PersonGender};{d.Specialty}"));

                List<string> pLines = new List<string>();
                List<string> rLines = new List<string>();
                List<string> cLines = new List<string>();

                foreach (var p in patients)
                {
                    pLines.Add($"{p.ID};{p.Name};{p.Age};{p.PersonGender}");
                    foreach (var r in p.medical_record)
                        rLines.Add($"{p.ID};{r.ID};{r.Diagnosis};{r.Treatment};{r.Date}");
                    foreach (var c in p.chronic_diseases)
                        cLines.Add($"{p.ID};{c.ID};{c.Name};{c.Notes}");
                }

                File.WriteAllLines(PatientFile, pLines);
                File.WriteAllLines(RecordFile, rLines);
                File.WriteAllLines(ChronicFile, cLines);

                File.WriteAllLines(AppointmentFile, appointments.Select(a =>
                    $"{a.ID};{a.AppointmentPatient.ID};{a.AppointmentDoctor.ID};{a.AppointmentDate};{a.Status};{a.Fee}"));

                File.WriteAllLines(PaymentFile, payments.Select(p =>
                    $"{p.ID};{p.PaymentAppointment.ID};{p.Amount};{p.PaymentDate};{p.Status}"));

                }
                catch (IOException)
                {
                    throw new Exception("عذراً، لا يمكن حفظ البيانات لأن أحد الملفات مفتوح في برنامج آخر. يرجى إغلاق ملفات الـ txt وحاول مجدداً.");
                }
                catch (Exception ex)
                {
                    throw new Exception($"حدث خطأ غير متوقع أثناء الحفظ: {ex.Message}");
                }
        }

        public void LoadData(out List<Doctor> doctors, out List<Patient> patients, out List<Appointment> appointments, out List<Payment> payments)
        {
            doctors = new List<Doctor>();
            patients = new List<Patient>();
            appointments = new List<Appointment>();
            payments = new List<Payment>();

            if (File.Exists(DoctorFile))
            {
                foreach (var line in File.ReadAllLines(DoctorFile))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(';');
                    if (parts.Length == 5 && int.TryParse(parts[0], out int id) &&
                        int.TryParse(parts[2], out int age) &&
                        Enum.TryParse(parts[3], out Gender gender))
                    {
                        var d = new Doctor(parts[1], age, gender, parts[4]);
                        d.LoadID(id);
                        doctors.Add(d);
                    }
                }
                if (doctors.Any()) Doctor.UpdateCounter(doctors.Max(d => d.ID));
            }

            if (File.Exists(PatientFile))
            {
                foreach (var line in File.ReadAllLines(PatientFile))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(';');
                    if (parts.Length == 4 && int.TryParse(parts[0], out int id) &&
                                            int.TryParse(parts[2], out int age) &&
                                            Enum.TryParse(parts[3], out Gender gender))
                    {
                        var p = new Patient(parts[1], age, gender);
                        p.LoadID(id);
                        patients.Add(p);
                    }
                }
            }

            if (File.Exists(RecordFile) && patients.Any())
            {
                int maxId = 0;
                foreach (var line in File.ReadAllLines(RecordFile))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(';');
                    if (parts.Length == 5 && int.TryParse(parts[0], out int pId) &&
                        int.TryParse(parts[1], out int rId) &&
                        DateTime.TryParse(parts[4], out DateTime date))
                    {
                        var owner = patients.FirstOrDefault(p => p.ID == pId);
                        if (owner != null)
                        {
                            var r = new MedicalRecord(parts[2], parts[3], date);
                            r.LoadID(rId);
                            owner.medical_record.Add(r);
                            maxId = Math.Max(maxId, rId);
                        }
                    }
                }
                MedicalRecord.UpdateCounter(maxId);
            }

            if (File.Exists(ChronicFile) && patients.Any())
            {
                int maxId = 0;
                foreach (var line in File.ReadAllLines(ChronicFile))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(';');
                    if (parts.Length == 4 && int.TryParse(parts[0], out int pId) && int.TryParse(parts[1], out int cId))
                    {
                        var owner = patients.FirstOrDefault(p => p.ID == pId);
                        if (owner != null)
                        {
                            var c = new ChronicDisease(parts[2], parts[3]);
                            c.LoadID(cId);
                            owner.chronic_diseases.Add(c);
                            maxId = Math.Max(maxId, cId);
                        }
                    }
                }
                ChronicDisease.UpdateCounter(maxId);
            }

            if (patients.Any()) Patient.UpdateCounter(patients.Max(p => p.ID));

            if (File.Exists(AppointmentFile))
            {
                foreach (var line in File.ReadAllLines(AppointmentFile))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(';');
                    if (parts.Length == 6 && int.TryParse(parts[0], out int id) &&
                        int.TryParse(parts[1], out int pId) &&
                        int.TryParse(parts[2], out int dId) &&
                        DateTime.TryParse(parts[3], out DateTime date) &&
                        Enum.TryParse(parts[4], out AppointmentStatus status) &&
                        decimal.TryParse(parts[5], out decimal fee))
                    {
                        var patient = patients.FirstOrDefault(p => p.ID == pId);
                        var doctor = doctors.FirstOrDefault(d => d.ID == dId);
                        if (patient != null && doctor != null)
                        {
                            var a = new Appointment(patient, doctor, fee, date);
                            a.LoadID(id);
                            a.Status = status;
                            appointments.Add(a);
                        }
                    }
                }
                if (appointments.Any()) Appointment.UpdateCounter(appointments.Max(a => a.ID));
            }

            if (File.Exists(PaymentFile))
            {
                foreach (var line in File.ReadAllLines(PaymentFile))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(';');
                    if (parts.Length == 5 && int.TryParse(parts[0], out int id) &&
                        int.TryParse(parts[1], out int appId) &&
                        decimal.TryParse(parts[2], out decimal amount) &&
                        DateTime.TryParse(parts[3], out DateTime date) &&
                        Enum.TryParse(parts[4], out PaymentStatus status))
                    {
                        var app = appointments.FirstOrDefault(a => a.ID == appId);
                        if (app != null)
                        {
                            var pay = new Payment(app, amount, date, status);
                            pay.LoadID(id);
                            payments.Add(pay);
                        }
                    }
                }
                if (payments.Any()) Payment.UpdateCounter(payments.Max(p => p.ID));
            }
        }
    }
}
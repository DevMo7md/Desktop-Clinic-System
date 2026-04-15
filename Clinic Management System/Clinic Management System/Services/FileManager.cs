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
                $"{p.ID};{p.PaymentAppointment.ID};{p.Amount};{p.PaymentDate}"));
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
                    var parts = line.Split(';');
                    var d = new Doctor(parts[1], int.Parse(parts[2]), (Gender)Enum.Parse(typeof(Gender), parts[3]), parts[4]);
                    d.LoadID(int.Parse(parts[0]));
                    doctors.Add(d);
                }
                if (doctors.Any()) Doctor.UpdateCounter(doctors.Max(d => d.ID));
            }

            if (File.Exists(PatientFile))
            {
                foreach (var line in File.ReadAllLines(PatientFile))
                {
                    var parts = line.Split(';');
                    var p = new Patient(parts[1], int.Parse(parts[2]), (Gender)Enum.Parse(typeof(Gender), parts[3]));
                    p.LoadID(int.Parse(parts[0]));
                    patients.Add(p);
                }
            }

            if (File.Exists(RecordFile) && patients.Any())
            {
                int maxId = 0;
                foreach (var line in File.ReadAllLines(RecordFile))
                {
                    var parts = line.Split(';');
                    var owner = patients.FirstOrDefault(p => p.ID == int.Parse(parts[0]));
                    if (owner != null)
                    {
                        var r = new MedicalRecord(parts[2], parts[3], DateTime.Parse(parts[4]));
                        r.LoadID(int.Parse(parts[1]));
                        owner.medical_record.Add(r);
                        maxId = Math.Max(maxId, r.ID);
                    }
                }
                MedicalRecord.UpdateCounter(maxId);
            }

            if (File.Exists(ChronicFile) && patients.Any())
            {
                int maxId = 0;
                foreach (var line in File.ReadAllLines(ChronicFile))
                {
                    var parts = line.Split(';');
                    var owner = patients.FirstOrDefault(p => p.ID == int.Parse(parts[0]));
                    if (owner != null)
                    {
                        var c = new ChronicDisease(parts[2], parts[3]);
                        c.LoadID(int.Parse(parts[1]));
                        owner.chronic_diseases.Add(c);
                        maxId = Math.Max(maxId, c.ID);
                    }
                }
                ChronicDisease.UpdateCounter(maxId);
            }

            if (patients.Any()) Patient.UpdateCounter(patients.Max(p => p.ID));

            if (File.Exists(AppointmentFile))
            {
                foreach (var line in File.ReadAllLines(AppointmentFile))
                {
                    var parts = line.Split(';');
                    var patient = patients.FirstOrDefault(p => p.ID == int.Parse(parts[1]));
                    var doctor = doctors.FirstOrDefault(d => d.ID == int.Parse(parts[2]));
                    if (patient != null && doctor != null)
                    {
                        var a = new Appointment(patient, doctor, decimal.Parse(parts[5]), DateTime.Parse(parts[3]));
                        a.LoadID(int.Parse(parts[0]));
                        a.Status = (AppointmentStatus)Enum.Parse(typeof(AppointmentStatus), parts[4]);
                        appointments.Add(a);
                    }
                }
                if (appointments.Any()) Appointment.UpdateCounter(appointments.Max(a => a.ID));
            }

            if (File.Exists(PaymentFile))
            {
                foreach (var line in File.ReadAllLines(PaymentFile))
                {
                    var parts = line.Split(';');
                    var app = appointments.FirstOrDefault(a => a.ID == int.Parse(parts[1]));
                    if (app != null)
                    {
                        var pay = new Payment(app, decimal.Parse(parts[2]), DateTime.Parse(parts[3]));
                        pay.LoadID(int.Parse(parts[0]));
                        payments.Add(pay);
                    }
                }
                if (payments.Any()) Payment.UpdateCounter(payments.Max(p => p.ID));
            }
        }
    }
}
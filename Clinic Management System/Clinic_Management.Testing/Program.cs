using Clinic_Management_System.Models;
using Clinic_Management_System.Services;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;

namespace Clinic_Management.Testing
{
    class Program
    {
        static void Main(string[] args)
        {
            
            FileManager fileManager = new FileManager();
            List<Doctor> doctors = new List<Doctor>();
            List<Patient> patients = new List<Patient>();
            List<Appointment> appointments = new List<Appointment>();
            List<Payment> payments = new List<Payment>();

            Console.WriteLine("=== Start Smoke Testing ===\n");

            // 2.  (Create)
            Doctor doc = new Doctor("Dr. Mansour", 45, Gender.Male, "Dentist");
            Patient pat = new Patient("Ahmed Ali", 22, Gender.Male);

            pat.medical_record.Add(new MedicalRecord("Tooth Extraction", "Painkillers", DateTime.Now));
            pat.chronic_diseases.Add(new ChronicDisease("Diabetes", "Type 2"));

            Appointment app = new Appointment(pat, doc, 500.0m, DateTime.Now);

            Payment pay = new Payment(app, 500.0m, DateTime.Now);

            doctors.Add(doc);
            patients.Add(pat);
            appointments.Add(app);
            payments.Add(pay);

            Console.WriteLine("Step 1: Saving data to text files...");
            fileManager.SaveData(doctors, patients, appointments, payments);
            Console.WriteLine("✔ Data Saved Successfully.\n");

            doctors = new List<Doctor>();
            patients = new List<Patient>();
            appointments = new List<Appointment>();
            payments = new List<Payment>();
            Console.WriteLine("Step 2: Memory Cleared (Lists are now empty).\n");

            Console.WriteLine("Step 3: Loading data back from files...");
            fileManager.LoadData(out doctors, out patients, out appointments, out payments);
            Console.WriteLine("✔ Data Loaded Successfully.\n");

            Console.WriteLine("=== Final Results ===");
            Console.WriteLine($"Doctors Count: {doctors.Count} (Expected: 1)");
            Console.WriteLine($"Patients Count: {patients.Count} (Expected: 1)");
            Console.WriteLine($"Appointments Count: {appointments.Count} (Expected: 1)");

            if (appointments.Count > 0)
            {
                var loadedApp = appointments[0];
                Console.WriteLine($"\nTesting Relationship Integrity:");
                Console.WriteLine($"- Appointment ID: {loadedApp.ID}");
                Console.WriteLine($"- Patient Name: {loadedApp.AppointmentPatient.Name}"); // هنا التأكد من الربط
                Console.WriteLine($"- Doctor Name: {loadedApp.AppointmentDoctor.Name}");   // هنا التأكد من الربط
                Console.WriteLine($"- Fee: {loadedApp.Fee} EGP");
            }

            if (patients.Count > 0 && patients[0].medical_record.Count > 0)
            {
                Console.WriteLine($"\nTesting Sub-Lists:");
                Console.WriteLine($"- Patient Medical Records: {patients[0].medical_record.Count}");
                Console.WriteLine($"- Diagnosis: {patients[0].medical_record[0].Diagnosis}");
            }

            Console.WriteLine("\n=== End of Test ===");
            Console.ReadLine();
        }
    }
}
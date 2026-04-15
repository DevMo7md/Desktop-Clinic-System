using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic_Management_System.Models
{
    public class Appointment
    {
        private static int _idCounter = 0;
        public int ID { get; private set; }
        public Patient AppointmentPatient { get; set; }
        public Doctor AppointmentDoctor { get; set; }
        public DateTime AppointmentDate { get; set; }
        public AppointmentStatus Status { get; set; }
        public decimal Fee { get; set; }

        public Appointment(Patient patient, Doctor doctor, decimal fee)
        {
            ID = ++_idCounter;
            AppointmentPatient = patient;
            AppointmentDoctor = doctor;
            AppointmentDate = DateTime.Now;
            Status = AppointmentStatus.Pending;
            Fee = fee;
        }

        public Appointment(Patient patient, Doctor doctor,decimal fee, DateTime date)
        {
            ID = ++_idCounter;
            AppointmentPatient = patient;
            AppointmentDoctor = doctor;
            AppointmentDate = date;
            Status = AppointmentStatus.Pending;
            Fee = fee;
        }

        public string GetAppointmentDetails()
        {
            return $"Patient: {AppointmentPatient.Name} | Doctor: {AppointmentDoctor.Name} | Date: {AppointmentDate}";
        }
        public void LoadID(int id)
        {
            ID = id;
        }
        // to update the counter if program closed
        public static void UpdateCounter(int lastId)
        {
            _idCounter = lastId;
        }
    }
}

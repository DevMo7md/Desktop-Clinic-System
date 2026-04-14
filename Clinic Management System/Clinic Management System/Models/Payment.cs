using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic_Management_System.Models
{
    public class Payment
    {
        private static int _idCounter = 0;
        public int ID { get; private set; }
        public required Appointment PaymentAppointment { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }

        public Payment(Appointment appointment, decimal amount)
        {
            ID = ++_idCounter;
            PaymentAppointment = appointment;
            Amount = amount;
            PaymentDate = DateTime.Now;
        }
        public Payment(Appointment appointment, decimal amount, DateTime paymentDate)
        {
            ID = ++_idCounter;
            PaymentAppointment = appointment;
            Amount = amount;
            PaymentDate = paymentDate;
        }

        public string GetPaymentDetails()
        {
            return $"Payment ID: {ID} | Appointment: {PaymentAppointment.GetAppointmentDetails()} | Amount: {Amount} | Date: {PaymentDate}";
        }

        // to update the counter if program closed
        public static void UpdateCounter(int lastId)
        {
            _idCounter = lastId;
        }
    }
}
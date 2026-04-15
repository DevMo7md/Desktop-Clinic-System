using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic_Management_System.Models
{
    public class Person
    {
        private static int _idCounter = 0;
        public int ID { get; private set; }
        public string Name { get; set; }
        private int age;
        public Gender PersonGender { get; set; }
        public int Age
        {
            get { return age; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("Age cannot be negative or zero .");
                }
                age = value;
            }
        }
        public Person(string name, int age, Gender gender)
        {
            ID = ++_idCounter;
            Name = name;
            Age = age;
            PersonGender = gender;
        }

        public void LoadID(int id) 
        { 
            ID = id; 
        }

        public virtual string DisplayInfo()
        {
            return $"Name : {Name}, Age: {Age}, Gander: {PersonGender}";
        }

        // to update the counter if program closed 
        public static void UpdateCounter(int lastId)
        {
            _idCounter = lastId;
        }
    }
}

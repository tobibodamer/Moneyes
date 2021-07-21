﻿using System;

namespace MoneyesParser
{
    class Sale : ISale
    {
        public DateTime PaymentDate { get; init; }

        public DateTime BookingDate { get; init; }

        public decimal Amount { get; init; }

        public string City { get; init; }

        public string Country { get; init; }

        public string Name { get; init; }

        public SaleType SaleType { get; init; }

        public string BookingType { get; init; }

        public string IntendedUse { get; init; }

        public override bool Equals(object obj)
        {
            return obj is Sale sale &&
                   PaymentDate == sale.PaymentDate &&
                   Amount == sale.Amount &&
                   City == sale.City &&
                   Country == sale.Country &&
                   Name == sale.Name &&
                   SaleType == sale.SaleType &&
                   BookingType == sale.BookingType;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PaymentDate, Amount, City, Country, Name, SaleType, BookingType);
        }

        public override string ToString()
        {
            string date = PaymentDate == default ? $"{BookingDate:d}" : PaymentDate.ToShortDateString();

            return string.Format("{0, -40} | {1, -40} | {2, 10:C} | {3, -10} | {4, -20}", 
                Name.Substring(0, Math.Min(Name.Length, 40)), 
                IntendedUse.Substring(0, Math.Min(IntendedUse.Length, 40)), 
                Amount, date, BookingType, City);
            //return $"{Name}   {Amount:C}   {PaymentDate:d}   {BookingType}   {City}";
        }

        
    }


}

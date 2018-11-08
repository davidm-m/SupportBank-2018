using System;
using NLog;

namespace SupportBank
{
    internal class Transaction
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
        public DateTime Date;
        public string FromAccount;
        public string ToAccount;
        public string Narrative;
        public float Amount;

        public Transaction(DateTime date, string from, string to, string narrative, float amount)
        {
            Date = date;
            FromAccount = from;
            ToAccount = to;
            Narrative = narrative;
            Amount = amount;
        }

        public override string ToString()
        {
            return $"Date: {Date:d}, from: {FromAccount}, to: {ToAccount}, narrative: {Narrative}, amount: {Amount:0.00}";
        }
    }
}
using NLog;

namespace SupportBank
{
    internal class Account
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
        public string Name;
        public float Credit = 0;

        public Account(string name)
        {
            Name = name;
        }

        public void ProcessTransaction(Transaction transaction)
        {
            if (Name == transaction.FromAccount)
            {
                Credit -= transaction.Amount;
            }
            else if (Name == transaction.ToAccount)
            {
                Credit += transaction.Amount;
            }
        }

        public override string ToString()
        {
            return $"Name: {Name}, credit: {Credit:0.00}";
        }
    }
}
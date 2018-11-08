using System.Collections.Generic;
using NLog;

namespace SupportBank
{
    internal static class AccountProcessor
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        public static List<Account> ProcessAccounts(List<Transaction> transactions)
        {
            var accounts = new List<Account>();
            logger.Debug("Starting account processing");

            foreach (var t in transactions)
            {
                if (accounts.Find(a => a.Name == t.FromAccount) == null)
                {
                    var newAccount = new Account(t.FromAccount);
                    newAccount.ProcessTransaction(t);
                    accounts.Add(newAccount);
                }
                else
                {
                    accounts.Find(a => a.Name == t.FromAccount).ProcessTransaction(t);
                }

                if (accounts.Find(a => a.Name == t.ToAccount) == null)
                {
                    var newAccount = new Account(t.ToAccount);
                    newAccount.ProcessTransaction(t);
                    accounts.Add(newAccount);
                }
                else
                {
                    accounts.Find(a => a.Name == t.ToAccount).ProcessTransaction(t);
                }
            }
            logger.Debug("Finished account processing");
            return accounts;
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Transactions;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace SupportBank
{
    class Program
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            var config = new LoggingConfiguration();
            var target = new FileTarget { FileName = @"C:\Work\Logs\SupportBank.log", Layout = @"${longdate} ${level} - ${logger}: ${message}" };
            config.AddTarget("File Logger", target);
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, target));
            LogManager.Configuration = config;

            logger.Debug("Program started");

            var transactions = new List<Transaction>();
            var accounts = new List<Account>();

            const string filename = "DodgyTransactions2015.csv";
            logger.Debug("Opening file " + filename);
            using (var sr = new StreamReader("C:\\Work\\Training\\SupportBank-2018\\" + filename))
            {
                logger.Debug("File opened");
                sr.ReadLine(); //we shouldn't need the first line
                string line;
                var count = 1;
                while (((line = sr.ReadLine()) != null))
                {
                    count++;
                    var entries = line.Split(',');
                    if (entries.Length > 5)
                    {
                        Console.WriteLine("Line " + count + " of " + filename + "has more entries than expected");
                        logger.Warn("Line " + count + " of " + filename + "has more entries than expected, tentatively reading entry anyway");
                    }
                    else if (entries.Length < 5)
                    {
                        Console.WriteLine("Line " + count + " of " + filename + "has fewer entries than expected - entry has been ignored");
                        logger.Error("Line " + count + " of " + filename + "has fewer entries than expected, skipping entry");
                        continue; //skip this entry as it cannot be readable
                    }

                    if (!DateTime.TryParse(entries[0], out var date))
                    {
                        Console.WriteLine("Improperly formatted date on line " + count + " of " + filename + " - entry has been ignored");
                        logger.Error("Improperly formatted date on line " + count + " of " + filename + ", skipping entry");
                        continue;
                    }
                    if (!float.TryParse(entries[4], out var amount))
                    {
                        Console.WriteLine("Improperly formatted amount on line " + count + " of " + filename + " - entry has been ignored");
                        logger.Error("Improperly formatted amount on line " + count + " of " + filename + ", skipping entry");
                        continue;
                    }
                    transactions.Add(new Transaction(date, entries[1], entries[2], entries[3], amount));
                }
                logger.Debug("File fully read");
            }

            logger.Debug("Starting account processing");

            foreach (var t in transactions)
            {
                if (accounts.Find(a => a.Name == t.From) == null)
                {
                    var newAccount = new Account(t.From);
                    newAccount.ProcessTransaction(t);
                    accounts.Add(newAccount);
                }
                else
                {
                    accounts.Find(a => a.Name == t.From).ProcessTransaction(t);
                }

                if (accounts.Find(a => a.Name == t.To) == null)
                {
                    var newAccount = new Account(t.To);
                    newAccount.ProcessTransaction(t);
                    accounts.Add(newAccount);
                }
                else
                {
                    accounts.Find(a => a.Name == t.To).ProcessTransaction(t);
                }
            }

            logger.Debug("Getting user input");
            Console.Write("Enter command: ");
            var input = Console.ReadLine();

            if (input != null && input.ToLowerInvariant().StartsWith("list "))
            {
                input = input.ToLowerInvariant().Substring(5);
                if (input == "all")
                {
                    logger.Debug("Listing all accounts");
                    foreach (var a in accounts)
                    {
                        Console.WriteLine(a.ToString());
                    }
                }
                else
                {
                    logger.Debug("Looking for transactions matching " + input);
                    var matches = transactions.Where(t => t.From.ToLowerInvariant() == input || t.To.ToLowerInvariant() == input).ToList();
                    if (!matches.Any())
                    {
                        logger.Debug("No transactions found");
                        Console.WriteLine("No transactions with that name found.");
                    }
                    else
                    {
                        logger.Debug(matches.Count.ToString() + "transactions found");
                        foreach (var t in matches)
                        {
                            Console.WriteLine(t.ToString());
                        }
                    }
                }
            }
            else
            {
                logger.Warn("User entered null or otherwise invalid input");
                Console.WriteLine("Invalid input.");
            }

            Console.ReadLine();
            logger.Debug("Closing program");
        }
    }

    internal class Transaction
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
        public DateTime Date;
        public string From;
        public string To;
        public string Narrative;
        public float Amount;

        public Transaction(DateTime date, string from, string to, string narrative, float amount)
        {
            this.Date = date;
            this.From = from;
            this.To = to;
            this.Narrative = narrative;
            this.Amount = amount;
        }

        public override string ToString()
        {
            return $"Date: {Date:d}, from: {From}, to: {To}, narrative: {Narrative}, amount: {Amount:0.00}";
        }
    }

    internal class Account
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
        public string Name;
        public float Credit = 0;

        public Account(string name)
        {
            this.Name = name;
        }

        public void ProcessTransaction(Transaction transaction)
        {
            if (Name == transaction.From)
            {
                Credit -= transaction.Amount;
            }
            else if (Name == transaction.To)
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

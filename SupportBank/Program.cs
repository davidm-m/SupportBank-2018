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
            using (var sr = new StreamReader("C:\\Users\\dgm\\Documents\\Work\\Training\\Support Bank 2018\\Transactions2014.csv"))
            {
                sr.ReadLine(); //we shouldn't need the first line
                string line;
                while (((line = sr.ReadLine()) != null))
                {
                    var entries = line.Split(',');
                    transactions.Add(new Transaction(entries[0], entries[1], entries[2], entries[3], entries[4]));
                }
            }

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

            //get user input
            Console.Write("Enter command: ");
            var input = Console.ReadLine();

            if (input != null && input.ToLowerInvariant().StartsWith("list "))
            {
                input = input.ToLowerInvariant().Substring(5);
                if (input == "all")
                {
                    foreach (var a in accounts)
                    {
                        Console.WriteLine(a.ToString());
                    }
                }
                else
                {
                    var matches = transactions.Where(t => t.From.ToLowerInvariant() == input || t.To.ToLowerInvariant() == input).ToList();
                    if (!matches.Any())
                    {
                        Console.WriteLine("No transactions with that name found.");
                    }
                    else
                    {
                        foreach (var t in matches)
                        {
                            Console.WriteLine(t.ToString());
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Invalid input.");
            }

            Console.ReadLine();
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

        public Transaction(string date, string from, string to, string narrative, string amount)
        {
            var dateParsed = DateTime.Parse(date);
            this.Date = dateParsed;
            this.From = from;
            this.To = to;
            this.Narrative = narrative;
            var amountfl = float.Parse(amount);
            this.Amount = amountfl;
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

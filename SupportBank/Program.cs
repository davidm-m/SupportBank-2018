using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Transactions;
using Newtonsoft.Json;
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

            Console.Write("Enter filename: ");
            var fileName = Console.ReadLine();
            while (!File.Exists("C:\\Work\\Training\\SupportBank-2018\\" + fileName))
            {
                logger.Warn("User asked for file " + fileName + " which does not exist.");
                Console.WriteLine("File does not exist.");
                Console.Write("Enter filename: ");
                fileName = Console.ReadLine();
            }
            logger.Debug("Found file " + fileName);
            var transactions = FileReader.ReadFile(fileName);

            var accounts = AccountProcessor.ProcessAccounts(transactions);
            
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
                    var matches = transactions.Where(t => t.FromAccount.ToLowerInvariant() == input || t.ToAccount.ToLowerInvariant() == input).ToList();
                    if (!matches.Any())
                    {
                        logger.Debug("No transactions found");
                        Console.WriteLine("No transactions with that name found.");
                    }
                    else
                    {
                        logger.Debug(matches.Count.ToString() + " transactions found");
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

    internal static class FileReader
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        public static List<Transaction> ReadFile(string fileName)
        {
            if (fileName.EndsWith(".csv"))
            {
                logger.Debug("File detected as CSV");
                return ReadCsv(fileName);
            }
            else if (fileName.EndsWith(".json"))
            {
                logger.Debug("File detected as json");
                return ReadJson(fileName);
            }
            else
            {
                logger.Error("Requested file " + fileName + " is not in a readable format");
                return null;
            }
        }

        public static List<Transaction> ReadCsv(string fileName)
        {
            var transactions = new List<Transaction>();
            logger.Debug("Opening file " + fileName);
            using (var sr = new StreamReader("C:\\Work\\Training\\SupportBank-2018\\" + fileName))
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
                        Console.WriteLine("Line " + count + " of " + fileName + "has more entries than expected");
                        logger.Warn("Line " + count + " of " + fileName + "has more entries than expected, tentatively reading entry anyway");
                    }
                    else if (entries.Length < 5)
                    {
                        Console.WriteLine("Line " + count + " of " + fileName + "has fewer entries than expected - entry has been ignored");
                        logger.Error("Line " + count + " of " + fileName + "has fewer entries than expected, skipping entry");
                        continue; //skip this entry as it cannot be readable
                    }

                    if (!DateTime.TryParse(entries[0], out var date))
                    {
                        Console.WriteLine("Improperly formatted date on line " + count + " of " + fileName + " - entry has been ignored");
                        logger.Error("Improperly formatted date on line " + count + " of " + fileName + ", skipping entry");
                        continue;
                    }
                    if (!float.TryParse(entries[4], out var amount))
                    {
                        Console.WriteLine("Improperly formatted amount on line " + count + " of " + fileName + " - entry has been ignored");
                        logger.Error("Improperly formatted amount on line " + count + " of " + fileName + ", skipping entry");
                        continue;
                    }
                    transactions.Add(new Transaction(date, entries[1], entries[2], entries[3], amount));
                }
                logger.Debug("File fully read");
            }
            return transactions;
        }

        public static List<Transaction> ReadJson(string fileName)
        {
            logger.Debug("Opening file " + fileName);
            var text = System.IO.File.ReadAllText("C:\\Work\\Training\\SupportBank-2018\\" + fileName);
            return JsonConvert.DeserializeObject<List<Transaction>>(text);
        }
    }

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

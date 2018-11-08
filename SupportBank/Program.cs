using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
            var lowerInput = input.ToLowerInvariant();
            input = TranslateInstructions(transactions, accounts, lowerInput);

            Console.ReadLine();
            logger.Debug("Closing program");
        }

        private static string TranslateInstructions(List<Transaction> transactions, List<Account> accounts, string input)
        {
            if (input != null && input.StartsWith("list "))
            {
                
                if (input == "list all")
                {
                    listAll(accounts);
                }
                else
                {
                    var accountName = input.Substring(5);
                    ListTransactionsForAccount(transactions, accountName);
                }
            }
            else
            {
                logger.Warn("User entered null or otherwise invalid input");
                Console.WriteLine("Invalid input.");
            }

            return input;
        }

        private static void ListTransactionsForAccount(List<Transaction> transactions, string accountName)
        {
            var matches = findTransactions(transactions, accountName);
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

        private static List<Transaction> findTransactions(List<Transaction> transactions, string accountName)
        {
            logger.Debug("Looking for transactions matching " + accountName);
            return transactions.Where(t => t.FromAccount.ToLowerInvariant() == accountName || t.ToAccount.ToLowerInvariant() == accountName).ToList();
        }

        private static void listAll(List<Account> accounts)
        {
            logger.Debug("Listing all accounts");
            foreach (var a in accounts)
            {
                Console.WriteLine(a.ToString());
            }
        }
    }
}

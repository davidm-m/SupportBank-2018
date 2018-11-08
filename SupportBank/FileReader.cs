using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using Newtonsoft.Json;
using NLog;

namespace SupportBank
{
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
            else if (fileName.EndsWith(".xml"))
            {
                logger.Debug("File detected as xml");
                return ReadXml(fileName);
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

        public static List<Transaction> ReadXml(string fileName)
        {
            var transactions = new List<Transaction>();
            var startDate = new DateTime(1900, 1, 1);
            startDate = startDate.AddDays(-1);
            using (var reader = XmlReader.Create("C:\\Work\\Training\\SupportBank-2018\\" + fileName))
            {
                reader.ReadToDescendant("SupportTransaction");
                while (reader.Name != "TransactionList")
                {
                    reader.MoveToAttribute("Date");
                    var dateInt = int.Parse(reader.Value);
                    var date = startDate.AddDays(dateInt);

                    reader.Read();
                    reader.Read();
                    reader.Read();

                    var narrative = reader.Value;

                    reader.Read();
                    reader.Read();
                    reader.Read();
                    reader.Read();

                    var amount = float.Parse(reader.Value);

                    reader.Read();
                    reader.Read();
                    reader.Read();
                    reader.Read();
                    reader.Read();
                    reader.Read();

                    var from = reader.Value;

                    reader.Read();
                    reader.Read();
                    reader.Read();
                    reader.Read();

                    var to = reader.Value;

                    transactions.Add(new Transaction(date, from, to, narrative, amount));

                    reader.Read();
                    reader.Read();
                    reader.Read();
                    reader.Read();
                    reader.Read();
                    reader.Read();
                    reader.Read();
                }
                
            }
            return transactions;
        }
    }
}
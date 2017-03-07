using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Sql;
using System.Data.SqlClient;
using System.IO;
using System.Security;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace ATMconsole
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                LogProvider.BeginLog();
                SQLoperator.EstablishConnection();
                BankAccount.Greet();
                SQLoperator.CloseConnection();
                LogProvider.EndLog();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.ReadLine();
            }
        }
    }

    class LogProvider
    {
        static string logpath = Directory.GetCurrentDirectory() + @"/log.txt";
        static StreamWriter outputFile = new StreamWriter(logpath, true);
        public static void BeginLog()
        {
            try
            {
                outputFile.Write("\nLog beginning: " + DateTime.Now.ToShortDateString() + " @ " + DateTime.Now.ToShortTimeString() + "\n");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        public static void LogString(string StringToLog)
        {
            try
            {
                Console.WriteLine(StringToLog);
                outputFile.Write("\n" + DateTime.Now.ToShortTimeString() + ":\t" + StringToLog);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        public static void EndLog()
        {
            outputFile.Close();
        }
    }

    class SQLoperator
    {
        static string databaseLocation = @"Data Source = (localDB)\MSSQLLocalDB;AttachDbFilename=c:\users\censor\documents\visual studio 2015\Projects\ATMconsole\ATMconsole\Bank.mdf";
        static SqlConnection bankConnection = new SqlConnection(@databaseLocation);

        public static void EstablishConnection()
        {
            try
            {
                bankConnection.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                LogProvider.LogString(e.ToString());
            }
        }

        public static void AddEntry(string name, SecureString card, SecureString PIN)
        {
            try
            {
                String query = "INSERT INTO dbo.BankUsersData (Name, CardNumber, PIN) " +
                                             "Values (@name, @card, @PIN)";
                SqlCommand insertCommand = new SqlCommand(query, bankConnection);
                insertCommand.Parameters.AddWithValue("@name", name);
                insertCommand.Parameters.AddWithValue("@card", BankAccount.SecureStringToString(card));
                insertCommand.Parameters.AddWithValue("@PIN", BankAccount.SecureStringToString(PIN));
                
                insertCommand.ExecuteNonQuery();
                LogProvider.LogString("Succesfully added new user to the database: " + name);
            }
            catch (Exception e)
            {
                LogProvider.LogString(e.ToString());
            }
        }

        static bool CheckCard(SecureString card, SecureString PIN)
        {
            try
            {
                using (SqlCommand checkCard = new SqlCommand("SELECT COUNT(*) from dbo.BankUsersData where CardNumber like @card AND PIN like @pin", bankConnection))
                {
                    checkCard.Parameters.AddWithValue("@card", BankAccount.SecureStringToString(card));
                    checkCard.Parameters.AddWithValue("@pin", BankAccount.SecureStringToString(PIN));
                    int userCount = (int)checkCard.ExecuteScalar();
                    if (userCount > 0)
                    {
                        return true;
                    }
                    else
                    {
                        LogProvider.LogString("Failed connection attempt: " + card.ToString());
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                LogProvider.LogString(e.ToString());
                return false;
            }
        }

        public static void CloseConnection()
        {
            try
            {
                bankConnection.Close();
            }
            catch (Exception e)
            {
                LogProvider.LogString(e.ToString());
            }
        }
    }

    class BankAccount
    { 
        static string name;
        static SecureString cardNumber;
        static SecureString pinCode;
        static int balance;
        static Int64 defaultCardNumber = 480000000000;
        static string[] commands = new string[] { "Log In", "Register", "Exit" };

        static public SecureString ConvertToSecureString(string password)
        {
            if (password == null)
                throw new ArgumentNullException("password");

            var securePassword = new SecureString();

            foreach (char c in password)
                securePassword.AppendChar(c);

            securePassword.MakeReadOnly();
            return securePassword;
        }

        static public String SecureStringToString(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        static private SecureString IssueCard()
        {
            Random rnd = new Random();
            Int64 NewCardNumber = rnd.Next(0, int.MaxValue);
            NewCardNumber += defaultCardNumber;
            Console.WriteLine("You have been issued a credit card. Write down the number to be able to access you bank account:\n" + NewCardNumber.ToString());
            return ConvertToSecureString(NewCardNumber.ToString());
        }

        public static void registerAccount()
        {
            Console.WriteLine("Welcome to our bank. You will need to write your name and desired pin-code.");
            while (true)
            {
                Console.WriteLine("Your name:");
                name = Console.ReadLine();
                if (name.Length <= 50)
                {
                    Console.WriteLine("Your PIN code:");
                    var tempPin = Console.ReadLine();
                    if ((tempPin.Length == 4) & (Regex.IsMatch(tempPin, @"^\d+$")))
                    {
                        pinCode = ConvertToSecureString(tempPin);
                        break;
                    }
                }
            }
            Console.WriteLine("Press Enter to be issued a new credit card.");
            Console.ReadKey();
            cardNumber = IssueCard();
            try
            {
                SQLoperator.AddEntry(name, cardNumber, pinCode);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                LogProvider.LogString(e.ToString());
            }
        }

        public static void logIn()
        {
            Console.WriteLine("Test.");
        }

        public static void Greet()
        {
            int inputN;
            Console.WriteLine("Welcome to Bank Accounting System! \nUse one of available commands by typing the number:");
            while (true)
            {
                int n = 1;
                foreach (string c in commands)
                {
                    Console.WriteLine(n + ". " + c);
                    n++;
                }
                string input = Console.ReadLine();

                if (Regex.IsMatch(input, @"^\d+$"))
                {
                    Int32.TryParse(input, out inputN);
                    if ((inputN < commands.Length) & inputN > 0) { break; }
                }
                Console.WriteLine("\nNo such command.");
            }
            string command = commands[inputN - 1].ToLower();
            switch (command)
            {
                case "register":
                    {
                        registerAccount();
                        return;
                    }
                case "log in":
                    {
                        logIn();
                        return;
                    }
                default:
                    {
                        Environment.Exit(0);
                        return;
                    }
            }

        }
    }
}

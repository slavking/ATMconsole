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
                Console.ReadKey();
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
                outputFile.Write("\n" + DateTime.Now.ToShortTimeString() + ":\t" + StringToLog);
            }
            catch (Exception e)
            {
                outputFile.Write("\n" + DateTime.Now.ToShortTimeString() + ": Closed LogWriter");
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

        public static bool CheckCard(SecureString card, SecureString PIN)
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
                        LogProvider.LogString("Failed connection attempt: " + BankAccount.SecureStringToString(card));
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

        public static string CheckBalance(SecureString card)
        {
            try
            {
                string balance;
                using (SqlCommand checkBalance = new SqlCommand("SELECT cardBalance from dbo.BankUsersData where CardNumber like @card", bankConnection))
                {
                    checkBalance.Parameters.AddWithValue("@card", BankAccount.SecureStringToString(card));
                    using (SqlDataReader reader = checkBalance.ExecuteReader())
                    {
                        if (reader != null)
                        {
                            while (reader.Read())
                            {
                                balance = reader["cardBalance"].ToString();
                                Console.WriteLine(balance);
                                return balance;
                            }
                        }
                    }
                    
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
            return null;
        }

        public static void ChangeBalance(SecureString card, int amount)
        {
            try
            {
                using (SqlCommand checkBalance = new SqlCommand("UPDATE dbo.BankUsersData (cardBalance) VALUES (@amount) where CardNumber like @card", bankConnection))
                {
                    checkBalance.Parameters.AddWithValue("@card", BankAccount.SecureStringToString(card));
                    checkBalance.Parameters.AddWithValue("@amount", amount.ToString());
                    checkBalance.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                LogProvider.LogString(ex.ToString());
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
        static float balance;
        static Int64 defaultCardNumber = 4800000000000000;
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
            int Tries = 0;
            while (Tries < 4)
            {
                Console.WriteLine("\nCard Number: ");
                string cardN = Console.ReadLine();
                if (Regex.IsMatch(cardN, @"^\d+$"))
                {
                    Console.WriteLine("\nPin Code: ");
                    string pin = Console.ReadLine();
                    if (Regex.IsMatch(pin, @"^\d+$") & pin.Length == 4)
                    {
                        cardNumber = ConvertToSecureString(cardN);
                        pinCode = ConvertToSecureString(pin);
                        if (SQLoperator.CheckCard(cardNumber, pinCode))
                        {
                            break;
                        }
                        else { Console.WriteLine("Wrong pin."); }
                    }
                }
                Tries++;
                Console.WriteLine("Autorisation failed.");
            }
            if (Tries > 3)
            {
                LogProvider.LogString("Failed login attempt. Connection terminated.");
                SQLoperator.CloseConnection();
                LogProvider.EndLog();
                Environment.Exit(0);
            }

            
            AccountOperation();
        }

        static void AccountOperation()
        {
            string[] accountActions = new string[] { "Check balance", "Deposit money", "Withdraw money" };
            Console.WriteLine("Select action:");
            int inputN;
            while (true)
            {
                int n = 1;
                foreach (string c in accountActions)
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
            string command = accountActions[inputN - 1].ToLower();
            switch (command)
            {
                case "check balance":
                    {
                        balance = float.Parse(SQLoperator.CheckBalance(cardNumber));
                        Console.WriteLine("Balance: " + balance.ToString("0.00") + "$");
                        return;
                    }
                case "deposit money":
                    {
                        Console.WriteLine("Money Deposit Template");
                        return;
                    }
                case "withdraw money":
                    {
                        Console.WriteLine("Money Withrdawal Template");
                        return;
                    }
                default:
                    {
                        SQLoperator.CloseConnection();
                        LogProvider.EndLog();
                        Environment.Exit(0);
                        return;
                    }
            }
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
                        SQLoperator.CloseConnection();
                        LogProvider.EndLog();
                        Environment.Exit(0);
                        return;
                    }
            }

        }
    }
}

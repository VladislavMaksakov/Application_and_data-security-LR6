using System.Text;
using Microsoft.Data.Sqlite;

class Program
{

   private const string DbFileName = "users_db.sqlite";
   private const string ConnectionString = "Data Source=" + DbFileName;

   static void Main()
   {
      Console.OutputEncoding = Encoding.UTF8;
      if (!File.Exists(DbFileName))
      {
         Console.ForegroundColor = ConsoleColor.Red;
         Console.WriteLine($"[ПОМИЛКА] Файл '{DbFileName}' не знайдено!");
         Console.WriteLine("Спочатку запустіть скрипт-генератор або поверніть файл у папку.");
         Console.ResetColor();
         return;
      }

      while (true)
      {
         Console.WriteLine("\n==========================================");
         Console.WriteLine("     БАНКІВСЬКА СИСТЕМА (LAB DEMO)");
         Console.WriteLine("==========================================");
         Console.WriteLine("1. Вхід (ВРАЗЛИВИЙ до SQL Injection)");
         Console.WriteLine("2. Вхід (ЗАХИЩЕНИЙ - Prepared Statements)");
         Console.WriteLine("3. Вихід");
         Console.Write("\nОберіть опцію > ");
         var choice = Console.ReadLine();
         switch (choice)
         {
            case "1":
               VulnerableLogin();
               break;
            case "2":
               SecureLogin();
               break;
            case "3":
               return;
            default:
               Console.WriteLine("Невірний вибір.");
               break;
         }
      }
   }

   // --- ВРАЗЛИВИЙ МЕТОД (Демонстрація атаки) ---
   static void VulnerableLogin()
   {
      Console.WriteLine("\n[ВРАЗЛИВИЙ РЕЖИМ] Введіть дані для входу.");
      Console.Write("Логін: ");
      string username = Console.ReadLine();
      Console.Write("Пароль: ");
      string password = Console.ReadLine();

      using (var connection = new SqliteConnection(ConnectionString))
      {
         connection.Open();

         // !!! ВРАЗЛИВІСТЬ: Пряма інтерполяція рядків без перевірки
         string query = $"SELECT * FROM Clients WHERE Login = '{username}' AND Password = '{password}'";

         // Виводимо сформований SQL запит, щоб бачити, як виглядає атака зсередини
         Console.ForegroundColor = ConsoleColor.Yellow;
         Console.WriteLine($"[DEBUG SQL]: {query}");
         Console.ResetColor();

         try
         {
            var command = new SqliteCommand(query, connection);
            using (var reader = command.ExecuteReader())
            {
               if (reader.HasRows)
               {
                  while (reader.Read())
                  {
                     PrintUserData(reader);
                  }
               }
               else
               {
                  Console.WriteLine(">>> Доступ заборонено (Користувача не знайдено)");
               }
            }
         }
         catch (Exception ex)
         {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[SQL Error]: {ex.Message}");
            Console.ResetColor();
         }
      }
   }

   // --- ЗАХИЩЕНИЙ МЕТОД (Демонстрація захисту) ---
   static void SecureLogin()
   {
      Console.WriteLine("\n[ЗАХИЩЕНИЙ РЕЖИМ] Введіть дані для входу.");
      Console.Write("Логін: ");
      string username = Console.ReadLine();
      Console.Write("Пароль: ");
      string password = Console.ReadLine();

      using (var connection = new SqliteConnection(ConnectionString))
      {
         connection.Open();
         string query = "SELECT * FROM Clients WHERE Login = @u AND Password = @p";
         var command = new SqliteCommand(query, connection);
         command.Parameters.AddWithValue("@u", username);
         command.Parameters.AddWithValue("@p", password);
         using (var reader = command.ExecuteReader())
         {
            if (reader.Read())
            {
               PrintUserData(reader);
            }
            else
            {
               Console.WriteLine(">>> Доступ заборонено (Невірний логін або пароль)");
            }
         }
      }
   }
   static void PrintUserData(SqliteDataReader reader)
   {
      Console.ForegroundColor = ConsoleColor.Green;
      Console.WriteLine("\n>>> ДОСТУП ДОЗВОЛЕНО! КАРТКА КЛІЄНТА <<<");
      Console.WriteLine("--------------------------------------------------");
      Console.WriteLine($"ПІБ:            {reader["PIB"]}");
      Console.WriteLine($"Логін:          {reader["Login"]}");
      Console.WriteLine($"Телефон:        {reader["Phone"]}");
      Console.WriteLine($"ІПН:            {reader["IPN"]}");
      Console.WriteLine($"Паспорт:        {reader["PassportSeriesNum"]} (вид. {reader["PassportDate"]})");
      Console.WriteLine($"Ким виданий:    {reader["PassportIssuer"]}");
      Console.WriteLine($"Адреса:         {reader["Address"]}");
      Console.WriteLine($"IBAN рахунок:   {reader["IBAN"]}");
      Console.WriteLine("--------------------------------------------------");
      Console.ResetColor();
   }
}
using System;
using System.Text;
using System.IO; // Необхідно для роботи з файлами (File.Exists)
using Microsoft.Data.Sqlite; // Основна бібліотека для роботи з SQLite

class Program
{
   // Шлях до файлу бази даних. Має лежати поруч із .exe файлом або в папці проекту
   private const string DbFileName = "users_db.sqlite";
   // Рядок підключення для ADO.NET
   private const string ConnectionString = "Data Source=" + DbFileName;

   static void Main()
   {
      // Налаштовуємо консоль на відображення українських символів (кирилиці)
      Console.OutputEncoding = Encoding.UTF8;

      // --- ПЕРЕВІРКА СЕРЕДОВИЩА ---
      // Перед початком роботи переконуємося, що файл БД існує.
      // Якщо його немає, програма впаде з помилкою при спробі підключення.
      if (!File.Exists(DbFileName))
      {
         Console.ForegroundColor = ConsoleColor.Red;
         Console.WriteLine($"[ПОМИЛКА] Файл '{DbFileName}' не знайдено!");
         Console.WriteLine("Спочатку запустіть скрипт-генератор або поверніть файл у папку.");
         Console.ResetColor();
         return; // Зупиняємо програму
      }

      // --- ГОЛОВНИЙ ЦИКЛ ПРОГРАМИ ---
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

         // Обробка вибору користувача через switch
         switch (choice)
         {
            case "1":
               VulnerableLogin(); // Виклик "дірявого" методу
               break;
            case "2":
               SecureLogin(); // Виклик безпечного методу
               break;
            case "3":
               return; // Вихід з програми
            default:
               Console.WriteLine("Невірний вибір.");
               break;
         }
      }
   }

   // ---------------------------------------------------------
   // ВРАЗЛИВИЙ МЕТОД (Демонстрація атаки SQL Injection)
   // ---------------------------------------------------------
   static void VulnerableLogin()
   {
      Console.WriteLine("\n[ВРАЗЛИВИЙ РЕЖИМ] Введіть дані для входу.");

      // Отримуємо вхідні дані від користувача
      Console.Write("Логін: ");
      string username = Console.ReadLine();
      Console.Write("Пароль: ");
      string password = Console.ReadLine();

      // Створюємо підключення до БД. 
      // 'using' автоматично закриє підключення після завершення блоку.
      using (var connection = new SqliteConnection(ConnectionString))
      {
         connection.Open(); // Відкриваємо з'єднання

         // [!!!] КРИТИЧНА ВРАЗЛИВІСТЬ [!!!]
         // Тут використовується інтерполяція рядків ($"...").
         // Змінні username та password просто вклеюються всередину рядка запиту.
         // Якщо ввести: admin' -- , то структура запиту зміниться, і пароль буде проігноровано.
         string query = $"SELECT * FROM Clients WHERE Login = '{username}' AND Password = '{password}'";

         // ВИВІД ДЛЯ НАЛАГОДЖЕННЯ:
         // Показуємо в консолі, який саме SQL-запит пішов у базу.
         // Це наочно демонструє викладачу, як виглядає ін'єкція.
         Console.ForegroundColor = ConsoleColor.Yellow;
         Console.WriteLine($"[DEBUG SQL]: {query}");
         Console.ResetColor();

         try
         {
            // Створюємо команду на основі "брудного" рядка
            var command = new SqliteCommand(query, connection);

            // Виконуємо запит і отримуємо результат (reader)
            using (var reader = command.ExecuteReader())
            {
               if (reader.HasRows) // Якщо є хоча б один результат
               {
                  // Цикл while тут важливий!
                  // При атаці ' OR 1=1 -- умова стає істинною для ВСІХ записів.
                  // Цей цикл виведе на екран усю базу даних, а не одного користувача.
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
            // Якщо синтаксис SQL буде порушено, ми побачимо помилку тут
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[SQL Error]: {ex.Message}");
            Console.ResetColor();
         }
      }
   }

   // ---------------------------------------------------------
   // ЗАХИЩЕНИЙ МЕТОД (Демонстрація захисту)
   // ---------------------------------------------------------
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

         // [!!!] ЗАХИСТ: ВИКОРИСТАННЯ ПАРАМЕТРІВ [!!!]
         // Замість значень ми ставимо плейсхолдери (маркери) @u та @p.
         // Сам текст запиту є константою і не змінюється від вводу користувача.
         string query = "SELECT * FROM Clients WHERE Login = @u AND Password = @p";

         var command = new SqliteCommand(query, connection);

         // [!!!] БЕЗПЕЧНА ПЕРЕДАЧА ДАНИХ [!!!]
         // Ми передаємо дані окремо від SQL-коду.
         // База даних трактує вміст змінних виключно як текстові літерали,
         // навіть якщо там є спецсимволи типу ' або --.
         command.Parameters.AddWithValue("@u", username);
         command.Parameters.AddWithValue("@p", password);

         using (var reader = command.ExecuteReader())
         {
            // Тут використовуємо if, а не while, бо очікуємо лише одного користувача при коректному вході.
            // Хоча навіть при while атака б не спрацювала.
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

   // --- ДОПОМІЖНИЙ МЕТОД ---
   // Просто виводить дані з поточного рядка результату
   static void PrintUserData(SqliteDataReader reader)
   {
      Console.ForegroundColor = ConsoleColor.Green;
      Console.WriteLine("\n>>> ДОСТУП ДОЗВОЛЕНО! КАРТКА КЛІЄНТА <<<");
      Console.WriteLine("--------------------------------------------------");
      // Зчитуємо дані за назвами колонок у базі даних
      Console.WriteLine($"ПІБ:            {reader["PIB"]}");
      Console.WriteLine($"Логін:          {reader["Login"]}");
      Console.WriteLine($"Телефон:        {reader["Phone"]}");
      Console.WriteLine($"ІПН:            {reader["IPN"]}"); // Чутлива інформація
      Console.WriteLine($"Паспорт:        {reader["PassportSeriesNum"]} (вид. {reader["PassportDate"]})");
      Console.WriteLine($"Ким виданий:    {reader["PassportIssuer"]}");
      Console.WriteLine($"Адреса:         {reader["Address"]}");
      Console.WriteLine($"IBAN рахунок:   {reader["IBAN"]}"); // Фінансова інформація
      Console.WriteLine("--------------------------------------------------");
      Console.ResetColor();
   }
}
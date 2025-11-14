using System;
using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StudentLibraryApp
{
    // Вікно для відображення та управління активними видачами книг
    public partial class ActiveLoansWindow : Window
    {
        // Конструктор вікна активних видач
        public ActiveLoansWindow()
        {
            // Ініціалізація компонентів вікна
            InitializeComponent();
            // Встановлення початкової дати для фільтра (місяць тому)
            dpFrom.SelectedDate = DateTime.Today.AddMonths(-1);
            // Встановлення кінцевої дати для фільтра (сьогодні)
            dpTo.SelectedDate = DateTime.Today;
            // Завантаження активних видач при відкритті вікна
            LoadActiveLoans();
            // Додавання обробника події для завантаження рядків у таблицю
            dgActiveLoans.LoadingRow += DgActiveLoans_LoadingRow;
        }

        // Метод для завантаження активних видач з бази даних
        private void LoadActiveLoans()
        {
            // Отримання початкової та кінцевої дати з елементів вибору дати
            var fromDate = dpFrom.SelectedDate?.ToString("yyyy-MM-dd") ?? "1900-01-01";
            var toDate = dpTo.SelectedDate?.ToString("yyyy-MM-dd") ?? "2100-01-01";

            // Виконання запиту до бази даних для отримання активних видач
            var dt = DatabaseService.ExecuteQuery($@"
                SELECT l.LoanID, r.FullName, b.Title, l.LoanDate, l.DueDate,
                       DATEDIFF(day, l.DueDate, GETDATE()) AS DaysOverdue
                FROM Loans l
                JOIN Readers r ON l.ReaderID = r.ReaderID
                JOIN Books b ON l.BookID = b.BookID
                WHERE l.ReturnDate IS NULL
                  AND l.LoanDate BETWEEN '{fromDate}' AND '{toDate}'
                ORDER BY l.DueDate ASC");

            // Встановлення джерела даних для таблиці активних видач
            dgActiveLoans.ItemsSource = dt.DefaultView;
        }

        // Обробник події сортування для таблиці активних видач
        private void dgActiveLoans_Sorting(object sender, DataGridSortingEventArgs e)
        {
            // Позначення події як обробленої, щоб запобігти стандартному сортуванню
            e.Handled = true;
            // Отримання колонки, по якій виконується сортування
            DataGridColumn column = e.Column;
            // Визначення напрямку сортування
            ListSortDirection direction = (column.SortDirection != ListSortDirection.Ascending) 
                ? ListSortDirection.Ascending 
                : ListSortDirection.Descending;
            // Встановлення нового напрямку сортування для колонки
            column.SortDirection = direction;
            // Скидання напрямку сортування для інших колонок
            foreach (var col in dgActiveLoans.Columns)
            {
                if (col.SortMemberPath != column.SortMemberPath)
                {
                    col.SortDirection = null;
                }
            }

            // Застосування сортування до джерела даних
            if (dgActiveLoans.ItemsSource is DataView dv)
            {
                dv.Sort = $"{column.SortMemberPath} {(direction == ListSortDirection.Ascending ? "ASC" : "DESC")}";
            }
        }

        // Обробник події завантаження рядка в таблицю
        private void DgActiveLoans_LoadingRow(object? sender, DataGridRowEventArgs e)
        {
            // Перевірка, чи елемент рядка є DataRowView
            if (e.Row.Item is DataRowView item)
            {
                // Отримання кількості прострочених днів
                int daysOverdue = item["DaysOverdue"] as int? ?? 0;
                // Якщо є прострочення, зміна фону рядка на червоний
                if (daysOverdue > 0)
                {
                    e.Row.Background = new SolidColorBrush(Colors.LightCoral);
                }
                // Інакше встановлення стандартного фону
                else
                {
                    e.Row.Background = new SolidColorBrush(Colors.White);
                    // Чергування кольорів для парних та непарних рядків
                    if (e.Row.GetIndex() % 2 != 0)
                    {
                        e.Row.Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));
                    }
                }
            }
        }

        // Обробник події натискання на кнопку "Фільтр"
        private void Filter_Click(object sender, RoutedEventArgs e) => LoadActiveLoans();
        // Обробник події натискання на кнопку "Скинути"
        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            // Скидання значень у полях вибору дати
            dpFrom.SelectedDate = null;
            dpTo.SelectedDate = null;
            // Перезавантаження активних видач
            LoadActiveLoans();
        }

        // Обробник події натискання на кнопку "Повернути"
        private void ReturnBook_Click(object sender, RoutedEventArgs e)
        {
            // Перевірка, чи відправник є кнопкою з відповідним тегом
            if (sender is Button button && button.Tag is int loanId)
            {
                // Підтвердження дії користувачем
                if (MessageBox.Show("Повернути книгу?", "Підтвердження", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Отримання деталей видачі для перевірки терміну повернення
                        var loanDetailsDt = DatabaseService.ExecuteQuery($"SELECT DueDate FROM Loans WHERE LoanID = {loanId}");
                        // Перевірка, чи знайдено видачу
                        if (loanDetailsDt.Rows.Count == 0)
                        {
                            MessageBox.Show("Помилка: видачу не знайдено.", "Помилка");
                            return;
                        }
                        // Отримання терміну повернення
                        var dueDate = (DateTime)loanDetailsDt.Rows[0]["DueDate"];

                        // Оновлення запису в базі даних, встановлення дати повернення
                        DatabaseService.ExecuteNonQuery($"UPDATE Loans SET ReturnDate = GETDATE() WHERE LoanID = {loanId}");

                        MessageBox.Show("Книгу повернено.", "Успіх");
                        // Перезавантаження активних видач
                        LoadActiveLoans();

                        // Перевірка, чи книга повернута із запізненням
                        if (DateTime.Today > dueDate)
                        {
                            // Повідомлення про запізнення та відкриття вікна штрафів
                            MessageBox.Show("Книгу повернено із запізненням. Відкрито вікно штрафів.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
                            new FinesWindow().ShowDialog();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Обробка можливих помилок
                        MessageBox.Show("Помилка: " + ex.Message);
                    }
                }
            }
        }
    }
}
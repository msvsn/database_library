using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace StudentLibraryApp
{
    // Вікно для видачі книг читачам
    public partial class IssueBookWindow : Window
    {
        // ID вибраного читача
        private int? selectedReaderId = null;
        // ID вибраної книги
        private int? selectedBookId = null;

        // Конструктор вікна видачі книги
        public IssueBookWindow()
        {
            // Ініціалізація компонентів
            InitializeComponent();
            // Встановлення поточної дати як дати видачі
            dpLoanDate.SelectedDate = DateTime.Today;
            // Завантаження списків читачів та книг
            LoadAllReaders();
            LoadAllBooks();
        }

        // Завантаження списку читачів з можливістю пошуку
        private void LoadAllReaders(string search = "")
        {
            // Формування запиту в залежності від наявності пошукового терміну
            var query = string.IsNullOrWhiteSpace(search) ? 
                "SELECT ReaderID, FullName, CardNumber, ReaderType FROM Readers" :
                $"SELECT ReaderID, FullName, CardNumber, ReaderType FROM Readers WHERE FullName LIKE '%{search}%' OR CardNumber LIKE '%{search}%'";
            var dt = DatabaseService.ExecuteQuery(query);
            // Заповнення таблиці читачів
            dgReaders.ItemsSource = dt.DefaultView;
        }

        // Завантаження списку книг з можливістю пошуку
        private void LoadAllBooks(string search = "")
        {
            // Формування запиту для пошуку книг
            var query = search == "" ? 
                "SELECT b.*, a.FullName AS AuthorName FROM Books b LEFT JOIN Authors a ON b.AuthorID = a.AuthorID" :
                $"SELECT b.*, a.FullName AS AuthorName FROM Books b LEFT JOIN Authors a ON b.AuthorID = a.AuthorID " +
                $"WHERE b.Title LIKE '%{search}%' OR a.FullName LIKE '%{search}%' OR b.ISBN LIKE '%{search}%'";
            var dt = DatabaseService.ExecuteQuery(query);
            // Заповнення таблиці книг
            dgBooks.ItemsSource = dt.DefaultView;
        }

        // Обробник зміни тексту в полі пошуку читача
        private void txtReaderSearch_TextChanged(object sender, TextChangedEventArgs e)
            => LoadAllReaders(txtReaderSearch.Text);

        // Обробник зміни тексту в полі пошуку книги
        private void txtBookSearch_TextChanged(object sender, TextChangedEventArgs e)
            => LoadAllBooks(txtBookSearch.Text);

        // Обробник вибору читача в таблиці
        private void dgReaders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Збереження ID вибраного читача
            if (dgReaders.SelectedItem is DataRowView row)
                selectedReaderId = (int)row["ReaderID"];
        }

        // Обробник вибору книги в таблиці
        private void dgBooks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Збереження ID вибраної книги
            if (dgBooks.SelectedItem is DataRowView row)
                selectedBookId = (int)row["BookID"];
        }

        // Обробник натискання на кнопку "Видати книгу"
        private void Issue_Click(object sender, RoutedEventArgs e)
        {
            // Перевірка, чи вибрано читача та книгу
            if (!selectedReaderId.HasValue || !selectedBookId.HasValue)
            {
                MessageBox.Show("Оберіть читача і книгу.", "Увага");
                return;
            }
            // Перевірка наявності книги
            var dt = DatabaseService.ExecuteQuery($"SELECT QuantityAvailable FROM Books WHERE BookID = {selectedBookId}");
            if ((int)dt.Rows[0][0] <= 0)
            {
                MessageBox.Show("Цієї книги немає в наявності.", "Помилка");
                return;
            }
            // Перевірка наявності несплачених штрафів у читача
            var finesDt = DatabaseService.ExecuteQuery($"SELECT COUNT(*) FROM Loans WHERE ReaderID = {selectedReaderId} AND FineAmount > 0");
            if ((int)finesDt.Rows[0][0] > 0)
            {
                MessageBox.Show("Цей читач має несплачені штрафи і не може брати нові книги.", "Боржник");
                return;
            }
            try
            {
                // Формування дати видачі
                var loanDate = dpLoanDate.SelectedDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                // Додавання запису про видачу в базу даних
                DatabaseService.ExecuteNonQuery($@"
                    INSERT INTO Loans (BookID, ReaderID, LoanDate) 
                    VALUES ({selectedBookId}, {selectedReaderId}, '{loanDate}')");

                // Зменшення кількості доступних книг
                DatabaseService.ExecuteNonQuery($@"
                    UPDATE Books SET QuantityAvailable = QuantityAvailable - 1 
                    WHERE BookID = {selectedBookId}");

                MessageBox.Show("Книгу видано.", "Успіх");
                // Закриття вікна після успішної видачі
                this.Close();
            }
            catch (Exception ex) { MessageBox.Show("Помилка: " + ex.Message); }
        }
    }
}
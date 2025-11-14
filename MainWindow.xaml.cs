using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualBasic;

namespace StudentLibraryApp
{
    // Головне вікно програми
    public partial class MainWindow : Window
    {
        // Конструктор головного вікна
        public MainWindow()
        {
            // Ініціалізація компонентів
            InitializeComponent();
            // Додавання обробника події, яка спрацьовує після завантаження вікна
            this.Loaded += MainWindow_Loaded;
        }

        // Обробник події завантаження вікна
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Завантаження списку книг при відкритті програми
            LoadBooks();
        }

        // Метод для завантаження списку книг з бази даних
        public void LoadBooks()
        {
            try
            {
                // Запит до бази даних для отримання повної інформації про книги
                var dt = DatabaseService.ExecuteQuery(@"
                    SELECT b.BookID, b.Title, 
                           ISNULL(a.FullName, 'Невідомий') AS AuthorName,
                           ISNULL(g.Name, 'Без жанру') AS GenreName,
                           ISNULL(p.Name, 'Невідомо') AS PublisherName,
                           b.YearPublished, b.QuantityAvailable
                    FROM Books b
                    LEFT JOIN Authors a ON b.AuthorID = a.AuthorID
                    LEFT JOIN Genres g ON b.GenreID = g.GenreID
                    LEFT JOIN Publishers p ON b.PublisherID = p.PublisherID");
                // Встановлення джерела даних для таблиці книг
                dgBooks.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                // Обробка помилок при завантаженні книг
                MessageBox.Show("Книги: " + ex.Message);
            }
        }

        // Обробник натискання на кнопку "Видача книги"
        private void IssueBook_Click(object sender, RoutedEventArgs e)
        {
            // Відкриття вікна видачі книги
            new IssueBookWindow().ShowDialog();
            // Оновлення списку книг після закриття вікна
            LoadBooks();
        }

        // Обробник натискання на кнопку "Активні видачі"
        private void ActiveLoans_Click(object sender, RoutedEventArgs e)
        {
            // Відкриття вікна активних видач
            new ActiveLoansWindow().ShowDialog();
            // Оновлення списку книг
            LoadBooks();
        }

        // Обробник натискання на кнопку "Штрафи"
        private void Fines_Click(object sender, RoutedEventArgs e)
        {
            // Відкриття вікна штрафів
            new FinesWindow().ShowDialog();
        }

        // Обробник натискання на кнопку "Користувачі"
        private void ManageReaders_Click(object sender, RoutedEventArgs e)
        {
            // Відкриття вікна керування читачами
            new ManageReadersWindow().ShowDialog();
        }

        // Обробник натискання на кнопку "Звіти"
        private void Reports_Click(object sender, RoutedEventArgs e)
        {
            // Відкриття вікна звітів
            new ReportsWindow().ShowDialog();
        }

        // Обробник натискання на кнопку "Архів"
        private void Archive_Click(object sender, RoutedEventArgs e)
        {
            // Відкриття вікна архіву
            new ArchiveWindow().ShowDialog();
        }

        // Обробник зміни тексту в полі пошуку
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Якщо поле пошуку порожнє, завантажити повний список книг
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                LoadBooks();
                return;
            }

            try
            {
                // Виконання пошукового запиту
                var dt = DatabaseService.ExecuteQuery($@"
                    SELECT b.BookID, b.Title, 
                           ISNULL(a.FullName, 'Невідомий') AS AuthorName,
                           ISNULL(g.Name, 'Без жанру') AS GenreName,
                           ISNULL(p.Name, 'Невідомо') AS PublisherName,
                           b.YearPublished, b.QuantityAvailable
                    FROM Books b
                    LEFT JOIN Authors a ON b.AuthorID = a.AuthorID
                    LEFT JOIN Genres g ON b.GenreID = g.GenreID
                    LEFT JOIN Publishers p ON b.PublisherID = p.PublisherID
                    WHERE b.Title LIKE '%{txtSearch.Text}%' 
                       OR a.FullName LIKE '%{txtSearch.Text}%'
                       OR p.Name LIKE '%{txtSearch.Text}%'");
                // Оновлення таблиці результатами пошуку
                dgBooks.ItemsSource = dt.DefaultView;
            }
            catch { }
        }

        // Обробник натискання на кнопку "Додати книгу"
        private void AddBook_Click(object sender, RoutedEventArgs e)
        {
            var manageBookWindow = new ManageBookWindow();
            // Відкриття вікна додавання книги і оновлення списку, якщо додавання було успішним
            if (manageBookWindow.ShowDialog() == true)
            {
                LoadBooks();
            }
        }

        // Обробник натискання на кнопку "Редагувати книгу"
        private void EditBook_Click(object sender, RoutedEventArgs e)
        {
            // Перевірка, чи вибрано книгу в таблиці
            if (dgBooks.SelectedItem is DataRowView row)
            {
                int bookId = (int)row["BookID"];
                var manageBookWindow = new ManageBookWindow(bookId);
                // Відкриття вікна редагування та оновлення списку, якщо редагування було успішним
                if (manageBookWindow.ShowDialog() == true)
                {
                    LoadBooks();
                }
            }
            else
            {
                MessageBox.Show("Будь ласка, оберіть книгу для редагування.", "Увага");
            }
        }

        // Обробник натискання на кнопку "Видалити книгу"
        private void DeleteBook_Click(object sender, RoutedEventArgs e)
        {
            if (dgBooks.SelectedItem is DataRowView row)
            {
                // Підтвердження видалення
                if (MessageBox.Show("Ви впевнені, що хочете видалити цю книгу?", "Підтвердження", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    int bookId = (int)row["BookID"];
                    try
                    {
                        // Перевірка, чи є у книги активні видачі
                        var activeLoans = DatabaseService.ExecuteQuery($"SELECT COUNT(*) FROM Loans WHERE BookID = {bookId} AND ReturnDate IS NULL");
                        if ((int)activeLoans.Rows[0][0] > 0)
                        {
                            MessageBox.Show("Неможливо видалити книгу, оскільки вона має активні видачі.", "Помилка");
                            return;
                        }

                        // Видалення книги з бази даних
                        DatabaseService.ExecuteNonQuery($"DELETE FROM Books WHERE BookID = {bookId}");
                        // Оновлення списку книг
                        LoadBooks();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Помилка при видаленні книги: {ex.Message}", "Помилка");
                    }
                }
            }
            else
            {
                MessageBox.Show("Будь ласка, оберіть книгу для видалення.", "Увага");
            }
        }
    }
}
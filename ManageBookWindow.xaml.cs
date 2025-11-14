using System;
using System.Data;
using System.Windows;
using System.Collections.Generic;

namespace StudentLibraryApp
{
    // Вікно для додавання та редагування інформації про книгу
    public partial class ManageBookWindow : Window
    {
        // ID книги для редагування. Null, якщо додається нова книга
        private int? _bookId;

        // Конструктор вікна. Приймає необов'язковий bookId
        public ManageBookWindow(int? bookId = null)
        {
            // Ініціалізація компонентів
            InitializeComponent();
            _bookId = bookId;
            // Завантаження даних для випадаючих списків
            LoadAuthors();
            LoadGenres();
            LoadPublishers();

            // Якщо bookId вказано, завантажити дані існуючої книги
            if (_bookId.HasValue)
            {
                LoadBookData();
            }
        }

        // Завантаження списку авторів
        private void LoadAuthors()
        {
            var dt = DatabaseService.ExecuteQuery("SELECT AuthorID, FullName FROM Authors ORDER BY FullName");
            cmbAuthor.ItemsSource = dt.DefaultView;
        }

        // Завантаження списку жанрів
        private void LoadGenres()
        {
            var dt = DatabaseService.ExecuteQuery("SELECT GenreID, Name FROM Genres ORDER BY Name");
            cmbGenre.ItemsSource = dt.DefaultView;
        }

        // Завантаження списку видавництв
        private void LoadPublishers()
        {
            var dt = DatabaseService.ExecuteQuery("SELECT PublisherID, Name FROM Publishers ORDER BY Name");
            cmbPublisher.ItemsSource = dt.DefaultView;
        }

        // Завантаження даних конкретної книги для редагування
        private void LoadBookData()
        {
            var dt = DatabaseService.ExecuteQuery($"SELECT * FROM Books WHERE BookID = {_bookId!.Value}");
            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                txtTitle.Text = row["Title"].ToString();
                if (row["AuthorID"] != DBNull.Value)
                    cmbAuthor.SelectedValue = row["AuthorID"];
                if (row["GenreID"] != DBNull.Value)
                    cmbGenre.SelectedValue = row["GenreID"];
                if (row["PublisherID"] != DBNull.Value)
                    cmbPublisher.SelectedValue = row["PublisherID"];
                txtYear.Text = row["YearPublished"].ToString();
                txtIsbn.Text = row["ISBN"].ToString();
                txtQuantity.Text = row["QuantityAvailable"].ToString();
            }
        }

        // Обробник натискання на кнопку "Зберегти"
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Перевірка коректності заповнення форми
            if (!ValidateForm())
            {
                return;
            }

            try
            {
                // Отримання або створення ID автора, жанру, видавництва
                int authorId = (cmbAuthor.SelectedValue != null)
                    ? (int)cmbAuthor.SelectedValue
                    : DatabaseService.GetOrCreateAuthorId(cmbAuthor.Text);

                int genreId = (cmbGenre.SelectedValue != null)
                    ? (int)cmbGenre.SelectedValue
                    : DatabaseService.GetOrCreateGenreId(cmbGenre.Text);

                int publisherId = (cmbPublisher.SelectedValue != null)
                    ? (int)cmbPublisher.SelectedValue
                    : DatabaseService.GetOrCreatePublisherId(cmbPublisher.Text);
                // Оновлення списків, щоб відобразити нові значення (якщо були створені)
                LoadAuthors();
                LoadGenres();
                LoadPublishers();
                cmbAuthor.SelectedValue = authorId;
                cmbGenre.SelectedValue = genreId;
                cmbPublisher.SelectedValue = publisherId;

                // Збір даних з форми
                string title = txtTitle.Text;
                string isbn = txtIsbn.Text;
                int year = int.Parse(txtYear.Text);
                int newQuantityAvailable = int.Parse(txtQuantity.Text);

                // Підготовка параметрів для SQL-запиту
                var parameters = new Dictionary<string, object>
                {
                    { "@Title", title },
                    { "@AuthorID", authorId },
                    { "@GenreID", genreId },
                    { "@PublisherID", publisherId },
                    { "@YearPublished", year },
                    { "@ISBN", isbn }
                };

                string sql;
                // Якщо редагується існуюча книга
                if (_bookId.HasValue)
                {
                    // Розрахунок нової загальної кількості книг
                    var dt = DatabaseService.ExecuteQuery($"SELECT QuantityAvailable, QuantityTotal FROM Books WHERE BookID = {_bookId.Value}");
                    int oldQuantityAvailable = (int)dt.Rows[0]["QuantityAvailable"];
                    int oldQuantityTotal = (int)dt.Rows[0]["QuantityTotal"];
                    int loanedCount = oldQuantityTotal - oldQuantityAvailable;
                    int newQuantityTotal = newQuantityAvailable + loanedCount;

                    parameters.Add("@QuantityAvailable", newQuantityAvailable);
                    parameters.Add("@QuantityTotal", newQuantityTotal);
                    parameters.Add("@BookID", _bookId.Value);

                    // SQL-запит на оновлення
                    sql = @"UPDATE Books SET 
                                Title = @Title, AuthorID = @AuthorID, GenreID = @GenreID, 
                                PublisherID = @PublisherID, YearPublished = @YearPublished, ISBN = @ISBN, 
                                QuantityAvailable = @QuantityAvailable, QuantityTotal = @QuantityTotal
                             WHERE BookID = @BookID";
                }
                // Якщо додається нова книга
                else
                {
                    parameters.Add("@QuantityAvailable", newQuantityAvailable);
                    parameters.Add("@QuantityTotal", newQuantityAvailable);

                    // SQL-запит на додавання
                    sql = @"INSERT INTO Books (Title, AuthorID, GenreID, PublisherID, YearPublished, ISBN, QuantityAvailable, QuantityTotal)
                             VALUES (@Title, @AuthorID, @GenreID, @PublisherID, @YearPublished, @ISBN, @QuantityAvailable, @QuantityTotal)";
                }

                // Виконання запиту
                DatabaseService.ExecuteNonQuery(sql, parameters);
                DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка збереження: {ex.Message}", "Помилка");
            }
        }

        // Метод для валідації полів форми
        private bool ValidateForm()
        {
            bool isValid = true;
            // Скидання стилів помилок
            txtTitle.BorderBrush = System.Windows.Media.Brushes.Gray;
            cmbAuthor.BorderBrush = System.Windows.Media.Brushes.Gray;
            cmbGenre.BorderBrush = System.Windows.Media.Brushes.Gray;
            cmbPublisher.BorderBrush = System.Windows.Media.Brushes.Gray;
            txtYear.BorderBrush = System.Windows.Media.Brushes.Gray;
            txtQuantity.BorderBrush = System.Windows.Media.Brushes.Gray;
            txtIsbn.BorderBrush = System.Windows.Media.Brushes.Gray;

            // Перевірка кожного поля
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                txtTitle.BorderBrush = System.Windows.Media.Brushes.Red;
                isValid = false;
            }
            if (cmbAuthor.SelectedValue == null && string.IsNullOrWhiteSpace(cmbAuthor.Text))
            {
                cmbAuthor.BorderBrush = System.Windows.Media.Brushes.Red;
                isValid = false;
            }
            if (cmbGenre.SelectedValue == null && string.IsNullOrWhiteSpace(cmbGenre.Text))
            {
                cmbGenre.BorderBrush = System.Windows.Media.Brushes.Red;
                isValid = false;
            }
            if (cmbPublisher.SelectedValue == null && string.IsNullOrWhiteSpace(cmbPublisher.Text))
            {
                cmbPublisher.BorderBrush = System.Windows.Media.Brushes.Red;
                isValid = false;
            }
            if (!int.TryParse(txtYear.Text, out _) || txtYear.Text.Length != 4)
            {
                txtYear.BorderBrush = System.Windows.Media.Brushes.Red;
                isValid = false;
            }
            if (!int.TryParse(txtQuantity.Text, out int quantity) || quantity < 0)
            {
                txtQuantity.BorderBrush = System.Windows.Media.Brushes.Red;
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(txtIsbn.Text))
            {
                txtIsbn.BorderBrush = System.Windows.Media.Brushes.Red;
                isValid = false;
            }

            // Повідомлення про помилку, якщо валідація не пройдена
            if (!isValid)
            {
                MessageBox.Show("Будь ласка, виправте помилки.", "Помилка валідації");
            }

            return isValid;
        }
    }
}

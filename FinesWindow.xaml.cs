using System;
using System.Windows;
using System.Windows.Controls;

namespace StudentLibraryApp
{
    // Вікно для керування штрафами
    public partial class FinesWindow : Window
    {
        // Конструктор вікна штрафів
        public FinesWindow()
        {
            // Ініціалізація компонентів
            InitializeComponent();
            // Завантаження даних про штрафи
            LoadFines();
            // Додавання обробників фокусу для текстового поля пошуку
            txtSearch.GotFocus += TxtSearch_GotFocus;
            txtSearch.LostFocus += TxtSearch_LostFocus;
        }

        // Обробник отримання фокусу текстовим полем пошуку
        private void TxtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            // Якщо текст є плейсхолдером, очистити його
            if (txtSearch.Text == "Пошук за ПІБ...")
            {
                txtSearch.Text = "";
                txtSearch.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        // Обробник втрати фокусу текстовим полем пошуку
        private void TxtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            // Якщо текстове поле порожнє, відновити плейсхолдер
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = "Пошук за ПІБ...";
                txtSearch.Foreground = System.Windows.Media.Brushes.Gray;
                // Перезавантажити всі штрафи
                LoadFines();
            }
        }

        // Метод завантаження штрафів з можливістю пошуку
        private void LoadFines(string searchTerm = "")
        {
            // Базовий запит для отримання активних штрафів
            string query = @"
                SELECT l.LoanID, r.FullName, b.Title, l.FineAmount
                FROM Loans l
                JOIN Readers r ON l.ReaderID = r.ReaderID
                JOIN Books b ON l.BookID = b.BookID
                WHERE l.FineAmount > 0 AND l.ReturnDate IS NOT NULL";

            // Додавання умови пошуку, якщо вона задана
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query += $" AND r.FullName LIKE '%{searchTerm}%'";
            }

            // Виконання запиту та заповнення таблиці
            var dt = DatabaseService.ExecuteQuery(query);
            dgFines.ItemsSource = dt.DefaultView;
        }

        // Обробник зміни тексту в полі пошуку
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Виконувати пошук тільки якщо текст не є плейсхолдером
            if (txtSearch.Text != "Пошук за ПІБ...")
            {
                LoadFines(txtSearch.Text);
            }
        }

        // Обробник натискання на кнопку "Сплатити"
        private void PayFine_Click(object sender, RoutedEventArgs e)
        {
            // Перевірка, чи відправник є кнопкою з відповідним тегом (ID видачі)
            if (sender is Button button && button.Tag is int loanId)
            {
                // Підтвердження дії користувачем
                if (MessageBox.Show("Підтвердити сплату штрафу?", "Підтвердження", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Оновлення запису в базі даних: сума штрафу робиться від'ємною, що означає "сплачено"
                        DatabaseService.ExecuteNonQuery($"UPDATE Loans SET FineAmount = FineAmount * -1 WHERE LoanID = {loanId} AND FineAmount > 0");

                        MessageBox.Show("Штраф сплачено.", "Успіх");
                        // Перезавантаження списку штрафів
                        LoadFines();
                    }
                    catch (Exception ex)
                    {
                        // Обробка помилок
                        MessageBox.Show($"Помилка при сплаті штрафу: {ex.Message}", "Помилка");
                    }
                }
            }
        }
    }
}

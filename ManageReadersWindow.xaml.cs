using System.Data;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace StudentLibraryApp
{
    // Вікно для керування даними читачів
    public partial class ManageReadersWindow : Window
    {
        // ID вибраного читача для редагування
        private int? selectedReaderId = null;

        // Конструктор вікна
        public ManageReadersWindow()
        {
            InitializeComponent();
            // Завантаження списку читачів при відкритті
            LoadReaders();
            // Встановлення значення за замовчуванням для типу читача
            cmbReaderType.SelectedIndex = 0;
        }

        // Метод для завантаження списку читачів з можливістю пошуку
        private void LoadReaders(string searchTerm = "")
        {
            string query = "SELECT * FROM Readers";
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                // Додавання умови пошуку
                query += $" WHERE FullName LIKE '%{searchTerm}%' OR CardNumber LIKE '%{searchTerm}%'";
            }
            query += " ORDER BY FullName";
            var dt = DatabaseService.ExecuteQuery(query);
            // Заповнення таблиці
            dgReaders.ItemsSource = dt.DefaultView;
        }

        // Обробник вибору рядка в таблиці читачів
        private void dgReaders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgReaders.SelectedItem is DataRowView row)
            {
                // Заповнення полів форми даними вибраного читача
                selectedReaderId = (int)row["ReaderID"];
                txtCardNumber.Text = row["CardNumber"].ToString();
                txtFullName.Text = row["FullName"].ToString();
                txtPhone.Text = row["Phone"].ToString();
                txtEmail.Text = row["Email"].ToString();
                txtGroup.Text = row["GroupOrDepartment"].ToString();
                cmbReaderType.Text = row["ReaderType"].ToString();
            }
        }

        // Метод для валідації полів форми
        private bool ValidateReader()
        {
            // Валідація номера картки
            if (string.IsNullOrWhiteSpace(txtCardNumber.Text) || txtCardNumber.Text.Length != 6 || 
                !Regex.IsMatch(txtCardNumber.Text, @"^[A-Z]{2}\d{4}$"))
            {
                MessageBox.Show("Номер картки: 2 великі літери + 4 цифри (наприклад: ST0001)", "Валідація", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Валідація ПІБ
            if (string.IsNullOrWhiteSpace(txtFullName.Text) || txtFullName.Text.Length < 2 ||
                !Regex.IsMatch(txtFullName.Text, @"^[А-ЯҐЄІЇа-яґєії'\- ]+$"))
            {
                MessageBox.Show("ПІБ може містити тільки українські літери, пробіл або дефіс.", 
                        "Валідація", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Валідація номера телефону
            if (!string.IsNullOrEmpty(txtPhone.Text) && 
                !Regex.IsMatch(txtPhone.Text, @"^(\+380|0)\d{9}$"))
            {
                MessageBox.Show("Телефон: +380XXXXXXXXX або 0XXXXXXXXX", "Валідація");
                return false;
            }

            // Валідація email
            if (!string.IsNullOrEmpty(txtEmail.Text) && 
                !Regex.IsMatch(txtEmail.Text, @"^[\w\.-]+@[\w\.-]+\.\w+$"))
            {
                MessageBox.Show("Некоректний email (наприклад: something@gmail.com)", "Валідація");
                return false;
            }

    return true;
}
        // Обробник натискання на кнопку "Додати"
        private void AddReader_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateReader()) return;

            try
            {
                // Додавання нового читача в базу даних
                DatabaseService.ExecuteNonQuery($@"
                    INSERT INTO Readers (CardNumber, FullName, Phone, Email, ReaderType, GroupOrDepartment)
                    VALUES ('{txtCardNumber.Text}', '{txtFullName.Text}', '{txtPhone.Text}', 
                            '{txtEmail.Text}', '{cmbReaderType.Text}', '{txtGroup.Text}')");

                MessageBox.Show("Читача додано.");
                // Оновлення списку та очищення полів
                LoadReaders();
                ClearFields();
            }
            catch (System.Exception ex) { MessageBox.Show("Помилка: " + ex.Message); }
        }

        // Обробник натискання на кнопку "Оновити"
        private void UpdateReader_Click(object sender, RoutedEventArgs e)
        {
            if (selectedReaderId == null) { MessageBox.Show("Оберіть читача.", "Увага"); return; }
            if (!ValidateReader()) return;

            try
            {
                // Оновлення даних читача в базі
                DatabaseService.ExecuteNonQuery($@"
                    UPDATE Readers SET 
                        CardNumber = '{txtCardNumber.Text}', 
                        FullName = '{txtFullName.Text}',
                        Phone = '{txtPhone.Text}', 
                        Email = '{txtEmail.Text}', 
                        ReaderType = '{cmbReaderType.Text}',
                        GroupOrDepartment = '{txtGroup.Text}'
                    WHERE ReaderID = {selectedReaderId}");

                MessageBox.Show("Дані оновлено.");
                LoadReaders();
            }
            catch (System.Exception ex) { MessageBox.Show("Помилка: " + ex.Message); }
        }

        // Обробник зміни тексту в полі пошуку
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadReaders(txtSearch.Text);
        }

        // Обробник натискання на кнопку "Очистити"
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
        }

        // Метод для очищення полів форми
        private void ClearFields()
        {
            selectedReaderId = null;
            txtCardNumber.Text = "";
            txtFullName.Text = "";
            txtPhone.Text = "";
            txtEmail.Text = "";
            txtGroup.Text = "";
            cmbReaderType.SelectedIndex = 0;
        }
    }
}
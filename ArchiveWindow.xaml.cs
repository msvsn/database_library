using System.Data;
using System.Windows;

namespace StudentLibraryApp
{
    // Вікно для відображення архіву зданих книг
    public partial class ArchiveWindow : Window
    {
        // Конструктор вікна архіву
        public ArchiveWindow()
        {
            // Ініціалізація компонентів вікна
            InitializeComponent();
            // Завантаження архівних даних при відкритті вікна
            LoadArchive();
        }

        // Метод для завантаження архіву зданих книг з бази даних
        private void LoadArchive()
        {
            // Виконання запиту до бази даних для отримання архівних записів
            var dt = DatabaseService.ExecuteQuery(@"
                SELECT 
                    r.FullName,
                    b.Title,
                    l.LoanDate,
                    l.ReturnDate,
                    ABS(ISNULL(l.FineAmount, 0)) AS FineAmount,
                    CASE 
                        WHEN l.FineAmount > 0 THEN 'НЕ СПЛАЧЕНО'
                        ELSE 'Сплачено'
                    END AS Status
                FROM Loans l
                JOIN Readers r ON l.ReaderID = r.ReaderID
                JOIN Books b ON l.BookID = b.BookID
                WHERE l.ReturnDate IS NOT NULL
                ORDER BY l.ReturnDate DESC");

            // Встановлення джерела даних для таблиці архіву
            dgArchive.ItemsSource = dt.DefaultView;
        }
    }
}
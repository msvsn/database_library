using System.Windows;

namespace StudentLibraryApp
{
    // Вікно для генерації та відображення звітів
    public partial class ReportsWindow : Window
    {
        // Конструктор вікна звітів
        public ReportsWindow()
        {
            InitializeComponent();
        }

        // Звіт: Топ-5 найпопулярніших книг
        private void TopBooks_Click(object sender, RoutedEventArgs e)
        {
            lblReportTitle.Text = "Топ-5 найпопулярніших книг";
            var dt = DatabaseService.ExecuteQuery(@"
                SELECT TOP 5 b.Title, a.FullName AS Author, COUNT(l.LoanID) AS TimesBorrowed
                FROM Loans l
                JOIN Books b ON l.BookID = b.BookID
                JOIN Authors a ON b.AuthorID = a.AuthorID
                GROUP BY b.Title, a.FullName
                ORDER BY TimesBorrowed DESC");
            dgReports.ItemsSource = dt.DefaultView;
        }

        // Звіт: Топ-5 найактивніших читачів
        private void TopReaders_Click(object sender, RoutedEventArgs e)
        {
            lblReportTitle.Text = "Топ-5 найактивніших читачів";
            var dt = DatabaseService.ExecuteQuery(@"
                SELECT TOP 5 r.FullName, COUNT(l.LoanID) AS BooksTaken
                FROM Loans l
                JOIN Readers r ON l.ReaderID = r.ReaderID
                GROUP BY r.FullName
                ORDER BY BooksTaken DESC");
            dgReports.ItemsSource = dt.DefaultView;
        }

        // Звіт: Статистика популярності жанрів
        private void GenreStats_Click(object sender, RoutedEventArgs e)
        {
            lblReportTitle.Text = "Статистика популярності жанрів";
            var dt = DatabaseService.ExecuteQuery(@"
                SELECT g.Name AS Genre, COUNT(l.LoanID) AS TimesBorrowed
                FROM Loans l
                JOIN Books b ON l.BookID = b.BookID
                JOIN Genres g ON b.GenreID = g.GenreID
                GROUP BY g.Name
                ORDER BY TimesBorrowed DESC");
            dgReports.ItemsSource = dt.DefaultView;
        }

        // Звіт: Книги, які не видавались більше року
        private void IdleBooks_Click(object sender, RoutedEventArgs e)
        {
            lblReportTitle.Text = "Книги, які не видавались більше року";
            var dt = DatabaseService.ExecuteQuery(@"
                SELECT b.Title, a.FullName AS Author, MAX(l.LoanDate) AS LastLoanDate
                FROM Books b
                LEFT JOIN Loans l ON b.BookID = l.BookID
                JOIN Authors a ON b.AuthorID = a.AuthorID
                GROUP BY b.Title, a.FullName
                HAVING MAX(l.LoanDate) < DATEADD(year, -1, GETDATE()) OR MAX(l.LoanDate) IS NULL
                ORDER BY LastLoanDate ASC");
            dgReports.ItemsSource = dt.DefaultView;
        }

        // Звіт: Загальна сума зібраних штрафів
        private void FinesSummary_Click(object sender, RoutedEventArgs e)
        {
            lblReportTitle.Text = "Загальна сума зібраних штрафів";
            var dt = DatabaseService.ExecuteQuery(@"
                SELECT SUM(ABS(FineAmount)) AS TotalFines
                FROM Loans
                WHERE FineAmount < 0");
            dgReports.ItemsSource = dt.DefaultView;
        }

        // Звіт: Майстер-звіт по зв'язках
        private void MasterReport_Click(object sender, RoutedEventArgs e)
        {
            lblReportTitle.Text = "Майстер-звіт (особи, книги та їх зв'язки)";
            var dt = DatabaseService.ExecuteQuery(@"
                SELECT
                    COALESCE(Person, 'Невідома особа') AS Person,
                    Type,
                    COALESCE(BookTitle, 'Немає пов''язаних книг') AS Book,
                    Relationship
                FROM (
                    SELECT
                        R.FullName AS Person,
                        'Читач' AS Type,
                        B.Title AS BookTitle,
                        'Брав(ла) книгу' AS Relationship
                    FROM Readers R
                    LEFT JOIN Loans L ON R.ReaderID = L.ReaderID
                    LEFT JOIN Books B ON L.BookID = B.BookID
                    UNION ALL
                    SELECT
                        A.FullName AS Person,
                        'Автор' AS Type,
                        B.Title AS BookTitle,
                        'Автор книги' AS Relationship
                    FROM Books B
                    FULL OUTER JOIN Authors A ON B.AuthorID = A.AuthorID
                ) AS MasterList
                ORDER BY Person, Book;
            ");
            dgReports.ItemsSource = dt.DefaultView;
        }

        // Звіт: Аналіз інтересів читачів за жанрами
        private void ReaderInterests_Click(object sender, RoutedEventArgs e)
        {
            lblReportTitle.Text = "Аналіз інтересів читачів за жанрами";
            var dt = DatabaseService.ExecuteQuery(@"
                SELECT TOP 200 R.FullName, G.Name AS Genre, COUNT(B.BookID) AS BooksBorrowedInGenre
                FROM Readers R
                CROSS JOIN Genres G
                LEFT JOIN Loans L ON L.ReaderID = R.ReaderID
                LEFT JOIN Books B ON L.BookID = B.BookID AND B.GenreID = G.GenreID
                GROUP BY R.FullName, G.Name
                ORDER BY R.FullName, BooksBorrowedInGenre DESC;
            ");
            dgReports.ItemsSource = dt.DefaultView;
        }
    }
}

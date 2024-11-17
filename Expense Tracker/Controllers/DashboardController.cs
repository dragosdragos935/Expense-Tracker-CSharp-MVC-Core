using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Expense_Tracker.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Define date range: Last 7 days
            DateTime startDate = DateTime.Today.AddDays(-6);
            DateTime endDate = DateTime.Today;

            // Fetch transactions within the date range, including category details
            var selectedTransactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Date >= startDate && t.Date <= endDate)
                .ToListAsync();

            // Calculate Total Income
            int totalIncome = selectedTransactions
                .Where(t => t.Category.Type.Equals("Income", StringComparison.OrdinalIgnoreCase))
                .Sum(t => t.Amount);
            ViewBag.TotalIncome = totalIncome.ToString("C0");

            // Calculate Total Expense
            int totalExpense = selectedTransactions
                .Where(t => t.Category.Type.Equals("Expense", StringComparison.OrdinalIgnoreCase))
                .Sum(t => t.Amount);
            ViewBag.TotalExpense = totalExpense.ToString("C0");

            // Calculate Balance
            int balance = totalIncome - totalExpense;

            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            culture.NumberFormat.CurrencyNegativePattern = 1;

            ViewBag.Balance = String.Format(culture,"{0:C0}",balance);


            //Doughnut Chart -Expense By Category

            ViewBag.DoughnutChartData = selectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Category.CategoryId)
                .Select(k => new
                {
                    categoryTitleWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
                    amount = k.Sum(j=>j.Amount),
                    formattedAmount = k.Sum(j => j.Amount),
                })
                .OrderByDescending(l=>l.amount)
                .ToList();

            //Spline Chart- Income vs Expense

            //Income

            List<SplineChartData> IncomeSummary =selectedTransactions
                .Where(i=>i.Category.Type=="Income")
                .GroupBy(j=>j.Date)
                .Select(k=>new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMMM"),
                    income=k.Sum(l=>l.Amount)
                })
                .ToList();



            //Expense
            List<SplineChartData> ExpenseSummary = selectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMMM"),
                    expense = k.Sum(l => l.Amount)
                })
                .ToList();
            //Combine Income & Expense

            string[] Last7Days = Enumerable.Range(0, 7)
                .Select(i=>startDate.AddDays(i).ToString("dd-MMMM"))
                .ToArray();


            ViewBag.SplineChartData = from day in Last7Days
                                      join income in IncomeSummary on day equals income.day
                                      into dayIncomeJoined
                                      from income in dayIncomeJoined.DefaultIfEmpty()
                                      join expense in ExpenseSummary on day equals expense.day into expenseJoined
                                      from expense in expenseJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          income = income == null ? 0 : income.income,
                                          expense = expense == null ? 0 : expense.expense,

                                      };

            //Recent Transactions
            ViewBag.RecentTransactions = await _context.Transactions
                .Include(i=>i.Category)
                .OrderByDescending(j=>j.Date)
                .Take(5)
                .ToListAsync();

            return View();
        }
    }


    public class SplineChartData
    {
        public string day;
        public int income;
        public int expense;
    }



}

using Data_service.Data;
using Data_service.IRepository;
using Entities_Dtos.DTOs;
using Entities_Dtos.Types;
using Entities_Dtos.Responses;
using Entities_Dtos.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Data_service.Repository;

public class ReportingRepository : IReportingRepository
{
    private readonly SMSAlertDbContext _context;
    private readonly ILogger<ReportingRepository> _logger;

    public ReportingRepository(SMSAlertDbContext context, ILogger<ReportingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AlertChargeReport> GetAlertChargeReportAsync(AlertChargeSearchDto searchDto)
    {
        try
        {
            var query = _context.SMSAlerts
                .Include(a => a.Customer)
                .Include(a => a.Account)
                .Include(a => a.DebitQueue)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchDto.Email))
                query = query.Where(a => a.Customer.Email == searchDto.Email);

            if (!string.IsNullOrEmpty(searchDto.AccountNumber))
                query = query.Where(a => a.Account.AccountNumber == searchDto.AccountNumber);

            if (!string.IsNullOrEmpty(searchDto.ChargeStatus))
                query = query.Where(a => a.IsCharged == (searchDto.ChargeStatus.ToLower() == "charged"));

            query = query.Where(a =>
                a.CreatedAt >= searchDto.StartDate &&
                a.CreatedAt <= searchDto.EndDate);

            // Get paginated results
            var totalCount = await query.CountAsync();
            var pageSize = searchDto.PageSize;
            var pageNumber = searchDto.PageNumber;

            var alerts = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var failedCharges = alerts
                .Where(a => !a.IsCharged && a.DebitQueue?.Status == QueueStatus.Failed)
                .Select(a => new FailedChargeDetail
                {
                    Email = a.Customer.Email,
                    AccountNumber = a.Account?.AccountNumber,
                    ChargeAmount = a.ChargeAmount + a.VATAmount,
                    FailureReason = a.DebitQueue?.FailureReason,
                    FailureDate = a.DebitQueue?.LastRetryDate ?? a.DebitQueue.CreatedAt
                })
                .ToList();

            return new AlertChargeReport
            {
                Date = DateTime.UtcNow,
                TotalAlerts = totalCount,
                SuccessfulCharges = alerts.Count(a => a.IsCharged),
                FailedCharges = alerts.Count(a => !a.IsCharged),
                TotalChargeAmount = alerts.Sum(a => a.ChargeAmount),
                TotalVatAmount = alerts.Sum(a => a.VATAmount),
                FailedChargeDetails = failedCharges
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetAlertChargeReportAsync method error", typeof(ReportingRepository));
            return null;
        }
    }

    public async Task<UnrecoupedChargesReportDto> GetUnrecoupedChargesReportAsync(DateTime asOfDate)
    {
        try
        {
            var unrecoupedDebits = await _context.DirectDebitQueues
                .Include(d => d.Customer)
                .Include(d => d.SourceAccount)
                .Include(d => d.SMSAlert)
                .Where(d => d.Status == QueueStatus.Failed &&
                       d.CreatedAt <= asOfDate)
                .ToListAsync();

            var details = unrecoupedDebits
                .GroupBy(d => new { d.Customer.Email, d.SourceAccount.AccountNumber })
                .Select(g => new UnrecoupedDetailDto
                {
                    Email = g.Key.Email,
                    AccountNumber = g.Key.AccountNumber,
                    UnrecoupedAmount = g.Sum(d => d.TotalChargeAmount),
                    RetryCount = g.Max(d => d.RetryCount),
                    FirstAttemptDate = g.Min(d => d.CreatedAt),
                    LastAttemptDate = g.Max(d => d.LastRetryDate ?? d.CreatedAt)
                })
                .ToList();

            return new UnrecoupedChargesReportDto
            {
                AsOfDate = asOfDate,
                TotalUnrecoupedAmount = details.Sum(d => d.UnrecoupedAmount),
                TotalUnrecoupedCount = details.Count,
                Details = details
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetUnrecoupedChargesReportAsync method error", typeof(ReportingRepository));
            return null;
        }
    }

    public async Task<DailyReconciliationDto> GetDailyReconciliationAsync(DateTime date)
    {
        try
        {
            var startDate = date.Date;
            var endDate = date.Date.AddDays(1).AddSeconds(-1);

            var accountingEntries = await _context.AccountingEntries
                .Where(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate)
                .ToListAsync();

            var telcoCharges = accountingEntries
                .Where(e => e.EntryType == EntryType.TelcoSessionCharge)
                .GroupBy(e => e.CreditAccountNumber)
                .ToDictionary(
                    g => GetTelcoProviderName(g.Key),
                    g => g.Sum(e => e.DebitAmount)
                );

            return new DailyReconciliationDto
            {
                Date = date,
                TotalSMSCharges = accountingEntries
                    .Where(e => e.EntryType == EntryType.SMSAlertCharge)
                    .Sum(e => e.DebitAmount),
                TotalQBECharges = accountingEntries
                    .Where(e => e.EntryType == EntryType.QuickBalanceEnquiryCharge)
                    .Sum(e => e.DebitAmount),
                TotalVatCollected = accountingEntries
                    .Where(e => e.EntryType == EntryType.VATDebit)
                    .Sum(e => e.VATAmount),
                TotalTelcoCharges = accountingEntries
                    .Where(e => e.EntryType == EntryType.TelcoSessionCharge)
                    .Sum(e => e.DebitAmount),
                TelcoProviderCharges = telcoCharges
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetDailyReconciliationAsync method error", typeof(ReportingRepository));
            return null;
        }
    }

    public async Task<IEnumerable<FailedChargeDetail>> GetFailedChargesDetailAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var failedCharges = await _context.DirectDebitQueues
                .Include(d => d.Customer)
                .Include(d => d.SourceAccount)
                .Include(d => d.SMSAlert)
                .Where(d => d.Status == QueueStatus.Failed &&
                       d.CreatedAt >= startDate &&
                       d.CreatedAt <= endDate)
                .Select(d => new FailedChargeDetail
                {
                    Email = d.Customer.Email,
                    AccountNumber = d.SourceAccount.AccountNumber,
                    ChargeAmount = d.TotalChargeAmount,
                    FailureReason = d.FailureReason,
                    FailureDate = d.LastRetryDate ?? d.CreatedAt
                })
                .ToListAsync();

            return failedCharges;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetFailedChargesDetailAsync method error", typeof(ReportingRepository));
            return new List<FailedChargeDetail>();
        }
    }

    private string GetTelcoProviderName(string accountNumber)
    {
        return accountNumber switch
        {
            var acc when acc == AccountingConstants.AccountNumbers.TelcoSuspense.MTN => "MTN",
            var acc when acc == AccountingConstants.AccountNumbers.TelcoSuspense.AIRTEL => "AIRTEL",
            var acc when acc == AccountingConstants.AccountNumbers.TelcoSuspense.GLO => "GLO",
            var acc when acc == AccountingConstants.AccountNumbers.TelcoSuspense.NINE_MOBILE => "9MOBILE",
            _ => "UNKNOWN"
        };
    }
}

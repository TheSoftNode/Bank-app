namespace Entities_Dtos.Types;

public enum AccountType
{
    Savings,
    Current,
    Domiciliary
}

public enum CurrencyType
{
    NGN,
    USD,
    EUR,
    GBP
}

public enum AlertType
{
    TransactionNotification,
    QuickBalanceEnquiry,
    AccountStatement,
    SecurityAlert
}

public enum DeliveryStatus
{
    Pending,
    Delivered,
    Failed,
    Expired
}

public enum QueueStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    RetryQueued
}

public enum EntryType
{
    SMSAlertCharge,
    QuickBalanceEnquiryCharge,
    TelcoSessionCharge,
    VATDebit
}

public enum TelcoProvider
{
    MTN,
    Airtel,
    Glo,
    NineMobile
}

public enum TransactionType
{
    Credit,
    Debit,
    Reversal
}

public enum UserRole
{
    Customer,
    Admin,
    SuperAdmin
}

public enum BatchChargeStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
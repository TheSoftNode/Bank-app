namespace Entities_Dtos.Constants;

public static class AccountingConstants
{
    public const decimal SMS_ALERT_CHARGE = 4.0M;
    public const decimal VAT_RATE = 0.075M;
    public const decimal QBE_CHARGE = 10.0M;
    public const decimal TELCO_SESSION_CHARGE = 6.98M;

    public static class AccountNumbers
    {
        public const string SMS_ALERT_INCOME = "XXX55190000301";
        public const string QBE_ALERT_INCOME = "XXX55190000303";
        public const string VAT_PAYABLE = "XXX33104000101";
        public const string USSD_INCOME = "XXX55099006601";

        public static class TelcoSuspense
        {
            public const string MTN = "48934389041101";
            public const string AIRTEL = "48934389041201";
            public const string NINE_MOBILE = "48934389041301";
            public const string GLO = "48934389041401";
        }
    }
}

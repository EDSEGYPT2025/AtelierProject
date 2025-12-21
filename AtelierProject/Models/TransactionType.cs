namespace AtelierProject.Models
{
    public enum TransactionType
    {
        Income = 1,        // إيراد (إيجار، خدمات صالون)
        Expense = 2,       // مصروف (خارج من الخزنة)
        InsuranceIn = 3,   // استلام تأمين (أمانات داخلة)
        InsuranceOut = 4   // رد تأمين (أمانات خارجة)
    }
}
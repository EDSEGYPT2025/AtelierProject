using System.ComponentModel.DataAnnotations;

namespace AtelierProject.Models
{
    public enum TransactionType
    {
        [Display(Name = "إيراد")]
        Income = 1,

        [Display(Name = "مصروف")]
        Expense = 2,

        [Display(Name = "استلام تأمين (عهدة)")]
        InsuranceIn = 3,

        [Display(Name = "رد تأمين")]
        InsuranceOut = 4
    }
}
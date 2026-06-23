using System;

namespace PCS_API.Models
{
    public class InvestmentInterestScheduleModel
    {
        public Guid ScheduleId { get; set; }
        public Guid InvestmentId { get; set; }
        public int StartMonth { get; set; }
        public int EndMonth { get; set; }
        public decimal InterestRate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace CharityRabbit.Models
{
    public class GoodWorksModel
    {
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required.")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; } = string.Empty;

        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public double Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public double Longitude { get; set; }

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public TimeSpan? EstimatedDuration { get; set; }

        [Required(ErrorMessage = "Contact Name is required.")]
        public string ContactName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string ContactEmail { get; set; } = string.Empty;

        [Phone]
        public string ContactPhone { get; set; } = string.Empty;

        public string EffortLevel { get; set; } = "Moderate";

        public bool IsAccessible { get; set; } = false;
        public bool IsVirtual { get; set; } = false;
    }

}

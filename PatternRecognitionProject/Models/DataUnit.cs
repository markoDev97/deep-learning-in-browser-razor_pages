using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PatternRecognitionProject.Models
{
    public class DataUnit
    {
        public int ID { get; set; }
        [Required]
        [Range(0, 3)]
        [Display(Name="Класна ознака")]
        public int ClassLabel { get; set; }
    }
}

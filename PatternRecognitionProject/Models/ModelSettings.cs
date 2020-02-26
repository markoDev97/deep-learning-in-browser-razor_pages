using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PatternRecognitionProject.Models
{
    public class ModelSettings
    {
        public float LearningRate { get; set; }
        public int NoEpochs { get; set; }
        public string Framework { get; set; }
    }
}

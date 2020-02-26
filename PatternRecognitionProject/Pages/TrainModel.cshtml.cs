using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using PatternRecognitionProject.Models;
using ScottPlot;

namespace PatternRecognitionProject
{
    public class TrainModel : PageModel
    {
        private readonly TrainingServiceProvider _serviceProvider;
        public string TrainingPlotFileName { get; private set; }
        public int Class { get; private set; }
        public ShowClassifyButtonSingleton ButtonSingleton { get; set; }
        public TrainModel(TrainingServiceProvider serviceProvider, ShowClassifyButtonSingleton buttonSingleton)
        {
            _serviceProvider = serviceProvider;
            Class = -1;
            ButtonSingleton = buttonSingleton;
        }
        public void OnGet()
        {

        }
        public void OnPost(string learningRate, string noEpochs, string framework)
        {
            if (float.TryParse(learningRate, out var lr) && int.TryParse(noEpochs, out var ne)  && lr!=0 && noEpochs!=null)
            {
                var result = _serviceProvider.Train(new ModelSettings
                {
                    LearningRate = lr,
                    NoEpochs = ne,
                    Framework = framework
                });
                var epochs = result.Item1; var accuracies = result.Item2; var valAccuracies = result.Item3; 
                var plt = new Plot(400, 300);
                plt.PlotScatter(epochs, accuracies, Color.Red, label: "Training accuracy");
                if (valAccuracies!=null)
                    plt.PlotScatter(epochs, valAccuracies, Color.Cyan, label: "Validation accuracy");
                plt.Legend(location: legendLocation.upperLeft);
                plt.SaveFig("wwwroot/AccuracyPlot.jpg");
                ButtonSingleton.Show = true;
            }
        }
        public void OnPostPicture()
        {
            var capture = new VideoCapture(0);
            capture.Open(0);
            var frame = new Mat();
            if (capture.IsOpened())
            {
                Thread.Sleep(500);
                capture.Read(frame);
                frame = frame
                    .CvtColor(ColorConversionCodes.BGR2GRAY)
                    .PyrDown()
                    .PyrDown()
                    .PyrDown();
                var image = BitmapConverter.ToBitmap(frame);
                Class = _serviceProvider.Classify(image);
            }
            capture.Dispose();
        }
    }
}

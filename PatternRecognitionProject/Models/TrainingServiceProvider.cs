using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConvNetSharp.Core;
using ConvNetSharp.Core.Fluent;
using ConvNetSharp.Core.Training;
using ConvNetSharp.Volume;
using ConvNetSharp.Volume.Double;
using PatternRecognitionProject.Data;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Keras.Layers;
using Keras.Models;
using Numpy;
using Keras.Optimizers;
using ScottPlot;

namespace PatternRecognitionProject.Models
{
    public class TrainingServiceProvider
    {
        private readonly SgdTrainer<double> _trainer;
        private List<double[]> _images;
        private List<double[]> _labels;
        private string _framework;
        private readonly Sequential _model;
        private readonly FluentNet<double> _cnn;
        public TrainingServiceProvider()
        {
            _framework = "keras";
            _model = BuildKerasModel();
            _cnn = BuildCNN();
            _trainer = new SgdTrainer<double>(_cnn);
        }
        public Tuple<double[], double[], double[]> Train(ModelSettings settings)
        {
            _framework = settings.Framework;
            if (_framework == "keras")
                return TrainKerasModel(settings);
            else
                return TrainConvNetSharpModel(settings);
        }
        public int Classify(Bitmap bitmap)
        {
            if (_framework == "keras")
            {
                var pred = ClassFromNumpy(_model.Predict(np.array(ImagePixels(bitmap))));
                return pred;
            }
            else
            {
                _trainer.Net.Forward(BuilderInstance.Volume.From(ImagePixels(bitmap), new Shape(80, 60, 1)));
                var predictedClass = _trainer.Net.GetPrediction();
                return predictedClass[0];
            }
        }
        public int ClassFromNumpy(NDarray darray)
        {
            var array = new List<double>(darray.GetData<double>());
            return array.IndexOf(array.Max());
        }
        private Tuple<double[], double[], double[]> TrainConvNetSharpModel(ModelSettings settings)
        {
            EditTrainerSettings(settings);
            return TrainModel(settings);
        }
        private Tuple<double[], double[], double[]> TrainKerasModel(ModelSettings settings)
        {
            var data = GetData();
            var xTrain = data.Item1.Take((int)Math.Floor(0.8 * data.Item1.Count));
            var yTrain = data.Item2.Take((int)Math.Floor(0.8 * data.Item2.Count));
            var xVal = data.Item1.TakeLast(data.Item1.Count - (int)Math.Floor(0.8 * data.Item1.Count));
            var yVal = data.Item2.TakeLast(data.Item2.Count - (int)Math.Floor(0.8 * data.Item2.Count));
            var xValidation = xVal.Take(xVal.Count() / 2);
            var yValidation = yVal.Take(yVal.Count() / 2);
            var history = _model.Fit(np.array(xTrain), np.array(yTrain), epochs: settings.NoEpochs, validation_data: new NDarray[]
                { np.array(xValidation), np.array(yValidation)});
            var accuracies = history.HistoryLogs["accuracy"];
            var valAccuracies = history.HistoryLogs["val_accuracy"];
            var epochs = new double[history.Epoch.Length];
            for (var i = 0; i < history.Epoch.Length; i++)
                epochs[i] = history.Epoch[i] + 1;
            return new Tuple<double[], double[], double[]>(epochs, accuracies, valAccuracies);
        }
        private Tuple<double[], double[], double[]> TrainModel(ModelSettings settings)
        {
            var data = GetConvNetSharpData();
            _images = data.Item1;
            _labels = data.Item2;
            var confusionMatrix = new int[settings.NoEpochs, 4, 4];
            var validationConfusionMatrix = new int[settings.NoEpochs, 4, 4];
            var threshold = (int)Math.Floor(0.9*_images.Count);
            for (var k = 0; k < settings.NoEpochs; k++)
            {
                for (var i = 0; i < threshold; i++)
                {
                    var image = _images[i];
                    var vol = BuilderInstance.Volume.From(_labels[i], new Shape(1, 1, 4, 1));
                    try
                    {
                        _trainer.Train(BuilderInstance.Volume.From(image, new Shape(80, 60, 1)), vol);
                    }
                    catch(ArgumentException)
                    {

                    }
                    var prediction = _trainer.Net.GetPrediction()[0];
                    confusionMatrix[k, LabelFromOneHot(_labels[i]), prediction]++;
                }
                for (var i = threshold; i < _images.Count; i++)
                {
                    var image = _images[i];
                    _trainer.Net.Forward(BuilderInstance.Volume.From(image, new Shape(80, 60, 1)));
                    var prediction = _trainer.Net.GetPrediction()[0];
                    validationConfusionMatrix[k, LabelFromOneHot(_labels[i]), prediction]++;
                }
            }
            return GetEpochsAndAccuracies(confusionMatrix, settings.NoEpochs, validationConfusionMatrix, threshold);
        }
        private Tuple<double[], double[], double[]> GetEpochsAndAccuracies(int[,,] confusionMatrix, int noEpochs, 
            int[,,] validationConfusionMatrix, int threshold)
        {
            var epochs = new double[noEpochs];
            var accuracies = new double[noEpochs];
            var valAccuracies = new double[noEpochs];
            for (var i = 0; i < noEpochs; i++)
            {
                epochs[i] = i + 1;
                accuracies[i] = GetAccuracy(confusionMatrix, i, threshold);
                valAccuracies[i] = GetAccuracy(validationConfusionMatrix, i, _images.Count-threshold);
            }
            return new Tuple<double[], double[], double[]>(epochs, accuracies, valAccuracies);
        }
        private double GetAccuracy(int[,,] confusionMatrix, int epoch, int datasetSamples)
        {
            var accuracy = 0.0;
            for (var i = 0; i < 4; i++)
                accuracy += confusionMatrix[epoch, i, i];
            return accuracy / datasetSamples;
        }
        private void EditTrainerSettings(ModelSettings settings)
        {
            _trainer.LearningRate = settings.LearningRate;
            _trainer.Momentum = 0.34;
        }
        private Sequential BuildKerasModel()
        {
            var model = new Sequential();
            model.Add(new Dense(50, activation: "tanh"));
            model.Add(new Dense(20));
            model.Add(new Flatten());
            model.Add(new Dense(4, activation: "softmax"));
            model.Compile("sgd", "categorical_crossentropy", new string[] { "accuracy" });
            return model;
        }
        private FluentNet<double> BuildCNN() =>
            FluentNet<double>.Create(80, 60, 1)
            .Conv(5, 5, 7)
            .Relu()
            .Pool(2, 2)
            .Conv(5, 5, 5)
            .Relu()
            .Pool(2, 2)
            .FullyConn(40)
            .Tanh()
            .FullyConn(4)
            .Softmax(4)
            .Build();

        private double[] ImagePixels(Bitmap img)
        {
            var allNumbers = new List<double>();
            for (var i = 0; i < img.Width; i++)
            {
                for (var j = 0; j < img.Height; j++)
                {
                    var color = img.GetPixel(i, j);
                    allNumbers.Add(color.R);//всушност има само еден канал
                }
            }
            return allNumbers.ToArray();
        }
        private double[] OneHotFromLabel(int label)
        {
            var vec = new double[4];
            vec[label] = 1.0;
            return vec;
        }
        private int LabelFromOneHot(double[] oneHot)
        {
            for(var i=0; i<oneHot.Length; i++)
            {
                if (oneHot[i] == 1.0)
                    return i;
            }
            return -1;
        }
        private double[,] ImageToDouble(Bitmap image)
        {
            var res = new double[image.Width, image.Height];
            for (var i = 0; i < image.Width; i++)
            {
                for (var j = 0; j < image.Height; j++)
                    res[i, j] = image.GetPixel(i, j).R;
            }
            return res;
        }
        private Tuple<List<double[]>, List<double[]>> GetConvNetSharpData()
        {
            var res = new List<double[]>();
            var vectors = new List<double[]>();
            var files = Directory.GetFiles(@"wwwroot\Dataset");
            foreach (var file in files)
            {
                var image = BitmapConverter.ToBitmap(BitmapConverter.ToMat((Bitmap)Image.FromFile($"{file}")).PyrDown());
                var imgToDouble = ImagePixels(image);
                res.Add(imgToDouble);
                var label = int.Parse(file.Split(new char[] { '_' })[1].Split(new char[] { '.' })[0]);
                vectors.Add(OneHotFromLabel(label));
            }
            return new Tuple<List<double[]>, List<double[]>>(res, vectors);
        }
        private Tuple<List<NDarray<double>>, List<NDarray<double>>> GetData()
        {
            var res = new List<NDarray<double>>();
            var vectors = new List<NDarray<double>>();
            var files = Directory.GetFiles(@"wwwroot\Dataset");
            var keyValue = new Dictionary<int, int>();
            foreach (var file in files)
            {
                var image = BitmapConverter.ToBitmap(BitmapConverter.ToMat((Bitmap)Image.FromFile($"{file}")).PyrDown());
                var imgToDouble = ImageToDouble(image);
                var npArray = np.array(imgToDouble);
                res.Add(npArray);
                var label = int.Parse(file.Split(new char[] { '_' })[1].Split(new char[] { '.' })[0]);
                vectors.Add(np.array(OneHotFromLabel(label)));
                if (keyValue.ContainsKey(label))
                    keyValue[label]++;
                else
                    keyValue[label] = 1;
            }
            Console.WriteLine("0 : " + keyValue[0]);
            Console.WriteLine("1 : " + keyValue[1]);
            Console.WriteLine("2 : " + keyValue[2]);
            Console.WriteLine("3 : " + keyValue[3]);
            return new Tuple<List<NDarray<double>>, List<NDarray<double>>>(res, vectors);
        }
    }
}

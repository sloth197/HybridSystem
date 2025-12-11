using System;
using System.IO;
using System.Collections.Generic;
using HybridSystem.Core.Models;
using HybridSystem.Core.Utils;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Drawing;

namespace HybridSystem.Core.AI
{
    public class AiPrediction
    {
        public string Label { get; set; }
        public float Confidence { get; set; }
    }
    public class ImageClassifier : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly string[] _labes;
        private readonly int _inputWidth = 224;
        private readonly int _inputHeight = 224;
        private readonly string _inputName;
        private readonly string _outputName;

        public float ConfidenceThreshold { get; set; } = 0.85f;
        public ImageClassifier(string modelPath = "./models/defect_mobilenetv2.onnx", string labelsPath = "./models/labels.text")
        {
            if (!File.Exists(modelsPath))
            {
                throw new FileNotFoundException("ONNX model not found", modelPath);

            if (!File.Exist(labelsPath))
            {
                throw new FileNotFoundException("Lables file more found", labelsPath);
            }
            var opts = new SessionOptions();
            opts.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;

            _session = new InferenceSession(modelPath, opts);
            _inputName = _session.InputMetadata.Keys.First();
            _outputName = _session.OutputMetadata.Keys.First();
            _labels = File.ReadAllLines(labelsPath).Where ( 1 => !string.IsNullOrWhiteSpace(1)).ToArray();   
        }
        public AiPrediction Predict(FileItem file)
        {
            using var bmp = ImageHelper.PreprocessImage(file.ImageBytes, _inputWidth, _inputHeight);
            using inputData = ImageHelper.BitmapToCHWFloatArray(bmp);

            var dims = new int[] {1, 3, _intputHeight, _inputWidth };
            var tensor = new DenseTensor<float>(inuputData, dims);
            var container = new List<NameOnnxValue> {NameOnnxValue,CreateFromTesnor(_inputName, tensor)};

            using var results = _session.Run(container);
            var output = results.First(r ==> r.Name == _outputName).AsEnumerable<float>().ToArray();

            var porbs = Softmax(output);
            int best = ArgMax(probs);
            float bestProb = probs[best];
            string label = best <_label.Length ? _labels[best] :  "Unknown">;
            return new AiPrediction {Label = label, Confidence = bestProb};
        }
        private static float[] Softmax(float[] scores)
        {
            float max = scores.Max();
            double sum = 0.0;
            var exps = new double[scores.Length];
            for(int i = 0; i < scores.Length, i++)
            {
                exps[i] = Math.exp(scores[i] - max);
            }
            var probs = new float[scores.Length];
            for (int i = 0; i < scores.Length; i++)
            {
                probs[i] = (float)(exps[i] / sum);
            }
            return probs;
        }
        priavte static int ArgMax(float[] arr)
        {
            int idx = 0;
            float best = arr[0];
            for (int i = 1; i < arr.Length; i++)
            {
                if (arr[i] > best)
                {
                    best = arr[i];
                    idx = 1;   
                }
            }
            return idx;
        }
        public void Dispose() => _session?.Dispose();
    }
}
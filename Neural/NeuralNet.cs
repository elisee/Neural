using System;
using System.Diagnostics;

namespace Neural
{
    public class NeuralNet
    {
        public class Layer
        {
            public readonly float[] Biases;
            public readonly float[] Weights;
            public readonly float[] Values;

            public Layer(int count, int previousCount)
            {
                Biases = new float[count];
                Weights = new float[count * previousCount];
                Values = new float[count];
            }
        }

        public readonly Layer[] Layers;

        public NeuralNet(int[] neuronCountByLayer)
        {
            Layers = new Layer[neuronCountByLayer.Length - 1];
            for (var i = 1; i < neuronCountByLayer.Length; i++) Layers[i - 1] = new Layer(neuronCountByLayer[i], neuronCountByLayer[i - 1]);

            // Setup random weights
            var random = new Random();

            // See https://stackoverflow.com/questions/218060/random-gaussian-variables
            double StandardNormalRandom()
            {
                var u1 = 1.0 - random.NextDouble();
                var u2 = 1.0 - random.NextDouble();
                return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            }

            foreach (var layer in Layers)
            {
                for (var i = 0; i < layer.Weights.Length; i++) layer.Weights[i] = (float)StandardNormalRandom();
            }
        }

        public float[] Run(float[] inputs)
        {
            Debug.Assert(inputs.Length == Layers[0].Weights.Length / Layers[0].Values.Length);

            // Forward propagation
            var previousLayerValues = inputs;

            foreach (var layer in Layers)
            {
                for (var i = 0; i < layer.Values.Length; i++)
                {
                    var value = layer.Biases[i];

                    for (var j = 0; j < previousLayerValues.Length; j++) value += previousLayerValues[j] * layer.Weights[i];

                    // Sigmoid
                    // layer.Values[i] = 1f / (1f + MathF.Exp(-value));

                    // ReLU
                    layer.Values[i] = value > 0f ? value : 0f;
                }

                previousLayerValues = layer.Values;
            }

            return previousLayerValues;
        }
    }
}

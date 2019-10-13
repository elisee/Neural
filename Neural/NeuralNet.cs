#define SIGMOID

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
            public readonly float[] Deltas;

            public readonly float[] Outputs;

            public Layer(int count, int previousLayerCount)
            {
                Biases = new float[count];
                Weights = new float[count * previousLayerCount];
                Deltas = new float[count];

                Outputs = new float[count];
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

                // Biases should be left at 0
                // for (var i = 0; i < layer.Biases.Length; i++) layer.Biases[i] = (float)StandardNormalRandom();
            }
        }

        public float[] Compute(float[] inputs)
        {
            Debug.Assert(inputs.Length == Layers[0].Weights.Length / Layers[0].Outputs.Length);

            // Forward propagation
            var previousLayerActivations = inputs;

            foreach (var layer in Layers)
            {
                for (var i = 0; i < layer.Outputs.Length; i++)
                {
                    var value = layer.Biases[i];

                    for (var j = 0; j < previousLayerActivations.Length; j++) value += previousLayerActivations[j] * layer.Weights[i * previousLayerActivations.Length + j];

#if SIGMOID
                    // Sigmoid
                    layer.Outputs[i] = 1f / (1f + MathF.Exp(-value));
#else
                    // ReLU
                    layer.Outputs[i] = value > 0f ? value : 0f;
#endif
                }

                previousLayerActivations = layer.Outputs;
            }

            return Layers[Layers.Length - 1].Outputs;
        }

        public void BackpropagateError(float[] expectedOutputs)
        {
            // Compute error for output layer
            {
                var layer = Layers[Layers.Length - 1];

                for (var i = 0; i < layer.Outputs.Length; i++)
                {
                    var error = expectedOutputs[i] - layer.Outputs[i];

#if SIGMOID
                    // Sigmoid derivative
                    var derivative = layer.Outputs[i] * (1f - layer.Outputs[i]);
#else
                    // ReLU derivative
                    var derivative = layer.Outputs[i] > 0f ? 1f : 0f;
#endif

                    layer.Deltas[i] = error * derivative;
                }
            }

            // Propagate error to hidden layers
            for (var l = Layers.Length - 2; l >= 0; l--)
            {
                var layer = Layers[l];
                var nextLayer = Layers[l + 1];

                for (var i = 0; i < layer.Outputs.Length; i++)
                {
                    var error = 0f;

                    for (var j = 0; j < nextLayer.Outputs.Length; j++)
                    {
                        var weight = nextLayer.Weights[j * layer.Outputs.Length + i];
                        error += nextLayer.Deltas[j] * weight;
                    }

#if SIGMOID
                    // Sigmoid derivative
                    var derivative = layer.Outputs[i] * (1f - layer.Outputs[i]);
#else
                    // ReLU derivative
                    var derivative = layer.Outputs[i] > 0f ? 1f : 0f;
#endif

                    layer.Deltas[i] = error * derivative;
                }
            }
        }

        public void Train(float learningRate, float[] inputs)
        {
            for (var l = 0; l < Layers.Length - 1; l++)
            {
                var layer = Layers[l];

                for (var i = 0; i < layer.Outputs.Length; i++)
                {
                    for (var j = 0; j < inputs.Length; j++)
                    {
                        layer.Weights[i * inputs.Length + j] += learningRate * layer.Deltas[i] * inputs[j];
                    }

                    layer.Biases[i] += learningRate * layer.Deltas[i] * 1f;
                }

                inputs = layer.Outputs;
            }
        }
    }
}

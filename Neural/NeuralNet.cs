using System;
using System.Diagnostics;

namespace Neural
{
    class NeuralNet
    {
        class Layer
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

        readonly Layer[] _layers;

        public NeuralNet(int[] neuronCountByLayer)
        {
            _layers = new Layer[neuronCountByLayer.Length - 1];
            for (var i = 1; i < neuronCountByLayer.Length; i++) _layers[i - 1] = new Layer(neuronCountByLayer[i], neuronCountByLayer[i - 1]);

            // TODO: Setup random weights & bias
            var random = new Random();

            foreach (var layer in _layers)
            {
                for (var i = 0; i < layer.Weights.Length; i++) layer.Weights[i] = (float)random.NextDouble();
            }
        }

        public float[] Run(float[] inputs)
        {
            Debug.Assert(inputs.Length == _layers[0].Weights.Length / _layers[0].Values.Length);

            var previousLayerValues = inputs;

            foreach (var layer in _layers)
            {
                for (var i = 0; i < layer.Values.Length; i++)
                {
                    layer.Values[i] = 0;
                    for (var j = 0; j < previousLayerValues.Length; j++) layer.Values[i] += previousLayerValues[j] * layer.Weights[i] - layer.Biases[i];
                }

                previousLayerValues = layer.Values;
            }

            return previousLayerValues;
        }
    }
}

// using ComputeSharp;

using Neural.UI;
using NeuralBasics.Math;
using SDL2;
using System;
using System.Diagnostics;
using System.IO;

namespace Neural
{
    class Program
    {
        enum AutoMode { None, Train, Check }

        static void Main(string[] args)
        {
            // TODO: Use ComputeSharp to run code on GPU
            /*
            using ReadWriteBuffer<float> buffer = Gpu.Default.AllocateReadWriteBuffer<float>(1000);
            Gpu.Default.For(1000, id => buffer[id.X] = id.X);
            float[] array = buffer.GetData();
            */

            var dataPath = FileHelper.FindAppFolder("Data");

            int imageWidth, imageHeight;
            byte[][] images;
            byte[] labels;

            {
                var bytes = File.ReadAllBytes(Path.Combine(dataPath, "MNIST", "train-images.idx3-ubyte"));
                var reader = new PacketReader();
                reader.Open(bytes);

                var magic = reader.ReadInt();
                if (magic != 0x00000803) throw new Exception("Wrong magic number");

                var imageCount = reader.ReadInt();
                Trace.WriteLine($"Found {imageCount} images.");

                imageHeight = reader.ReadInt();
                imageWidth = reader.ReadInt();

                images = new byte[imageCount][];
                for (var i = 0; i < imageCount; i++) images[i] = reader.ReadBytes(imageHeight * imageWidth).ToArray();
            }

            {
                var bytes = File.ReadAllBytes(Path.Combine(dataPath, "MNIST", "train-labels.idx1-ubyte"));
                var reader = new PacketReader();
                reader.Open(bytes);

                var magic = reader.ReadInt();
                if (magic != 0x00000801) throw new Exception("Wrong magic number");

                var itemCount = reader.ReadInt();
                Trace.WriteLine($"Found {itemCount} labels.");

                labels = new byte[itemCount];
                for (var i = 0; i < itemCount; i++) labels[i] = reader.ReadByte();
            }

            {
                var displayedImageIndex = 0;

                var layerSetup = new int[] { imageWidth * imageHeight, 600, 400, 200, 100, 10 };
                var net = new NeuralNet(layerSetup);
                var netInputs = new float[imageWidth * imageHeight];
                var netLearningRate = 0.1f;
                var netResult = -1;

                void ResetNetwork()
                {
                    net = new NeuralNet(layerSetup);
                    RunNetworkAndRenderImage(displayedImageIndex);
                }

                void RunNetwork(int imageIndex)
                {
                    for (var i = 0; i < netInputs.Length; i++) netInputs[i] = images[imageIndex][i] / (float)byte.MaxValue;

                    var output = net.Compute(netInputs);
                    Debug.Assert(output.Length == 10);

                    // Get the digit the network has seen
                    var max = 0f;
                    for (var i = 0; i < output.Length; i++)
                    {
                        if (output[i] > max)
                        {
                            max = output[i];
                            netResult = i;
                        }
                    }

                    var expectedOutput = new float[10];
                    expectedOutput[labels[imageIndex]] = 1f;
                    net.BackpropagateError(expectedOutput);
                }

                var windowWidth = 1280;
                var windowHeight = 720;
                var imageScale = 8;

                var labelText = "";
                var imageText = "";
                var statusText = "";

                SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
                SDL.SDL_CreateWindowAndRenderer(windowWidth, windowHeight, 0, out var window, out var renderer);
                SDL.SDL_SetWindowTitle(window, "Neural");

                var font = Font.LoadFromChevyRayFolder(renderer, Path.Combine(dataPath, "Fonts", "ChevyRay - Softsquare Mono"));
                var fontStyle = new FontStyle(font) { Scale = 2, LetterSpacing = 1 };
                var smallFontStyle = new FontStyle(font) { Scale = 1, LetterSpacing = 1 };

                var imageTexture = IntPtr.Zero;
                var imageSourceRect = new SDL.SDL_Rect { x = 0, y = 0, w = imageWidth, h = imageHeight };
                var imageDestRect = new SDL.SDL_Rect { x = windowWidth / 2 - (imageWidth / 2 * imageScale), y = windowHeight / 2 - (imageHeight / 2 * imageScale), w = imageWidth * imageScale, h = imageHeight * imageScale };

                void LoadImageForRender(int imageIndex)
                {
                    if (imageTexture != IntPtr.Zero) SDL.SDL_DestroyTexture(imageTexture);

                    var pixels = new byte[imageWidth * imageHeight * 4];

                    for (var i = 0; i < imageWidth * imageHeight; i++)
                    {
                        pixels[i * 4 + 0] = pixels[i * 4 + 1] = pixels[i * 4 + 2] = images[imageIndex][i];
                        pixels[i * 4 + 3] = 0xff;
                    }

                    unsafe
                    {
                        fixed (byte* pixelsPointer = pixels)
                        {
                            var surface = SDL.SDL_CreateRGBSurfaceWithFormatFrom((IntPtr)pixelsPointer, imageWidth, imageHeight, 24, imageWidth * 4, SDL.SDL_PIXELFORMAT_ARGB8888);
                            imageTexture = SDL.SDL_CreateTextureFromSurface(renderer, surface);
                            SDL.SDL_FreeSurface(surface);
                        }
                    }

                    labelText = $"Label: {labels[imageIndex]}, Network output: {netResult}";
                    imageText = $"({imageIndex}/{images.Length})";
                }

                void RunNetworkAndRenderImage(int imageIndex)
                {
                    RunNetwork(imageIndex);
                    LoadImageForRender(imageIndex);
                }

                RunNetworkAndRenderImage(displayedImageIndex);

                var running = true;

                AutoMode autoMode = AutoMode.None;
                var autoImageIndex = 0;
                var correctlyPredicted = 0;

                while (running)
                {
                    while (running && SDL.SDL_PollEvent(out var @event) != 0)
                    {
                        switch (@event.type)
                        {
                            case SDL.SDL_EventType.SDL_QUIT:
                                running = false;
                                break;

                            case SDL.SDL_EventType.SDL_WINDOWEVENT:
                                switch (@event.window.windowEvent)
                                {
                                    case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE:
                                        running = false;
                                        break;
                                }

                                break;

                            case SDL.SDL_EventType.SDL_KEYDOWN:
                                switch (@event.key.keysym.sym)
                                {
                                    case SDL.SDL_Keycode.SDLK_RIGHT:
                                        if (displayedImageIndex < images.Length - 1) { displayedImageIndex++; RunNetworkAndRenderImage(displayedImageIndex); }
                                        break;

                                    case SDL.SDL_Keycode.SDLK_LEFT:
                                        if (displayedImageIndex > 0) { displayedImageIndex--; RunNetworkAndRenderImage(displayedImageIndex); }
                                        break;

                                    case SDL.SDL_Keycode.SDLK_p:
                                        if (autoMode == AutoMode.Train)
                                        {
                                            autoMode = AutoMode.None;
                                        }
                                        else
                                        {
                                            autoMode = AutoMode.Train;
                                            autoImageIndex = 0;
                                        }
                                        break;

                                    /* case SDL.SDL_Keycode.SDLK_t:
                                        net.Train(netLearningRate, netInputs);
                                        RunNetworkAndRenderImage(displayedImageIndex);
                                        break; */

                                    case SDL.SDL_Keycode.SDLK_c:
                                        if (autoMode == AutoMode.Check)
                                        {
                                            autoMode = AutoMode.None;
                                        }
                                        else
                                        {
                                            autoMode = AutoMode.Check;
                                            autoImageIndex = 0;
                                            correctlyPredicted = 0;
                                        }
                                        break;

                                    case SDL.SDL_Keycode.SDLK_r:
                                        if (autoMode == AutoMode.None)
                                        {
                                            ResetNetwork();
                                            statusText = "";
                                        }
                                        break;
                                }

                                break;
                        }
                    }

                    if (!running) break;

                    SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
                    SDL.SDL_RenderClear(renderer);

                    // Draw image
                    SDL.SDL_RenderCopy(renderer, imageTexture, ref imageSourceRect, ref imageDestRect);

                    // Draw network
                    {
                        var start = new Point(10, 10);
                        var size = 4;
                        var spacing = 2;
                        var columnOffset = -1;
                        var maxPerColumn = 50;

                        for (var i = 0; i < net.Layers.Length; i++)
                        {
                            var layer = net.Layers[i];

                            for (var j = 0; j < layer.Outputs.Length; j++)
                            {
                                if (j % maxPerColumn == 0) columnOffset++;

                                var value = Math.Clamp(layer.Outputs[j], 0f, 1f);

                                SDL.SDL_SetRenderDrawColor(renderer, (byte)(0xff * (1f - value)), (byte)(0xff * value), 0x00, 0xff);
                                var rect = new SDL.SDL_Rect { x = start.X + (i + columnOffset) * (size + spacing), y = start.Y + (j % maxPerColumn) * (size + spacing), w = size, h = size };
                                SDL.SDL_RenderFillRect(renderer, ref rect);
                            }
                        }
                    }

                    // Draw info
                    fontStyle.DrawText(8, windowHeight - fontStyle.Size - 8, "Left/Right to navigate, P to train, C to check accuracy, R to reset network");
                    fontStyle.DrawText(windowWidth / 2 - fontStyle.MeasureText(labelText) / 2, imageDestRect.y + imageDestRect.h + 8, labelText);
                    fontStyle.DrawText(windowWidth - fontStyle.MeasureText(imageText) - 8, windowHeight - fontStyle.Size - 8, imageText);
                    fontStyle.DrawText(windowWidth - fontStyle.MeasureText(statusText) - 8, 8, statusText);

                    smallFontStyle.DrawText(8, windowHeight - fontStyle.Size * 4 - 12, "Written by Elisee Maurer (@elisee)");
                    smallFontStyle.DrawText(8, windowHeight - fontStyle.Size * 3 - 12, "MNIST database: yann.lecun.com/exdb/mnist/");
                    smallFontStyle.DrawText(8, windowHeight - fontStyle.Size * 2 - 12, "Font by Chevy Ray - pixel-fonts.com");

                    SDL.SDL_RenderPresent(renderer);
                    // Thread.Sleep(1);

                    switch (autoMode)
                    {
                        case AutoMode.Train:
                            for (var i = 0; i < 10; i++)
                            {
                                RunNetwork(autoImageIndex);
                                net.Train(netLearningRate, netInputs);
                                autoImageIndex++;

                                if (autoImageIndex == images.Length)
                                {
                                    autoMode = AutoMode.None;
                                    break;
                                }
                            }

                            statusText = $"Trained on {autoImageIndex} images.";
                            RunNetworkAndRenderImage(displayedImageIndex);
                            break;

                        case AutoMode.Check:
                            for (var i = 0; i < 10; i++)
                            {
                                RunNetwork(autoImageIndex);
                                if (labels[autoImageIndex] == netResult) correctlyPredicted++;

                                autoImageIndex++;
                                if (autoImageIndex == images.Length)
                                {
                                    autoMode = AutoMode.None;
                                    break;
                                }
                            }

                            var percent = Math.Round(100f * correctlyPredicted / autoImageIndex, 2, MidpointRounding.AwayFromZero);
                            statusText = $"Correctly predicted {percent:0.00}% ({correctlyPredicted}/{autoImageIndex})";
                            break;
                    }
                }
            }
        }
    }
}

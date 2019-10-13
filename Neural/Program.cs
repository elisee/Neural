// using ComputeSharp;

using Neural.UI;
using SDL2;
using System;
using System.IO;
using System.Threading;

namespace Neural
{
    class Program
    {
        static void Main(string[] args)
        {
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
                Console.WriteLine($"Found {imageCount} images.");

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
                Console.WriteLine($"Found {itemCount} labels.");

                labels = new byte[itemCount];
                for (var i = 0; i < itemCount; i++) labels[i] = reader.ReadByte();
            }

            {
                var windowWidth = 1280;
                var windowHeight = 720;
                var imageScale = 8;

                var imageIndex = 0;
                var imageText = "";

                var imageTexture = IntPtr.Zero;
                var imageSourceRect = new SDL.SDL_Rect { x = 0, y = 0, w = imageWidth, h = imageHeight };
                var imageDestRect = new SDL.SDL_Rect { x = windowWidth / 2 - (imageWidth / 2 * imageScale), y = windowHeight / 2 - (imageHeight / 2 * imageScale), w = imageWidth * imageScale, h = imageHeight * imageScale };

                SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
                SDL.SDL_CreateWindowAndRenderer(windowWidth, windowHeight, 0, out var window, out var renderer);
                SDL.SDL_SetWindowTitle(window, "Neural");

                void LoadImage()
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

                    imageText = $"Image #{imageIndex}. Label: {labels[imageIndex]}";
                }

                LoadImage();

                var font = Font.LoadFromChevyRayFolder(renderer, Path.Combine(dataPath, "Fonts", "ChevyRay - Softsquare Mono"));
                var fontStyle = new FontStyle(font) { Scale = 2, LetterSpacing = 1 };

                var running = true;

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
                                if (@event.key.keysym.sym == SDL.SDL_Keycode.SDLK_RIGHT) { if (imageIndex < images.Length - 1) { imageIndex++; LoadImage(); } }
                                else if (@event.key.keysym.sym == SDL.SDL_Keycode.SDLK_LEFT) { if (imageIndex > 0) { imageIndex--; LoadImage(); } }
                                break;
                        }
                    }

                    if (!running) break;

                    SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
                    SDL.SDL_RenderClear(renderer);

                    SDL.SDL_RenderCopy(renderer, imageTexture, ref imageSourceRect, ref imageDestRect);

                    fontStyle.DrawText(windowWidth / 2 - fontStyle.MeasureText(imageText) / 2, imageDestRect.y + imageDestRect.h + 8, imageText);

                    SDL.SDL_RenderPresent(renderer);
                    Thread.Sleep(1);
                }
            }
        }
    }
}

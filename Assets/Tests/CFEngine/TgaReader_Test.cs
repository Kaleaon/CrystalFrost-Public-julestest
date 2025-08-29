using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using CrystalFrost.Lib;
using System;

namespace CrystalFrostEngine.Tests
{
    public class TgaReader_Test
    {
        [Test]
        public void TgaReader_Reads24bpp()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<TgaReader>>();
            var tgaReader = new TgaReader(mockLogger.Object);

            // Create a minimal 1x1 24bpp TGA file
            // Header is 18 bytes, pixel data is 3 bytes (BGR)
            var tgaData = new byte[21];

            // --- TGA Header ---
            // ImageType: 2 (Uncompressed, True-color)
            tgaData[2] = 2;
            // Width: 1
            var widthBytes = BitConverter.GetBytes((Int16)1);
            tgaData[12] = widthBytes[0];
            tgaData[13] = widthBytes[1];
            // Height: 1
            var heightBytes = BitConverter.GetBytes((Int16)1);
            tgaData[14] = heightBytes[0];
            tgaData[15] = heightBytes[1];
            // BitsPerPixel: 24
            tgaData[16] = 24;

            // --- Pixel Data (1x1 red pixel, BGR) ---
            tgaData[18] = 0x00; // Blue
            tgaData[19] = 0x00; // Green
            tgaData[20] = 0xFF; // Red

            // Act
            tgaReader.Read(tgaData);

            // Assert
            Assert.AreEqual(1, tgaReader.Width);
            Assert.AreEqual(1, tgaReader.Height);
            Assert.AreEqual(24, tgaReader.BitsPerPixel);
            Assert.IsNotNull(tgaReader.Bitmap);
            Assert.AreEqual(3, tgaReader.Bitmap.Length);

            // Check that BGR was converted to RGB
            Assert.AreEqual(0xFF, tgaReader.Bitmap[0]); // Red
            Assert.AreEqual(0x00, tgaReader.Bitmap[1]); // Green
            Assert.AreEqual(0x00, tgaReader.Bitmap[2]); // Blue
        }

        [Test]
        public void TgaReader_Reads32bpp()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<TgaReader>>();
            var tgaReader = new TgaReader(mockLogger.Object);

            // Create a minimal 1x1 32bpp TGA file
            // Header is 18 bytes, pixel data is 4 bytes (BGRA)
            var tgaData = new byte[22];

            // --- TGA Header ---
            // ImageType: 2 (Uncompressed, True-color)
            tgaData[2] = 2;
            // Width: 1
            var widthBytes = BitConverter.GetBytes((Int16)1);
            tgaData[12] = widthBytes[0];
            tgaData[13] = widthBytes[1];
            // Height: 1
            var heightBytes = BitConverter.GetBytes((Int16)1);
            tgaData[14] = heightBytes[0];
            tgaData[15] = heightBytes[1];
            // BitsPerPixel: 32
            tgaData[16] = 32;

            // --- Pixel Data (1x1 red pixel with alpha, BGRA) ---
            tgaData[18] = 0x00; // Blue
            tgaData[19] = 0x00; // Green
            tgaData[20] = 0xFF; // Red
            tgaData[21] = 0x80; // Alpha

            // Act
            tgaReader.Read(tgaData);

            // Assert
            Assert.AreEqual(1, tgaReader.Width);
            Assert.AreEqual(1, tgaReader.Height);
            Assert.AreEqual(32, tgaReader.BitsPerPixel);
            Assert.IsNotNull(tgaReader.Bitmap);
            Assert.AreEqual(4, tgaReader.Bitmap.Length);

            // Check that BGRA was converted to RGBA
            Assert.AreEqual(0xFF, tgaReader.Bitmap[0]); // Red
            Assert.AreEqual(0x00, tgaReader.Bitmap[1]); // Green
            Assert.AreEqual(0x00, tgaReader.Bitmap[2]); // Blue
            Assert.AreEqual(0x80, tgaReader.Bitmap[3]); // Alpha
        }
    }
}

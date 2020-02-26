using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using PatternRecognitionProject.Data;
using PatternRecognitionProject.Models;
using System.IO;
using System.Drawing;

namespace PatternRecognitionProject
{
    public class NewDataEntryModel : PageModel
    {
        private readonly EntryContext _context;
        private CurrentImageSingleton _imageSingleton;
        public string ImageFileName { get; private set; }
        public int DatasetSize { get; private set; }

        public NewDataEntryModel(EntryContext context, CurrentImageSingleton imageSingleton)
        {
            _context = context;
            _imageSingleton = imageSingleton;
            DatasetSize = Directory.GetFiles("wwwroot/Dataset").Length;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public DataUnit DataUnit { get; set; }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public IActionResult OnPostFinal()
        {
            if (ModelState.IsValid)
            {
                var image = _imageSingleton.Image;
                image.Save($"wwwroot/Dataset/{RandomString(15)}_{DataUnit.ClassLabel}.png");
            }
            return RedirectToPage("NewDataEntry");
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
                    .PyrDown();
                var image = BitmapConverter.ToBitmap(frame);
                _imageSingleton.Image = image;
                try
                {
                    image.Save("wwwroot/temporary.png");
                    ImageFileName = "temporary.png";
                }
                catch (Exception)
                {

                }
            }
            capture.Dispose();
        }
        private Bitmap ImageFromBytes(byte[] bytes)
        {
            var res = new Bitmap(160, 120);
            for(var i=0; i<res.Width; i++)
            {
                for (var j = 0; j < res.Height; j++)
                    res.SetPixel(i, j, Color.FromArgb(bytes[res.Width * i + j], 0, 0));
            }
            return res;
        }
        private byte[] ImagePixels(Bitmap img)
        {
            var allNumbers = new List<byte>();
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
        private string RandomString(int length)
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}

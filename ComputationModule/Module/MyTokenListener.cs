using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ColorfulSoft.DeOldify;
using ComputationModule.BalticLSC;
using ComputationModule.Messages;
using Serilog;

namespace ComputationModule.Module
{
    public class MyTokenListener : TokenListener
    {
        public MyTokenListener(JobRegistry registry, DataHandler data) : base(registry, data) {}
        
        public override void DataReceived(string pinName)
        {
            // Place your code here:
            
        }

        public override void OptionalDataReceived(string pinName)
        {
            // Place your code here:

        }

        public override void DataReady()
        {
            // Place your code here:

        }

        public override void DataComplete()
        {
            Registry.SetStatus(Status.Working);
            
            Log.Debug($"Received input image");
            string file = Data.ObtainDataItem("input image");
            
            Log.Debug($"Read file: {file}");
            Bitmap image = new Bitmap(Image.FromFile(file)); 
            
            Log.Debug($"Starting Colorize");
            Bitmap output = DeOldify.Colorize(image);
            
            string outFile = file.Substring(0,file.Length-Path.GetExtension(file).Length) + "_colour.jpg";
            Log.Debug($"Saving file: {outFile}");
            output.Save(outFile,ImageFormat.Jpeg);
            
            Log.Debug($"Sending output image");
            Data.SendDataItem("output image", outFile, true);
            
            Log.Debug($"Finishing prcessing");
            Data.FinishProcessing();
        }
    }
}
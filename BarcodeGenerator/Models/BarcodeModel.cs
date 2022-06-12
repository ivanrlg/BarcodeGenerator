namespace BarcodeGenerator.Models
{
    public class BarcodeModel
    {
        public string Value { get; set; }
        public int ImageHeight { get; set; }
        public int ImageWidth { get; set; }
        public int Margin { get; set; }
        public string Symbology { get; set; }
        public string OutputFormat { get; set; }
     
    }
}

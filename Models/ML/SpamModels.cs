using Microsoft.ML.Data;

namespace SesliKitapWeb.Models.ML
{
    public class SpamInput
    {
        [LoadColumn(0)]
        public string Text { get; set; }
        
        public SpamInput(string text)
        {
            Text = text;
        }
        
        public SpamInput() 
        {
            Text = string.Empty;
        }
    }

    public class SpamPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool IsSpam { get; set; }
        
        [ColumnName("Score")]
        public float Probability { get; set; }
    }
}

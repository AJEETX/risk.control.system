using iText.Kernel.Pdf;
using iText.Signatures;
namespace risk.control.system.Services.Tool
{
    public interface IItrVerificationService
    {
        bool IsPdfSignatureValid(string pdfPath);
        bool CheckMetadataTampering(string pdfPath);
    }
    internal class ItrVerificationService : IItrVerificationService
    {
        public bool IsPdfSignatureValid(string pdfPath)
        {
            using (PdfReader reader = new PdfReader(pdfPath))
            using (PdfDocument pdfDoc = new PdfDocument(reader))
            {
                SignatureUtil signUtil = new SignatureUtil(pdfDoc);
                IList<string> names = signUtil.GetSignatureNames();

                // If there are no signatures, it's either an unofficial copy or heavily altered
                if (names.Count == 0) return false;

                foreach (string name in names)
                {
                    PdfPKCS7 pkcs7 = signUtil.ReadSignatureData(name);

                    // Verify if the document has been modified after the signature was applied
                    if (!pkcs7.VerifySignatureIntegrityAndAuthenticity())
                    {
                        return false; // Document has been tampered with!
                    }
                }
            }
            return true; // Document is untampered
        }
        public bool CheckMetadataTampering(string pdfPath)
        {
            using (PdfReader reader = new PdfReader(pdfPath))
            using (PdfDocument pdfDoc = new PdfDocument(reader))
            {
                // In iText 7/8, it is GetDocumentInfo()
                PdfDocumentInfo info = pdfDoc.GetDocumentInfo();

                string producer = info.GetProducer()?.ToLower() ?? "";
                string creator = info.GetCreator()?.ToLower() ?? "";

                // Flag if it was modified using common external PDF editors
                if (producer.Contains("microsoft word") ||
                    producer.Contains("ilovepdf") ||
                    producer.Contains("nitro") ||
                    producer.Contains("smallpdf") ||
                    producer.Contains("sejda"))
                {
                    return false; // High probability of manual modification
                }
            }
            return true;
        }
    }
}

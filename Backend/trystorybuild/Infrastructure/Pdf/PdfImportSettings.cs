namespace Infrastructure.Pdf;

public class PdfImportSettings
{
    public string PdfUploadDirectory { get; set; } = "Uploads/Pdf";
    public string ImagesOutputDirectory { get; set; } = "wwwroot/images/lessons";
}

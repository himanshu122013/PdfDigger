using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using System.Text;

public class PdfController : Controller
{
    private readonly IWebHostEnvironment _env;

    public PdfController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpGet]
    public IActionResult RemovePassword()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> RemovePassword(IFormFile pdfFile, string password)
    {
        if (pdfFile == null || pdfFile.Length == 0)
        {
            ViewBag.ErrorMessage = "Please upload a PDF file.";
            return View();
        }

        try
        {
            // Set unethicalreading before reading the file
            PdfReader.unethicalreading = true;

            // Read the uploaded PDF file into a MemoryStream asynchronously.
            using (var inputStream = new MemoryStream())
            {
                await pdfFile.CopyToAsync(inputStream);
                inputStream.Position = 0;

                // Initialize PdfReader with the provided password.
                byte[] passwordBytes = Encoding.ASCII.GetBytes(password);
                using (var reader = new PdfReader(inputStream, passwordBytes))
                {
                    if (!reader.IsEncrypted())
                    {
                        ViewBag.ErrorMessage = "The PDF is not encrypted.";
                        return View();
                    }

                    // Create output stream for the decrypted PDF.
                    using (var outputStream = new MemoryStream())
                    {
                        using (var stamper = new PdfStamper(reader, outputStream))
                        {
                            // Remove encryption by re-saving without encryption settings.
                            stamper.Writer.SetEncryption(null, null, 0, PdfWriter.DO_NOT_ENCRYPT_METADATA);
                        }
                        reader.Close();

                        // Build output file name: original file name (without extension) + "_unlocked" + original extension.
                        string originalFileName = Path.GetFileNameWithoutExtension(pdfFile.FileName);
                        string extension = Path.GetExtension(pdfFile.FileName);
                        string outputFileName = $"{originalFileName}_unlocked{extension}";

                        // Define the path to save the file (ensure that wwwroot/downloads exists)
                        string downloadsFolder = Path.Combine(_env.WebRootPath, "downloads");
                        if (!Directory.Exists(downloadsFolder))
                        {
                            Directory.CreateDirectory(downloadsFolder);
                        }
                        string outputPath = Path.Combine(downloadsFolder, outputFileName);

                        // Save the file to disk.
                        await System.IO.File.WriteAllBytesAsync(outputPath, outputStream.ToArray());

                        // Build the download link (relative URL).
                        string downloadLink = $"/downloads/{outputFileName}";

                        ViewBag.DownloadLink = downloadLink;
                        ViewBag.SuccessMessage = "PDF unlocked successfully!";
                        return View();
                    }
                }
            }
        }
        catch (iTextSharp.text.exceptions.BadPasswordException)
        {
            ViewBag.ErrorMessage = "Incorrect password!";
            return View();
        }
        catch (IOException ex)
        {
            ViewBag.ErrorMessage = $"File Error: {ex.Message}";
            return View();
        }
        catch (Exception ex)
        {
            ViewBag.ErrorMessage = $"An error occurred: {ex.Message}";
            return View();
        }
    }

    // GET: /Pdf/Merge
    [HttpGet]
    public IActionResult Merge()
    {
        return View();
    }

    // POST: /Pdf/Merge
    [HttpPost]
    public async Task<IActionResult> Merge(List<IFormFile> pdfFiles)
    {
        // Validate that at least two files have been uploaded.
        if (pdfFiles == null || pdfFiles.Count < 2)
        {
            ViewBag.ErrorMessage = "Please upload at least two PDF files for merging.";
            return View();
        }

        try
        {
            PdfReader.unethicalreading = true;

            using (var outputStream = new MemoryStream())
            {
                // Create a Document for merging.
                using (Document document = new Document())
                {
                    // Create PdfCopy to add pages from each PDF.
                    using (PdfCopy copy = new PdfCopy(document, outputStream))
                    {
                        document.Open();

                        // Process each uploaded PDF file.
                        foreach (var pdfFile in pdfFiles)
                        {
                            // Read the file into a MemoryStream.
                            using (var inputStream = new MemoryStream())
                            {
                                await pdfFile.CopyToAsync(inputStream);
                                inputStream.Position = 0;

                                // Initialize PdfReader from the stream.
                                using (PdfReader reader = new PdfReader(inputStream))
                                {
                                    // Loop through each page of the PDF.
                                    for (int page = 1; page <= reader.NumberOfPages; page++)
                                    {
                                        try
                                        {
                                            var importedPage = copy.GetImportedPage(reader, page);
                                            copy.AddPage(importedPage);
                                        }
                                        catch (Exception innerEx)
                                        {
                                            // Log or handle issues importing a page.
                                            ViewBag.ErrorMessage = $"Error importing page {page} of file {pdfFile.FileName}: {innerEx.Message}";
                                            return View();
                                        }
                                    }
                                    copy.FreeReader(reader);
                                }
                            }
                        }

                        document.Close();
                    }
                }

                // Build the output file name.
                string outputFileName = "Merged.pdf";
                // Define a path to save the merged file, e.g., in wwwroot/downloads.
                string downloadsFolder = Path.Combine(_env.WebRootPath, "downloads");
                if (!Directory.Exists(downloadsFolder))
                {
                    Directory.CreateDirectory(downloadsFolder);
                }
                string outputPath = Path.Combine(downloadsFolder, outputFileName);

                // Save the merged PDF to disk.
                await System.IO.File.WriteAllBytesAsync(outputPath, outputStream.ToArray());

                // Build the download link (relative URL).
                string downloadLink = $"/downloads/{outputFileName}";

                ViewBag.SuccessMessage = "PDF Merged successfully.";
                ViewBag.DownloadLink = downloadLink;
                return View();
            }
        }
        catch (Exception ex)
        {
            ViewBag.ErrorMessage = $"An error occurred during merging: {ex.Message}";
            return View();
        }
    }

    // GET: /Pdf/ExtractPages
    [HttpGet]
    public IActionResult ExtractPages()
    {
        return View();
    }

    // POST: /Pdf/ExtractPages
    [HttpPost]
    public async Task<IActionResult> ExtractPages(IFormFile pdfFile, int startPage, int endPage)
    {
        if (pdfFile == null || pdfFile.Length == 0)
        {
            ViewBag.ErrorMessage = "Please upload a PDF file.";
            return View();
        }

        try
        {
            using (var inputStream = new MemoryStream())
            {
                await pdfFile.CopyToAsync(inputStream);
                inputStream.Position = 0;

                using (PdfReader reader = new PdfReader(inputStream))
                {
                    int totalPages = reader.NumberOfPages;

                    // Validate the requested range.
                    if (startPage < 1 || endPage > totalPages || startPage > endPage)
                    {
                        ViewBag.ErrorMessage = $"Please specify a valid page range between 1 and {totalPages}.";
                        return View();
                    }

                    // Create output file name.
                    string originalFileName = System.IO.Path.GetFileNameWithoutExtension(pdfFile.FileName);
                    string extension = System.IO.Path.GetExtension(pdfFile.FileName);
                    string outputFileName = $"{originalFileName}_pages_{startPage}_to_{endPage}{extension}";

                    // Define the folder to save the file.
                    string downloadsFolder = System.IO.Path.Combine(_env.WebRootPath, "downloads");
                    if (!Directory.Exists(downloadsFolder))
                    {
                        Directory.CreateDirectory(downloadsFolder);
                    }
                    string outputPath = System.IO.Path.Combine(downloadsFolder, outputFileName);

                    // Create a new document and copy the specified pages.
                    using (Document document = new Document())
                    using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                    using (PdfCopy copy = new PdfCopy(document, fs))
                    {
                        document.Open();
                        for (int page = startPage; page <= endPage; page++)
                        {
                            PdfImportedPage importedPage = copy.GetImportedPage(reader, page);
                            copy.AddPage(importedPage);
                        }
                        document.Close();
                    }

                    string downloadLink = $"/downloads/{outputFileName}";
                    ViewBag.SuccessMessage = "PDF pages extracted successfully.";
                    ViewBag.DownloadLink = downloadLink;
                }
            }
            return View();
        }
        catch (Exception ex)
        {
            ViewBag.ErrorMessage = $"An error occurred: {ex.Message}";
            return View();
        }
    }
}
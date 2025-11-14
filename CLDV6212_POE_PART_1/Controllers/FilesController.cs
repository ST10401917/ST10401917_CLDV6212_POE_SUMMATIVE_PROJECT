using CLDV6212_POE_PART_1.Services;
using Azure.Storage.Files.Shares.Models;
using CLDV6212_POE_PART_1.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CLDV6212_POE_PART_1.Controllers
{
    public class FilesController : Controller
    {
        private readonly AzureFileShareService _fileShareService;
        public FilesController(AzureFileShareService fileShareService)
        {
            _fileShareService = fileShareService;
        }
        public async Task<IActionResult> Index()
        {
            List<FileModel> files;
            try
            {
                files = await _fileShareService.ListFilesAsync("contracts");
            }
            catch
            {
                ViewBag.Message = "Error retrieving files from Azure File Share.";
                files = new List<FileModel>();
            }
            return View(files);
        }


        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file != null || file.Length == 0)
            {
                ModelState.AddModelError("File", "Please select a file to upload");
                return await Index();
            }
            try
            {
                using (var stream = file.OpenReadStream())
                {
                    string directoryName = "contracts";
                    string fileName = file.FileName;
                    await _fileShareService.UploadFileAsync(directoryName, stream, file.FileName);
                }
                TempData["Message"] = $"File '{file.FileName}' uploaded successfully.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error uploading file '{ex.Message}'.";
            }
            return RedirectToAction("Index");
        }
    }
}
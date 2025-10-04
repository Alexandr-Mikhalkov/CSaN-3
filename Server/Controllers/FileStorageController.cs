using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace FileStorageServer.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FileStorageController : ControllerBase
    {
        private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "FileStorage");

        public FileStorageController()
        {
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }

        [HttpGet]
        public IActionResult GetAllFiles()
        {
            var files = Directory.GetFiles(_storagePath).Select(Path.GetFileName).ToList();
            return Ok(files);
        }

        [HttpGet("{filename}")]
        public IActionResult GetFile(string filename)
        {
            var filePath = Path.Combine(_storagePath, filename);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound($"File {filename} not found");
            }

            return PhysicalFile(filePath, "application/octet-stream", filename);
        }

        [HttpPut("{filename}")]
        public async Task<IActionResult> PutFile(string filename)
        {
            var filePath = Path.Combine(_storagePath, filename);

            using (var fileStream = System.IO.File.Create(filePath))
            {
                await Request.Body.CopyToAsync(fileStream);
            }

            return Ok($"File {filename} created/updated");
        }

        [HttpPost("{filename}")]
        public async Task<IActionResult> AppendToFile(string filename)
        {
            var filePath = Path.Combine(_storagePath, filename);

            using (var fileStream = System.IO.File.Open(filePath, FileMode.Append))
            {
                await Request.Body.CopyToAsync(fileStream);
            }

            return Ok($"Data appended to {filename}");
        }

        [HttpDelete("{filename}")]
        public IActionResult DeleteFile(string filename)
        {
            var filePath = Path.Combine(_storagePath, filename);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound($"File {filename} not found");
            }

            System.IO.File.Delete(filePath);
            return Ok($"File {filename} deleted");
        }

        [HttpPost("copy")]
        public IActionResult CopyFile([FromQuery] string source, [FromQuery] string destination)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(destination))
            {
                return BadRequest("Both source and destination parameters are required");
            }

            var sourcePath = Path.Combine(_storagePath, source);
            var destPath = Path.Combine(_storagePath, destination);

            if (!System.IO.File.Exists(sourcePath))
            {
                return NotFound($"Source file {source} not found");
            }

            System.IO.File.Copy(sourcePath, destPath, overwrite: true);
            return Ok($"File {source} copied to {destination}");
        }

        [HttpPost("move")]
        public IActionResult MoveFile([FromQuery] string source, [FromQuery] string destination)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(destination))
            {
                return BadRequest("Both source and destination parameters are required");
            }

            var sourcePath = Path.Combine(_storagePath, source);
            var destPath = Path.Combine(_storagePath, destination);

            if (!System.IO.File.Exists(sourcePath))
            {
                return NotFound($"Source file {source} not found");
            }

            System.IO.File.Move(sourcePath, destPath, overwrite: true);
            return Ok($"File {source} moved to {destination}");
        }
    }
}
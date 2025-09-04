using System.IO;

namespace FileExplorer.Files
{
    public class Endpoints
    {
        private readonly IFileService fileService;
        private readonly string _routePrefix = "api/v1/files";

        public Endpoints(IFileService fileService)
        {
            this.fileService = fileService;
        }
        public void MapEndpoints(WebApplication app)
        {

            app.MapGet($"{_routePrefix}", (string? home, string? path) =>
            {
                try
                {
                    var selectedHome = string.IsNullOrEmpty(home) ? "default" : home;
                    var normalizedPath = path?.TrimStart('/') ?? "";
                    var files = fileService.GetFiles(selectedHome, normalizedPath);
                    return Results.Ok(files);

                }
                catch (DirectoryNotFoundException ex)
                {
                    return Results.NotFound(new { message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);

                }
            });
            app.MapGet($"{_routePrefix}/download", async (HttpRequest request, HttpResponse response) =>
            {
                var home = request.Query["home"].FirstOrDefault() ?? "default";
                var path = request.Query["path"].FirstOrDefault();
                if (string.IsNullOrEmpty(path))
                    return Results.BadRequest("File path is required.");

                try
                {
                    var stream = fileService.GetFileStream(home, path);
                    if (stream == null)
                        return Results.NotFound();

                    var filename = Path.GetFileName(path);
                    var ext = Path.GetExtension(path).ToLowerInvariant();

                    // Minimal content type mapping
                    var contentType = ext switch
                    {
                        ".png" => "image/png",
                        ".jpg" or ".jpeg" => "image/jpeg",
                        ".gif" => "image/gif",
                        ".txt" => "text/plain",
                        ".pdf" => "application/pdf",
                        _ => "application/octet-stream"
                    };

                    response.ContentType = contentType;
                    response.Headers.ContentDisposition = $"attachment; filename=\"{filename}\"";

                    await stream.CopyToAsync(response.Body);
                    return Results.Empty;
                }
                catch (FileNotFoundException ex)
                {
                    return Results.NotFound(new { message = ex.Message });
                }
                catch (DirectoryNotFoundException ex)
                {
                    return Results.NotFound(new { message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });


            app.MapPost($"{_routePrefix}", async (HttpRequest request) =>
            {
                
                if (!request.HasFormContentType)
                    return Results.BadRequest("Expected multipart/form-data");

                var form = await request.ReadFormAsync();
                var file = form.Files.FirstOrDefault();
                if (file == null || file.Length == 0)
                    return Results.BadRequest("No file uploaded");

                var home = form["home"].FirstOrDefault() ?? "default";
                var path = form["path"].FirstOrDefault() ?? file.FileName;
                var selectedHome = string.IsNullOrEmpty(home) ? "default" : home;
                var normalizedPath = path?.TrimStart('/') ?? "";

                await fileService.SaveFile(selectedHome, normalizedPath, file.OpenReadStream());
                return Results.Created();
            });
            app.MapPut($"{_routePrefix}", (string? home, string source, string destination, string operation) =>
            {
                try
                {
                    var selectedHome = string.IsNullOrEmpty(home) ? "default" : home;
                    if (operation == "copy")
                    {
                        fileService.CopyFile(selectedHome, source, destination);
                        return Results.Ok(new { message = "File copied successfully." });
                    }
                    else if (operation == "move")
                    {
                        fileService.MoveFile(selectedHome, source, destination);
                        return Results.Ok(new { message = "File moved successfully." });
                    }
                    else
                    {
                        return Results.BadRequest(new { message = "Invalid operation. Use 'copy' or 'move'." });
                    }
                }
                catch (FileNotFoundException ex)
                {
                    return Results.NotFound(new { message = ex.Message });
                }
                catch (DirectoryNotFoundException ex)
                {
                    return Results.NotFound(new { message = ex.Message });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    return Results.Problem("An internal server error occurred");
                }

            });
            app.MapDelete($"{_routePrefix}", (string? home, string? path) =>
            {
                try
                {
                    var selectedHome = string.IsNullOrEmpty(home) ? "default" : home;
                    var normalizedPath = path?.TrimStart('/') ?? "";
                    fileService.DeleteFile(selectedHome, normalizedPath);
                    return Results.NoContent();
                }
                catch(FileNotFoundException ex)
                {
                    return Results.NotFound(new { message = ex.Message });
                }
                catch (DirectoryNotFoundException ex)
                {
                    return Results.NotFound(new { message = ex.Message });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    return Results.Problem("An internal server error occurred");
                }

            });
            app.MapGet($"{_routePrefix}/search", (string? home, string? path, string query) =>
            {
                var selectedHome = string.IsNullOrEmpty(home) ? "default" : home;
                var files = fileService.SearchFiles(selectedHome, query);
                return Results.Ok(files);
            });

        }
    }
}

using System.IO;

namespace FileExplorer.Files
{
    public static class FileEndpoints
    {
        public static void MapFileEndpoints(this WebApplication app, string _routePrefix = "api/v1/files")
        {
            var fileService = app.Services.GetRequiredService<IFileService>();
            app.MapGet(_routePrefix, (string? path, string? query) =>
            {
                try
                {
                    var normalizedPath = path?.TrimStart('/') ?? "";
                    var files = fileService.GetFiles(normalizedPath, query);
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
                var path = request.Query["path"].FirstOrDefault();
                if (string.IsNullOrEmpty(path))
                    return Results.BadRequest("File path is required.");

                try
                {
                    var stream = fileService.GetFileStream(path);
                    if (stream == null)
                        return Results.NotFound();

                    var filename = Path.GetFileName(path);
                    var ext = Path.GetExtension(path).ToLowerInvariant();

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


            app.MapPost(_routePrefix, async (HttpRequest request) =>
            {

                if (!request.HasFormContentType)
                    return Results.BadRequest("Expected multipart/form-data");

                var form = await request.ReadFormAsync();
                var file = form.Files.FirstOrDefault();
                if (file == null || file.Length == 0)
                    return Results.BadRequest("No file uploaded");

                var path = form["path"].FirstOrDefault() ?? file.FileName;
                var normalizedPath = path?.TrimStart('/') ?? "";

                await fileService.SaveFile(normalizedPath, file.OpenReadStream());
                return Results.Created();
            });
            app.MapPut(_routePrefix, (string source, string destination, string operation) =>
            {
                try
                {
                    if (operation == "copy")
                    {
                        fileService.CopyFile(source, destination);
                        return Results.Ok(new { message = "File copied successfully." });
                    }
                    else if (operation == "move")
                    {
                        fileService.MoveFile(source, destination);
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
            app.MapDelete(_routePrefix, (string? path) =>
            {
                try
                {
                    var normalizedPath = path?.TrimStart('/') ?? "";
                    fileService.DeleteFile(normalizedPath);
                    return Results.NoContent();
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
        }
    }
}

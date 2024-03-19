using System.Text;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public class PixelController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PixelController> _logger;

		public PixelController(HttpClient httpClient,ILogger<PixelController> logger)
		{
			_httpClient = httpClient;
			_logger = logger;
		}

		[HttpPost]
		[Route("Pixels/SendToMatrix")]
		public async Task<IActionResult> SendToMatrix(string fileName)
		{
			try
			{
				fileName = System.Net.WebUtility.UrlDecode(fileName);
				var bitmapPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", fileName);
				using (var image = Image.Load<Rgba32>(bitmapPath))
				{
						var width = image.Width;
						var height = image.Height;
						if (width != 32 || height != 32)
						{
							throw new Exception($"Unexpected image dimensions: {width}x{height}");
						}
						var sb = new StringBuilder(width * height * 3); 
						for (var y = 0; y < height; y++)
						{
								for (var x = 0; x < width; x++)
								{
										var p = image[x, y];
										if (p.A < 50)
										{
											//_logger.LogInformation($"a: {p.A}");
											sb.Append("000000");
										}
										else
										{
											sb.Append(p.R.ToString("X2"));
											sb.Append(p.G.ToString("X2"));
											sb.Append(p.B.ToString("X2"));
										}
								}
						}
						await SendPostRequest(sb.ToString());
				}
			}
			catch (Exception e)
			{
				_logger.LogError(e, "SendToMatrix");
					return Json(new { success = false, error = e.Message });
			}
			return Json(new { success = true });
		}

		private async Task SendPostRequest(string postData)
		{
				// URL of the target host
				var targetUrl = "http://192.168.1.41/";

				// Create the POST request
				//_logger.LogInformation(postData);
				var content = new StringContent(postData, Encoding.UTF8, "application/text");
				var response = await _httpClient.PostAsync(targetUrl, content);

				// Check if the request was successful
				if (!response.IsSuccessStatusCode)
				{
						var errorResponse = await response.Content.ReadAsStringAsync();
						_logger.LogWarning(errorResponse);
				}
		}		
}
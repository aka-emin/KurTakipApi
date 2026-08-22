using KurTakipApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace KurTakipApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FonController : ControllerBase
{
    private readonly IFonService _fonService;

    public FonController(IFonService fonService)
    {
        _fonService = fonService;
    }

    /// <summary>
    /// Takip listesindeki yatırım fonlarının güncel TEFAS verilerini döner.
    /// </summary>
    [HttpGet("anlik")]
    public async Task<IActionResult> AnlikFonlariGetir()
    {
        var fonlar = await _fonService.AnlikFonlariGetirAsync();
        return Ok(fonlar);
    }
}

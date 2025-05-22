using Microsoft.AspNetCore.Mvc;

using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

namespace risk.control.system.Controllers.Api
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [ApiController]
    public class IcheckifyApiController : ControllerBase
    {
        private readonly IHttpClientService httpClientService;
        private static string BaseUrl = "http://icheck-webSe-kOnc2X2NMOwe-196777346.ap-southeast-2.elb.amazonaws.com";
        private static string PanIdfyUrl = "https://pan-card-verification-at-lowest-price.p.rapidapi.com/verification/marketing/pan";
        private static string RapidAPIKey = "df0893831fmsh54225589d7b9ad1p15ac51jsnb4f768feed6f";
        private static string PanTask_id = "pan-card-verification-at-lowest-price.p.rapidapi.com";

        public IcheckifyApiController(IHttpClientService httpClientService)
        {
            this.httpClientService = httpClientService;
        }

        [HttpPost("mask")]
        public async Task<IActionResult> Mask(MaskImage image)
        {
            var maskedImageDetail = await httpClientService.GetMaskedImage(image, BaseUrl);

            return Ok(maskedImageDetail);
        }

        [HttpPost("match")]
        public async Task<IActionResult> Match(MatchImage image)
        {
            var maskedImageDetail = await httpClientService.GetFaceMatch(image, BaseUrl);

            return Ok(maskedImageDetail);
        }

        [HttpGet("pan")]
        public async Task<IActionResult> Pan(string pan = "FNLPM8635N")
        {
            var verifiedPanResponse = await httpClientService.VerifyPanNew(pan, PanIdfyUrl, RapidAPIKey, PanTask_id);

            return Ok(verifiedPanResponse);
        }

        [HttpGet("GetAddressByLatLng")]
        public async Task<IActionResult> GetAddressByLatLng(string lat, string lng)
        {
            var verifiedPanResponse = await httpClientService.GetRawAddress(lat, lng);

            return Ok(verifiedPanResponse);
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using POSRestoran01.Services.Interfaces;

namespace POSRestoran01.Controllers
{
    public class ReceiptController : BaseController
    {
        private readonly IReceiptService _receiptService;

        public ReceiptController(IReceiptService receiptService)
        {
            _receiptService = receiptService;
        }

     
        [HttpGet]
        public async Task<IActionResult> Preview(int orderId)
        {
            try
            {
                var canGenerate = await _receiptService.CanGenerateReceiptAsync(orderId);
                if (!canGenerate)
                {
                    TempData["Error"] = "Struk tidak dapat digenerate untuk order ini";
                    return RedirectToAction("Index", "Dashboard");
                }

                var receipt = await _receiptService.GenerateReceiptAsync(orderId);
                if (receipt == null)
                {
                    TempData["Error"] = "Order tidak ditemukan";
                    return RedirectToAction("Index", "Dashboard");
                }

                return View(receipt);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Terjadi kesalahan saat memuat struk";
                return RedirectToAction("Index", "Dashboard");
            }
        }

     
        [HttpGet]
        public async Task<IActionResult> Print(int orderId)
        {
            try
            {
                var canGenerate = await _receiptService.CanGenerateReceiptAsync(orderId);
                if (!canGenerate)
                {
                    return Json(new { success = false, message = "Struk tidak dapat digenerate untuk order ini" });
                }

                var receipt = await _receiptService.GenerateReceiptAsync(orderId);
                if (receipt == null)
                {
                    return Json(new { success = false, message = "Order tidak ditemukan" });
                }

                return View("PrintReceipt", receipt);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan saat mencetak struk" });
            }
        }

 
        [HttpGet]
        public async Task<IActionResult> GetReceiptData(int orderId)
        {
            try
            {
                var receipt = await _receiptService.GenerateReceiptAsync(orderId);
                if (receipt == null)
                {
                    return Json(new { success = false, message = "Order tidak ditemukan" });
                }

                return Json(new { success = true, data = receipt });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan saat memuat data struk" });
            }
        }
    }
}
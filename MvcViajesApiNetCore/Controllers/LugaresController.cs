using Microsoft.AspNetCore.Mvc;
using MvcOAuthEmpleados.Filters;
using MvcViajesApiNetCore.Services;
using NugetViajesSMG.Models;

namespace MvcViajesApiNetCore.Controllers
{
    public class LugaresController : Controller
    {
        private ServiceViajes service;

        public LugaresController(ServiceViajes service)
        {
            this.service = service;
        }

        public async Task<IActionResult> Index()
        {
            List<Lugar> lugar =
                await this.service.GetLugaresAsync();
            return View(lugar);
        }


        public async Task<IActionResult> Details(int idLugar)
        {
            Lugar lugar = await
                    this.service.FindLugarAsync(idLugar);
            return View(lugar);
        }
    }
}

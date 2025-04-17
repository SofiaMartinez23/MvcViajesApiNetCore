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

        public async Task<IActionResult> _Comentarios(int idLugar)
        {
            List<Comentario> comentarios = await
                    this.service.GetComentarioLugarAsync(idLugar);
            return PartialView("_Comentarios", comentarios);
        }

        public IActionResult Create()
        {
            return View();
        }

        [AuthorizeUsuarios]
        [HttpPost]
        public async Task<IActionResult> Create(Lugar lugar)
        {
            await this.service.InsertLugarAsync(
                lugar.Nombre, lugar.Descripcion, lugar.Ubicacion,
                lugar.Categoria, lugar.Horario, lugar.Imagen,
                lugar.Tipo);

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int idLugar)
        {
            Lugar lugar = await this.service.FindLugarAsync(idLugar);
            return View(lugar);
        }

        [AuthorizeUsuarios]
        [HttpPost]
        public async Task<IActionResult> Edit(Lugar lugar)
        {
            await this.service.UpdateLugarAsync(
                lugar.IdLugar, lugar.Nombre, lugar.Descripcion, lugar.Ubicacion,
                lugar.Categoria, lugar.Horario, lugar.Imagen,
                lugar.Tipo);

            return RedirectToAction("Perfil", "Usuarios");
        }

       

    }
}

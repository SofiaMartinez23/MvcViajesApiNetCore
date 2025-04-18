using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcOAuthEmpleados.Filters;
using MvcViajesApiNetCore.Services;
using NugetViajesSMG.Models;
using System.Security.Claims;

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


        public async Task<IActionResult> Details( int idLugar)
        {
            Lugar lugar = await
                    this.service.FindLugarAsync(idLugar);
            return View(lugar);
        }

        public async Task<IActionResult> _Comentarios(int idLugar)
        {
            UsuarioCompletoView perfil = await this.service.GetPerfilAsync();

            if (perfil != null)
            {
                ViewData["IdUsuarioActual"] = perfil.IdUsuario;
            }
            else
            {
                return RedirectToAction("Error");
            }
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

        [AuthorizeUsuarios]
        [HttpPost]
        public async Task<IActionResult> CreateComment(int idlugar, string comentario)
        {
            try
            {
                await this.service.InsertComentarioAsync(idlugar, comentario);
                return RedirectToAction("Details", new { idLugar = idlugar });
            }
            catch (UnauthorizedAccessException ex)
            {
                return RedirectToAction("Login", "Managed");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error al publicar el comentario: " + ex.Message;
                return RedirectToAction("Details", new { idLugar = idlugar }); 
            }
        }


        [AuthorizeUsuarios]
        [HttpPost]
        public async Task<IActionResult> EditComment(int idlugar, Comentario comentario)
        {
            await this.service.UpdateComentarioAsync(
                comentario.IdComentario,
                comentario.IdLugar,
                comentario.Comentarios,
                comentario.NombreUsuario
            );

            return RedirectToAction("Details", new { idLugar = idlugar });
        }



        [AuthorizeUsuarios]
        public async Task<IActionResult> DeleteComment(int idlugar, int idcomentario)
        {
            await this.service.DeleteComentarioAsync(idcomentario);
            return RedirectToAction("Details", new { idLugar = idlugar });
        }

        [AuthorizeUsuarios]
        [HttpPost]
        public async Task<IActionResult> AddToFavoritos(int idLugar)
        {
            try
            {
                await this.service.InsertFavoritoAsync(idLugar);
                return RedirectToAction("Details", new { idLugar = idLugar });
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Managed");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error al agregar a favoritos: " + ex.Message;
                return RedirectToAction("Details", new { idLugar = idLugar });
            }
        }

    }

}


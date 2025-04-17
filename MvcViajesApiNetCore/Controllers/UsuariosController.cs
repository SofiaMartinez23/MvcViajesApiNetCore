using Microsoft.AspNetCore.Mvc;
using MvcOAuthEmpleados.Filters;
using MvcViajesApiNetCore.Services;
using NugetViajesSMG.Models;

namespace MvcViajesApiNetCore.Controllers
{
    public class UsuariosController : Controller
    {
        private ServiceViajes service;

        public UsuariosController(ServiceViajes service)
        {
            this.service = service;
        }

        [AuthorizeUsuarios]
        public async Task<IActionResult> Index(string nombreBusqueda)
        {
            List<UsuarioCompletoView> usuarios;

            if (!string.IsNullOrEmpty(nombreBusqueda))
            {
                usuarios = await this.service.BuscarUsuariosPorNombreAsync(nombreBusqueda);
            }
            else
            {
                usuarios = await this.service.GetUsuariossAsync();
            }

            return View(usuarios);
        }


        [AuthorizeUsuarios]
        public async Task<IActionResult> Details(int idUsuario)
        {
            UsuarioCompletoView usuario = await
                    this.service.FindUsuarioAsync(idUsuario);

            return View(usuario);
        }

        public async Task<IActionResult> Edit(int idUsuario)
        {
            UsuarioCompletoView usuario = await this.service.FindUsuarioAsync(idUsuario);
            return View(usuario);
        }

        [AuthorizeUsuarios]
        [HttpPost]
        public async Task<IActionResult> Edit(UsuarioCompletoView usuario)
        {
            await this.service.UpdateUsuarioAsync(
                usuario.Nombre, usuario.Email, usuario.Edad,
                usuario.Nacionalidad, usuario.PreferenciaViaje, usuario.Clave,
                usuario.ConfirmarClave, usuario.AvatarUrl);

            return RedirectToAction("Perfil");
        }

        [AuthorizeUsuarios]
        public async Task<IActionResult> Perfil()
        {
            UsuarioCompletoView usuario = await
                this.service.GetPerfilAsync();

            var lugares = await this.service.GetLugaresPorUsuarioAsync(usuario.IdUsuario);
            ViewBag.LugaresCreadosCount = lugares.Count;

            var lugaresFavoritos = await this.service.GetFavoritosUsuarioAsync(usuario.IdUsuario);
            ViewBag.LugaresFavoritosCount = lugaresFavoritos.Count;

            return View(usuario);
        }

        [AuthorizeUsuarios]
        public async Task<IActionResult> _Lugares(int idUsuario)
        {
            List<Lugar> lugares = await
                    this.service.GetLugaresPorUsuarioAsync(idUsuario);
            return PartialView("_Lugares", lugares);
        }

        [AuthorizeUsuarios]
        public async Task<IActionResult> _LugaresUsuario(int idUsuario)
        {
            List<Lugar> lugares = await
                    this.service.GetLugaresPorUsuarioAsync(idUsuario);
            return PartialView("_LugaresUsuario", lugares);
        }

        [AuthorizeUsuarios]
        public async Task<IActionResult> _Favoritos(int idUsuario)
        {
            List<LugarFavorito> fav = await
                    this.service.GetFavoritosUsuarioAsync(idUsuario);
            return PartialView("_Favoritos", fav);
        }


        [AuthorizeUsuarios]
        public async Task<IActionResult> DeleteLugar(int idLugar)
        {
            await this.service.DeleteLugarAsync(idLugar);
            return RedirectToAction("Index");
        }
    }
}

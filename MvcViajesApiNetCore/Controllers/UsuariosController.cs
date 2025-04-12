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
        public async Task<IActionResult> Index()
        {
            List<UsuarioCompletoView> usuarios =
                await this.service.GetUsuariossAsync();
            return View(usuarios);
        }

        [AuthorizeUsuarios]
        public async Task<IActionResult> Details(int idUsuario)
        {
            UsuarioCompletoView usuario = await
                    this.service.FindUsuarioAsync(idUsuario);
            return View(usuario);
        }

        [AuthorizeUsuarios]
        public async Task<IActionResult> Perfil()
        {
            UsuarioCompletoView usuario = await
                this.service.GetPerfilAsync();
            return View(usuario);
        }

    }
}
